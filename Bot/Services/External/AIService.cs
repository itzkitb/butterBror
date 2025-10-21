using bb.Core.Configuration;
using bb.Models.AI;
using bb.Models.Platform;
using bb.Utils;
using DankDB;
using feels.Dank.Cache.LRU;
using Microsoft.CodeAnalysis;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using static bb.Core.Bot.Logger;

namespace bb.Services.External
{
    /// <summary>
    /// Provides artificial intelligence capabilities with multiple model support and chat history management.
    /// </summary>
    public class AIService
    {
        /// <summary>
        /// Dictionary of available AI models with their corresponding API identifiers.
        /// </summary>
        public readonly Dictionary<string, ModelInfo> models = new()
                {
                    { "qwen", new ModelInfo() { 
                        Name = "qwen",
                        Key = "qwen/qwen3-235b-a22b:free",
                        IsGenerating = true
                    } },
                    { "deepseek", new ModelInfo() {
                        Name = "deepseek",
                        Key = "deepseek/deepseek-chat-v3.1:free",
                        IsGenerating = true
                    }  },
                    { "gemma", new ModelInfo() {
                        Name = "gemma",
                        Key = "google/gemma-3-27b-it:free",
                        IsGenerating = false
                    }  },
                    { "meta", new ModelInfo() {
                        Name = "meta",
                        Key = "meta-llama/llama-3.3-70b-instruct:free",
                        IsGenerating = false
                    }  },
                    { "microsoft", new ModelInfo() {
                        Name = "microsoft",
                        Key = "microsoft/phi-4-reasoning-plus:free",
                        IsGenerating = true
                    }  },
                    { "nvidia", new ModelInfo() {
                        Name = "nvidia",
                        Key = "nvidia/llama-3.1-nemotron-ultra-253b-v1:free",
                        IsGenerating = false
                    }  },
                    { "mistral", new ModelInfo() {
                        Name = "mistral",
                        Key = "cognitivecomputations/dolphin3.0-mistral-24b:free",
                        IsGenerating = false
                    }  },
                    { "openai", new ModelInfo() {
                        Name = "openai",
                        Key = "openai/gpt-oss-120b:free",
                        IsGenerating = true
                    }  },
                };

        public TimeSpan GetModelTimeout(ModelInfo model) 
        {
            if (model == null) return TimeSpan.Zero;

            if (model.IsGenerating)
            {
                return TimeSpan.FromMinutes(4);
            }
            else
            {
                return TimeSpan.FromMinutes(1);
            }
        }
        private bool _tokensInitialized = false;

        /// <summary>
        /// Manages multiple OpenRouter API tokens with automatic switching on rate limits.
        /// </summary>
        public class TokenManager
        {
            private static List<TokenInfo> _tokens = new();
            private static int _currentTokenIndex = 0;
            private static DateTime _lastUpdate = DateTime.MinValue;
            private static readonly object _lock = new object();
            private static readonly TimeSpan _updateInterval = TimeSpan.FromHours(1);

            public static void Initialize()
            {
                var tokens = bb.Program.BotInstance.Settings.Get<List<string>>("open_router_tokens") ?? new List<string>();
                _tokens = tokens.Select(token => new TokenInfo { Token = token, Usage = 0, Limit = null, LastUpdated = DateTime.MinValue }).ToList();
                _currentTokenIndex = 0;
                UpdateTokenInfo();
            }

            public static TokenInfo? GetCurrentToken()
            {
                lock (_lock)
                {
                    if (_tokens.Count == 0)
                        return null;

                    if (DateTime.UtcNow - _lastUpdate > _updateInterval)
                    {
                        UpdateTokenInfo();
                    }

                    while (_currentTokenIndex < _tokens.Count)
                    {
                        var token = _tokens[_currentTokenIndex];
                        if (token.IsAvailable)
                            return token;

                        _currentTokenIndex++;
                    }

                    _currentTokenIndex = 0;
                    return _tokens.Count > 0 ? _tokens[0] : null;
                }
            }

            public static void UpdateTokenInfo()
            {
                lock (_lock)
                {
                    foreach (var token in _tokens)
                    {
                        try
                        {
                            var info = GetTokenInfoAsync(token.Token).Result;
                            if (info != null)
                            {
                                token.Usage = info.Usage;
                                token.Limit = info.Limit;
                                token.LastUpdated = DateTime.UtcNow;
                            }
                        }
                        catch (Exception ex)
                        {
                            Write($"Failed to update token info for token {token.Token}: {ex.Message}", LogLevel.Error);
                        }
                    }
                    _lastUpdate = DateTime.UtcNow;
                }
            }

            public static async Task<TokenInfo> GetTokenInfoAsync(string token)
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var response = await client.GetAsync("https://openrouter.ai/api/v1/key");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var json = JObject.Parse(content);
                    var data = json["data"] as JObject;
                    if (data != null)
                    {
                        return new TokenInfo
                        {
                            Token = token,
                            Usage = data.Value<long>("usage"),
                            Limit = data.Value<long?>("limit") == 0 ? 20 : data.Value<long?>("limit"),
                            LastUpdated = DateTime.UtcNow
                        };
                    }
                }
                return null;
            }

            public static bool SwitchToNextToken()
            {
                lock (_lock)
                {
                    if (_tokens.Count == 0)
                        return false;

                    var originalIndex = _currentTokenIndex;
                    do
                    {
                        _currentTokenIndex = (_currentTokenIndex + 1) % _tokens.Count;
                        if (_tokens[_currentTokenIndex].IsAvailable)
                            return true;
                    } while (_currentTokenIndex != originalIndex);

                    return false;
                }
            }

            public static int GetTokenCount()
            {
                lock (_lock)
                {
                    return _tokens.Count;
                }
            }
        }

        public class TokenInfo
        {
            public string Token { get; set; }
            public long? Limit { get; set; }
            public long Usage { get; set; }
            public DateTime LastUpdated { get; set; }
            public bool IsAvailable => Limit == null || Usage < Limit;
        }

        public class ModelInfo
        {
            public string Name { get; set; }
            public string Key { get; set; }
            public bool IsGenerating { get; set; }
        }

        /// <summary>
        /// Sends a request to the AI API and processes the response.
        /// </summary>
        /// <param name="request">The user's input text to process. Must not be null or empty.</param>
        /// <param name="model">The requested AI model (optional). Must be in the available models list.</param>
        /// <param name="platform">The platform context for the request.</param>
        /// <param name="username">The username of the requester.</param>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="language">The preferred language for the response.</param>
        /// <param name="repetitionPenalty">Value to control output repetition (0-2). Default is 1.0.</param>
        /// <param name="includeChatHistory">Indicates whether to include chat history (default: true).</param>
        /// <param name="includeSystemPrompt">Indicates whether to include system prompt (default: true).</param>
        /// <returns>
        /// An array containing the model name and response content, or error information.
        /// Array format: [model, response] for successful requests, [ERR, error_message] for failures.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when required parameters are null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when parameters are out of valid range.</exception>
        public async Task<string[]> Request(
            string request,
            PlatformsEnum platform,
            string? model = null,
            string? username = null,
            string? userId = null,
            string? language = null,
            double repetitionPenalty = 1.0,
            bool includeChatHistory = true,
            bool includeSystemPrompt = true)
        {
            if (string.IsNullOrWhiteSpace(request))
                throw new ArgumentException("Request cannot be empty", nameof(request));

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentNullException(nameof(userId), "User ID cannot be null or empty");

            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentNullException(nameof(username), "Username cannot be null or empty");

            if (string.IsNullOrWhiteSpace(language))
                throw new ArgumentNullException(nameof(language), "Language cannot be null or empty");

            if (repetitionPenalty < 0 || repetitionPenalty > 2)
                throw new ArgumentOutOfRangeException(nameof(repetitionPenalty), "Repetition penalty must be between 0 and 2");

            if (!_tokensInitialized)
            {
                TokenManager.Initialize();
                Write("AI tokens initialized");
                _tokensInitialized = true;
            }

            var selectedModel = ValidateModelSelection(model);
            if (selectedModel == null)
                return new[] { "ERR", "Model not found" };

            var systemMessage = BuildSystemMessage(platform, selectedModel, language);

            var userInfoMessage = new Message
            {
                Role = "system",
                Content = $"User info:\n" +
                          $"- Username: {username}\n" +
                          $"- ID: {userId}\n" +
                          $"- Language: {language}\n" +
                          $"- Current UTC time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}"
            };

            var userMessage = new Message
            {
                Role = "user",
                Content = request
            };

            var messages = BuildMessageList(
                systemMessage,
                userInfoMessage,
                userMessage,
                includeSystemPrompt,
                includeChatHistory,
                platform,
                userId);

            var requestBody = new RequestBody
            {
                Model = selectedModel.Key,
                Messages = messages,
                RepetitionPenalty = repetitionPenalty,
                Temperature = 0.7
            };

            return await ProcessRequestWithTokenManagement(requestBody, userId, selectedModel, includeChatHistory);
        }

        #region Helper Methods

        private ModelInfo ValidateModelSelection(string? model)
        {
            if (string.IsNullOrWhiteSpace(model))
                return models["qwen"];

            var normalizedModel = model.ToLower();
            if (!models.ContainsKey(normalizedModel))
                return null;

            return models[normalizedModel];
        }

        private Message BuildSystemMessage(PlatformsEnum platform, ModelInfo model, string language)
        {
            var baseSystem = $@"You're a bot on the platform: {Enum.GetName(platform)}. Your name: {bb.Program.BotInstance.TwitchName}.
CRITICAL RESTRICTIONS:
- 50 WORDS MAXIMUM per response
- DO NOT make statements about your age or the ages of others

PROHIBITED CONTENT:
- Content about self-harm, suicide, eating disorders
- Harassment, discrimination
- Spam and advertising

! Follow platform-specific guidelines.";

            var emoteRules = GetEmoteRules(platform);

            return new Message
            {
                Role = "system",
                Content = $"{baseSystem}\n{emoteRules}\n" +
                          "REMEMBER: Keep it short, keep it safe, and follow the rules!"
            };
        }

        private string GetEmoteRules(PlatformsEnum platform)
        {
            if (platform != PlatformsEnum.Twitch)
                return string.Empty;

            return @"EMOTE USAGE:
- Use 7TV global emotes instead of standard emojis (But this is not necessary)
- Combine emotes creatively (e.g., 'FeelsStrongMan Clap' or 'FeelsDankMan CrayonTime')
- Use overlay emotes appropriately (PETPET, RainTime, SteerR for interactive effects)
- It is recommended to use 1 or 2 emotes at the beginning or end of the text
- Emotes must have spaces on both sides

7TV EMOTE REFERENCE:
REACTIONS & EMOTIONS:
- peepoHappy: Express joy
- peepoSad: Disappointment
- FeelsOkayMan: Approve or acknowledge something
- FeelsStrongMan: Show determination or crying
- FeelsWeirdMan: Express confusion or disgust
- FeelsDankMan: Silly situations
- Stare: Intense focus or judgment
- EZ: Show confidence or superiority
- Gayge: Rainbow pride
- ApuApustaja: Tired or exhausted state

CHARACTERS & MEMES:
- BasedGod: Respected person reference
- BillyApprove: Strong approval (Billy Herrington)
- forsenPls: Dancing
- gachiGASM: Excitement (gachi muchi)
- WAYTOODANK: Explosion of excitement
- sevenTV: Cryptocurrency mining
- glorp: Alien cat
- RoxyPotato: Cat looking up
- ppL: Small pixel Pepe reference

DANCING & CELEBRATION:
- AlienDance: 3D alien dancing
- PepePls: Sad Pepe dancing
- peepoPls: Pixel dancing frog

ACTIVITY EMOTES (combine with others):
- Clap: Applause (use with FeelsStrongMan Clap)
- GuitarTime: Playing guitar (FeelsStrongMan GuitarTime)
- PianoTime: Playing piano (FeelsOkayMan PianoTime)
- TeaTime: Drinking tea (FeelsOkayMan TeaTime)
- WineTime: Drinking wine (BasedGod WineTime)
- CrayonTime: Eating crayons (FeelsDankMan CrayonTime)
- nymnCorn: Eating popcorn (FeelsDankMan nymnCorn)

OVERLAY EMOTES (apply effects to other emotes):
- PETPET: Hand petting effect
- RainTime: Rain overlay effect
- SteerR: Steering wheel driving effect";
        }

        private List<Message> BuildMessageList(
            Message systemMessage,
            Message userInfoMessage,
            Message userMessage,
            bool includeSystemPrompt,
            bool includeChatHistory,
            PlatformsEnum platform,
            string userId)
        {
            var messages = new List<Message>();

            if (includeSystemPrompt)
            {
                messages.Add(systemMessage);
                messages.Add(userInfoMessage);
            }

            messages.Add(userMessage);

            if (includeChatHistory)
            {
                var history = GetChatHistory(platform, userId);
                if (history != null && history.Count > 0)
                {
                    messages.Insert(0, new Message
                    {
                        Role = "system",
                        Content = $"Chat history:\n{string.Join("\n", history)}"
                    });
                }
            }

            return messages;
        }

        private List<string> GetChatHistory(PlatformsEnum platform, string userId)
        {
            var historyString = (string)bb.Program.BotInstance.UsersBuffer.GetParameter(platform, DataConversion.ToLong(userId), Users.GPTHistory);
            return DataConversion.ParseStringList(historyString) ?? new List<string>();
        }

        private async Task<string[]> ProcessRequestWithTokenManagement(RequestBody requestBody, string userId, ModelInfo model, bool includeChatHistory)
        {
            int maxAttempts = TokenManager.GetTokenCount();
            int attempts = 0;

            while (attempts < maxAttempts)
            {
                var tokenInfo = TokenManager.GetCurrentToken();
                if (tokenInfo == null)
                    return new[] { "ERR", "No available tokens" };

                try
                {
                    var (statusCode, responseContent) = await SendApiRequest(requestBody, model, tokenInfo.Token);

                    if (statusCode == HttpStatusCode.TooManyRequests || statusCode == HttpStatusCode.Unauthorized)
                    {
                        attempts++;
                        if (!TokenManager.SwitchToNextToken())
                            break;
                        continue;
                    }

                    if (statusCode == HttpStatusCode.OK)
                    {
                        var result = JsonConvert.DeserializeObject<ResponseBody>(responseContent);
                        if (result?.Choices == null || result.Choices.Count == 0)
                            return new[] { "ERR", "Invalid API response structure" };

                        var cleanedContent = CleanResponseContent(result.Choices[0].Message.Content);

                        if (requestBody.Messages.Any(m => m.Role == "user") && includeChatHistory)
                        {
                            var userMessage = requestBody.Messages.First(m => m.Role == "user").Content;
                            UpdateChatHistory(PlatformsEnum.Twitch, userId, userMessage, cleanedContent);
                        }

                        return new[] { model.Name, cleanedContent };
                    }

                    return HandleApiError(statusCode, responseContent);
                }
                catch (Exception ex)
                {
                    attempts++;
                    Write($"Token request failed: {ex.Message} {ex.StackTrace}", LogLevel.Error);
                    if (!TokenManager.SwitchToNextToken())
                        break;
                }
            }

            return new[] { "ERR", "All tokens failed to process request" };
        }

        private async Task<(HttpStatusCode, string)> SendApiRequest(RequestBody requestBody, ModelInfo model, string apiKey)
        {
            try
            {
                TimeSpan timeout = GetModelTimeout(model);

                using var client = new HttpClient
                {
                    Timeout = timeout
                };

                var jsonContent = JsonConvert.SerializeObject(requestBody);
                var request = new HttpRequestMessage(HttpMethod.Post, new Uri("https://openrouter.ai/api/v1/chat/completions"))
                {
                    Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
                };

                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                var response = await client.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                return (response.StatusCode, responseContent);
            }
            catch (Exception ex)
            {
                Write($"API request failed: {ex.Message}", LogLevel.Error);
                return (HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        private string CleanResponseContent(string content)
        {
            var cleaned = Regex.Replace(content, @"<think>.*?</think>", string.Empty, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            cleaned = cleaned.ReplaceLineEndings(" ");
            return cleaned.Trim();
        }

        private void UpdateChatHistory(PlatformsEnum platform, string userId, string userMessage, string aiResponse)
        {
            var history = GetChatHistory(platform, userId);

            history.Add($"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} [user]: {userMessage}");
            history.Add($"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} [AI]: {aiResponse}");

            if (history.Count > 10)
                history.RemoveRange(0, history.Count - 6);

            bb.Program.BotInstance.UsersBuffer.SetParameter(
                platform,
                DataConversion.ToLong(userId),
                Users.GPTHistory,
                DataConversion.SerializeStringList(history));
        }

        private string[] HandleApiError(HttpStatusCode statusCode, string responseContent)
        {
            var errorDetails = $"API Error: {statusCode}";

            if (statusCode == HttpStatusCode.Unauthorized)
                errorDetails += " - Invalid API key";
            else if (statusCode == HttpStatusCode.TooManyRequests)
                errorDetails += " - Rate limit exceeded";
            else if (!string.IsNullOrWhiteSpace(responseContent))
                errorDetails += $"\nResponse: {responseContent}";

            Write(errorDetails, LogLevel.Error);
            return new[] { "ERR", errorDetails };
        }

        #endregion
    }
}
