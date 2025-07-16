using butterBror.Utils.DataManagers;
using butterBror.Utils.Types;
using butterBror.Utils.Types.AI;
using DankDB;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using static butterBror.Utils.Bot.Console;

namespace butterBror.Utils.Tools.API
{
    /// <summary>
    /// Provides artificial intelligence capabilities with multiple model support and chat history management.
    /// </summary>
    public class AI
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
        [ConsoleSector("butterBror.Utils.Tools.API.AI", "Request")]
        public static async Task<string[]> Request(string request, string umodel, Platforms platform, string username, string userID, string lang, double repetitionPenalty, bool chatHistory = true)
        {
            Engine.Statistics.FunctionsUsed.Add();

            DateTime requestTime = DateTime.UtcNow;
            var api_key = Manager.Get<string>(Engine.Bot.Pathes.Settings, "openrouter_token");
            var uri = new Uri("https://openrouter.ai/api/v1/chat/completions");

            string selected_model = "meta-llama/llama-4-maverick:free";
            string model = "qwen";
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

            var system_message = new Types.AI.Message
            {
                role = "system",
                content = $@"You are bot on platform: {Platform.strings[(int)platform]}. Your name is {Engine.Bot.BotName}. DO NOT POST CONFIDENTIAL INFORMATION, DO NOT USE PROFANITY! WRITE LESS THAN 50 WORDS! SHORTEN YOUR TEXT!"
            };

            var user_info_message = new Types.AI.Message
            {
                role = "system",
                content = $"User info:\n1) Username: {username}\n2) ID: {userID}\n3) Language (YOUR ANSWER MUST BE IN IT unless the user says otherwise!): {lang}\nCurrent date and time: {DateTime.UtcNow.ToString("dd-MM-yyyy HH:mm")} UTC"
            };

            var user_message = new Types.AI.Message
            {
                role = "user",
                content = request
            };

            List<Types.AI.Message> messages = new List<Types.AI.Message> { system_message, user_info_message, user_message };

            if (chatHistory)
            {
                var history = UsersData.Get<List<string>>(userID, "gpt_history", platform);
                if (history is not null) // Fix #AC0
                {
                    messages.Insert(0, new Types.AI.Message
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
                        List<string> loadedHistory = UsersData.Get<List<string>>(userID, "gpt_history", platform);
                        List<string> history = loadedHistory ?? new List<string>(); // Fix #AC1

                        history.Add($"{requestTime.ToString("dd-MM-yyyy HH:mm")} [user]: {request}");
                        history.Add($"{DateTime.UtcNow.ToString("dd-MM-yyyy HH:mm")} [AI]: {result.choices[0].message.content}");

                        if (history.Count > 10)
                        {
                            history.RemoveRange(0, history.Count - 6);
                        }

                        UsersData.Save(userID, "gpt_history", chatHistory, platform);
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
