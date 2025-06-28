using butterBror.Utils.DataManagers;
using DankDB;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using static butterBror.Utils.Things.Console;

namespace butterBror.Utils.Tools.API
{
    public class AI
    {
        public class Data
        {
            public required string text { get; set; }
            public required string model { get; set; }
        }

        public static readonly Dictionary<string, string> available_models = new()
                {
                    { "qwen", "qwen/qwen3-30b-a3b:free" },
                    { "deepseek", "deepseek/deepseek-r1-0528-qwen3-8b:free" },
                    { "gemma", "google/gemma-3n-e4b-it:free" },
                    { "meta", "meta-llama/llama-3.3-8b-instruct:free" },
                    { "microsoft", "microsoft/phi-4-reasoning-plus:free" },
                    { "nvidia", "nvidia/llama-3.3-nemotron-super-49b-v1:free" },
                    { "mistral", "mistralai/devstral-small:free" }
                };
        public static readonly Dictionary<string, TimeSpan> models_timeout = new()
                {
                    { "qwen", TimeSpan.FromSeconds(240) },
                    { "deepseek", TimeSpan.FromSeconds(240) },
                    { "microsoft", TimeSpan.FromSeconds(240) },
                    { "gemma", TimeSpan.FromSeconds(60) },
                    { "meta", TimeSpan.FromSeconds(60) },
                    { "nvidia", TimeSpan.FromSeconds(60) },
                    { "mistral", TimeSpan.FromSeconds(60) }
                };
        public static readonly List<string> generating_models = new() { "qwen", "deepseek", "microsoft" };

        public class Message
        {
            public string role { get; set; }
            public string content { get; set; }
        }

        public class RequestBody
        {
            public string model { get; set; }
            public List<Message> messages { get; set; }
            public double repetition_penalty { get; set; }
        }

        public class Choice
        {
            public Message message { get; set; }
        }

        public class ResponseBody
        {
            public List<Choice> choices { get; set; }
            public string model { get; set; }
        }

        [ConsoleSector("butterBror.Utils.Tools.API.AI", "Request")]
        public static async Task<string[]> Request(string request, string umodel, Platforms platform, string username, string userID, string lang, double repetitionPenalty, bool chatHistory = true)
        {
            Core.Statistics.FunctionsUsed.Add();

            DateTime requestTime = DateTime.UtcNow;
            var api_key = Manager.Get<string>(Core.Bot.Pathes.Settings, "openrouter_token");
            var uri = new Uri("https://openrouter.ai/api/v1/chat/completions");

            string selected_model = "meta-llama/llama-4-maverick:free";
            string model = "qwen";
            if (umodel is not null)
            {
                model = umodel.ToLower();
                if (!available_models.ContainsKey(model))
                {
                    return new[] { "ERR", "Model not found" };
                }

                selected_model = available_models[model];
            }

            if (string.IsNullOrWhiteSpace(request))
                return new[] { "ERR", "Empty request" };

            var system_message = new Message
            {
                role = "system",
                content = $@"You are bot on platform: {Platform.strings[(int)platform]}. Your name is {Core.Bot.BotName}. DO NOT POST CONFIDENTIAL INFORMATION, DO NOT USE PROFANITY! WRITE LESS THAN 50 WORDS! SHORTEN YOUR TEXT!"
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
                var history = UsersData.Get<List<string>>(userID, "gpt_history", platform);
                messages.Insert(0, new Message
                {
                    role = "system",
                    content = $"Chat history:\n{string.Join('\n', history)}"
                });
            }

            RequestBody request_body = new RequestBody
            {
                model = selected_model,
                messages = messages,
                repetition_penalty = repetitionPenalty
            };

            using var client = new HttpClient();
            client.Timeout = models_timeout[model];
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
                        var history = UsersData.Get<List<string>>(userID, "gpt_history", platform);
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
