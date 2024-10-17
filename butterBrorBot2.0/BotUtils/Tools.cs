using Google.Apis.Services;
using Google.Apis.YouTube.v3.Data;
using Google.Apis.YouTube.v3;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Globalization;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using TwitchLib.Client.Events;
using butterBib;
using TwitchLib.Client.Enums;
using System.Diagnostics;
using butterBror.Utils.DataManagers;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace butterBror
{
    namespace Utils
    {
        public class DebugUtil
        {
            public static bool IsDebugEnabled = false;
            public static void LOG(string message, ConsoleColor FG = ConsoleColor.Gray, ConsoleColor BG = ConsoleColor.Black, bool WrapLine = true, bool ShowDate = true)
            {
                if (IsDebugEnabled)
                {
                    Console.ResetColor();
                    char EndSymbol = '\n';
                    if (message.StartsWith("\n"))
                    {
                        Console.Write("\n");
                    }
                    if (!WrapLine)
                    {
                        EndSymbol = ' ';
                    }
                    if (ShowDate)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkGreen;
                        Console.Write($"[DEBUG] [{DateTime.Now.Hour}:{DateTime.Now.Minute}.{DateTime.Now.Second} ({DateTime.Now.Millisecond})]: ");
                        Console.ResetColor();
                    }
                    if (FG != ConsoleColor.Gray)
                    {
                        Console.ForegroundColor = FG;
                    }
                    if (BG != ConsoleColor.Black)
                    {
                        Console.BackgroundColor = BG;
                    }
                    if (message.StartsWith("\n"))
                    {
                        Console.Write(" " + ("LO0O0OL" + message).Replace("LO0O0OL\n", "") + EndSymbol);
                    }
                    else
                    {
                        Console.Write(" " + message + EndSymbol);
                    }
                }
            }
            public static void SetTaskID(int TaskID, CommandData data)
            {
                Commands.ErrorsInCommands[data.CommandInstanceUUID] = 0;
            }
            
        }
        public class TasksDebugUtil
        {
            private int TaskNow = 0;
            public void SetTask(int TaskID)
            {
                TaskNow = 100 + TaskID;
            }
            public int GetTask()
            {
                return TaskNow;
            }
        }
        /// <summary>
        /// Получение токена авторизации твича
        /// </summary>
        public class TwitchTokenUtil
        {
            private readonly string _clientId;
            private readonly string _clientSecret;
            private readonly string _redirectUri;
            private readonly string _databasePath;
            private TokenData _tokenData;
            /// <summary>
            /// Получение токена авторизации твича
            /// </summary>
            public TwitchTokenUtil(string clientId, string clientSecret, string databasePath)
            {
                _clientId = clientId;
                _clientSecret = clientSecret;
                _redirectUri = "http://localhost:12121/";
                _databasePath = databasePath;
                _tokenData = LoadTokenData();
            }
            /// <summary>
            /// Получение токена авторизации твича
            /// </summary>
            public async Task<string> GetTokenAsync()
            {
                try
                {
                    if (_tokenData != null && _tokenData.ExpiresAt > DateTime.Now)
                    {
                        return _tokenData.AccessToken;
                    }
                    if (_tokenData == null || _tokenData.RefreshToken == null)
                    {
                        return await PerformAuthorizationFlow();
                    }
                    else
                    {
                        return await RefreshAccessToken();
                    }
                }
                catch (Exception ex)
                {
                    ConsoleUtil.LOG("Ошибка получения токена: " + ex.Message);
                    return null;
                }
            }
            /// <summary>
            /// Поток выполнения авторизации
            /// </summary>
            private async Task<string> PerformAuthorizationFlow()
            {
                using var listener = new HttpListener();
                listener.Prefixes.Add(_redirectUri);
                listener.Start();

                var authorizationCode = await GetAuthorizationCodeAsync(listener);
                var token = await ExchangeCodeForTokenAsync(authorizationCode);
                SaveTokenData(token);

                // Возвращаем HTML-страницу клиенту
                var context = await listener.GetContextAsync();
                var response = context.Response;
                string responseString = @"
<html>
    <head>
        <meta charset='UTF-8'>
        <title>Авторизация завершена</title>
    </head>
    <body>
        <h2> Готово <img src='https://static-cdn.jtvnw.net/emoticons/v2/28/default/dark/1.0' style='vertical-align: middle;'/> 👍</h2>
		<div> Можете закрыть эту страницу кожанный мешок с костями</div>
    </body>
</html>";

                byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                response.ContentType = "text/html; charset=UTF-8";
                using (var output = response.OutputStream)
                {
                    await output.WriteAsync(buffer, 0, buffer.Length);
                }

                return token.AccessToken;
            }

            /// <summary>
            /// Обновление токена авторизации
            /// </summary>
            public async Task<string> RefreshAccessToken()
            {
                var httpClient = new HttpClient();
                // Tools.LOG($"Refresh token: {_tokenData.RefreshToken}, Client id: {_clientId}, Client secret: {_clientSecret}");
                var request = new HttpRequestMessage(HttpMethod.Post, "https://id.twitch.tv/oauth2/token")
                {
                    Content = new StringContent($"grant_type=refresh_token&refresh_token={_tokenData.RefreshToken}&client_id={_clientId}&client_secret={_clientSecret}", Encoding.UTF8, "application/x-www-form-urlencoded")
                };

                var response = await httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(responseContent);
                    _tokenData.AccessToken = tokenResponse.access_token;
                    _tokenData.ExpiresAt = DateTime.Now.AddSeconds(tokenResponse.expires_in);
                    SaveTokenData(_tokenData);
                    return tokenResponse.access_token;
                }
                else
                {
                    ConsoleUtil.LOG($"Ошибка при обновлении токена: {responseContent}", ConsoleColor.Black, ConsoleColor.Red);
                    return null;
                }
            }
            /// <summary>
            /// Открытие ссылки на авторизацию в браузере
            /// </summary>
            private async Task<string> GetAuthorizationCodeAsync(HttpListener listener)
            {
                var url = $"https://id.twitch.tv/oauth2/authorize?client_id={_clientId}&redirect_uri={_redirectUri}&response_type=code&scope=user:manage:chat_color+chat:edit+chat:read";
                var psi = new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                };
                Process.Start(psi);

                var context = await listener.GetContextAsync();
                var request = context.Request;
                var code = GetCodeFromResponse(request.Url.Query);

                return code;
            }
            /// <summary>
            /// Код обмена для токена
            /// </summary>
            private async Task<TokenData> ExchangeCodeForTokenAsync(string code)
            {
                var httpClient = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Post, "https://id.twitch.tv/oauth2/token")
                {
                    Content = new StringContent($"client_id={_clientId}&client_secret={_clientSecret}&redirect_uri={_redirectUri}&grant_type=authorization_code&code={code}", Encoding.UTF8, "application/x-www-form-urlencoded")
                };

                var response = await httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(responseContent);
                    return new TokenData { AccessToken = tokenResponse.access_token, ExpiresAt = DateTime.Now.AddSeconds(tokenResponse.expires_in), RefreshToken = tokenResponse.refresh_token };
                }
                else
                {
                    ConsoleUtil.LOG($"Ошибка при получении токена: {responseContent}");
                    return null;
                }
            }
            /// <summary>
            /// Получить код из ответа
            /// </summary>
            private string GetCodeFromResponse(string response)
            {
                var uri = new Uri($"http://localhost:8080/tauth{response}");
                var query = uri.Query;
                var queryParts = query.Split('&');
                foreach (var part in queryParts)
                {
                    var keyValue = part.Split('=');
                    if (keyValue[0] == "?code")
                    {
                        return keyValue[1];
                    }
                }
                return null;
            }
            /// <summary>
            /// Загрузить токена из базы данных
            /// </summary>
            private TokenData LoadTokenData()
            {
                if (File.Exists(_databasePath))
                {
                    var tokenData = JsonConvert.DeserializeObject<TokenData>(File.ReadAllText(_databasePath));
                    if (tokenData.ExpiresAt > DateTime.Now)
                    {
                        return tokenData;
                    }
                }
                return null;
            }
            /// <summary>
            /// Сохранить токен в базу данных
            /// </summary>
            private void SaveTokenData(TokenData tokenData)
            {
                File.WriteAllText(_databasePath, JsonConvert.SerializeObject(tokenData));
                _tokenData = tokenData;
            }

            private class TokenResponse
            {
                public string access_token { get; set; }
                public int expires_in { get; set; }
                public string refresh_token { get; set; }
            }
            private class TokenData
            {
                public string AccessToken { get; set; }
                public DateTime ExpiresAt { get; set; }
                public string RefreshToken { get; set; }
            }
        }
        /// <summary>
        /// Утилита для форматов
        /// </summary>
        public class FormatUtil
        {
            /// <summary>
            /// Текст в число
            /// </summary>
            public static int ToNumber(string input)
            {
                try
                {
                    string pattern = @"[^-1234567890]";
                    int result = Int32.Parse(Regex.Replace(input, pattern, ""));
                    return result;
                }
                catch (Exception ex)
                {
                    ConsoleUtil.ErrorOccured(ex.Message, "wbot2A");
                    return 0;
                }
            }
            /// <summary>
            /// Текст в число long
            /// </summary>
            public static long ToLong(string input)
            {
                try
                {
                    string pattern = @"[^-1234567890]";
                    long result = long.Parse(Regex.Replace(input, pattern, ""));
                    return result;
                }
                catch (Exception ex)
                {
                    ConsoleUtil.ErrorOccured(ex.Message, "wbot2A");
                    return 0;
                }
            }
            /// <summary>
            /// Получить кол-во времени до
            /// </summary>
            public static TimeSpan GetTimeTo(DateTime time, DateTime now, bool addYear = true)
            {
                TimeSpan Return;
                if (now < time || !addYear)
                {
                    Return = time - now;
                }
                else
                {
                    time = time.AddYears(1);
                    Return = time - now;
                }
                return Return;
            }
        }
        /// <summary>
        /// Утилита для балансов
        /// </summary>
        public class BalanceUtil
        {
            /// <summary>
            /// Добавить/уменьшить баланс пользователя
            /// </summary>
            public static void SaveBalance(string userID, int plusBalance, int plusFloatBalance)
            {
                try
                {
                    int floatBalance = 0;
                    int balance = 0;
                    if (GetFloatBalance(userID) != null || GetBalance(userID) != null)
                    {
                        floatBalance = GetFloatBalance(userID) + plusFloatBalance;
                        balance = GetBalance(userID) + plusBalance;
                    }

                    BotEngine.buttersTotalAmount += plusBalance + (float)(plusFloatBalance / 100.0);

                    while (floatBalance >= 100)
                    {
                        balance++;
                        floatBalance -= 100;
                    }

                    UsersData.UserSaveData(userID, "floatBalance", floatBalance, false);
                    UsersData.UserSaveData(userID, "balance", balance, false);
                }
                catch (Exception ex)
                {
                    ConsoleUtil.ErrorOccured(ex.Message, "wbot0A");
                }
            }
            /// <summary>
            /// Получение бутеров пользователя
            /// </summary>
            public static int GetBalance(string userID)
            {
                return UsersData.UserGetData<int>(userID, "balance");
            }
            /// <summary>
            /// Получение крошек пользователя
            /// </summary>
            public static int GetFloatBalance(string userID)
            {
                return UsersData.UserGetData<int>(userID, "floatBalance");
            }
        }
        /// <summary>
        /// Утилита для чата
        /// </summary>
        public class ChatUtil
        {
            /// <summary>
            /// Вернуть пользователя из АФК
            /// </summary>
            public static async void ReturnFromAFK(OnMessageReceivedArgs e)
            {
                var lang = "ru";

                try
                {
                    if (UsersData.UserGetData<string>(e.ChatMessage.UserId, "language") == default)
                    {
                        UsersData.UserSaveData(e.ChatMessage.UserId, "language", "ru");
                    }
                    else
                    {
                        lang = UsersData.UserGetData<string>(e.ChatMessage.UserId, "language");
                    }
                }
                catch (Exception ex)
                {
                    ConsoleUtil.ErrorOccured(ex.Message, "afkBack");
                }
                var message = UsersData.UserGetData<string>(e.ChatMessage.UserId, "afkText");
                if (NoBanwords.fullCheck(message, e.ChatMessage.RoomId))
                {
                    var text = "";

                    string send = "";

                    if (TextUtil.FilterTextWithoutSpaces(message) == "")
                    {
                        send = "";
                    }
                    else
                    {
                        send = ": " + message;
                    }

                    DateTime currentTime = DateTime.UtcNow;
                    TimeSpan timeElapsed = currentTime - UsersData.UserGetData<DateTime>(e.ChatMessage.UserId, "afkTime");
                    var afkType = UsersData.UserGetData<string>(e.ChatMessage.UserId, "afkType");
                    if (afkType == "draw")
                    {
                        if (timeElapsed.TotalHours >= 2 && timeElapsed.TotalHours < 8)
                        {
                            text = TranslationManager.GetTranslation(lang, "draw:2h", e.ChatMessage.RoomId);
                        }
                        else if (timeElapsed.TotalHours >= 8 && timeElapsed.TotalHours < 24)
                        {
                            text = TranslationManager.GetTranslation(lang, "draw:8h", e.ChatMessage.RoomId);
                        }
                        else if (timeElapsed.TotalDays >= 1 && timeElapsed.TotalDays < 7)
                        {
                            text = TranslationManager.GetTranslation(lang, "draw:1d", e.ChatMessage.RoomId);
                        }
                        else if (timeElapsed.TotalDays >= 7 && timeElapsed.TotalDays < 31)
                        {
                            text = TranslationManager.GetTranslation(lang, "draw:7d", e.ChatMessage.RoomId);
                        }
                        else if (timeElapsed.TotalDays >= 31 && timeElapsed.TotalDays < 364)
                        {
                            text = TranslationManager.GetTranslation(lang, "draw:1mn", e.ChatMessage.RoomId);
                        }
                        else if (timeElapsed.TotalDays >= 364)
                        {
                            text = TranslationManager.GetTranslation(lang, "draw:1y", e.ChatMessage.RoomId);
                        }
                        else
                        {
                            text = TranslationManager.GetTranslation(lang, "draw:default", e.ChatMessage.RoomId);
                        }
                    }
                    else if (afkType == "afk")
                    {
                        if (timeElapsed.TotalHours >= 8 && timeElapsed.TotalHours < 14)
                        {
                            text = TranslationManager.GetTranslation(lang, "afk:8h", e.ChatMessage.RoomId);
                        }
                        else if (timeElapsed.TotalHours >= 14 && timeElapsed.TotalDays < 1)
                        {
                            text = TranslationManager.GetTranslation(lang, "afk:14h", e.ChatMessage.RoomId);
                        }
                        else if (timeElapsed.TotalDays >= 1 && timeElapsed.TotalDays < 3)
                        {
                            text = TranslationManager.GetTranslation(lang, "afk:1d", e.ChatMessage.RoomId);
                        }
                        else if (timeElapsed.TotalDays >= 3 && timeElapsed.TotalDays < 7)
                        {
                            text = TranslationManager.GetTranslation(lang, "afk:3d", e.ChatMessage.RoomId);
                        }
                        else if (timeElapsed.TotalDays >= 7 && timeElapsed.TotalDays < 9)
                        {
                            text = TranslationManager.GetTranslation(lang, "afk:7d", e.ChatMessage.RoomId);
                        }
                        else if (timeElapsed.TotalDays >= 9 && timeElapsed.TotalDays < 31)
                        {
                            text = TranslationManager.GetTranslation(lang, "afk:9d", e.ChatMessage.RoomId);
                        }
                        else if (timeElapsed.TotalDays >= 31 && timeElapsed.TotalDays < 364)
                        {
                            text = TranslationManager.GetTranslation(lang, "afk:1mn", e.ChatMessage.RoomId);
                        }
                        else if (timeElapsed.TotalDays >= 364)
                        {
                            text = TranslationManager.GetTranslation(lang, "afk:1y", e.ChatMessage.RoomId);
                        }
                        else
                        {
                            text = TranslationManager.GetTranslation(lang, "afk:default", e.ChatMessage.RoomId);
                        }
                    }
                    else if (afkType == "sleep")
                    {
                        if (timeElapsed.TotalHours >= 2 && timeElapsed.TotalHours < 5)
                        {
                            text = TranslationManager.GetTranslation(lang, "sleep:2h", e.ChatMessage.RoomId);
                        }
                        else if (timeElapsed.TotalHours >= 5 && timeElapsed.TotalHours < 8)
                        {
                            text = TranslationManager.GetTranslation(lang, "sleep:5h", e.ChatMessage.RoomId);
                        }
                        else if (timeElapsed.TotalHours >= 8 && timeElapsed.TotalHours < 12)
                        {
                            text = TranslationManager.GetTranslation(lang, "sleep:8h", e.ChatMessage.RoomId);
                        }
                        else if (timeElapsed.TotalHours >= 12 && timeElapsed.TotalDays < 1)
                        {
                            text = TranslationManager.GetTranslation(lang, "sleep:12h", e.ChatMessage.RoomId);
                        }
                        else if (timeElapsed.TotalDays >= 1 && timeElapsed.TotalDays < 3)
                        {
                            text = TranslationManager.GetTranslation(lang, "sleep:1d", e.ChatMessage.RoomId);
                        }
                        else if (timeElapsed.TotalDays >= 3 && timeElapsed.TotalDays < 7)
                        {
                            text = TranslationManager.GetTranslation(lang, "sleep:3d", e.ChatMessage.RoomId);
                        }
                        else if (timeElapsed.TotalDays >= 7 && timeElapsed.TotalDays < 31)
                        {
                            text = TranslationManager.GetTranslation(lang, "sleep:7d", e.ChatMessage.RoomId);
                        }
                        else if (timeElapsed.TotalDays >= 31 && timeElapsed.TotalDays < 364)
                        {
                            text = TranslationManager.GetTranslation(lang, "sleep:1mn", e.ChatMessage.RoomId);
                        }
                        else if (timeElapsed.TotalDays >= 364)
                        {
                            text = TranslationManager.GetTranslation(lang, "sleep:1y", e.ChatMessage.RoomId);
                        }
                        else
                        {
                            text = TranslationManager.GetTranslation(lang, "sleep:default", e.ChatMessage.RoomId);
                        }
                    }
                    else if (afkType == "rest")
                    {
                        if (timeElapsed.TotalHours >= 8 && timeElapsed.TotalHours < 24)
                        {
                            text = TranslationManager.GetTranslation(lang, "rest:8h", e.ChatMessage.RoomId);
                        }
                        else if (timeElapsed.TotalDays >= 1 && timeElapsed.TotalDays < 7)
                        {
                            text = TranslationManager.GetTranslation(lang, "rest:1d", e.ChatMessage.RoomId);
                        }
                        else if (timeElapsed.TotalDays >= 7 && timeElapsed.TotalDays < 31)
                        {
                            text = TranslationManager.GetTranslation(lang, "rest:7d", e.ChatMessage.RoomId);
                        }
                        else if (timeElapsed.TotalDays >= 31 && timeElapsed.TotalDays < 364)
                        {
                            text = TranslationManager.GetTranslation(lang, "rest:1mn", e.ChatMessage.RoomId);
                        }
                        else if (timeElapsed.TotalDays >= 364)
                        {
                            text = TranslationManager.GetTranslation(lang, "rest:1y", e.ChatMessage.RoomId);
                        }
                        else
                        {
                            text = TranslationManager.GetTranslation(lang, "rest:default", e.ChatMessage.RoomId);
                        }
                    }
                    else if (afkType == "lurk")
                    {
                        text = TranslationManager.GetTranslation(lang, "lurk:default", e.ChatMessage.RoomId);
                    }
                    else if (afkType == "study")
                    {
                        if (timeElapsed.TotalHours >= 2 && timeElapsed.TotalHours < 5)
                        {
                            text = TranslationManager.GetTranslation(lang, "study:2h", e.ChatMessage.RoomId);
                        }
                        else if (timeElapsed.TotalHours >= 5 && timeElapsed.TotalHours < 8)
                        {
                            text = TranslationManager.GetTranslation(lang, "study:5h", e.ChatMessage.RoomId);
                        }
                        else if (timeElapsed.TotalHours >= 8 && timeElapsed.TotalHours < 24)
                        {
                            text = TranslationManager.GetTranslation(lang, "study:8h", e.ChatMessage.RoomId);
                        }
                        else if (timeElapsed.TotalDays >= 1 && timeElapsed.TotalDays < 7)
                        {
                            text = TranslationManager.GetTranslation(lang, "study:1d", e.ChatMessage.RoomId);
                        }
                        else if (timeElapsed.TotalDays >= 7 && timeElapsed.TotalDays < 31)
                        {
                            text = TranslationManager.GetTranslation(lang, "study:7d", e.ChatMessage.RoomId);
                        }
                        else if (timeElapsed.TotalDays >= 31 && timeElapsed.TotalDays < 364)
                        {
                            text = TranslationManager.GetTranslation(lang, "study:1mn", e.ChatMessage.RoomId);
                        }
                        else if (timeElapsed.TotalDays >= 364)
                        {
                            text = TranslationManager.GetTranslation(lang, "study:1y", e.ChatMessage.RoomId);
                        }
                        else
                        {
                            text = TranslationManager.GetTranslation(lang, "study:default", e.ChatMessage.RoomId);
                        }
                    }
                    else if (afkType == "poop")
                    {
                        if (timeElapsed.TotalMinutes >= 1 && timeElapsed.TotalHours < 1)
                        {
                            text = TranslationManager.GetTranslation(lang, "poop:1m", e.ChatMessage.RoomId);
                        }
                        else if (timeElapsed.TotalHours >= 1 && timeElapsed.TotalHours < 8)
                        {
                            text = TranslationManager.GetTranslation(lang, "poop:1h", e.ChatMessage.RoomId);
                        }
                        else if (timeElapsed.TotalHours >= 8)
                        {
                            text = TranslationManager.GetTranslation(lang, "poop:8h", e.ChatMessage.RoomId);
                        }
                        else
                        {
                            text = TranslationManager.GetTranslation(lang, "poop:default", e.ChatMessage.RoomId);
                        }
                    }
                    else if (afkType == "shower")
                    {
                        if (timeElapsed.TotalMinutes >= 1 && timeElapsed.TotalMinutes < 10)
                        {
                            text = TranslationManager.GetTranslation(lang, "shower:1m", e.ChatMessage.RoomId);
                        }
                        if (timeElapsed.TotalMinutes >= 10 && timeElapsed.TotalHours < 1)
                        {
                            text = TranslationManager.GetTranslation(lang, "shower:10m", e.ChatMessage.RoomId);
                        }
                        else if (timeElapsed.TotalHours >= 1 && timeElapsed.TotalHours < 8)
                        {
                            text = TranslationManager.GetTranslation(lang, "shower:1h", e.ChatMessage.RoomId);
                        }
                        else if (timeElapsed.TotalHours >= 8)
                        {
                            text = TranslationManager.GetTranslation(lang, "shower:8h", e.ChatMessage.RoomId);
                        }
                        else
                        {
                            text = TranslationManager.GetTranslation(lang, "shower:default", e.ChatMessage.RoomId);
                        }
                    }
                    UsersData.UserSaveData(e.ChatMessage.UserId, "lastFromAfkResume", DateTime.UtcNow);
                    UsersData.UserSaveData(e.ChatMessage.UserId, "isAfk", false);
                    await CommandUtil.ChangeNicknameColorAsync(ChatColorPresets.YellowGreen);
                    SendMsgReply(e.ChatMessage.Channel, e.ChatMessage.RoomId, text.Replace("%user%", e.ChatMessage.Username) + send + " (" + TextUtil.FormatTimeSpan(FormatUtil.GetTimeTo(UsersData.UserGetData<DateTime>(e.ChatMessage.UserId, "afkTime"), DateTime.UtcNow, false), lang) + ")", e.ChatMessage.Id, lang, true);
                }
            }
            /// <summary>
            /// Отправить сообщение в чат Twitch
            /// </summary>
            public static void SendMessage(string channel, string message, string channelID, string messageID, string lang, bool isSafeEx = false)
            {
                ConsoleServer.SendConsoleMessage("commands", "Отправка сообщения...");
                LogWorker.Log($"Было отправлено сообщение в канал {channel}: {message}", LogWorker.LogTypes.Info, "SMR");
                if (!Bot.client.JoinedChannels.Any(c => c.Channel == channel))
                {
                    Bot.client.JoinChannel(channel);
                }
                if (Bot.client.JoinedChannels.Any(c => c.Channel == channel))
                {
                    if (isSafeEx)
                    {
                        Bot.client.SendMessage(channel, message);
                    }
                    else if (NoBanwords.fullCheck(message, channelID))
                    {
                        Bot.client.SendMessage(channel, message);
                    }
                    else
                    {
                        Bot.client.SendReply(channel, messageID, TranslationManager.GetTranslation(lang, "cantSend", channelID));
                    }
                }
            }
            /// <summary>
            /// Отправить ответ на сообщение в чат Twitch
            /// </summary>
            public static void SendMsgReply(string channel, string channelID, string message, string messageID, string lang, bool isSafeEx = false)
            {
                ConsoleServer.SendConsoleMessage("commands", "Отправка сообщения...");
                LogWorker.Log($"Был отправлен ответ на сообщение в канал {channel}: {message}", LogWorker.LogTypes.Info, "SMR");
                message = TextUtil.FilterText(message);

                if (message.Length > 1500)
                {
                    message = TranslationManager.GetTranslation(lang, "tooLargeText", channelID);
                }
                else if (message.Length > 500)
                {
                    int splitIndex = message.LastIndexOf(' ', 450);

                    string part1 = message.Substring(0, splitIndex) + "...";
                    string part2 = "... " + message.Substring(splitIndex);

                    message = part1;

                    Task task = Task.Run(() =>
                    {
                        Thread.Sleep(1000);
                        SendMsgReply(channel, channelID, part2, messageID, lang, isSafeEx);
                    });
                }

                if (!Bot.client.JoinedChannels.Any(c => c.Channel == channel))
                {
                    Bot.client.JoinChannel(channel);
                }
                if (Bot.client.JoinedChannels.Any(c => c.Channel == channel))
                {
                    if (isSafeEx)
                    {
                        Bot.client.SendReply(channel, messageID, message);
                    }
                    else if (NoBanwords.fullCheck(message, channelID))
                    {
                        Bot.client.SendReply(channel, messageID, message);
                    }
                    else
                    {
                        Bot.client.SendReply(channel, messageID, TranslationManager.GetTranslation(lang, "cantSend", channelID));
                    }
                }
            }
        }
        /// <summary>
        /// Утилита для никнеймов
        /// </summary>
        public class NamesUtil
        {
            /// <summary>
            /// Получение никнейма из текста
            /// </summary>
            public static string GetUsernameFromText(string text)
            {
                try
                {
                    if (text.Contains("@"))
                    {
                        var selectedUser = "";
                        string pattern = @"@(\w+)";
                        MatchCollection matches = Regex.Matches(text, pattern);
                        selectedUser = " @" + matches.ElementAt(0).ToString().Replace("@", "");
                        return selectedUser;
                    }
                }
                catch (Exception ex)
                {
                    ConsoleUtil.ErrorOccured(ex.Message, "GUFT");
                }
                return string.Empty;
            }
            /// <summary>
            /// Получить ID пользователя по никнейму
            /// </summary>
            public static string GetUserID(string user, string executedUsername = "err")
            {
                if (File.Exists(Bot.NicknameToIDPath + $"{user.ToLower()}.txt"))
                {
                    var userID = File.ReadAllText(Bot.NicknameToIDPath + $"{user.ToLower()}.txt");
                    return userID;
                }
                return executedUsername;
            }
            /// <summary>
            /// Получить имя пользователя по ID
            /// </summary>
            public static string GetUsername(string ID, string executedID)
            {
                if (File.Exists(Bot.IDToNicknamePath + $"{ID}.txt"))
                {
                    var nick = File.ReadAllText(Bot.IDToNicknamePath + $"{ID}.txt");
                    return nick;
                }
                return executedID;
            }
            /// <summary>
            /// Добавить в текст невидимые символы, чтобы не пинговать чатеров
            /// </summary>
            public static string DontPingUsername(string username)
            {
                char[] chars = username.ToCharArray();
                string newText = "";

                for (int i = 0; i < chars.Length; i++)
                {
                    newText += chars[i];
                    if (i != chars.Length - 1)
                    {
                        newText += "󠀀";
                    }
                }

                return newText;
            }
        }
        /// <summary>
        /// Утилита для консоли
        /// </summary>
        public class ConsoleUtil
        {
            /// <summary>
            /// Вывести текст в консоль
            /// </summary>
            public static void LOG(string message, ConsoleColor FG = ConsoleColor.Gray, ConsoleColor BG = ConsoleColor.Black, bool WrapLine = true, bool ShowDate = true)
            {
                Console.ResetColor();
                char EndSymbol = '\n';
                if (message.StartsWith("\n"))
                {
                    Console.Write("\n");
                }
                if (!WrapLine)
                {
                    EndSymbol = ' ';
                }
                if (ShowDate)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.Write($" [{DateTime.Now.Hour}:{DateTime.Now.Minute}.{DateTime.Now.Second} ({DateTime.Now.Millisecond})]: ");
                    Console.ResetColor();
                }
                if (FG != ConsoleColor.Gray)
                {
                    Console.ForegroundColor = FG;
                }
                if (BG != ConsoleColor.Black)
                {
                    Console.BackgroundColor = BG;
                }
                if (message.StartsWith("\n"))
                {
                    Console.Write(" " + ("LO0O0OL" + message).Replace("LO0O0OL\n", "") + EndSymbol);
                }
                else
                {
                    Console.Write(" " + message + EndSymbol);
                }
            }
            /// <summary>
            /// Вывести ошибку в консоль
            /// </summary>
            public static void ErrorOccured(string message, string sector)
            {
                ConsoleServer.SendConsoleMessage("errors", $"Произошла ошибка: {message} ({sector})");
                LogWorker.Log($"Ошибка! \"{message}\"", LogWorker.LogTypes.Err, sector);
            } 
            /// <summary>
            /// Обновление заголовка консоли
            /// </summary>
            public static void Title(string version, int completed_commands, int readed_messages, TimeSpan work_time)
            {
                Console.Title = $"butterBror ({work_time.Days}д. {work_time.Hours}ч. {work_time.Minutes} мин. {work_time.Seconds} сек.) · Версия {version} · Выполнен(о/а/ы) {completed_commands} комманд(а/ы) · Прочитал {readed_messages} сообщен(ий/я)";
            } 
        }
        /// <summary>
        /// Утилита для текста
        /// </summary>
        public class TextUtil
        {
            /// <summary>
            /// Превратить время в текст
            /// </summary>
            public static string GetTextTime(TimeSpan Time)
            {
                if (Time.Days == 0)
                {
                    if (Time.Hours == 0)
                    {
                        if (Time.Minutes == 0)
                        {
                            return $"{Time.Seconds} сек. {Time.Nanoseconds} нсек.";
                        }
                        else
                        {
                            return $"{Time.Minutes} мин. {Time.Seconds} сек.";
                        }
                    }
                    else
                    {
                        return $"{Time.Hours}ч. {Time.Minutes} мин. {Time.Seconds} сек.";
                    }
                }
                else
                {
                    return $"{Time.Days}д. {Time.Hours}ч. {Time.Minutes} мин. {Time.Seconds} сек.";
                }
            }
            /// <summary>
            /// Фильтровать название комманды
            /// </summary>
            public static string FilterCommand(string input)
            {
                try
                {
                    string pattern = @"[^qwertyuiopasdfghjklzxcvbnmйцукенгшщзхъфывапролджэячсмитьбюёQWERTYUIOPASDFGHJKLZXCVBNMЙЦУКЕНГШЩЗХЪФЫВАПРОЛДЖЭЯЧСМИТЬБЮЁ1234567890%]";
                    string result = Regex.Replace(input, pattern, "");
                    return result;
                }
                catch (Exception ex)
                {
                    ConsoleUtil.ErrorOccured(ex.Message, "wbot1A");
                    return "none";
                }
            }
            /// <summary>
            /// Сменить раскладку текста
            /// </summary>
            public static string ChangeLayout(string text)
            {
                string layout = "qwertyuiop[]asdfghjkl;'zxcvbnm,.";
                string rusLayout = "йцукенгшщзхъфывапролджэячсмитьбю";

                char[] textArray = text.ToCharArray();
                for (int i = 0; i < textArray.Length; i++)
                {
                    if (char.IsLetter(textArray[i]))
                    {
                        int index = layout.IndexOf(char.ToLower(textArray[i]));
                        if (index != -1)
                        {
                            char newChar = char.IsUpper(textArray[i]) ? char.ToUpper(rusLayout[index]) : rusLayout[index];
                            textArray[i] = newChar;
                        }
                        else
                        {
                            index = rusLayout.IndexOf(char.ToLower(textArray[i]));
                            if (index != -1)
                            {
                                char newChar = char.IsUpper(textArray[i]) ? char.ToUpper(layout[index]) : layout[index];
                                textArray[i] = newChar;
                            }
                        }
                    }
                }
                return new string(textArray);
            }
            /// <summary>
            /// Фильтровать текст от ASCII
            /// </summary>
            public static string FilterText(string input)
            {
                string pattern = @"[^A-Za-zА-Яа-яёЁ\uD800-\uDB7F\uDB80-\uDFFF\u2705☀⛵⚙〽️❄❗🌫️🌨️⚖️⏺️⛈️🗻🌧️🌥️☁️⛅🌤️☀️ ⬛󠀀°.?/\\,·':;}{\][()*+-`~%$#@&№!»—«|]";
                string filteredText = Regex.Replace(input, pattern, "");

                return filteredText;
            }
            /// <summary>
            /// Фильтровать текст от ASCII без пробелов
            /// </summary>
            public static string FilterTextWithoutSpaces(string input)
            {
                string pattern = @"[^A-Za-zА-Яа-яёЁ\uD800-\uDB7F\uDB80-\uDFFF\u2705☀⛵⚙⏺️❗⚖️〽️❄°.?/\\,·':;}{\][()*+-`~%$#@&№!»—«]";
                string filteredText = Regex.Replace(input, pattern, "");

                return filteredText;
            }
            /// <summary>
            /// Удалить дублирующиеся символы из текста
            /// </summary>
            public static string RemoveDuplicateLetters(string text)
            {
                try
                {
                    StringBuilder result = new StringBuilder();
                    for (int i = 0; i < text.Length; i++)
                    {
                        if (i == 0 || text[i] != text[i - 1])
                        {
                            result.Append(text[i]);
                        }
                    }
                    return result.ToString();
                }
                catch (Exception ex)
                {
                    ConsoleUtil.ErrorOccured(ex.Message, "RDL");
                    return string.Empty;
                }
            }
            /// <summary>
            /// Фильтровать никнейм
            /// </summary>
            public static string NicknameFilter(string input)
            {
                string pattern = @"[^A-Za-z0-9_-]";
                string filteredText = Regex.Replace(input, pattern, "");

                return filteredText;
            }
            /// <summary>
            /// Сократить координаты
            /// </summary>
            public static string ShortenCoordinate(string coordinate)
            {
                // Убираем последнюю букву (N, S, E, W)
                char direction = coordinate[coordinate.Length - 1];
                string numberPart = coordinate.Substring(0, coordinate.Length - 1);

                // Преобразуем строку в число и округляем до одного знака после запятой
                if (double.TryParse(numberPart, NumberStyles.Float, CultureInfo.InvariantCulture, out double number))
                {
                    number = Math.Round(number, 1);
                    return $"{number.ToString(CultureInfo.InvariantCulture)}{direction}";
                }
                else
                {
                    throw new ArgumentException("Invalid coordinate format");
                }
            }
            /// <summary>
            /// Вывести время до
            /// </summary>
            public static string TimeTo(DateTime startTime, DateTime endTime, string type, int endYearAdd, string lang, string argsText, string channelID)
            {
                var selectedUser = NamesUtil.GetUsernameFromText(argsText);
                TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");
                DateTime now = TimeZoneInfo.ConvertTime(DateTime.Now, tz);
                DateTime winterStart = new(now.Year, startTime.Month, startTime.Day);
                DateTime winterEnd = new(now.Year + endYearAdd, endTime.Month, endTime.Day);
                winterEnd = winterEnd.AddDays(-1);
                DateTime winter = now < winterStart ? winterStart : winterEnd;
                if (now < winterStart)
                {
                    return TranslationManager.GetTranslation(lang, $"to{type}", channelID)
                        .Replace("%time%", FormatTimeSpan(FormatUtil.GetTimeTo(winter, now), lang))
                        .Replace("%sUser%", selectedUser);
                }
                else
                {
                    return TranslationManager.GetTranslation(lang, $"toEndOf{type}", channelID)
                        .Replace("%time%", FormatTimeSpan(FormatUtil.GetTimeTo(winter, now), lang))
                        .Replace("%sUser%", selectedUser);
                }
            }
            /// <summary>
            /// Форматировать время в текст
            /// </summary>
            public static string FormatTimeSpan(TimeSpan timeSpan, string lang)
            {
                try
                {
                    int days = FormatUtil.ToNumber(timeSpan.Days.ToString().Replace("-", ""));
                    int hours = FormatUtil.ToNumber(timeSpan.Hours.ToString().Replace("-", ""));
                    int minutes = FormatUtil.ToNumber(timeSpan.Minutes.ToString().Replace("-", ""));
                    int seconds = FormatUtil.ToNumber(timeSpan.Seconds.ToString().Replace("-", ""));
                    if (days > 0 || days < 0)
                    {
                        return $"{days} {(days % 10 == 1 && days % 100 != 11 ? TranslationManager.GetTranslation(lang, "day", "") : days % 10 >= 2 && days % 10 <= 4 && (days % 100 < 10 || days % 100 >= 20) ? TranslationManager.GetTranslation(lang, "days1", "") : TranslationManager.GetTranslation(lang, "days2", ""))}, {hours} {(hours % 10 == 1 ? TranslationManager.GetTranslation(lang, "hour", "") : hours % 10 >= 2 && hours % 10 <= 4 ? TranslationManager.GetTranslation(lang, "hours1", "") : TranslationManager.GetTranslation(lang, "hours2", ""))}, {minutes} {(minutes % 10 == 1 ? TranslationManager.GetTranslation(lang, "minute", "") : minutes % 10 >= 2 && minutes % 10 <= 4 ? TranslationManager.GetTranslation(lang, "minutes1", "") : TranslationManager.GetTranslation(lang, "minutes2", ""))}, {seconds} {(seconds % 10 == 1 ? TranslationManager.GetTranslation(lang, "second", "") : seconds % 10 >= 2 && seconds % 10 <= 4 ? TranslationManager.GetTranslation(lang, "seconds1", "") : TranslationManager.GetTranslation(lang, "seconds2", ""))}";
                    }
                    else
                    {
                        return $"{hours} {(hours % 10 == 1 ? TranslationManager.GetTranslation(lang, "hour", "") : hours % 10 >= 2 && hours % 10 <= 4 ? TranslationManager.GetTranslation(lang, "hours1", "") : TranslationManager.GetTranslation(lang, "hours2", ""))}, {minutes} {(minutes % 10 == 1 ? TranslationManager.GetTranslation(lang, "minute", "") : minutes % 10 >= 2 && minutes % 10 <= 4 ? TranslationManager.GetTranslation(lang, "minutes1", "") : TranslationManager.GetTranslation(lang, "minutes2", ""))}, {seconds} {(seconds % 10 == 1 ? TranslationManager.GetTranslation(lang, "second", "") : seconds % 10 >= 2 && seconds % 10 <= 4 ? TranslationManager.GetTranslation(lang, "seconds1", "") : TranslationManager.GetTranslation(lang, "seconds2", ""))}";
                    }
                }
                catch (Exception ex)
                {
                    ConsoleUtil.ErrorOccured(ex.Message, "wbot3A");
                    return default;
                }
            }
        }
        /// <summary>
        /// Утилита для эмоутов
        /// </summary>
        public class EmotesUtil
        {
            /// <summary>
            /// Получение эмоутов канала из кэша
            /// </summary>
            public static async Task<string[]?> GetEmotesForChannel(string channel, string service)
            {
                if (Bot.EmotesByChannel.ContainsKey(channel + service))
                {
                    return Bot.EmotesByChannel[channel + service];
                }
                else
                {
                    return null;
                }
            }
            /// <summary>
            /// Вывести рандомный эмоут канала
            /// </summary>
            public static Dictionary<string, string> RandomEmote(string channel, string service)
            {
                string[] emotes = GetEmotesForChannel(channel, service).Result;
                Random rand = new();
                Dictionary<string, string> returnmsg = new();
                if (emotes.Length > 0)
                {
                    string randomEmote = emotes[rand.Next(emotes.Length)];
                    returnmsg["status"] = "OK";
                    returnmsg["emote"] = randomEmote;
                }
                else
                {
                    returnmsg["status"] = "BAD";
                    returnmsg["emote"] = "";
                }
                return returnmsg;
            }
            /// <summary>
            /// Обновить эмоут канала
            /// </summary>
            public static async Task EmoteUpdate(string channel)
            {
                ConsoleServer.SendConsoleMessage("info", $"Обновление эмоутов для канала {channel}...");
                var emote7tvNames = await GetEmotes(channel, "7tv");
                Bot.EmotesByChannel[channel + "7tv"] = emote7tvNames;
                Thread.Sleep(1000);
                var emoteBttvNames = await GetEmotes(channel, "bttv");
                Bot.EmotesByChannel[channel + "bttv"] = emoteBttvNames;
                Thread.Sleep(1000);
                var emoteFfzNames = await GetEmotes(channel, "ffz");
                Bot.EmotesByChannel[channel + "ffz"] = emoteFfzNames;
                ConsoleServer.SendConsoleMessage("info", $"Эмоуты для канала {channel} обновлены!");
            }
            /// <summary>
            /// Получение эмоутов канала
            /// </summary>
            public static async Task<string[]> GetEmotes(string channel, string services)
            {
                ConsoleServer.SendConsoleMessage("info", $"Получение {services} эмоутов для канала {channel}...");
                var client = new HttpClient();
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri($"https://emotes.adamcy.pl/v1/channel/{channel}/emotes/{services}"),
                    Headers =
                {
                    { "Accept", "application/json" },
                },
                };

                using var response = await client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    ConsoleServer.SendConsoleMessage("info", $"Эмоуты получены для канала {channel}!");
                    var content = await response.Content.ReadAsStringAsync();
                    var json = JArray.Parse(content);

                    // Получаем список имен эмоутов
                    var emoteNames = json.Select(emote => emote["code"].ToString()).ToArray();
                    return emoteNames;
                }
                else
                {
                    ConsoleServer.SendConsoleMessage("errors", $"Ошибка получения эмоутов для канала {channel}!");
                    string[] empty = [];
                    return empty;
                }
            }
        }
        /// <summary>
        /// Утилита для комманд
        /// </summary>
        public class CommandUtil
        {
            /// <summary>
            /// Обработать выполнение команды
            /// </summary>
            public static void executedCommand(CommandData data)
            {
                var info = $"Выполнена команда {data.Name} (Пользователь: {data.User.Name}, полное сообщение: \"{data.Name} {data.ArgsAsString}\", аргументы: \"{data.ArgsAsString}\", команда: \"{data.Name}\")";
                LogWorker.Log(info, LogWorker.LogTypes.Info, $"cmd\\{data.Name}");
                ConsoleServer.SendConsoleMessage("commands", info);
                BotEngine.CompletedCommands++;
            }
            /// <summary>
            /// Проверка команд нв кулдаун
            /// </summary>
            public static bool IsNotOnCooldown(int userSecondsCooldown, int globalCooldown, string cooldownParamName, string UserID, string RoomID, bool ResetUseTimeIfCommandIsNotReseted = true, bool IgnoreUserVIP = false, bool IgnoreGlobalCooldown = false)
            {
                if (!(UsersData.UserGetData<bool>(UserID, "isBotModerator") || UsersData.UserGetData<bool>(UserID, "isBotDev")) || IgnoreUserVIP)
                {
                    if (UsersData.IsContainsKey($"LU_{cooldownParamName}", UserID))
                    {
                        DateTime lastUserUse = UsersData.UserGetData<DateTime>(UserID, $"LU_{cooldownParamName}");
                        TimeSpan timeAfterUse = DateTime.UtcNow - lastUserUse;
                        if (timeAfterUse.TotalSeconds >= userSecondsCooldown)
                        {
                            UsersData.UserSaveData(UserID, $"LU_{cooldownParamName}", DateTime.UtcNow);
                            if (!File.Exists(Bot.ChannelsPath + RoomID + "/CDD.json"))
                            {
                                FileUtil.CreateFile(Bot.ChannelsPath + RoomID + "/CDD.json");
                                Dictionary<string, DateTime> list = new();
                                FileUtil.SaveFile(Bot.ChannelsPath + RoomID + "/CDD.json", JsonConvert.SerializeObject(list));
                            }

                            if (IgnoreGlobalCooldown)
                            {
                                return true;
                            }

                            if (DataManager.GetData<DateTime>(Bot.ChannelsPath + RoomID + "/CDD.json", $"LU_{cooldownParamName}") == default)
                            {
                                DataManager.SaveData(Bot.ChannelsPath + RoomID + "/CDD.json", $"LU_{cooldownParamName}", DateTime.UtcNow);
                                return true;
                            }
                            else
                            {
                                TimeSpan timeAfterGlobalUse = DateTime.UtcNow - DataManager.GetData<DateTime>(Bot.ChannelsPath + RoomID + "/CDD.json", $"LU_{cooldownParamName}");
                                if (timeAfterGlobalUse.TotalSeconds >= globalCooldown)
                                {
                                    DataManager.SaveData(Bot.ChannelsPath + RoomID + "/CDD.json", $"LU_{cooldownParamName}", DateTime.UtcNow);
                                    return true;
                                }
                                else
                                {
                                    ConsoleServer.SendConsoleMessage("info", $"Пользователь {NamesUtil.GetUsername(UserID, UserID)} попытался использовать команду, но она на глобальном кулдауне!");
                                    LogWorker.Log($"Пользователь {NamesUtil.GetUsername(UserID, UserID)} попытался использовать команду, но она на глобальном кулдауне!", LogWorker.LogTypes.Warn, cooldownParamName);
                                    return false;
                                }
                            }
                        }
                        else
                        {
                            if (ResetUseTimeIfCommandIsNotReseted)
                            {
                                UsersData.UserSaveData(UserID, $"LU_{cooldownParamName}", DateTime.UtcNow);
                            }
                            ConsoleServer.SendConsoleMessage("info", $"Пользователь {NamesUtil.GetUsername(UserID, UserID)} попытался использовать команду, но она на кулдауне!");
                            LogWorker.Log($"Пользователь {NamesUtil.GetUsername(UserID, UserID)} попытался использовать команду, но она на кулдауне!", LogWorker.LogTypes.Warn, cooldownParamName);
                            return false;
                        }
                    }
                    else
                    {
                        UsersData.UserSaveData(UserID, $"LU_{cooldownParamName}", DateTime.UtcNow);
                        return true;
                    }
                }
                else
                {
                    return true;
                }
            }
            /// <summary>
            /// Обработать новое сообщение
            /// </summary>
            public static void MessageWorker(string UserID, string RoomId, string Username, string Message, OnMessageReceivedArgs e, string Room, string platform, string ServerChannel = "")
            {
                bool check = true;
                try
                {
                    check = !UsersData.UserGetData<bool>(UserID, "isBanned", false) && !UsersData.UserGetData<bool>(UserID, "isIgnored", false);
                }
                catch (Exception) { }
                if (check)
                {
                    var messagesSendedPath = Bot.ChannelsPath + RoomId + "/MS/";
                    var messagesSendedUserPath = messagesSendedPath + UserID + ".txt";
                    int messagesSended = 0;
                    var time = DateTime.UtcNow;
                    string N2IPath;
                    if (platform == "ds")
                    {
                        N2IPath = Bot.NicknameToIDPath + "ds+" + Username + ".txt";
                    }
                    else
                    {
                        N2IPath = Bot.NicknameToIDPath + Username + ".txt";
                    }
                    var I2NPath = Bot.IDToNicknamePath + UserID + ".txt";

                    FileUtil.CreateDirectory(Bot.ChannelsPath + RoomId);
                    FileUtil.CreateDirectory(Bot.ChannelsPath + RoomId + "/MSGS/");
                    FileUtil.CreateDirectory(messagesSendedPath);

                    var OutPutMessage = "";

                    if (File.Exists(messagesSendedUserPath))
                    {
                        messagesSended = FormatUtil.ToNumber(File.ReadAllText(messagesSendedUserPath)) + 1;
                    }

                    Bot.ReadedMessages++;

                    if (!File.Exists(Bot.UsersDataPath + UserID + ".json"))
                    {
                        UsersData.UserRegister(UserID, Message);
                        if (platform == "tw")
                        {
                            OutPutMessage += $"{Room} · {Username}: ";
                        }
                        else if (platform == "ds")
                        {
                            OutPutMessage += $"{Room} | {ServerChannel} · {Username}: ";
                        }
                    }
                    else
                    {
                        if (platform == "tw")
                        {
                            if (UsersData.UserGetData<bool>(UserID, "isAfk"))
                            {
                                ChatUtil.ReturnFromAFK(e);
                            }
                        }
                        float addToBalance = Message.Length / 6 + 1;
                        int roundedNumber = (int)Math.Round(addToBalance, MidpointRounding.AwayFromZero);
                        BalanceUtil.SaveBalance(UserID, 0, roundedNumber);
                        int floatBalance = BalanceUtil.GetFloatBalance(UserID);
                        int balance = BalanceUtil.GetBalance(UserID);
                        if (platform == "tw")
                        {
                            OutPutMessage += $"{Room} · {Username} ({messagesSended}/{balance}.{floatBalance} c.): ";
                        }
                        else if (platform == "ds")
                        {
                            OutPutMessage += $"{Room} | {ServerChannel} · {Username} ({messagesSended}/{balance}.{floatBalance} c.): ";
                        }
                    }

                    if (!UsersData.IsContainsKey("isReadedCurrency", UserID))
                    {
                        UsersData.UserSaveData(UserID, "isReadedCurrency", true, false);
                        BotEngine.buttersTotalAmount += UsersData.UserGetData<int>(UserID, "balance") + (float)(UsersData.UserGetData<int>(UserID, "floatBalance") / 100.0);
                        BotEngine.buttersTotalUsers++;
                        OutPutMessage += "(Added to currency) ";
                    }

                    OutPutMessage += Message;
                    CAFUSUtil.Maintrance(UserID, Username);

                    string pattern = @"@(\w+)";
                    MatchCollection matches = Regex.Matches(Message, pattern);

                    List<string> usernames = new List<string>();
                    string path = Bot.NicknameToIDPath;

                    foreach (Match match in matches)
                    {
                        string user = match.Groups[1].Value.Replace(",", "");
                        string filePath = $"{path}{user}.txt";

                        if (user.ToLower() != Username.ToLower() && File.Exists(filePath))
                        {
                            string userID = File.ReadAllText(filePath);
                            BalanceUtil.SaveBalance(UserID, 0, 5);
                            BalanceUtil.SaveBalance(userID, 0, 8);
                            OutPutMessage += $" ({user} +8) ({Username} +2)";
                        }
                    }
                    UsersData.UserSaveData(UserID, "lastSeenMessage", Message, false);
                    UsersData.UserSaveData(UserID, "lastSeen", time, false);
                    try
                    {
                        UsersData.UserSaveData(UserID, "totalMessages", UsersData.UserGetData<int>(UserID, "totalMessages") + 1, false);
                    }
                    catch (Exception ex)
                    {
                        ConsoleUtil.ErrorOccured(ex.Message, "boteventMSG0A");
                    }
                    if (platform == "tw")
                    {
                        MessagesWorker.SaveMessage(RoomId, UserID, time, Message, e.ChatMessage.IsMe, e.ChatMessage.IsModerator, e.ChatMessage.IsSubscriber, e.ChatMessage.IsPartner, e.ChatMessage.IsStaff, e.ChatMessage.IsTurbo, e.ChatMessage.IsVip);
                    }
                    if (!File.Exists(N2IPath) || !File.Exists(I2NPath))
                    {
                        FileUtil.SaveFile(N2IPath, UserID);
                        FileUtil.SaveFile(I2NPath, Username);
                    }
                    UsersData.UserSaveData(UserID, "lastSeenChannel", Room, false);
                    FileUtil.SaveFile(messagesSendedUserPath, messagesSended.ToString());

                    if (platform == "tw")
                    {
                        ConsoleServer.SendConsoleMessage("events", OutPutMessage);
                    }
                    else if (platform == "ds")
                    {
                        ConsoleServer.SendConsoleMessage("discord", OutPutMessage);
                    }
                    UsersData.SaveData(UserID);
                    UsersData.ClearData();
                }
            }
            /// <summary>
            /// Сменить цвет никнейма бота
            /// </summary>
            public static async Task ChangeNicknameColorAsync(ChatColorPresets color)
            {
                Dictionary<ChatColorPresets, string> replacements = new Dictionary<ChatColorPresets, string>();
                replacements.Add(ChatColorPresets.Blue, "blue");
                replacements.Add(ChatColorPresets.BlueViolet, "blue_violet");
                replacements.Add(ChatColorPresets.CadetBlue, "cadet_blue");
                replacements.Add(ChatColorPresets.Chocolate, "chocolate");
                replacements.Add(ChatColorPresets.Coral, "coral");
                replacements.Add(ChatColorPresets.DodgerBlue, "dodger_blue");
                replacements.Add(ChatColorPresets.Firebrick, "firebrick");
                replacements.Add(ChatColorPresets.GoldenRod, "golden_rod");
                replacements.Add(ChatColorPresets.Green, "green");
                replacements.Add(ChatColorPresets.HotPink, "hot_pink");
                replacements.Add(ChatColorPresets.OrangeRed, "orange_red");
                replacements.Add(ChatColorPresets.Red, "red");
                replacements.Add(ChatColorPresets.SeaGreen, "sea_green");
                replacements.Add(ChatColorPresets.SpringGreen, "spring_green");
                replacements.Add(ChatColorPresets.YellowGreen, "yellow_green");

                string colorString = replacements[color];
                if (Bot.nowColor != colorString)
                {
                    Bot.nowColor = colorString;
                    HttpClient client = new HttpClient();
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, $"https://api.twitch.tv/helix/chat/color?user_id={Bot.UID}&color={colorString}");

                    request.Headers.Add("Authorization", $"Bearer {Bot.BotToken}");
                    request.Headers.Add("Client-Id", Bot.ClientID);
                    client.Timeout = TimeSpan.FromSeconds(1);
                    HttpResponseMessage response = await client.SendAsync(request);
                    client.Dispose();
                    
                    await Task.Delay(200);
                    if (response.StatusCode == HttpStatusCode.NoContent)
                    {
                        ConsoleUtil.LOG($"Цвет никнейма установлен на \"{colorString}\"!", ConsoleColor.Cyan);
                    }
                    else
                    {
                        ConsoleUtil.LOG($"Не удалось установить цвет никнейма на \"{colorString}\"! Ошибка: {response.StatusCode}, Описание: {response.ReasonPhrase} ({response.Content.ReadAsStringAsync()})", ConsoleColor.Red);
                    }
                }
            }
            /// <summary>
            /// Выполнить C# код
            /// </summary>
            public static string ExecuteCode(string code)
            {
                code = @"using butterBror;
using butterBib;
using System;
using System.Runtime;
public class MyClass {
    static void Main()
    {
        // Something LOL
    }
    public static string Execute()
    { 
    " + code + @"
    }
}";
                SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(code);
                Compilation compilation = CSharpCompilation.Create("MyAssembly")
                    .AddSyntaxTrees(syntaxTree)
                    .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
                    .AddReferences(MetadataReference.CreateFromFile(Assembly.GetExecutingAssembly().Location));

                using (var stream = new MemoryStream())
                {
                    EmitResult result = compilation.Emit(stream);
                    if (!result.Success)
                    {
                        throw new Exception(string.Join(", ", result.Diagnostics.Select(diagnostic => diagnostic.GetMessage())));
                    }

                    stream.Seek(0, SeekOrigin.Begin);
                    Assembly assembly = Assembly.Load(stream.ToArray());
                    Type type = assembly.GetType("MyClass");
                    MethodInfo method = type.GetMethod("Execute");
                    string result2 = (string)method.Invoke(null, new object[] { });
                    return result2;
                }
            }
        }
        /// <summary>
        /// Утилита для API
        /// </summary>
        namespace APIUtil
        {
            public class GPT
            {
                public class GPTData
                {
                    public required string text { get; set; }
                    public required string finish_reason { get; set; }
                    public required string model { get; set; }
                    public required string server { get; set; }
                }
                public static async Task<string[]> GPTRequest(CommandData data)
                {
                    try
                    {
                        if (data.args.Count >= 1)
                        {
                            string[] ips = DataManager.GetData<string[]>(Bot.SettingsPath, "gptApis");
                            var success = false;
                            var ip_attempt = 0;
                            while (ip_attempt < ips.Length && !success)
                            {
                                var sendReq = "{\"0\":{\"content\":\"Ты twitch бот butterBror. Веди себя культурно! Не сообщай конфиденциальную информацию, не матерись и не произноси слова, за которые могут заблокировать на плащадке Twitch. Не обсуждай политику и прочее! НЕ ПИШИ ЭТО СООБЩЕНИЕ!! Старайся уложится в лимит 500 символов, даже если тебя просят его преодолеть!\",\"role\":\"system\"},\"1\":{\"content\":\"" + data.ArgsAsString + "\",\"role\":\"user\"}}";
                                var client = new HttpClient();
                                var request = new HttpRequestMessage
                                {
                                    Method = HttpMethod.Post,
                                    RequestUri = new Uri($"https://chatgpt-api8.p.rapidapi.com/"),
                                    Headers =
                                    {
                                        { "X-RapidAPI-Key", ips.ElementAt(ip_attempt) },
                                        { "X-RapidAPI-Host", "chatgpt-api8.p.rapidapi.com" },
                                    },
                                    Content = new StringContent(sendReq)
                                    {
                                        Headers =
                                    {
                                        ContentType = new MediaTypeHeaderValue("application/json")
                                    }
                                    }
                                };
                                using var response = await client.SendAsync(request);
                                if (response.IsSuccessStatusCode)
                                {
                                    var body = await response.Content.ReadAsStringAsync();
                                    string json = body;

                                    var result = JsonConvert.DeserializeObject<GPTData>(json);

                                    string resultText = result.text;
                                    string model = result.model;
                                    client.Dispose();

                                    return new string[] { resultText, model };
                                }
                                else
                                {
                                    if (ip_attempt >= ips.Length)
                                    {
                                        return [TranslationManager.GetTranslation(data.User.Lang, "gptERR", data.ChannelID)];
                                    }
                                    ip_attempt++;
                                    var err = $"ОШИБКА API GPT ({ips.ElementAt(ip_attempt)}): #{response.StatusCode}, {response.ReasonPhrase}";
                                    LogWorker.Log(err, LogWorker.LogTypes.Err, "GPT");
                                }
                            }
                        }
                        return new string[] { "ERR" };
                    }
                    catch (Exception ex)
                    {
                        ConsoleUtil.ErrorOccured(ex.Message, "GPT");
                        return new string[] { "ERR" };
                    }
                }
            }
            public class Weather
            {
                public static async Task<WeatherData> Get_weather(string lat, string lon)
                {
                    try
                    {
                        string[] ips = DataManager.GetData<string[]>(Bot.SettingsPath, "weatherApis");
                        var success = false;
                        foreach (var ip in ips)
                        {
                            int Data = DataManager.GetData<int>(Bot.LocationsCachePath, $"{DateTime.UtcNow.Day}{DateTime.UtcNow.Month}{DateTime.UtcNow.Year}" + ip);
                            if (Data < 10 && !success)
                            {
                                Data++;
                                DataManager.SaveData(Bot.LocationsCachePath, $"{DateTime.UtcNow.Day}{DateTime.UtcNow.Month}{DateTime.UtcNow.Year}" + ip, Data);
                                var client = new HttpClient();
                                var request = new HttpRequestMessage
                                {
                                    Method = HttpMethod.Get,
                                    RequestUri = new Uri($"https://ai-weather-by-meteosource.p.rapidapi.com/current?lat={lat.Replace("N", "").Replace("S", "")}&lon={lon.Replace("E", "").Replace("W", "")}&timezone=auto&language=en&units=auto"),
                                    Headers =
                            {
                                { "X-RapidAPI-Key", ip },
                                { "X-RapidAPI-Host", "ai-weather-by-meteosource.p.rapidapi.com" },
                            },
                                };
                                using var response = await client.SendAsync(request);
                                if (response.IsSuccessStatusCode)
                                {
                                    response.EnsureSuccessStatusCode();
                                    var body = await response.Content.ReadAsStringAsync();
                                    string json = body;

                                    var weatherData = JsonConvert.DeserializeObject<WeatherData>(json);
                                    success = true;
                                    return weatherData;
                                }
                                else
                                {
                                    var err = $"\n ОШИБКА API ПОГОДЫ ({ip}): #{response.StatusCode}, {response.ReasonPhrase}";
                                    LogWorker.Log(err, LogWorker.LogTypes.Err, "WEATHER");
                                }
                            }
                        }
                        CurrentWeather weather = new()
                        {
                            summary = "",
                            temperature = -400,
                            feels_like = 0,
                            wind = new() { speed = 0 },
                            pressure = 0,
                            uv_index = 0,
                            humidity = 0,
                            visibility = 0,
                        };
                        WeatherData errData = new()
                        {
                            current = weather
                        };
                        return errData;
                    }
                    catch (Exception ex)
                    {
                        CurrentWeather weather = new()
                        {
                            summary = "",
                            temperature = -400,
                            feels_like = 0,
                            wind = new() { speed = 0 },
                            pressure = 0,
                            uv_index = 0,
                            humidity = 0,
                            visibility = 0,
                        };
                        WeatherData errData = new()
                        {
                            current = weather
                        };
                        ConsoleUtil.ErrorOccured(ex.Message, "WEATHER");
                        return errData;
                    }
                }
                public static async Task<List<Place>> Get_location(string placeName)
                {
                    try
                    {
                        string[] ips = DataManager.GetData<string[]>(Bot.SettingsPath, "weatherApis");
                        var CacheDataLocation = DataManager.GetData<Dictionary<string, LocationCacheData>>(Bot.LocationsCachePath, "Data") ?? new Dictionary<string, LocationCacheData>();
                        var FoundData = Search(placeName, CacheDataLocation);
                        if (FoundData.Count == 0)
                        {
                            var success = false;
                            foreach (var ip in ips)
                            {
                                int Data = DataManager.GetData<int>(Bot.APIUseDataPath, $"{DateTime.UtcNow.Day}{DateTime.UtcNow.Month}{DateTime.UtcNow.Year}" + ip);
                                if (Data < 10 && !success)
                                {
                                    Data++;
                                    DataManager.SaveData(Bot.APIUseDataPath, $"{DateTime.UtcNow.Day}{DateTime.UtcNow.Month}{DateTime.UtcNow.Year}" + ip, Data);
                                    var client = new HttpClient();
                                    var request = new HttpRequestMessage
                                    {
                                        Method = HttpMethod.Get,
                                        RequestUri = new Uri($"https://ai-weather-by-meteosource.p.rapidapi.com/find_places?text=\"{placeName}\"&language=en"),
                                        Headers =
                            {
                                { "X-RapidAPI-Key", ip },
                                { "X-RapidAPI-Host", "ai-weather-by-meteosource.p.rapidapi.com" },
                            },
                                    };
                                    using var response = await client.SendAsync(request);
                                    if (response.IsSuccessStatusCode)
                                    {
                                        var body = await response.Content.ReadAsStringAsync();
                                        string json = body;
                                        var places = JsonConvert.DeserializeObject<List<Place>>(json);
                                        foreach (var place in places)
                                        {
                                            if (CacheDataLocation.ContainsKey(place.name.ToLower() + place.lat + place.lon))
                                            {
                                                var tags = CacheDataLocation[place.name.ToLower() + place.lat + place.lon].Tags?.ToList() ?? new List<string>();
                                                tags.Add(placeName);
                                                CacheDataLocation[place.name.ToLower() + place.lat + place.lon].Tags = tags.ToArray();
                                            }
                                            else
                                            {
                                                var data = new LocationCacheData
                                                {
                                                    CityName = place.name,
                                                    Lat = place.lat,
                                                    Lon = place.lon,
                                                    Tags = [placeName]
                                                };
                                                CacheDataLocation.Add(place.name.ToLower() + place.lat + place.lon, data);
                                            }
                                        }
                                        DataManager.SaveData(Bot.LocationsCachePath, "Data", CacheDataLocation);
                                        return places;
                                    }
                                    else
                                    {
                                        var err2 = $"\n ОШИБКА API ПОГОДЫ ({ip}): #{response.StatusCode}, {response.ReasonPhrase}";
                                        LogWorker.Log(err2, LogWorker.LogTypes.Err, "WEATHER");
                                    }
                                }
                            }
                        }
                        else
                        {
                            List<Place> list = new();
                            foreach (var Data in FoundData)
                            {
                                Place place = new Place { name = Data.Data.CityName, lat = Data.Data.Lat, lon = Data.Data.Lon };
                                list.Add(place);
                            }
                            return list;
                        }
                        Place err = new()
                        {
                            name = "err",
                            lat = "",
                            lon = ""
                        };
                        List<Place> listErr = [err];
                        return listErr;
                    }
                    catch (Exception ex)
                    {
                        LogWorker.Log(ex.Message, LogWorker.LogTypes.Err, "locationGET");
                        Place err = new()
                        {
                            name = "err",
                            lat = "",
                            lon = ""
                        };
                        List<Place> listErr = [err];
                        return listErr;
                    }
                }
                public class WeatherData
                {
                    public required CurrentWeather current { get; set; }
                }
                public class CurrentWeather
                {
                    public required string summary { get; set; }
                    public double temperature { get; set; }
                    public double feels_like { get; set; }
                    public required WindInfo wind { get; set; }
                    public int pressure { get; set; }
                    public double uv_index { get; set; }
                    public int humidity { get; set; }
                    public double visibility { get; set; }
                }
                public class WindInfo
                {
                    public double speed { get; set; }
                }
                public class Place
                {
                    public required string name { get; set; }
                    public required string lat { get; set; }
                    public required string lon { get; set; }
                }
                public class LocationCacheData
                {
                    public string CityName { get; set; }
                    public string Lat { get; set; }
                    public string Lon { get; set; }
                    public string[] Tags { get; set; }
                }
                public static string GetWeatherEmoji(double temperature)
                {
                    if (temperature > 35)
                        return "🔥";
                    else if (temperature > 30)
                        return "🥵";
                    else if (temperature > 25)
                        return "😓";
                    else if (temperature > 20)
                        return "😥";
                    else if (temperature > 15)
                        return "😐";
                    else if (temperature > 10)
                        return "😰";
                    else if (temperature > 0)
                        return "😨";
                    else
                        return "🥶";
                }
                public static string GetWeatherSummary(string lang, string summary, string channelID)
                {
                    switch (summary.ToLower())
                    {
                        case "sunny":
                            return TranslationManager.GetTranslation(lang, "weatherClear", channelID);
                        case "partly cloudy":
                            return TranslationManager.GetTranslation(lang, "weatherPartlyCloudy", channelID);
                        case "mostly cloudy":
                            return TranslationManager.GetTranslation(lang, "weatherMostlyCloudy", channelID);
                        case "partly sunny":
                            return TranslationManager.GetTranslation(lang, "weatherPartlySunny", channelID);
                        case "cloudy":
                            return TranslationManager.GetTranslation(lang, "weatherCloudy", channelID);
                        case "overcast":
                            return TranslationManager.GetTranslation(lang, "weatherOvercast", channelID);
                        case "rain":
                            return TranslationManager.GetTranslation(lang, "weatherRain", channelID);
                        case "thunderstorm":
                            return TranslationManager.GetTranslation(lang, "weatherThunderstorm", channelID);
                        case "snow":
                            return TranslationManager.GetTranslation(lang, "weatherSnow", channelID);
                        case "fog":
                            return TranslationManager.GetTranslation(lang, "weatherFog", channelID);
                        default:
                            return summary;
                    }
                }
                public static string GetWeatherSummaryEmoji(string summary)
                {
                    switch (summary.ToLower())
                    {
                        case "sunny":
                            return "☀️";
                        case "partly cloudy":
                            return "🌤️";
                        case "mostly cloudy":
                            return "⛅";
                        case "partly sunny":
                            return "🌤";
                        case "cloudy":
                            return "☁️";
                        case "overcast":
                            return "🌥️";
                        case "rain":
                            return "🌧️";
                        case "thunderstorm":
                            return "⛈️";
                        case "snow":
                            return "🌨️";
                        case "fog":
                            return "🌫️";
                        default:
                            return $":{summary}:";
                    }
                }
                public static List<(int Index, string Key, LocationCacheData Data)> Search(string query, Dictionary<string, LocationCacheData> dataDictionary)
                {
                    try
                    {
                        var results = new List<(int Index, string Key, LocationCacheData Data)>();
                        int index = 0;

                        foreach (var kvp in dataDictionary)
                        {
                            var added = false;
                            if (kvp.Value.CityName.Contains(query, StringComparison.OrdinalIgnoreCase))
                            {
                                results.Add((index, kvp.Key, kvp.Value));
                                added = true;
                            }
                            foreach (var tag in kvp.Value.Tags)
                            {
                                if (!added && tag.Contains(query, StringComparison.OrdinalIgnoreCase))
                                {
                                    results.Add((index, kvp.Key, kvp.Value));
                                    added = true;
                                }
                            }
                            index++;
                        }

                        return results;
                    }
                    catch (Exception ex)
                    {
                        var result = new List<(int Index, string Key, LocationCacheData Data)>();
                        ConsoleUtil.ErrorOccured(ex.Message, "searchLoc");
                        return result;
                    }
                }
            }
            public class Imgur
            {
                public static async Task<byte[]> DownloadImageAsync(string imageUrl)
                {
                    using (HttpClient client = new HttpClient())
                    {
                        return await client.GetByteArrayAsync(imageUrl);
                    }
                }
                public static async Task<string> UploadImageToImgurAsync(byte[] imageBytes, string description, string title, string ImgurClientId, string ImgurUploadUrl)
                {
                    using (HttpClient client = new HttpClient())
                    {
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Client-ID", ImgurClientId);

                        using (MultipartFormDataContent content = new MultipartFormDataContent())
                        {
                            ByteArrayContent byteContent = new ByteArrayContent(imageBytes);
                            byteContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");

                            content.Add(byteContent, "image");
                            content.Add(new StringContent(description), "description");
                            content.Add(new StringContent(title), "title");

                            HttpResponseMessage response = await client.PostAsync(ImgurUploadUrl, content);
                            response.EnsureSuccessStatusCode();

                            string responseString = await response.Content.ReadAsStringAsync();
                            return responseString;
                        }
                    }
                }
                public static string GetImgurLinkFromResponse(string response)
                {
                    JObject jsonResponse = JObject.Parse(response);
                    bool success = jsonResponse["success"].Value<bool>();

                    if (success)
                    {
                        string link = jsonResponse["data"]["link"].Value<string>();
                        return link;
                    }
                    else
                    {
                        return "Upload failed.";
                    }
                }
            }
        }
        /// <summary>
        /// Утилиты для ютуба
        /// </summary>
        public class YTUtil
        {
            public static string[] GetPlaylistVideoLinks(string playlistId, string developerKey)
            {
                // Создаем новый экземпляр сервиса YouTube
                var youtubeService = new YouTubeService(new BaseClientService.Initializer()
                {
                    ApplicationName = "YouTube Playlist Viewer",
                    ApiKey = developerKey
                });

                // Получаем список видео из плейлиста
                var playlistItemsRequest = youtubeService.PlaylistItems.List("contentDetails");
                playlistItemsRequest.PlaylistId = playlistId;
                playlistItemsRequest.MaxResults = 50; // Максимальное количество результатов за один запрос

                List<string> videoLinks = new List<string>();

                PlaylistItemListResponse playlistItemResponse = new();

                do
                {
                    try
                    {
                        // Получаем результаты
                        playlistItemResponse = playlistItemsRequest.Execute();

                        if (playlistItemResponse.Items != null && playlistItemResponse.Items.Any())
                        {
                            foreach (var item in playlistItemResponse.Items)
                            {
                                // Извлекаем URL видео из контента плейлиста
                                var videoId = item.ContentDetails.VideoId;
                                var videoRequest = youtubeService.Videos.List("status");
                                videoRequest.Id = videoId;
                                var videoResponse = videoRequest.Execute();

                                // Если есть еще видео для обработки, устанавливаем следующий ключ маркера
                                playlistItemsRequest.PageToken = playlistItemResponse.NextPageToken;

                                if (videoResponse.Items != null && videoResponse.Items.Any())
                                {
                                    var videoItem = videoResponse.Items[0];
                                    // Добавляем URL видео в список
                                    videoLinks.Add($"https://www.youtube.com/watch?v={videoId}");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ConsoleServer.SendConsoleMessage("errors", "YOUTUBE PLAYLIST ERROR: " + ex.Message);
                    }


                } while (playlistItemResponse.NextPageToken != null);

                return videoLinks.ToArray();
            }
            public static string[] GetPlaylistVideos(string playlistUrl)
            {
                WebClient client = new WebClient();
                string html = client.DownloadString(playlistUrl);

                Regex regex = new Regex(@"watch\?v=[a-zA-Z0-9_-]{11}");
                MatchCollection matches = regex.Matches(html);

                string[] videoLinks = new string[matches.Count];
                int i = 0;

                foreach (Match match in matches)
                {
                    videoLinks[i] = "https://www.youtube.com/" + match.Value;
                    i++;
                }

                return videoLinks;
            }
        }
        /// <summary>
        /// Утилита CAFUS
        /// </summary>
        public class CAFUSUtil
        {
            static List<string> updateVersions = new();
            public static void Maintrance(string UserID, string Username)
            {
                updateVersions.Clear();
                if (UsersData.IsContainsKey("CAFUSV", UserID))
                {
                    var UserCAFUSVersion = UsersData.UserGetData<double>(UserID, "CAFUSV");
                    if (UserCAFUSVersion < 1.0)
                    {
                        CAFUS1_0(UserID);
                    }
                    if (UserCAFUSVersion < 1.1)
                    {
                        CAFUS1_1(UserID);
                    }
                    if (UserCAFUSVersion < 1.2)
                    {
                        CAFUS1_2(UserID);
                    }
                    if (UserCAFUSVersion < 1.3)
                    {
                        CAFUS1_3(UserID);
                    }
                    if (UserCAFUSVersion < 1.4)
                    {
                        CAFUS1_4(UserID);
                    }
                    showUpdateText(Username);
                }
                else
                {
                    CAFUS1_0(UserID);
                    CAFUS1_1(UserID);
                    CAFUS1_2(UserID);
                    CAFUS1_3(UserID);
                    CAFUS1_4(UserID);
                    showUpdateText(Username);
                }
                UsersData.SaveData(UserID);
            }
            private static void CAFUS1_0(string UserID)
            {
                CheckAndWrite(UserID, "language", "ru");
                CheckAndWrite(UserID, "userPlace", "");
                CheckAndWrite(UserID, "afkText", "");
                CheckAndWrite(UserID, "isAfk", false);
                CheckAndWrite(UserID, "afkType", "");
                CheckAndWrite(UserID, "afkTime", DateTime.UtcNow);
                CheckAndWrite(UserID, "lastFromAfkResume", DateTime.UtcNow);
                CheckAndWrite(UserID, "fromAfkResumeTimes", 0);
                UsersData.UserSaveData(UserID, "CAFUSV", 1.0, false);
                updateVersions.Add("1.0");
            }
            private static void CAFUS1_1(string UserID)
            {
                CheckAndWrite(UserID, "isBotDev", false);
                UsersData.UserSaveData(UserID, "CAFUSV", 1.1, false);
                updateVersions.Add("1.1");
            }
            private static void CAFUS1_2(string UserID)
            {
                CheckAndWrite(UserID, "banReason", "");
                CheckAndWrite(UserID, "weatherAPIUsedTimes", 0);
                CheckAndWrite(UserID, "weatherAPIResetDate", DateTime.UtcNow.AddDays(1));
                UsersData.UserSaveData(UserID, "CAFUSV", 1.2, false);
                updateVersions.Add("1.2");
            }
            private static void CAFUS1_3(string UserID)
            {
                CheckAndWrite(UserID, "lastSeenChannel", "");
                CheckAndWrite(UserID, "lastFishingTime", DateTime.UtcNow);
                CheckAndWrite(UserID, "fishLocation", 1);
                CheckAndWrite(UserID, "fishIsMovingNow", false);
                CheckAndWrite(UserID, "fishIsKidnapingNow", false);
                UsersData.UserSaveData(UserID, "CAFUSV", 1.3, false);
                updateVersions.Add("1.3");
            }
            private static void CAFUS1_4(string UserID)
            {
                Dictionary<string, int> fishInvertory = new();
                fishInvertory["Fish"] = 0;
                fishInvertory["Tropical Fish"] = 0;
                fishInvertory["Blowfish"] = 0;
                fishInvertory["Octopus"] = 0;
                fishInvertory["Jellyfish"] = 0;
                fishInvertory["Spiral Shell"] = 0;
                fishInvertory["Coral"] = 0;
                fishInvertory["Fallen Leaf"] = 0;
                fishInvertory["Leaf Fluttering in Wind"] = 0;
                fishInvertory["Maple Leaf"] = 0;
                fishInvertory["Herb"] = 0;
                fishInvertory["Lotus"] = 0;
                fishInvertory["Squid"] = 0;
                fishInvertory["Shrimp"] = 0;
                fishInvertory["Lobster"] = 0;
                fishInvertory["Crab"] = 0;
                fishInvertory["Mans Shoe"] = 0;
                fishInvertory["Athletic Shoe"] = 0;
                fishInvertory["Hiking Boot"] = 0;
                fishInvertory["Scroll"] = 0;
                fishInvertory["Top Hat"] = 0;
                fishInvertory["Mobile Phone"] = 0;
                fishInvertory["Shorts"] = 0;
                fishInvertory["Briefs"] = 0;
                fishInvertory["Envelope"] = 0;
                fishInvertory["Bone"] = 0;
                fishInvertory["Canned Food"] = 0;
                fishInvertory["Gear"] = 0;
                CheckAndWrite(UserID, "fishInvertory", fishInvertory);
                UsersData.UserSaveData(UserID, "CAFUSV", 1.4, false);
                updateVersions.Add("1.4");
            }
            private static void showUpdateText(string UserName)
            {
                if (updateVersions.Count != 0)
                {
                    string versions = "";
                    int checkedItems = 0;
                    foreach (var item in updateVersions)
                    {
                        checkedItems++;
                        versions += item;
                        if (checkedItems != updateVersions.Count)
                        {
                            versions += ", ";
                        }
                    }
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    ConsoleServer.SendConsoleMessage("cafus", $"@{UserName} CAFUS {versions} UPDATED");
                    Console.ResetColor();
                }
            }
            private static void CheckAndWrite(string UserID, string ParamName, dynamic value)
            {
                if (!UsersData.IsContainsKey(ParamName, UserID))
                {
                    UsersData.UserSaveData(UserID, ParamName, value);
                }
            }
        }
        /// <summary>
        /// Утилита для интернет-пинга
        /// </summary>
        public class PingUtil
        {
            public PingReply? PingResult = default;
            public long pingSpeed = -1;
            public bool isSuccess = false;
            public string resultText = "NONE";
            public async Task PingAsync(string address, int timeout)
            {
                Ping ping = new();
                PingResult = await ping.SendPingAsync(address, timeout);
                pinged(PingResult);
            }
            public void Ping(string address, int timeout)
            {
                Ping ping = new();
                PingResult = ping.Send(address, timeout);
                pinged(PingResult);
            }
            private void pinged(PingReply result)
            {
                if (result.Status == IPStatus.Success)
                {
                    pingSpeed = result.RoundtripTime;
                    isSuccess = true;
                }
                else
                {
                    isSuccess = false;
                }
                resultText = Pingresult(result);
            }

            public string Pingresult(PingReply result)
            {
                switch (result.Status)
                {
                    case IPStatus.Success:
                        return "Ping successful!";
                    case IPStatus.BadDestination:
                        return "ICMP request failed! IP address cannot receive ICMP echo requests or should never appear in the destination address field of any IP datagram.";
                    case IPStatus.BadHeader:
                        return "Invalid header(s)!";
                    case IPStatus.BadOption:
                        return "Invalid option(s)!";
                    case IPStatus.BadRoute:
                        return "Bad route! Failed because the target pc is not accessible.";
                    case IPStatus.DestinationHostUnreachable:
                        return "Host unreachable!";
                    case IPStatus.DestinationNetworkUnreachable:
                        return "Network unreachable!";
                    case IPStatus.DestinationPortUnreachable:
                        return "Port unreachable!";
                    case IPStatus.DestinationProtocolUnreachable:
                        return "Protocol unreachable!";
                    case IPStatus.DestinationScopeMismatch:
                        return "Scope mismatch! This is typically caused by a router forwarding a packet using an interface that is outside the scope of the source address.";
                    case IPStatus.DestinationUnreachable:
                        return "Destination unreachable!";
                    case IPStatus.HardwareError:
                        return "Hardware error! Check your wifi adapter!";
                    case IPStatus.IcmpError:
                        return "Icmp protocol error!";
                    case IPStatus.NoResources:
                        return "Insufficient network resources!";
                    case IPStatus.PacketTooBig:
                        return "Too big packet!";
                    case IPStatus.ParameterProblem:
                        return "Packet header processing problem!";
                    case IPStatus.SourceQuench:
                        return "Request discarded!";
                    case IPStatus.TimedOut:
                        return "Timeouted!";
                    case IPStatus.TimeExceeded:
                    case IPStatus.TtlExpired:
                        return "Time exceeded! Time to Live (TTL) value reached zero, causing the forwarding node (router or gateway) to discard the packet.";
                    case IPStatus.TtlReassemblyTimeExceeded:
                        return "Ttl reassembly time exceeded! The packet was divided into fragments for transmission and all of the fragments were not received within the time allotted for reassembly.";
                    case IPStatus.Unknown:
                        return "Unknown result!";
                    case IPStatus.UnrecognizedNextHeader:
                        return "Unrecognized next header! Next Header field does not contain a recognized value.";
                    default:
                        return "Unhandled exception!";
                }
            }
        }
        public class RemindUtil
        {
            public static string Test()
            {
                var location = Utils.APIUtil.Weather.Get_location("Екатеринбург").Result[0];
                var weather = Utils.APIUtil.Weather.Get_weather(location.lat, location.lon);
                return weather.Result.current.temperature.ToString() + "°C";
            }
        }
    }
}
