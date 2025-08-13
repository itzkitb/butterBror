using butterBror.Core.Bot.SQLColumnNames;
using butterBror.Models;
using butterBror.Models.AI;
using butterBror.Utils;
using DankDB;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using static butterBror.Core.Bot.Console;

namespace butterBror.Services.External
{
    /// <summary>
    /// Provides artificial intelligence capabilities with multiple model support and chat history management.
    /// </summary>
    public class AIService
    {
        /// <summary>
        /// Dictionary of available AI models with their corresponding API identifiers.
        /// </summary>
        public static readonly Dictionary<string, string> availableModels = new()
                {
                    { "qwen", "qwen/qwen3-30b-a3b:free" },
                    { "deepseek", "deepseek/deepseek-r1-0528-qwen3-8b:free" },
                    { "gemma", "google/gemma-3n-e4b-it:free" },
                    { "meta", "meta-llama/llama-3.3-8b-instruct:free" },
                    { "microsoft", "microsoft/phi-4-reasoning-plus:free" },
                    { "nvidia", "nvidia/llama-3.3-nemotron-super-49b-v1:free" },
                    { "mistral", "mistralai/devstral-small:free" }
                };

        /// <summary>
        /// Dictionary specifying timeout durations for different AI models.
        /// </summary>
        public static readonly Dictionary<string, TimeSpan> modelsTimeout = new()
                {
                    { "qwen", TimeSpan.FromSeconds(240) },
                    { "deepseek", TimeSpan.FromSeconds(240) },
                    { "microsoft", TimeSpan.FromSeconds(240) },
                    { "gemma", TimeSpan.FromSeconds(60) },
                    { "meta", TimeSpan.FromSeconds(60) },
                    { "nvidia", TimeSpan.FromSeconds(60) },
                    { "mistral", TimeSpan.FromSeconds(60) }
                };

        /// <summary>
        /// List of models capable of generating extended content responses.
        /// </summary>
        public static readonly List<string> generatingModels = new() { "qwen", "deepseek", "microsoft" };

        /// <summary>
        /// Sends a request to the AI API and processes the response.
        /// </summary>
        /// <param name="request">The user's input text to process.</param>
        /// <param name="umodel">The requested AI model (optional).</param>
        /// <param name="platform">The platform context for the request.</param>
        /// <param name="username">The username of the requester.</param>
        /// <param name="userID">The unique identifier of the user.</param>
        /// <param name="lang">The preferred language for the response.</param>
        /// <param name="repetitionPenalty">Value to control output repetition (0-2).</param>
        /// <param name="chatHistory">Indicates whether to include chat history (default: true).</param>
        /// <returns>An array containing the model name and response, or error information.</returns>
        /// <exception cref="Exception">Any exceptions during API communication are caught and logged.</exception>

        public static async Task<string[]> Request(string request, string umodel, PlatformsEnum platform, string username, string userID, string lang, double repetitionPenalty, bool chatHistory = true)
        {
            DateTime requestTime = DateTime.UtcNow;
            var api_key = Manager.Get<string>(Bot.Paths.Settings, "openrouter_token");
            var uri = new Uri("https://openrouter.ai/api/v1/chat/completions");

            string model = "qwen";
            string selected_model = availableModels[model];

            if (umodel is not null)
            {
                model = umodel.ToLower();
                if (!availableModels.ContainsKey(model))
                {
                    return new[] { "ERR", "Model not found" };
                }

                selected_model = availableModels[model];
            }

            if (string.IsNullOrWhiteSpace(request))
                return new[] { "ERR", "Empty request" };

            var system_message = new Message
            {
                role = "system",
                content = $@"You are bot on platform: {PlatformsPathName.strings[(int)platform]}. Your name is {Bot.BotName}. DO NOT POST CONFIDENTIAL INFORMATION, DO NOT USE PROFANITY! WRITE LESS THAN 50 WORDS! SHORTEN YOUR TEXT!"
            };

            var user_info_message = new Message
            {
                role = "system",
                content = $"User info:\n1) Username: {username}\n2) ID: {userID}\n3) Language (YOUR ANSWER MUST BE IN IT unless the user says otherwise!): {lang}\nCurrent date and time: {DateTime.UtcNow.ToString("dd-MM-yyyy HH:mm")} UTC"
            };

            var user_message = new Message
            {
                role = "user",
                content = request
            };

            List<Message> messages = new List<Message> { system_message, user_info_message, user_message };

            if (chatHistory)
            {
                List<string> history = DataConversion.ParseStringList((string)Bot.UsersBuffer.GetParameter(platform, DataConversion.ToLong(userID), Users.GPTHistory));
                if (history is not null) // Fix #AC0
                {
                    messages.Insert(0, new Message
                    {
                        role = "system",
                        content = $"Chat history:\n{string.Join('\n', history)}"
                    });
                }
            }

            RequestBody request_body = new RequestBody
            {
                model = selected_model,
                messages = messages,
                repetition_penalty = repetitionPenalty
            };

            using var client = new HttpClient();
            client.Timeout = modelsTimeout[model];
            var json_content = JsonConvert.SerializeObject(request_body);
            using var req = new HttpRequestMessage(HttpMethod.Post, uri)
            {
                Content = new StringContent(json_content, Encoding.UTF8, "application/json")
            };
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", api_key);

            try
            {
                var resp = await client.SendAsync(req);
                var body = await resp.Content.ReadAsStringAsync();

                if (resp.IsSuccessStatusCode)
                {
                    var result = JsonConvert.DeserializeObject<ResponseBody>(body);

                    if (chatHistory)
                    {
                        List<string> loadedHistory = DataConversion.ParseStringList((string)Bot.UsersBuffer.GetParameter(platform, DataConversion.ToLong(userID), Users.GPTHistory));
                        List<string> history = loadedHistory ?? new List<string>(); // Fix #AC1

                        history.Add($"{requestTime.ToString("dd-MM-yyyy HH:mm")} [user]: {request}");
                        history.Add($"{DateTime.UtcNow.ToString("dd-MM-yyyy HH:mm")} [AI]: {result.choices[0].message.content}");

                        if (history.Count > 10)
                        {
                            history.RemoveRange(0, history.Count - 6);
                        }

                        Bot.UsersBuffer.SetParameter(platform, DataConversion.ToLong(userID), Users.GPTHistory, DataConversion.SerializeStringList(history));
                    }

                    return new[] { model, result.choices[0].message.content };
                }

                Write($"API ERROR ({api_key}): #{resp.StatusCode}, {resp.ReasonPhrase}", "info", LogLevel.Warning);
                return new[] { "ERR", "API Error" };
            }
            catch (Exception ex)
            {
                Write(ex);
                return new[] { "ERR", "API Exception" };
            }
        }
    }
}
