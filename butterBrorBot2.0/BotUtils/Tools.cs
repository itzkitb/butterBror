using butterBror.Utils.DataManagers;
using DankDB;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Telegram.Bot;
using Telegram.Bot.Types;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using System.Windows;
using System.Management;
using System.Collections.Concurrent;
using System.Threading;
using System.Text.Json;

namespace butterBror
{
    namespace Utils
    {
        /// <summary>
        /// Getting a Twitch Authorization Token
        /// </summary>
        public class TwitchToken
        {
            private static string _clientId;
            private static string _clientSecret;
            private static string _redirectUri;
            private static string _databasePath;
            /// <summary>
            /// Getting a Twitch Authorization Token
            /// </summary>
            public TwitchToken(string clientId, string clientSecret, string databasePath)
            {
                _clientId = clientId;
                _clientSecret = clientSecret;
                _redirectUri = "http://localhost:12121/";
                _databasePath = databasePath;
            }
            /// <summary>
            /// Getting a Twitch Authorization Token
            /// </summary>
            public static async Task<TokenData> GetTokenAsync()
            {
                Engine.Statistics.functions_used.Add();
                try
                {
                    var token = LoadTokenData();
                    if (token != null && token.ExpiresAt > DateTime.Now)
                        return token;
                    if (token == null || string.IsNullOrEmpty(token.RefreshToken))
                        return await PerformAuthorizationFlow();

                    return await RefreshAccessToken(token);
                }
                catch (Exception ex)
                {
                    Console.WriteError(ex, "TwitchTokenUtil\\GetTokenAsync");
                    return null;
                }
            }
            /// <summary>
            /// Authorization execution flow
            /// </summary>
            private static async Task<TokenData> PerformAuthorizationFlow()
            {
                Engine.Statistics.functions_used.Add();
                try
                {
                    using var listener = new HttpListener();
                    listener.Prefixes.Add(_redirectUri);
                    listener.Start();

                    var authorizationCode = await GetAuthorizationCodeAsync(listener);
                    var token = await ExchangeCodeForTokenAsync(authorizationCode);
                    SaveTokenData(token);

                    var context = await listener.GetContextAsync();
                    var response = context.Response;
                    string responseString = @"
<html>
    <head>
        <meta charset='UTF-8'>
        <title>Authorization completed</title>
    </head>
    <body>
        <h2> Ready <img src='https://static-cdn.jtvnw.net/emoticons/v2/28/default/dark/1.0' style='vertical-align: middle;'/> 👍</h2>
		<span> You can close this page</span>
    </body>
</html>";

                    byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                    response.ContentLength64 = buffer.Length;
                    response.ContentType = "text/html; charset=UTF-8";
                    using (var output = response.OutputStream)
                    {
                        await output.WriteAsync(buffer);
                    }

                    return token;
                }
                catch (Exception ex)
                {
                    Console.WriteError(ex, "TwitchTokenUtil\\PerformAuthorizationFlow");
                    return null;
                }
            }

            /// <summary>
            /// Refreshing the authorization token
            /// </summary>
            public static async Task<TokenData> RefreshAccessToken(TokenData token)
            {
                Engine.Statistics.functions_used.Add();
                try
                {
                    Console.WriteLine("[TW-AUTH-UTIL] Refreshing token...", "info");
                    if (token == null || string.IsNullOrEmpty(token.RefreshToken))
                        return await PerformAuthorizationFlow();

                    using var httpClient = new HttpClient();
                    var request = new HttpRequestMessage(HttpMethod.Post, "https://id.twitch.tv/oauth2/token")
                    {
                        Content = new StringContent($"grant_type=refresh_token&refresh_token={token.RefreshToken}&client_id={_clientId}&client_secret={_clientSecret}", Encoding.UTF8, "application/x-www-form-urlencoded")
                    };

                    var response = await httpClient.SendAsync(request);
                    var responseContent = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(responseContent);
                        token.AccessToken = tokenResponse.access_token;
                        token.ExpiresAt = DateTime.Now.AddSeconds(tokenResponse.expires_in);
                        SaveTokenData(token);
                        Console.WriteLine("[TW-AUTH-UTIL] Token refreshed!", "info");
                        return token;
                    }
                    else
                    {
                        Console.WriteLine($"[TW-AUTH-UTIL] Error updating token: {responseContent}", "err", ConsoleColor.Black, ConsoleColor.Red);
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteError(ex, "TwitchTokenUtil\\RefreshAccessToken");
                    return null;
                }
            }
            /// <summary>
            /// Opening the authorization link in the browser
            /// </summary>
            private static async Task<string> GetAuthorizationCodeAsync(HttpListener listener)
            {
                Engine.Statistics.functions_used.Add();
                try
                {
                    Console.WriteLine("[TW-AUTH-UTIL] Getting auth data...", "info");
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
                    Console.WriteLine("[TW-AUTH-UTIL] Auth data getted", "info");

                    return code;
                }
                catch (Exception ex)
                {
                    Console.WriteError(ex, "TwitchTokenUtil\\GetAuthorizationCodeAsync");
                    return null;
                }
            }
            /// <summary>
            /// Exchange code for token
            /// </summary>
            private static async Task<TokenData> ExchangeCodeForTokenAsync(string code)
            {
                Engine.Statistics.functions_used.Add();
                try
                {
                    Console.WriteLine("[TW-AUTH-UTIL] Getting exchange code...", "info");
                    using var httpClient = new HttpClient();
                    var request = new HttpRequestMessage(HttpMethod.Post, "https://id.twitch.tv/oauth2/token")
                    {
                        Content = new StringContent($"client_id={_clientId}&client_secret={_clientSecret}&redirect_uri={_redirectUri}&grant_type=authorization_code&code={code}", Encoding.UTF8, "application/x-www-form-urlencoded")
                    };

                    var response = await httpClient.SendAsync(request);
                    var responseContent = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(responseContent);
                        Console.WriteLine("[TW-AUTH-UTIL] Getted exchange code!", "info");
                        return new TokenData { AccessToken = tokenResponse.access_token, ExpiresAt = DateTime.Now.AddSeconds(tokenResponse.expires_in), RefreshToken = tokenResponse.refresh_token };
                    }
                    else
                    {
                        Console.WriteLine($"[TW-AUTH-UTIL] Error receiving exchange token: {responseContent}", "err");
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteError(ex, "TwitchTokenUtil\\ExchangeCodeForTokenAsync");
                    return null;
                }
            }
            /// <summary>
            /// Get code from response
            /// </summary>
            private static string GetCodeFromResponse(string query)
            {
                Engine.Statistics.functions_used.Add();
                var queryParams = HttpUtility.ParseQueryString(query);
                return queryParams["code"];
            }
            /// <summary>
            /// Load token from database
            /// </summary>
            private static TokenData LoadTokenData()
            {
                Engine.Statistics.functions_used.Add();
                try
                {
                    if (FileUtil.FileExists(_databasePath))
                    {
                        var json = FileUtil.GetFileContent(_databasePath);
                        if (string.IsNullOrEmpty(json)) return null;
                        var tokenData = JsonConvert.DeserializeObject<TokenData>(json);
                        Console.WriteLine("[TW-AUTH-UTIL] Token data loaded!", "info");
                        return tokenData?.ExpiresAt > DateTime.Now ? tokenData : null;
                    }
                    return null;
                }
                catch
                {
                    return null;
                }
            }
            /// <summary>
            /// Save token to database
            /// </summary>
            private static void SaveTokenData(TokenData tokenData)
            {
                Engine.Statistics.functions_used.Add();
                try
                {
                    FileUtil.SaveFileContent(_databasePath, JsonConvert.SerializeObject(tokenData));
                }
                catch (Exception ex)
                {
                    Console.WriteError(ex, "TwitchTokenUtil\\SaveTokenData");
                }
            }

            private class TokenResponse
            {
                public string access_token { get; set; }
                public int expires_in { get; set; }
                public string refresh_token { get; set; }
            }
            public class TokenData
            {
                public string AccessToken { get; set; }
                public DateTime ExpiresAt { get; set; }
                public string RefreshToken { get; set; }
            }
        }
        /// <summary>
        /// Utility for formats
        /// </summary>
        public class Format
        {
            /// <summary>
            /// Text to number
            /// </summary>
            public static int ToInt(string input)
            {
                Engine.Statistics.functions_used.Add();
                return Int32.Parse(Regex.Replace(input, @"[^-1234567890]", ""));
            }
            /// <summary>
            /// Text to long number
            /// </summary>
            public static long ToLong(string input)
            {
                Engine.Statistics.functions_used.Add();
                return long.Parse(Regex.Replace(input, @"[^-1234567890]", ""));
            }
            /// <summary>
            /// Get the amount of time until
            /// </summary>
            public static TimeSpan GetTimeTo(DateTime time, DateTime now, bool addYear = true)
            {
                Engine.Statistics.functions_used.Add();
                if (now > time && addYear)
                    time.AddYears(1);

                if (now > time)
                    return now - time;
                else
                    return time - now;
            }
        }
        /// <summary>
        /// Balance Utility
        /// </summary>
        public class Balance
        {
            /// <summary>
            /// Add/reduce user balance
            /// </summary>
            public static void Add(string userID, int buttersAdd, int crumbsAdd, Platforms platform)
            {
                Engine.Statistics.functions_used.Add();
                int crumbs = GetBalanceFloat(userID, platform) + crumbsAdd;
                int butters = GetBalance(userID, platform) + buttersAdd;

                Engine.coins += buttersAdd + crumbsAdd / 100f;
                while (crumbs > 100)
                {
                    crumbs -= 100;
                    butters += 1;
                }
                UsersData.Save(userID, "floatBalance", crumbs, platform);
                UsersData.Save(userID, "balance", butters, platform);
            }
            /// <summary>
            /// Getting user balance
            /// </summary>
            public static int GetBalance(string userID, Platforms platform)
            {
                Engine.Statistics.functions_used.Add();
                return UsersData.Get<int>(userID, "balance", platform);
            }
            /// <summary>
            /// Getting user balance float
            /// </summary>
            public static int GetBalanceFloat(string userID, Platforms platform)
            {
                Engine.Statistics.functions_used.Add();
                return UsersData.Get<int>(userID, "floatBalance", platform);
            }
        }
        /// <summary>
        /// Chat utility
        /// </summary>
        public class Chat
        {
            /// <summary>
            /// Return user from AFK
            /// </summary>
            public static void ReturnFromAFK(string UserID, string RoomID, string channel, string username, string message_id, Message message_reply, Platforms platform)
            {
                Engine.Statistics.functions_used.Add();
                var language = "ru";

                try
                {
                    if (UsersData.Get<string>(UserID, "language", platform) == default)
                        UsersData.Save(UserID, "language", "ru", platform);
                    else
                        language = UsersData.Get<string>(UserID, "language", platform);
                }
                catch (Exception ex)
                {
                    Console.WriteError(ex, $"(NOTCRITICAL)ChatUtil\\ReturnFromAFK#{UserID}");
                }

                string? message = UsersData.Get<string>(UserID, "afkText", platform);
                if (!NoBanwords.Check(message, RoomID, platform))
                    return;

                string send = (TextUtil.CleanAsciiWithoutSpaces(message) == "" ? "" : ": " + message);

                TimeSpan timeElapsed = DateTime.UtcNow - UsersData.Get<DateTime>(UserID, "afkTime", platform);
                var afkType = UsersData.Get<string>(UserID, "afkType", platform);
                string translateKey = "";

                if (afkType == "draw")
                {
                    if (timeElapsed.TotalHours >= 2 && timeElapsed.TotalHours < 8) translateKey = "draw:2h";
                    else if (timeElapsed.TotalHours >= 8 && timeElapsed.TotalHours < 24) translateKey = "draw:8h";
                    else if (timeElapsed.TotalDays >= 1 && timeElapsed.TotalDays < 7) translateKey = "draw:1d";
                    else if (timeElapsed.TotalDays >= 7 && timeElapsed.TotalDays < 31) translateKey = "draw:7d";
                    else if (timeElapsed.TotalDays >= 31 && timeElapsed.TotalDays < 364) translateKey = "draw:1mn";
                    else if (timeElapsed.TotalDays >= 364) translateKey = "draw:1y";
                    else translateKey = "draw:default";
                }
                else if (afkType == "afk")
                {
                    if (timeElapsed.TotalHours >= 8 && timeElapsed.TotalHours < 14) translateKey = "default:8h";
                    else if (timeElapsed.TotalHours >= 14 && timeElapsed.TotalDays < 1) translateKey = "default:14h";
                    else if (timeElapsed.TotalDays >= 1 && timeElapsed.TotalDays < 3) translateKey = "default:1d";
                    else if (timeElapsed.TotalDays >= 3 && timeElapsed.TotalDays < 7) translateKey = "default:3d";
                    else if (timeElapsed.TotalDays >= 7 && timeElapsed.TotalDays < 9) translateKey = "default:7d";
                    else if (timeElapsed.TotalDays >= 9 && timeElapsed.TotalDays < 31) translateKey = "default:9d";
                    else if (timeElapsed.TotalDays >= 31 && timeElapsed.TotalDays < 364) translateKey = "default:1mn";
                    else if (timeElapsed.TotalDays >= 364) translateKey = "default:1y";
                    else translateKey = "default";
                }
                else if (afkType == "sleep")
                {
                    if (timeElapsed.TotalHours >= 2 && timeElapsed.TotalHours < 5) translateKey = "sleep:2h";
                    else if (timeElapsed.TotalHours >= 5 && timeElapsed.TotalHours < 8) translateKey = "sleep:5h";
                    else if (timeElapsed.TotalHours >= 8 && timeElapsed.TotalHours < 12) translateKey = "sleep:8h";
                    else if (timeElapsed.TotalHours >= 12 && timeElapsed.TotalDays < 1) translateKey = "sleep:12h";
                    else if (timeElapsed.TotalDays >= 1 && timeElapsed.TotalDays < 3) translateKey = "sleep:1d";
                    else if (timeElapsed.TotalDays >= 3 && timeElapsed.TotalDays < 7) translateKey = "sleep:3d";
                    else if (timeElapsed.TotalDays >= 7 && timeElapsed.TotalDays < 31) translateKey = "sleep:7d";
                    else if (timeElapsed.TotalDays >= 31 && timeElapsed.TotalDays < 364) translateKey = "sleep:1mn";
                    else if (timeElapsed.TotalDays >= 364) translateKey = "sleep:1y";
                    else translateKey = "sleep:default";
                }
                else if (afkType == "rest")
                {
                    if (timeElapsed.TotalHours >= 8 && timeElapsed.TotalHours < 24) translateKey = "rest:8h";
                    else if (timeElapsed.TotalDays >= 1 && timeElapsed.TotalDays < 7) translateKey = "rest:1d";
                    else if (timeElapsed.TotalDays >= 7 && timeElapsed.TotalDays < 31) translateKey = "rest:7d";
                    else if (timeElapsed.TotalDays >= 31 && timeElapsed.TotalDays < 364) translateKey = "rest:1mn";
                    else if (timeElapsed.TotalDays >= 364) translateKey = "rest:1y";
                    else translateKey = "rest:default";
                }
                else if (afkType == "lurk") translateKey = "lurk:default";
                else if (afkType == "study")
                {
                    if (timeElapsed.TotalHours >= 2 && timeElapsed.TotalHours < 5) translateKey = "study:2h";
                    else if (timeElapsed.TotalHours >= 5 && timeElapsed.TotalHours < 8) translateKey = "study:5h";
                    else if (timeElapsed.TotalHours >= 8 && timeElapsed.TotalHours < 24) translateKey = "study:8h";
                    else if (timeElapsed.TotalDays >= 1 && timeElapsed.TotalDays < 7) translateKey = "study:1d";
                    else if (timeElapsed.TotalDays >= 7 && timeElapsed.TotalDays < 31) translateKey = "study:7d";
                    else if (timeElapsed.TotalDays >= 31 && timeElapsed.TotalDays < 364) translateKey = "study:1mn";
                    else if (timeElapsed.TotalDays >= 364) translateKey = "study:1y";
                    else translateKey = "study:default";
                }
                else if (afkType == "poop")
                {
                    if (timeElapsed.TotalMinutes >= 1 && timeElapsed.TotalHours < 1) translateKey = "poop:1m";
                    else if (timeElapsed.TotalHours >= 1 && timeElapsed.TotalHours < 8) translateKey = "poop:1h";
                    else if (timeElapsed.TotalHours >= 8) translateKey = "poop:8h";
                    else translateKey = "poop:default";
                }
                else if (afkType == "shower")
                {
                    if (timeElapsed.TotalMinutes >= 1 && timeElapsed.TotalMinutes < 10) translateKey = "shower:1m";
                    else if (timeElapsed.TotalMinutes >= 10 && timeElapsed.TotalHours < 1) translateKey = "shower:10m";
                    else if (timeElapsed.TotalHours >= 1 && timeElapsed.TotalHours < 8) translateKey = "shower:1h";
                    else if (timeElapsed.TotalHours >= 8) translateKey = "shower:8h";
                    else translateKey = "shower:default";
                }
                string text = TranslationManager.GetTranslation(language, "afk:" + translateKey, RoomID, platform); // FIX AA0
                UsersData.Save(UserID, "lastFromAfkResume", DateTime.UtcNow, platform);
                UsersData.Save(UserID, "isAfk", false, platform);

                if (platform.Equals(Platforms.Twitch))
                    TwitchReply(channel, RoomID, text.Replace("%user%", username) + send + " (" + TextUtil.FormatTimeSpan(timeElapsed, language) + ")", message_id, language, true);
                if (platform.Equals(Platforms.Telegram))
                    TelegramReply(channel, message_reply.Chat.Id, text.Replace("%user%", username) + send + " (" + TextUtil.FormatTimeSpan(timeElapsed, language) + ")", message_reply, language);
            }

            /// <summary>
            /// Send a message to Twitch chat
            /// </summary>
            public static void TwitchSend(string channel, string message, string channelID, string messageID, string lang, bool isSafeEx = false)
            {
                Engine.Statistics.functions_used.Add();
                try
                {
                    Console.WriteLine("[TW] Sending a message...", "info");
                    LogWorker.Log($"[TW] A message was sent to the {channel} channel: {message}", LogWorker.LogTypes.Info, "ChatUtil\\SendMessage");
                    message = TextUtil.CleanAscii(message);

                    if (message.Length > 1500)
                        message = TranslationManager.GetTranslation(lang, "error:too_large_text", channelID, Platforms.Twitch);
                    else if (message.Length > 500)
                    {
                        int splitIndex = message.LastIndexOf(' ', 450);
                        string part2 = string.Concat("... ", message.AsSpan(splitIndex));

                        message = string.Concat(message.AsSpan(0, splitIndex), "...");

                        Task task = Task.Run(() =>
                        {
                            Thread.Sleep(1000);
                            TwitchSend(channel, channelID, part2, messageID, lang, isSafeEx);
                        });
                    }

                    if (!Maintenance.twitch_client.JoinedChannels.Contains(new JoinedChannel(channel)))
                        Maintenance.twitch_client.JoinChannel(channel);

                    if (isSafeEx || NoBanwords.Check(message, channelID, Platforms.Twitch))
                        Maintenance.twitch_client.SendMessage(channel, message);
                    else
                        Maintenance.twitch_client.SendReply(channel, messageID, TranslationManager.GetTranslation(lang, "error:message_could_not_be_sent", channelID, Platforms.Twitch));
                }
                catch (Exception ex)
                {
                    Console.WriteError(ex, $"ChatUtil\\SendMessage#CHNL:{channelID}\\MSG:\"{message}\"");
                }
            }
            /// <summary>
            /// Reply to a message in Twitch chat
            /// </summary>
            public static void TwitchReply(string channel, string channelID, string message, string messageID, string lang, bool isSafeEx = false)
            {
                Engine.Statistics.functions_used.Add();
                try
                {
                    Console.WriteLine("[TW] Sending a message...", "info");
                    LogWorker.Log($"[TW] A response to a message was sent to the {channel} channel: {message}", LogWorker.LogTypes.Info, "ChatUtil\\SendMsgReply");
                    message = TextUtil.CleanAscii(message);

                    if (message.Length > 1500)
                        message = TranslationManager.GetTranslation(lang, "error:too_large_text", channelID, Platforms.Twitch);
                    else if (message.Length > 500)
                    {
                        int splitIndex = message.LastIndexOf(' ', 450);
                        string part2 = string.Concat("... ", message.AsSpan(splitIndex));

                        message = string.Concat(message.AsSpan(0, splitIndex), "...");

                        Task task = Task.Run(() =>
                        {
                            Thread.Sleep(1000);
                            TwitchReply(channel, channelID, part2, messageID, lang, isSafeEx);
                        });
                    }

                    if (!Maintenance.twitch_client.JoinedChannels.Contains(new JoinedChannel(channel)))
                        Maintenance.twitch_client.JoinChannel(channel);

                    if (isSafeEx || NoBanwords.Check(message, channelID, Platforms.Twitch))
                        Maintenance.twitch_client.SendReply(channel, messageID, message);
                    else
                        Maintenance.twitch_client.SendReply(channel, messageID, TranslationManager.GetTranslation(lang, "error:message_could_not_be_sent", channelID, Platforms.Twitch));
                }
                catch (Exception ex)
                {
                    Console.WriteError(ex, $"ChatUtil\\SendMsgReply#CHNL:{channelID}\\MSG:\"{message}\"");
                }
            }
            /// <summary>
            /// Reply to a message in Telegram chat
            /// </summary>
            public static void TelegramReply(string channel, long channelID, string message, Message messageReply, string lang, bool isSafeEx = false)
            {
                Engine.Statistics.functions_used.Add();
                try
                {
                    Console.WriteLine("[TG] Sending message...", "info");
                    LogWorker.Log($"[TG] A message was sent to {channel}: {message}", LogWorker.LogTypes.Info, "ChatUtil\\SendMsgReply");

                    if (message.Length > 4096)
                        message = TranslationManager.GetTranslation(lang, "error:too_large_text", channelID.ToString(), Platforms.Telegram);

                    if (isSafeEx || NoBanwords.Check(message, channelID.ToString(), Platforms.Telegram))
                        Maintenance.telegram_client.SendMessage(
                            channelID,
                            message,
                            replyParameters: messageReply.Id
                        );
                    else
                        Maintenance.telegram_client.SendMessage(
                            channelID,
                            TranslationManager.GetTranslation(lang, "error:message_could_not_be_sent", channelID.ToString(), Platforms.Telegram),
                            replyParameters: messageReply.Id
                        );
                }
                catch (Exception ex)
                {
                    Console.WriteError(ex, $"[tg]ChatUtil\\SendMsgReply#CHNL:{channelID}\\MSG:\"{message}\"");
                }
            }
        }
        /// <summary>
        /// Nickname utility
        /// </summary>
        public class Names
        {
            /// <summary>
            /// Getting a nickname from text
            /// </summary>
            public static string GetUsernameFromText(string text)
            {
                Engine.Statistics.functions_used.Add();
                try
                {
                    if (!text.Contains('@'))
                        return string.Empty;

                    MatchCollection matches = Regex.Matches(text, @"@(\w+)");
                    return " @" + matches.ElementAt(0).ToString().Replace("@", "");
                }
                catch (Exception ex)
                {
                    Console.WriteError(ex, $"NamesUtil\\GetUsernameFromText#{text}");
                    return null;
                }
            }
            /// <summary>
            /// Get user ID by nickname
            /// </summary>
            public static string GetUserID(string user, Platforms platform)
            {
                Engine.Statistics.functions_used.Add();

                string key      = user.ToLowerInvariant();
                string dir      = Path.Combine(Maintenance.path_n2id, Platform.strings[(int)platform]);
                string filePath = Path.Combine(dir, key + ".txt");

                try
                {
                    if (FileUtil.FileExists(filePath))
                        return FileUtil.GetFileContent(filePath);

                    // Twitch API
                    if (platform is Platforms.Twitch)
                    {
                        if (string.IsNullOrEmpty(Maintenance.twitch_client_id) || string.IsNullOrEmpty(Maintenance.token_twitch.AccessToken))
                            return null;

                        using var client = new HttpClient();
                        client.DefaultRequestHeaders.Add("Client-ID", Maintenance.twitch_client_id);
                        client.DefaultRequestHeaders.Authorization =
                            new AuthenticationHeaderValue("Bearer", Maintenance.token_twitch.AccessToken);

                        var uri = new Uri($"https://api.twitch.tv/helix/users?login={Uri.EscapeDataString(user)}");
                        using var response = client.GetAsync(uri).Result;
                        if (!response.IsSuccessStatusCode)
                            return null;

                        var json = response.Content.ReadAsStringAsync().Result;
                        var obj = JObject.Parse(json);
                        var data = obj["data"] as JArray;
                        if (data != null && data.Count > 0)
                        {
                            string id = data[0]["id"]?.ToString();
                            if (!string.IsNullOrEmpty(id))
                            {
                                Directory.CreateDirectory(dir);
                                FileUtil.SaveFileContent(filePath, id);
                                return id;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteError(ex, $"NamesUtil\\GetUserID#{user}");
                }

                // Not found
                return null;
            }

            /// <summary>
            /// Get username by ID
            /// </summary>
            public static string GetUsername(string ID, Platforms platform)
            {
                Engine.Statistics.functions_used.Add();

                string dir      = Path.Combine(Maintenance.path_id2n, Platform.strings[(int)platform]);
                string filePath = Path.Combine(dir, ID + ".txt");

                try
                {
                    if (FileUtil.FileExists(filePath))
                        return FileUtil.GetFileContent(filePath);

                    // API
                    if (platform is Platforms.Twitch)
                    {
                        if (string.IsNullOrEmpty(Maintenance.twitch_client_id) ||
                            string.IsNullOrEmpty(Maintenance.token_twitch.AccessToken))
                        {
                            return null;
                        }

                        using var client = new HttpClient();
                        client.DefaultRequestHeaders.Add("Client-ID", Maintenance.twitch_client_id);
                        client.DefaultRequestHeaders.Authorization =
                            new AuthenticationHeaderValue("Bearer", Maintenance.token_twitch.AccessToken);

                        var uri = new Uri($"https://api.twitch.tv/helix/users?id={Uri.EscapeDataString(ID)}");
                        using var response = client.GetAsync(uri).Result;
                        if (!response.IsSuccessStatusCode)
                            return null;

                        var json = response.Content.ReadAsStringAsync().Result;
                        var obj = JObject.Parse(json);
                        var data = obj["data"] as JArray;
                        if (data != null && data.Count > 0)
                        {
                            string login = data[0]["login"]?.ToString();
                            if (!string.IsNullOrEmpty(login))
                            {
                                Directory.CreateDirectory(dir);
                                FileUtil.SaveFileContent(filePath, login);
                                return login;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteError(ex, $"NamesUtil\\GetUsername#{ID}");
                }

                return null;
            }

            /// <summary>
            /// Add invisible characters to text to avoid pinging chatters
            /// </summary>
            public static string DontPing(string username)
            {
                Engine.Statistics.functions_used.Add();
                return string.Join("󠀀", username);
            }
        }
        /// <summary>
        /// Console utility
        /// </summary>
        public class Console
        {
            public delegate void ConsoleHandler(LineInfo line);
            public static event ConsoleHandler on_chat_line;

            public delegate void ErrorHandler(LineInfo line);
            public static event ErrorHandler error_occured;
            /// <summary>
            /// Output text to console
            /// </summary>
            public static void WriteLine(string message, string channel, ConsoleColor FG = ConsoleColor.Gray, ConsoleColor BG = ConsoleColor.Black)
            {
                Engine.Statistics.functions_used.Add();
                on_chat_line(new LineInfo()
                {
                    Message = $"[{DateTime.Now.Hour}:{DateTime.Now.Minute}.{DateTime.Now.Second} ({DateTime.Now.Millisecond}):{message}\n",
                    Channel = channel,
                    BackgroundColor = BG,
                    ForegroundColor = FG
                });
            }
            public class LineInfo
            {
                public string Message { get; set; }
                public ConsoleColor ForegroundColor { get; set; }
                public ConsoleColor BackgroundColor { get; set; }
                public string Channel { get; set; }
            }
            /// <summary>
            /// Output error to console
            /// </summary>
            public static void WriteError(Exception ex, string sector)
            {
                Engine.Statistics.functions_used.Add();
                error_occured(new LineInfo()
                {
                    Message = $"[ ERROR ] {ex.Message} \nSTACKTRACE: {ex.StackTrace}\nSOURCE: {ex.Source}\nSECTOR: {sector}",
                    Channel = "err",
                    BackgroundColor = ConsoleColor.Red,
                    ForegroundColor = ConsoleColor.Black
                });
            }
        }
        /// <summary>
        /// Text Utility
        /// </summary>
        public class TextUtil
        {
            /// <summary>
            /// Filter command name
            /// </summary>
            public static string FilterCommand(string input)
            {
                Engine.Statistics.functions_used.Add();
                return Regex.Replace(input, @"[^qwertyuiopasdfghjklzxcvbnmйцукенгшщзхъфывапролджэячсмитьбюёQWERTYUIOPASDFGHJKLZXCVBNMЙЦУКЕНГШЩЗХЪФЫВАПРОЛДЖЭЯЧСМИТЬБЮЁ1234567890%]", ""); ;
            }
            /// <summary>
            /// Change text layout
            /// </summary>
            public static string ChangeLayout(string text)
            {
                Engine.Statistics.functions_used.Add();
                try
                {
                    var en = "qwertyuiop[]asdfghjkl;'zxcvbnm,.";
                    var ru = "йцукенгшщзхъфывапролджэячсмитьбю";
                    var map = en.Zip(ru, (e, r) => new[] { (e, r), (r, e) })
                               .SelectMany(p => p)
                               .ToDictionary(p => p.Item1, p => p.Item2);

                    return string.Concat(text.Select(c =>
                        char.IsLetter(c) && map.TryGetValue(char.ToLower(c), out var m)
                            ? char.IsUpper(c) ? char.ToUpper(m) : m : c));
                }
                catch (Exception ex)
                {
                    Console.WriteError(ex, $"TextUtil\\ChangeLayout#{text}");
                    return null;
                }
            }
            /// <summary>
            /// Filter text from ASCII
            /// </summary>
            public static string CleanAscii(string input)
            {
                Engine.Statistics.functions_used.Add();
                if (string.IsNullOrEmpty(input)) return input;

                return new string(input
                    .Where(c => c > 31 && c != 127)
                    .ToArray());
            }
            /// <summary>
            /// Filter text from ASCII without spaces
            /// </summary>
            public static string CleanAsciiWithoutSpaces(string input)
            {
                Engine.Statistics.functions_used.Add();
                return CleanAscii(input).Replace(" ", "");
            }
            /// <summary>
            /// Remove duplicate characters from text
            /// </summary>
            public static string RemoveDuplicates(string text)
            {
                Engine.Statistics.functions_used.Add();
                return text.Aggregate(new StringBuilder(), (sb, c) =>
                    sb.Length == 0 || c != sb[^1] ? sb.Append(c) : sb).ToString();
            }
            /// <summary>
            /// Filter nickname
            /// </summary>
            public static string UsernameFilter(string input)
            {
                Engine.Statistics.functions_used.Add();
                return Regex.Replace(input, @"[^A-Za-z0-9_-]", "");
            }
            /// <summary>
            /// Reduce coordinates
            /// </summary>
            /// <exception cref="ArgumentException"></exception>
            public static string ShortenCoordinate(string coordinate)
            {
                Engine.Statistics.functions_used.Add();
                if (double.TryParse(coordinate[..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out double number))
                    return $"{Math.Round(number, 1).ToString(CultureInfo.InvariantCulture)}{coordinate[^1]}";
                else
                    throw new ArgumentException("Invalid coordinate format");
            }
            /// <summary>
            /// Print time until
            /// </summary>
            public static string TimeTo(DateTime startTime, DateTime endTime, string type, int endYearAdd, string lang, string argsText, string channelID, Platforms platform)
            {
                Engine.Statistics.functions_used.Add();
                try
                {
                    var selectedUser = Names.GetUsernameFromText(argsText);
                    DateTime now = DateTime.UtcNow;
                    DateTime winterStart = new(now.Year, startTime.Month, startTime.Day);
                    DateTime winterEnd = new(now.Year + endYearAdd, endTime.Month, endTime.Day);
                    winterEnd.AddDays(-1);
                    DateTime winter = now < winterStart ? winterStart : winterEnd;
                    if (now < winterStart)
                        return ArgumentsReplacement(TranslationManager.GetTranslation(lang, $"command:{type}:start", channelID, platform),
                            new(){ { "time", FormatTimeSpan(Format.GetTimeTo(winter, now), lang) },
                            { "sUser", selectedUser } });
                    else
                        return ArgumentsReplacement(TranslationManager.GetTranslation(lang, $"command:{type}:end", channelID, platform),
                            new(){ { "time", FormatTimeSpan(Format.GetTimeTo(winter, now), lang) },
                            { "sUser", selectedUser } });
                }
                catch (Exception ex)
                {
                    Console.WriteError(ex, $"TextUtil\\TimeTo#start:{startTime}, end:{endTime}, type:{type}, endYearAdd:{endYearAdd}");
                    return null;
                }
            }
            /// <summary>
            /// Format time to text
            /// </summary>
            public static string FormatTimeSpan(TimeSpan timeSpan, string lang)
            {
                Engine.Statistics.functions_used.Add();
                int days = Math.Abs(timeSpan.Days);
                int hours = Math.Abs(timeSpan.Hours);
                int minutes = Math.Abs(timeSpan.Minutes);
                int seconds = Math.Abs(timeSpan.Seconds);

                string days_str = $"{days} {TranslationManager.GetTranslation(lang, "text:day", "", Platforms.Twitch)}.";
                string hours_str = $"{hours} {TranslationManager.GetTranslation(lang, "text:hour", "", Platforms.Twitch)}.";
                string minutes_str = $"{minutes} {TranslationManager.GetTranslation(lang, "text:minute", "", Platforms.Twitch)}.";
                string seconds_str = $"{seconds} {TranslationManager.GetTranslation(lang, "text:second", "", Platforms.Twitch)}.";

                if (timeSpan.TotalSeconds < 0)
                    timeSpan = -timeSpan;

                if (timeSpan.TotalSeconds < 60)
                    return seconds_str;
                else if (timeSpan.TotalMinutes < 60)
                    return $"{minutes_str} {seconds_str}";
                else if (timeSpan.TotalHours < 24)
                    return $"{hours_str} {minutes_str}";
                else
                    return $"{days_str} {hours_str}";
            }
            /// <summary>
            /// Replaces arguments
            /// </summary>
            /// <param name="original"></param>
            /// <param name="argument"></param>
            /// <param name="replace"></param>
            /// <returns></returns>
            public static string ArgumentsReplacement(string original, Dictionary<string, string> replacements)
            {
                string result = original;

                foreach (var replace in replacements)
                {
                    result = result.Replace($"%{replace.Key}%", replace.Value);
                }

                return result;
            }
            /// <summary>
            /// Replaces argument
            /// </summary>
            /// <param name="original"></param>
            /// <param name="argument"></param>
            /// <param name="replace"></param>
            /// <returns></returns>
            public static string ArgumentReplacement(string original, string key, string value)
            {
                return original.Replace($"%{key}%", value);
            }

            public static string CheckNull(string input)
            {
                if (input is null)
                    return "null";
                return input;
            }
        }
        /// <summary>
        /// Utility for emotes
        /// </summary>
        public class Emotes
        {
            /// <summary>
            /// Getting channel emotes from cache
            /// </summary>
            private static readonly SemaphoreSlim cache_lock = new(1, 1);

            public static async Task<List<string>?> GetEmotesForChannel(string channel, string channel_id)
            {
                Engine.Statistics.functions_used.Add();
                try
                {
                    if (Maintenance.channels_7tv_emotes.TryGetValue(channel_id, out var cached) &&
                        DateTime.UtcNow < cached.expiration)
                    {
                        return cached.emotes;
                    }

                    await cache_lock.WaitAsync();
                    try
                    {
                        if (Maintenance.channels_7tv_emotes.TryGetValue(channel_id, out cached) &&
                            DateTime.UtcNow < cached.expiration)
                        {
                            return cached.emotes;
                        }

                        var emotes = await GetEmotes(channel);
                        Maintenance.channels_7tv_emotes[channel_id] = (emotes, DateTime.UtcNow.Add(Maintenance.CacheTTL));
                        return emotes;
                    }
                    finally
                    {
                        cache_lock.Release();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteError(ex, $"EmotesUtil\\GetEmotesForChannel#{channel}");
                    return null;
                }
            }

            public static async Task<string> RandomEmote(string channel, string channel_id)
            {
                Engine.Statistics.functions_used.Add();
                try
                {
                    var emotes = await GetEmotesForChannel(channel, channel_id);
                    return emotes?.Count > 0
                        ? emotes[(new Random()).Next(emotes.Count)]
                        : null;
                }
                catch (Exception ex)
                {
                    Console.WriteError(ex, $"EmotesUtil\\RandomEmote#{channel}");
                    return null;
                }
            }

            public static async Task EmoteUpdate(string channel, string channel_id)
            {
                Engine.Statistics.functions_used.Add();
                try
                {
                    var emotes = await GetEmotes(channel);
                    Maintenance.channels_7tv_emotes[channel_id] = (emotes, DateTime.UtcNow.Add(Maintenance.CacheTTL));
                }
                catch (Exception ex)
                {
                    Console.WriteError(ex, $"EmotesUtil\\EmoteUpdate#{channel}");
                }
            }

            public static async Task<List<string>> GetEmotes(string channel)
            {
                Engine.Statistics.functions_used.Add();
                try
                {
                    if (Maintenance.userSearchCache.TryGetValue(channel, out var userCache) &&
                        DateTime.UtcNow < userCache.expiration)
                    {
                        return await GetEmotesFromCache(userCache.userId);
                    }

                    var userId = Maintenance.sevenTvService.SearchUser(channel, Maintenance.token_7tv).Result;
                    if (string.IsNullOrEmpty(userId))
                    {
                        Console.WriteLine($"[7tv] {channel} doesn't exist on 7tv!", "info");
                        return new List<string>();
                    }

                    Maintenance.userSearchCache[channel] = (userId, DateTime.UtcNow.Add(Maintenance.CacheTTL));
                    return await GetEmotesFromCache(userId);
                }
                catch (Exception ex)
                {
                    Console.WriteError(ex, $"EmotesUtil\\GetEmotes#{channel}");
                    return new List<string>();
                }
            }

            private static async Task<List<string>> GetEmotesFromCache(string userId)
            {
                Engine.Statistics.functions_used.Add();
                var emote = await Maintenance.sevenTv.GetUser(userId);
                if (emote?.connections?[0].emote_set?.emotes == null)
                {
                    Console.WriteLine($"[7tv] No emotes found for user {userId}", "info");
                    return new List<string>();
                }

                return emote.connections[0].emote_set.emotes.Select(e => e.name).ToList();
            }
        }
        /// <summary>
        /// Command Utility
        /// </summary>
        public class Command
        {
            /// <summary>
            /// Get arguments or null
            /// </summary>
            public static string GetArgument(List<string> args, int index)
            {
                Engine.Statistics.functions_used.Add();
                if (args.Count > index)
                    return args[index];
                return null;
            }

            /// <summary>
            /// Get named arguments or null
            /// </summary>
            public static string GetArgument(List<string> args, string arg_name)
            {
                Engine.Statistics.functions_used.Add();
                foreach (string arg in args)
                {
                    if (arg.StartsWith(arg_name + ":")) return arg.Replace(arg_name + ":", "");
                }
                return null;
            }

            /// <summary>
            /// Process command execution
            /// </summary>
            public static void ExecutedCommand(CommandData data)
            {
                Engine.Statistics.functions_used.Add();
                try
                {
                    var info = $"Executed command {data.name} (User: {data.user.username}, full message: \"{data.name} {data.arguments_string}\", arguments: \"{data.arguments_string}\", command: \"{data.name}\")";
                    LogWorker.Log(info, LogWorker.LogTypes.Info, $"CommandUtil\\executedCommand#{data.name}");
                    Console.WriteLine(info, "info");
                    Engine.completed_commands++;
                }
                catch (Exception ex)
                {
                    Console.WriteError(ex, $"CommandUtil\\executedCommand");
                }
            }
            /// <summary>
            /// Checking commands for cooldown
            /// </summary>
            public static bool CheckCooldown(
                int userSecondsCooldown,
                int globalCooldown,
                string cooldownParamName,
                string userID,
                string roomID,
                Platforms platform,
                bool resetUseTimeIfCommandIsNotReseted = true,
                bool ignoreUserVIP = false,
                bool ignoreGlobalCooldown = false
            )
            {
                Engine.Statistics.functions_used.Add();

                try
                {
                    // VIP or dev/mod bypass
                    bool isVipOrStaff = UsersData.Get<bool>(userID, "isBotModerator", platform)
                                        || UsersData.Get<bool>(userID, "isBotDev", platform);
                    if (isVipOrStaff && !ignoreUserVIP)
                        return true;

                    string userKey = $"LU_{cooldownParamName}";
                    string channelPath = Path.Combine(Maintenance.path_channels, Platform.strings[(int)platform], roomID);
                    string cddFile = Path.Combine(channelPath, "CDD.json");

                    DateTime now = DateTime.UtcNow;

                    // First user use
                    if (!UsersData.Contains(userKey, userID, platform))
                    {
                        UsersData.Save(userID, userKey, now, platform);
                        return true;
                    }

                    // User cooldown check
                    DateTime lastUserUse = UsersData.Get<DateTime>(userID, userKey, platform);
                    double userElapsedSec = (now - lastUserUse).TotalSeconds;
                    if (userElapsedSec < userSecondsCooldown)
                    {
                        if (resetUseTimeIfCommandIsNotReseted)
                            UsersData.Save(userID, userKey, now, platform);

                        var name = Names.GetUsername(userID, platform);
                        Console.WriteLine($"User {name} tried to use the command, but it's on cooldown!", "info");
                        LogWorker.Log($"User {name} tried to use the command, but it's on cooldown!",
                                      LogWorker.LogTypes.Warn, cooldownParamName);
                        return false;
                    }

                    // Reset user timer
                    UsersData.Save(userID, userKey, now, platform);

                    // Global cooldown bypass
                    if (ignoreGlobalCooldown)
                        return true;

                    // Ensure channel cooldowns file exists
                    if (!FileUtil.FileExists(cddFile))
                    {
                        Directory.CreateDirectory(channelPath);
                        Manager.Save(cddFile, userKey, DateTime.MinValue);
                    }

                    // Global cooldown check
                    DateTime lastGlobalUse = Manager.Get<DateTime>(cddFile, userKey);
                    double globalElapsedSec = (now - lastGlobalUse).TotalSeconds;

                    if (lastGlobalUse == default || globalElapsedSec >= globalCooldown)
                    {
                        Manager.Save(cddFile, userKey, now);
                        return true;
                    }
                    else
                    {
                        var name = Names.GetUsername(userID, platform);
                        Console.WriteLine($"User {name} tried to use the command, but it is on global cooldown!", "info");
                        LogWorker.Log($"User {name} tried to use the command, but it is on global cooldown!",
                                      LogWorker.LogTypes.Warn, cooldownParamName);
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteError(ex, $"CommandUtil\\IsNotOnCooldown#{userID}\\{cooldownParamName}");
                    return false;
                }
            }


            /// <summary>
            /// Get cooldown time
            /// </summary>
            public static TimeSpan GetCooldownTime(
                string userID, 
                string cooldownParamName, 
                int userSecondsCooldown, 
                Platforms platform
            )
            {
                Engine.Statistics.functions_used.Add();
                try
                {
                    return TimeSpan.FromSeconds(userSecondsCooldown) - (DateTime.UtcNow - UsersData.Get<DateTime>(userID, $"LU_{cooldownParamName}", platform));
                }
                catch (Exception ex)
                {
                    Console.WriteError(ex, $"CommandUtil\\GetCooldownTime#{userID}\\{cooldownParamName}");
                    return new TimeSpan(0);
                }
            }

            /// <summary>
            /// Process new message
            /// </summary>
            public static readonly ConcurrentDictionary<string, (SemaphoreSlim Semaphore, DateTime LastUsed)> messages_semaphores = new(StringComparer.Ordinal);

            public static async Task ProcessMessageAsync(
                string UserID,
                string RoomId,
                string Username,
                string Message,
                OnMessageReceivedArgs Twitch,
                string Room,
                Platforms Platform,
                Message Telegram,
                string ServerChannel = ""
            )
            {
                Engine.Statistics.functions_used.Add();
                Engine.Statistics.messages_readed.Add();
                var now = DateTime.UtcNow;
                var semaphore = messages_semaphores.GetOrAdd(UserID, id => (new SemaphoreSlim(1, 1), now));
                try
                {
                    await semaphore.Semaphore.WaitAsync().ConfigureAwait(false);
                    messages_semaphores.TryUpdate(UserID, (semaphore.Semaphore, now), semaphore);
                    // Skip banned or ignored users
                    if (UsersData.Get<bool>(UserID, "isBanned", Platform) ||
                        UsersData.Get<bool>(UserID, "isIgnored", Platform))
                        return;

                    // Prepare paths and counters
                    string platform_key = butterBror.Platform.strings[(int)Platform];
                    string channel_base = Path.Combine(Maintenance.path_channels, platform_key, RoomId);
                    string count_dir = Path.Combine(channel_base, "MS");
                    string user_count_file = Path.Combine(count_dir, UserID + ".txt");
                    int messages_count = 0;
                    DateTime now_utc = DateTime.UtcNow;

                    string nick2id = Path.Combine(Maintenance.path_n2id, platform_key, Username + ".txt");
                    string id2nick = Path.Combine(Maintenance.path_id2n, platform_key, UserID + ".txt");

                    // Ensure directories exist
                    FileUtil.CreateDirectory(channel_base);
                    FileUtil.CreateDirectory(Path.Combine(channel_base, "MSGS"));
                    FileUtil.CreateDirectory(count_dir);

                    // Count and increment
                    if (FileUtil.FileExists(user_count_file))
                        messages_count = Format.ToInt(FileUtil.GetFileContent(user_count_file)) + 1;
                    Maintenance.proccessed_messages++;

                    bool isNewUser = !FileUtil.FileExists(
                        Path.Combine(Maintenance.path_users, platform_key, UserID + ".json")
                    );

                    // Build message prefix
                    var prefix = new StringBuilder();
                    if (isNewUser)
                    {
                        UsersData.Register(UserID, Message, Platform);
                        prefix.Append(Platform == Platforms.Discord
                            ? $"{Room} | {ServerChannel} · {Username}: "
                            : $"{Room} · {Username}: ");
                    }
                    else
                    {
                        // Handle AFK return
                        if ((Platform == Platforms.Twitch || Platform == Platforms.Telegram) &&
                            UsersData.Get<bool>(UserID, "isAfk", Platform))
                        {
                            if (Platform == Platforms.Twitch)
                                Chat.ReturnFromAFK(UserID, RoomId, Room, Username, Twitch.ChatMessage.Id, null, Platform);
                            else
                                Chat.ReturnFromAFK(UserID, RoomId, Room, Username, "", Telegram, Platform);
                        }

                        // Award coins
                        int add_coins = Message.Length / 6 + 1;
                        Balance.Add(UserID, 0, add_coins, Platform);
                        int floatBal = Balance.GetBalanceFloat(UserID, Platform);
                        int bal = Balance.GetBalance(UserID, Platform);

                        prefix.Append(Platform == Platforms.Discord
                            ? $"{Room} | {ServerChannel} · {Username} ({messages_count}/{bal}.{floatBal} {Maintenance.coin_symbol}): "
                            : $"{Room} | {Username} ({messages_count}/{bal}.{floatBal} {Maintenance.coin_symbol}): ");
                    }

                    // Currency init for new users
                    if (!UsersData.Get<bool>(UserID, "isReadedCurrency", Platform))
                    {
                        UsersData.Save(UserID, "isReadedCurrency", true, Platform);
                        Engine.coins += (float)(UsersData.Get<int>(UserID, "balance", Platform)
                                       + UsersData.Get<int>(UserID, "floatBalance", Platform) / 100.0);
                        Engine.users++;
                        prefix.Append("(Added to currency) ");
                    }

                    // Append actual message
                    prefix.Append(Message);
                    string outputMessage = prefix.ToString();

                    // Additional processing
                    new CAFUS().Maintrance(UserID, Username, Platform);

                    // Mentions handling
                    foreach (Match m in Regex.Matches(Message, @"@(\w+)"))
                    {
                        var mentioned = m.Groups[1].Value.TrimEnd(',');
                        var mentionedId = Names.GetUserID(mentioned, Platform);
                        if (!string.Equals(mentioned, Username, StringComparison.OrdinalIgnoreCase)
                            && mentionedId != null)
                        {
                            Balance.Add(mentionedId, 0, Maintenance.currency_mentioned, Platform);
                            Balance.Add(UserID, 0, Maintenance.currency_mentioner, Platform);
                            prefix.Append($" ({mentioned} +{Maintenance.currency_mentioned}) " +
                                          $"({Username} +{Maintenance.currency_mentioner})");
                        }
                    }

                    // Save user state
                    UsersData.Save(UserID, "lastSeenMessage", Message, Platform);
                    UsersData.Save(UserID, "lastSeen", now_utc, Platform);
                    try
                    {
                        UsersData.Save(
                            UserID,
                            "totalMessages",
                            UsersData.Get<int>(UserID, "totalMessages", Platform) + 1,
                            Platform
                        );
                    }
                    catch (Exception ex)
                    {
                        Console.WriteError(
                            ex,
                            $"(NOTFATAL#TotalMessages)MessageWorker#User:{UserID} Room:{RoomId}"
                        );
                    }

                    // Persist message history
                    var msg = new MessagesWorker.Message
                    {
                        messageDate = now_utc,
                        messageText = Message,
                        isMe = Platform == Platforms.Twitch && Twitch.ChatMessage.IsMe,
                        isModerator = Platform == Platforms.Twitch && Twitch.ChatMessage.IsModerator,
                        isPartner = Platform == Platforms.Twitch && Twitch.ChatMessage.IsPartner,
                        isStaff = Platform == Platforms.Twitch && Twitch.ChatMessage.IsStaff,
                        isSubscriber = Platform == Platforms.Twitch && Twitch.ChatMessage.IsSubscriber,
                        isTurbo = Platform == Platforms.Twitch && Twitch.ChatMessage.IsTurbo,
                        isVip = Platform == Platforms.Twitch && Twitch.ChatMessage.IsVip
                    };
                    MessagesWorker.SaveMessage(RoomId, UserID, msg, Platform);

                    // Nickname mappings
                    if (!FileUtil.FileExists(nick2id) || !FileUtil.FileExists(id2nick))
                    {
                        FileUtil.SaveFileContent(nick2id, UserID);
                        FileUtil.SaveFileContent(id2nick, Username);
                    }

                    UsersData.Save(UserID, "lastSeenChannel", Room, Platform);
                    FileUtil.SaveFileContent(user_count_file, messages_count.ToString());

                    // Final console output
                    var logTag = Platform switch
                    {
                        Platforms.Twitch => "tw_chat",
                        Platforms.Discord => "ds_chat",
                        Platforms.Telegram => "tg_chat",
                        _ => "chat"
                    };
                    Console.WriteLine(outputMessage, logTag);
                }
                catch (Exception ex)
                {
                    Console.WriteError(ex, $"MessageWorker#{UserID}");
                }
                finally
                {
                    semaphore.Semaphore.Release();
                }
            }

            /// <summary>
            /// Run C# code
            /// </summary>
            public static string ExecuteCode(string userCode)
            {
                Engine.Statistics.functions_used.Add();

                // Формируем полный исходный код с необходимыми using и оберткой класса
                var fullCode = $@"
        using DankDB;
        using butterBror;
        using System;
        using System.Collections.Generic;
        using System.Linq;
        using System.Text;
        using System.IO;
        using System.Runtime;

        public static class MyClass 
        {{
            public static string Execute()
            {{
                {userCode}
            }}
        }}";

                var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                    .Distinct()
                    .ToArray();

                var references = assemblies
                    .Select(a => MetadataReference.CreateFromFile(a.Location))
                    .ToList();

                var requiredAssemblies = new[]
                {
        typeof(object).Assembly,
        typeof(Console).Assembly,
        typeof(Enumerable).Assembly,
        typeof(System.Runtime.GCSettings).Assembly,
    };

                foreach (var assembly in requiredAssemblies)
                {
                    if (!references.Any(r => r.Display.Contains(assembly.GetName().Name)))
                    {
                        references.Add(MetadataReference.CreateFromFile(assembly.Location));
                    }
                }

                var compilation = CSharpCompilation.Create(
                    "MyAssembly",
                    syntaxTrees: new[] { CSharpSyntaxTree.ParseText(fullCode) },
                    references: references,
                    options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

                using var stream = new MemoryStream();
                var emitResult = compilation.Emit(stream);

                if (!emitResult.Success)
                {
                    var errors = string.Join("\n", emitResult.Diagnostics
                        .Where(d => d.IsWarningAsError || d.Severity == DiagnosticSeverity.Error)
                        .Select(d => d.GetMessage()));

                    throw new CompilationException($"Compilation error: {errors}");
                }

                stream.Seek(0, SeekOrigin.Begin);
                var assemblyLoad = Assembly.Load(stream.ToArray());
                var type = assemblyLoad.GetType("MyClass");
                var method = type.GetMethod("Execute");

                return (string)method.Invoke(null, null);
            }

            public class CompilationException : Exception
            {
                public CompilationException(string message) : base(message) { }
            }
        }
        /// <summary>
        /// Utility for API
        /// </summary>
        namespace API
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
        { "qwen", "qwen/qwen3-0.6b-04-28:free" },
        { "deepseek", "deepseek/deepseek-v3-base:free" },
        { "gemma", "google/gemma-3-1b-it:free" },
        { "meta", "meta-llama/llama-4-maverick:free" }
    };

                public class Message
                {
                    public string role { get; set; }
                    public string content { get; set; }
                }

                public class RequestBody
                {
                    public string model { get; set; }
                    public List<Message> messages { get; set; }
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

                public static async Task<string[]> Request(CommandData data)
                {
                    Engine.Statistics.functions_used.Add();

                    if (data.arguments.Count < 1)
                        return new[] { "ERR", "Not enough arguments" };

                    var api_key = Manager.Get<string>(Maintenance.path_settings, "openrouter_token");
                    var uri = new Uri("https://openrouter.ai/api/v1/chat/completions");

                    string selected_model = "meta-llama/llama-4-maverick:free";
                    string model = "meta";
                    if (Command.GetArgument(data.arguments, "model") is not null)
                    {
                        model = Command.GetArgument(data.arguments, "model").ToLower();
                        if (!available_models.ContainsKey(model))
                        {
                            return new[] { "ERR", "Model not found" };
                        }

                        selected_model = available_models[model];
                        data.arguments.Remove($"model:{Command.GetArgument(data.arguments, "model")}");
                        data.arguments_string = string.Join(" ", data.arguments);
                    }

                    if (string.IsNullOrWhiteSpace(data.arguments_string))
                        return new[] { "ERR", "Empty request" };

                    var system_message = new Message
                    {
                        role = "system",
                        content = $@"Hello. You are {Platform.strings[(int)data.platform]} bot. Your name is {Maintenance.bot_name}. DO NOT POST CONFIDENTIAL INFORMATION, DO NOT USE PROFANITY, DO NOT WRITE WORDS THAT MAY GET YOU BLOCKED! DO NOT DISCUSS CONTROVERSIAL TOPICS! Try to write everything BRIEFLY! No more than 400 characters! Time: {DateTime.UtcNow:O} UTC"
                    };

                    var user_info_message = new Message
                    {
                        role = "system",
                        content = $"User info:\n1) Username: {data.user.username}\n2) ID: {data.user_id}\n3) Language (YOUR ANSWER MUST BE IN IT!): {data.user.language}\n"
                    };

                    var user_message = new Message
                    {
                        role = "user",
                        content = data.arguments_string
                    };

                    var request_body = new RequestBody
                    {
                        model = selected_model,
                        messages = new List<Message> { system_message, user_info_message, user_message }
                    };

                    using var client = new HttpClient();
                    client.Timeout = TimeSpan.FromSeconds(30);
                    var json_content = JsonConvert.SerializeObject(request_body);
                    Console.WriteLine($"[AI] Request: {json_content}", "info");
                    using var req = new HttpRequestMessage(HttpMethod.Post, uri)
                    {
                        Content = new StringContent(json_content, Encoding.UTF8, "application/json")
                    };
                    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", api_key);

                    try
                    {
                        var resp = await client.SendAsync(req);
                        var body = await resp.Content.ReadAsStringAsync();
                        Console.WriteLine($"[AI] Response: {body}", "info");

                        if (resp.IsSuccessStatusCode)
                        {
                            var result = JsonConvert.DeserializeObject<ResponseBody>(body);
                            return new[] { model, result.choices[0].message.content };
                        }

                        Console.WriteLine($"API ERROR ({api_key}): #{resp.StatusCode}, {resp.ReasonPhrase}", "err");
                        return new[] { "ERR", "API Error" };
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"API Exception ({api_key}): {ex.Message}", "err");
                        return new[] { "ERR", "API Exception" };
                    }
                }
            } // NEW API
            public class Weather
            {
                public static async Task<Data> Get(string lat, string lon)
                {
                    Engine.Statistics.functions_used.Add();

                    Data GetErrorData()
                        => new Data
                        {
                            current = new Current
                            {
                                summary = "",
                                temperature = -400,
                                feels_like = 0,
                                wind = new() { speed = 0 },
                                pressure = 0,
                                uv_index = 0,
                                humidity = 0,
                                visibility = 0
                            }
                        };

                    try
                    {
                        var tokens = Manager.Get<string[]>(Maintenance.path_settings, "weather_token");
                        string dateKey = DateTime.UtcNow.ToString("ddMMyyyy");
                        using var client = new HttpClient();

                        foreach (var token in tokens)
                        {
                            string cacheKey = dateKey + token;
                            int usage = Manager.Get<int>(Maintenance.path_cache, cacheKey);
                            if (usage >= 10) continue;

                            Manager.Save(Maintenance.path_cache, cacheKey, ++usage);

                            var uri = new Uri(
                                $"https://ai-weather-by-meteosource.p.rapidapi.com/current" +
                                $"?lat={lat.TrimEnd('N', 'S')}&lon={lon.TrimEnd('E', 'W')}" +
                                $"&timezone=auto&language=en&units=auto"
                            );
                            using var req = new HttpRequestMessage(HttpMethod.Get, uri);
                            req.Headers.Add("X-RapidAPI-Key", token);
                            req.Headers.Add("X-RapidAPI-Host", "ai-weather-by-meteosource.p.rapidapi.com");

                            using var resp = await client.SendAsync(req);
                            if (resp.IsSuccessStatusCode)
                                return JsonConvert.DeserializeObject<Data>(await resp.Content.ReadAsStringAsync());

                            LogWorker.Log(
                                $"\nAPI WEATHER ERROR ({token}): #{resp.StatusCode}, {resp.ReasonPhrase}",
                                LogWorker.LogTypes.Err,
                                $"ApiUtils\\Weather\\Get#{token}"
                            );
                        }

                        return GetErrorData();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteError(ex, $"ApiUtils\\Weather\\Get#{lat}\\{lon}");
                        return GetErrorData();
                    }
                }

                public static async Task<List<Place>> GetLocation(string placeName)
                {
                    Engine.Statistics.functions_used.Add();

                    List<Place> ErrorResult() => new() { new Place { name = "err", lat = "", lon = "" } };

                    try
                    {
                        var tokens = Manager.Get<string[]>(Maintenance.path_settings, "weather_token");
                        string dateKey = DateTime.UtcNow.ToString("ddMMyyyy");
                        var cache = Manager.Get<Dictionary<string, LocationCacheData>>(Maintenance.path_cache, "Data")
                                    ?? new Dictionary<string, LocationCacheData>();
                        var cached = cache.Values
                                          .Where(c => c.Tags?.Contains(placeName, StringComparer.OrdinalIgnoreCase) == true
                                                   || string.Equals(c.CityName, placeName, StringComparison.OrdinalIgnoreCase))
                                          .Select(c => new Place { name = c.CityName, lat = c.Lat, lon = c.Lon })
                                          .ToList();
                        if (cached.Count > 0)
                            return cached;

                        using var client = new HttpClient();
                        foreach (var token in tokens)
                        {
                            string usageKey = dateKey + token;
                            int uses = Manager.Get<int>(Maintenance.path_API_uses, usageKey);
                            if (uses >= 10)
                                continue;

                            Manager.Save(Maintenance.path_API_uses, usageKey, ++uses);

                            var uri = new Uri(
                                $"https://ai-weather-by-meteosource.p.rapidapi.com/find_places" +
                                $"?text={Uri.EscapeDataString(placeName)}&language=en"
                            );
                            using var req = new HttpRequestMessage(HttpMethod.Get, uri);
                            req.Headers.Add("X-RapidAPI-Key", token);
                            req.Headers.Add("X-RapidAPI-Host", "ai-weather-by-meteosource.p.rapidapi.com");

                            using var resp = await client.SendAsync(req);
                            if (!resp.IsSuccessStatusCode)
                            {
                                LogWorker.Log(
                                    $"API WEATHER ERROR ({token}): #{resp.StatusCode}, {resp.ReasonPhrase}",
                                    LogWorker.LogTypes.Err,
                                    $"ApiUtils\\Weather\\GetLocation#{token}"
                                );
                                continue;
                            }

                            var places = JsonConvert.DeserializeObject<List<Place>>(await resp.Content.ReadAsStringAsync());
                            foreach (var p in places)
                            {
                                string key = (p.name.ToLowerInvariant() + p.lat + p.lon);
                                if (cache.TryGetValue(key, out var entry))
                                {
                                    var tags = entry.Tags?.ToList() ?? new List<string>();
                                    if (!tags.Contains(placeName, StringComparer.OrdinalIgnoreCase))
                                        tags.Add(placeName);
                                    entry.Tags = tags.ToArray();
                                }
                                else
                                {
                                    cache[key] = new LocationCacheData
                                    {
                                        CityName = p.name,
                                        Lat = p.lat,
                                        Lon = p.lon,
                                        Tags = new[] { placeName }
                                    };
                                }
                            }

                            Manager.Save(Maintenance.path_cache, "Data", cache);
                            return places;
                        }

                        return ErrorResult();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteError(ex, $"Weather\\GetLocation#{placeName}");
                        return new() { new Place { name = "err", lat = "", lon = "" } };
                    }
                }

                public class Data
                {
                    public required Current current { get; set; }
                }
                public class Current
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
                public static string GetEmoji(double temperature)
                {
                    Engine.Statistics.functions_used.Add();
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
                public static string GetSummary(string lang, string summary, string channelID, Platforms platform)
                {
                    Engine.Statistics.functions_used.Add();
                    switch (summary.ToLower())
                    {
                        case "sunny":
                            return TranslationManager.GetTranslation(lang, "text:weather:clear", channelID, platform);
                        case "partly cloudy":
                            return TranslationManager.GetTranslation(lang, "text:weather:cloudy", channelID, platform);
                        case "mostly cloudy":
                            return TranslationManager.GetTranslation(lang, "text:weather:mostly_cloudy", channelID, platform);
                        case "partly sunny":
                            return TranslationManager.GetTranslation(lang, "text:weather:partly_cloudy", channelID, platform);
                        case "cloudy":
                            return TranslationManager.GetTranslation(lang, "text:weather:cloudy", channelID, platform);
                        case "overcast":
                            return TranslationManager.GetTranslation(lang, "text:weather:overcast", channelID, platform);
                        case "rain":
                            return TranslationManager.GetTranslation(lang, "text:weather:rain", channelID, platform);
                        case "thunderstorm":
                            return TranslationManager.GetTranslation(lang, "text:weather:thunderstorm", channelID, platform);
                        case "snow":
                            return TranslationManager.GetTranslation(lang, "text:weather:snow", channelID, platform);
                        case "fog":
                            return TranslationManager.GetTranslation(lang, "text:weather:fog", channelID, platform);
                        default:
                            return summary;
                    }
                }
                public static string GetSummaryEmoji(string summary)
                {
                    Engine.Statistics.functions_used.Add();
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
            }
            public class Imgur
            {
                public static async Task<byte[]> DownloadAsync(string imageUrl)
                {
                    Engine.Statistics.functions_used.Add();
                    using HttpClient client = new HttpClient();
                    return await client.GetByteArrayAsync(imageUrl);
                }
                public static async Task<string> UploadAsync(byte[] imageBytes, string description, string title, string ImgurClientId, string ImgurUploadUrl)
                {
                    Engine.Statistics.functions_used.Add();
                    try
                    {
                        using HttpClient client = new HttpClient();
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Client-ID", ImgurClientId);

                        using MultipartFormDataContent content = new();
                        ByteArrayContent byteContent = new(imageBytes);
                        byteContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");

                        content.Add(byteContent, "image");
                        content.Add(new StringContent(description), "description");
                        content.Add(new StringContent(title), "title");

                        HttpResponseMessage response = await client.PostAsync(ImgurUploadUrl, content);
                        response.EnsureSuccessStatusCode();

                        string responseString = await response.Content.ReadAsStringAsync();
                        return responseString;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteError(ex, $"ImgurAPI\\UploadImageToImgurAsync");
                        return null;
                    }
                }
                public static string GetLinkFromResponse(string response)
                {
                    Engine.Statistics.functions_used.Add();
                    try
                    {
                        JObject jsonResponse = JObject.Parse(response);
                        bool success = jsonResponse["success"].Value<bool>();

                        if (success)
                        {
                            string link = jsonResponse["data"]["link"].Value<string>();
                            return link;
                        }
                        else
                            return "Upload failed.";
                    }
                    catch (Exception ex)
                    {
                        Console.WriteError(ex, $"ImgurAPI\\UploadImageToImgurAsync");
                        return null;
                    }
                }
            }
            /*
            public class Currency
            {
                private readonly HttpClient _httpClient = new HttpClient();

                public async Task<Dictionary<string, string>> GetCurrenciesAsync()
                {
                    var url = "https://cdn.jsdelivr.net/npm/@fawazahmed0/currency-api@latest/v1/currencies.json";

                    var response = await _httpClient.GetAsync(url);
                    response.EnsureSuccessStatusCode();

                    var content = await response.Content.ReadAsStreamAsync();
                    var doc = await JsonDocument.ParseAsync(content);
                    var root = doc.RootElement;

                    var currencies = new Dictionary<string, string>();
                    foreach (var property in root.EnumerateObject())
                    {
                        currencies[property.Name.ToLower()] = property.Value.GetString();
                    }

                    return currencies;
                }

                private async Task<Dictionary<string, decimal>> GetExchangeRatesAsync(string baseCurrency)
                {
                    var url = $"https://cdn.jsdelivr.net/npm/@fawazahmed0/currency-api@latest/v1/currencies/{baseCurrency.ToLower()}.json";

                    var response = await _httpClient.GetAsync(url);
                    response.EnsureSuccessStatusCode();

                    var content = await response.Content.ReadAsStreamAsync();
                    var doc = await JsonDocument.ParseAsync(content);
                    var root = doc.RootElement;

                    foreach (var property in root.EnumerateObject())
                    {
                        if (property.Name == "date") continue;

                        var rates = new Dictionary<string, decimal>();
                        foreach (var rateProperty in property.Value.EnumerateObject())
                        {
                            rates[rateProperty.Name.ToLower()] = (decimal)rateProperty.Value.GetDouble();
                        }
                        return rates;
                    }

                    throw new Exception("Не найдены курсы в ответе API");
                }

                /// <summary>
                /// Конвертирует сумму из одной валюты в другую.
                /// </summary>
                /// <param name="fromCurrency">Исходная валюта (ISO-код)</param>
                /// <param name="toCurrency">Целевая валюта (ISO-код)</param>
                /// <param name="amount">Сумма для конвертации</param>
                /// <returns>Конвертированная сумма</returns>
                public async Task<decimal> ConvertAsync(string fromCurrency, string toCurrency, decimal amount)
                {
                    if (string.IsNullOrWhiteSpace(fromCurrency))
                        throw new ArgumentException("Исходная валюта не может быть пустой", nameof(fromCurrency));
                    if (string.IsNullOrWhiteSpace(toCurrency))
                        throw new ArgumentException("Целевая валюта не может быть пустой", nameof(toCurrency));
                    if (amount <= 0)
                        throw new ArgumentOutOfRangeException(nameof(amount), "Сумма должна быть больше нуля");

                    if (fromCurrency.Equals(toCurrency, StringComparison.OrdinalIgnoreCase))
                        return amount;

                    try
                    {
                        // Попробуем прямой курс
                        var fromRates = await GetExchangeRatesAsync(fromCurrency);
                        if (fromRates.TryGetValue(toCurrency.ToLower(), out var directRate) && directRate > 0)
                        {
                            return amount * directRate;
                        }

                        // Попробуем обратный курс
                        var toRates = await GetExchangeRatesAsync(toCurrency);
                        if (toRates.TryGetValue(fromCurrency.ToLower(), out var inverseRate) && inverseRate > 0)
                        {
                            return amount / inverseRate;
                        }

                        // Попробуем через USD
                        var usdRatesFrom = await GetExchangeRatesAsync("usd");
                        if (!usdRatesFrom.TryGetValue(fromCurrency.ToLower(), out var fromUsdRate))
                        {
                            var fromRates2 = await GetExchangeRatesAsync(fromCurrency);
                            if (fromRates2.TryGetValue("usd", out var fromToUsd))
                            {
                                fromUsdRate = 1 / fromToUsd;
                            }
                            else
                            {
                                throw new Exception($"Нет данных для конвертации {fromCurrency} в USD");
                            }
                        }

                        if (fromUsdRate <= 0)
                            throw new Exception($"Курс {fromCurrency} к USD равен нулю");

                        var usdRatesTo = await GetExchangeRatesAsync("usd");
                        if (!usdRatesTo.TryGetValue(toCurrency.ToLower(), out var toUsdRate))
                        {
                            var toRates2 = await GetExchangeRatesAsync(toCurrency);
                            if (toRates2.TryGetValue("usd", out var toToUsd) && toToUsd > 0)
                            {
                                toUsdRate = 1 / toToUsd;
                            }
                            else
                            {
                                throw new Exception($"Нет данных для конвертации {toCurrency} в USD");
                            }
                        }

                        // Конвертируем через USD: amount * fromUsdRate (из from в USD) / toUsdRate (из to в USD)
                        return amount * fromUsdRate / toUsdRate;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteError(ex, "Currency/ConvertAsync");
                    }
                }
            }
            */
        }
        /// <summary>
        /// Utilities for YouTube
        /// </summary>
        public class YouTube
        {
            public static string[] GetPlaylistLinks(string playlistId, string developerKey)
            {
                Engine.Statistics.functions_used.Add();
                try
                {
                    var youtubeService = new YouTubeService(new BaseClientService.Initializer()
                    {
                        ApplicationName = "YouTube Playlist Viewer",
                        ApiKey = developerKey
                    });

                    var playlistItemsRequest = youtubeService.PlaylistItems.List("contentDetails");
                    playlistItemsRequest.PlaylistId = playlistId;
                    playlistItemsRequest.MaxResults = 50;

                    List<string> videoLinks = [];

                    PlaylistItemListResponse playlistItemResponse = new();

                    do
                    {
                        try
                        {
                            playlistItemResponse = playlistItemsRequest.Execute();

                            if (playlistItemResponse.Items != null && playlistItemResponse.Items.Any())
                            {
                                foreach (var item in playlistItemResponse.Items)
                                {
                                    var videoId = item.ContentDetails.VideoId;
                                    var videoRequest = youtubeService.Videos.List("status");
                                    videoRequest.Id = videoId;
                                    var videoResponse = videoRequest.Execute();

                                    playlistItemsRequest.PageToken = playlistItemResponse.NextPageToken;

                                    if (videoResponse.Items != null && videoResponse.Items.Any())
                                    {
                                        var videoItem = videoResponse.Items[0];
                                        videoLinks.Add($"https://www.youtube.com/watch?v={videoId}");
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("YOUTUBE PLAYLIST ERROR: " + ex.Message, "err");
                        }
                    } while (playlistItemResponse.NextPageToken != null);

                    return [.. videoLinks];
                }
                catch (Exception ex)
                {
                    Console.WriteError(ex, $"YTAPI\\GetPlaylistVideoLinks");
                    return null;
                }
            }
            public static string[] GetPlaylistVideos(string playlistUrl)
            {
                Engine.Statistics.functions_used.Add();
                try
                {
                    MatchCollection matches = new Regex(@"watch\?v=[a-zA-Z0-9_-]{11}").Matches(new WebClient().DownloadString(playlistUrl));

                    string[] videoLinks = new string[matches.Count];
                    int i = 0;

                    foreach (Match match in matches)
                    {
                        videoLinks[i] = "https://www.youtube.com/" + match.Value;
                        i++;
                    }

                    return videoLinks;
                }
                catch (Exception ex)
                {
                    Console.WriteError(ex, $"YTAPI\\GetPlaylistVideos");
                    return null;
                }
            }
        }
        /// <summary>
        /// CAFUS utility
        /// </summary>
        public class CAFUS
        {
            private readonly List<string> _updated = new();
            private static readonly (double Version, Action<string, Platforms> Action)[] _migrations =
            {
        (1.0, Migrate0),
        (1.1, Migrate1),
        (1.2, Migrate2),
        (1.3, Migrate3),
        (1.4, Migrate4)
    };

            public void Maintrance(string userId, string username, Platforms platform)
            {
                Engine.Statistics.functions_used.Add();

                try
                {
                    _updated.Clear();
                    var current = UsersData.Get<double?>(userId, "CAFUSV", platform) ?? 0.0;

                    foreach (var (ver, action) in _migrations)
                    {
                        if (current < ver)
                        {
                            action(userId, platform);
                            UsersData.Save(userId, "CAFUSV", ver, platform);
                            _updated.Add(ver.ToString("0.0"));
                        }
                    }

                    if (_updated.Count > 0)
                        Console.WriteLine($"@{username} CAFUS {string.Join(", ", _updated)} UPDATED", "cafus");
                }
                catch (Exception ex)
                {
                    Console.WriteError(ex, "CAFUS\\Maintrance");
                }
            }

            private static void Migrate0(string uid, Platforms p)
            {
                Engine.Statistics.functions_used.Add();
                var defaults = new Dictionary<string, object>
                {
                    ["language"] = "ru",
                    ["userPlace"] = "",
                    ["afkText"] = "",
                    ["isAfk"] = false,
                    ["afkType"] = "",
                    ["afkTime"] = DateTime.UtcNow,
                    ["lastFromAfkResume"] = DateTime.UtcNow,
                    ["fromAfkResumeTimes"] = 0
                };
                SaveDefaults(uid, p, defaults);
            }

            private static void Migrate1(string uid, Platforms p)
            {
                Engine.Statistics.functions_used.Add();
                SaveIfMissing(uid, "isBotDev", false, p);
            }

            private static void Migrate2(string uid, Platforms p)
            {
                Engine.Statistics.functions_used.Add();
                SaveIfMissing(uid, "banReason", "", p);
                SaveIfMissing(uid, "weatherAPIUsedTimes", 0, p);
                SaveIfMissing(uid, "weatherAPIResetDate", DateTime.UtcNow.AddDays(1), p);
            }

            private static void Migrate3(string uid, Platforms p)
            {
                Engine.Statistics.functions_used.Add();
                var defaults = new Dictionary<string, object>
                {
                    ["lastSeenChannel"] = "",
                    ["lastFishingTime"] = DateTime.UtcNow,
                    ["fishLocation"] = 1,
                    ["fishIsMovingNow"] = false,
                    ["fishIsKidnapingNow"] = false
                };
                SaveDefaults(uid, p, defaults);
            }

            private static void Migrate4(string uid, Platforms p)
            {
                Engine.Statistics.functions_used.Add();
                var inventory = new Dictionary<string, int>
                {
                    ["Fish"] = 0,
                    ["Tropical Fish"] = 0,
                    ["Blowfish"] = 0,
                    ["Octopus"] = 0,
                    ["Jellyfish"] = 0,
                    ["Spiral Shell"] = 0,
                    ["Coral"] = 0,
                    ["Fallen Leaf"] = 0,
                    ["Leaf Fluttering in Wind"] = 0,
                    ["Maple Leaf"] = 0,
                    ["Herb"] = 0,
                    ["Lotus"] = 0,
                    ["Squid"] = 0,
                    ["Shrimp"] = 0,
                    ["Lobster"] = 0,
                    ["Crab"] = 0,
                    ["Mans Shoe"] = 0,
                    ["Athletic Shoe"] = 0,
                    ["Hiking Boot"] = 0,
                    ["Scroll"] = 0,
                    ["Top Hat"] = 0,
                    ["Mobile Phone"] = 0,
                    ["Shorts"] = 0,
                    ["Briefs"] = 0,
                    ["Envelope"] = 0,
                    ["Bone"] = 0,
                    ["Canned Food"] = 0,
                    ["Gear"] = 0
                };
                SaveIfMissing(uid, "fishInvertory", inventory, p);
            }

            private static void SaveIfMissing(string uid, string key, object value, Platforms p)
            {
                Engine.Statistics.functions_used.Add();
                if (!UsersData.Contains(key, uid, p))
                    UsersData.Save(uid, key, value, p);
            }

            private static void SaveDefaults(string uid, Platforms p, Dictionary<string, object> defaults)
            {
                foreach (var kv in defaults)
                    SaveIfMissing(uid, kv.Key, kv.Value, p);
            }
        }


        public class Device
        {
            public class Memory
            {
                public static ulong GetTotalMemoryBytes()
                {
                    Engine.Statistics.functions_used.Add();
                    // fckin piece of sht
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        return GetWindowsTotalMemory();
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        return GetLinuxTotalMemory();
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    {
                        return GetMacOSTotalMemory();
                    }
                    else
                    {
                        throw new PlatformNotSupportedException("Platform not supported");
                    }
                }
                private static ulong GetWindowsTotalMemory()
                {
                    Engine.Statistics.functions_used.Add();
                    var memoryStatus = new MEMORYSTATUSEX();
                    if (GlobalMemoryStatusEx(ref memoryStatus))
                    {
                        return memoryStatus.ullTotalPhys;
                    }
                    throw new Exception("Failed to get memory information on Windows");
                }

                private static ulong GetLinuxTotalMemory()
                {
                    Engine.Statistics.functions_used.Add();
                    string memInfo = FileUtil.GetFileContent("/proc/meminfo");
                    string totalMemoryLine = memInfo.Split('\n')[0];
                    string totalMemoryValue = totalMemoryLine.Split([' '], StringSplitOptions.RemoveEmptyEntries)[1];
                    return Convert.ToUInt64(totalMemoryValue) * 1024;
                }

                private static ulong GetMacOSTotalMemory()
                {
                    Engine.Statistics.functions_used.Add();
                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "sysctl",
                            Arguments = "-n hw.memsize",
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };
                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                    return Convert.ToUInt64(output.Trim());
                }

                [StructLayout(LayoutKind.Sequential)]
                private struct MEMORYSTATUSEX
                {
                    public uint dwLength;
                    public uint dwMemoryLoad;
                    public ulong ullTotalPhys;
                    public ulong ullAvailPhys;
                    public ulong ullTotalPageFile;
                    public ulong ullAvailPageFile;
                    public ulong ullTotalVirtual;
                    public ulong ullAvailVirtual;
                    public ulong ullAvailExtendedVirtual;
                }

                [DllImport("kernel32.dll", SetLastError = true)]
                private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);
            }

            public class Drives
            {
                public static DriveInfo[] Get()
                {
                    Engine.Statistics.functions_used.Add();
                    DriveInfo[] drives = DriveInfo.GetDrives();
                    return drives;
                }
            }

            public class Battery
            {
                public static float GetBatteryCharge()
                {
                    float charge = -1;

                    try
                    {
                        using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Battery"))
                        {
                            foreach (ManagementObject battery in searcher.Get())
                            {
                                charge = Convert.ToSingle(battery["EstimatedChargeRemaining"]);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteError(ex, "Device/Battery/GetBatteryCharge");
                    }

                    return charge;
                }

                public static bool IsCharging()
                {
                    bool isCharging = false;

                    try
                    {
                        using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Battery"))
                        {
                            foreach (ManagementObject battery in searcher.Get())
                            {
                                isCharging = Convert.ToInt32(battery["BatteryStatus"]) == 2;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteError(ex, "Device/Battery/IsCharging");
                    }

                    return isCharging;
                }
            }
        }

        public class Memory
        {
            public static double BytesToGB(long bytes)
            {
                Engine.Statistics.functions_used.Add();
                return bytes / (1024.0 * 1024.0 * 1024.0);
            }
            public static double BytesToGB(ulong bytes)
            {
                Engine.Statistics.functions_used.Add();
                return bytes / (1024.0 * 1024.0 * 1024.0);
            }
        }
    }
}