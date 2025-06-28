using butterBror.Utils.DataManagers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using static butterBror.Utils.Things.Console;

namespace butterBror.Utils.Tools
{
    public class TwitchToken
    {
        private static string client_id;
        private static string client_secret;
        private static string redirect_uri;
        private static string database_path;

        public TwitchToken(string client_id, string client_secret, string database_path)
        {
            TwitchToken.client_id = client_id;
            TwitchToken.client_secret = client_secret;
            redirect_uri = "http://localhost:12121/";
            TwitchToken.database_path = database_path;
        }

        [ConsoleSector("butterBror.Utils.Tools.TwitchToken", "GetTokenAsync")]
        public static async Task<TokenData> GetTokenAsync()
        {
            Core.Statistics.FunctionsUsed.Add();
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
                Write(ex);
                return null;
            }
        }

        [ConsoleSector("butterBror.Utils.Tools.TwitchToken", "PerformAuthorizationFlow")]
        private static async Task<TokenData> PerformAuthorizationFlow()
        {
            Core.Statistics.FunctionsUsed.Add();
            try
            {
                using var listener = new HttpListener();
                listener.Prefixes.Add(redirect_uri);
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
                Write(ex);
                return null;
            }
        }

        [ConsoleSector("butterBror.Utils.Tools.TwitchToken", "RefreshAccessToken")]
        public static async Task<TokenData> RefreshAccessToken(TokenData token)
        {
            Core.Statistics.FunctionsUsed.Add();
            try
            {
                if (token == null || string.IsNullOrEmpty(token.RefreshToken))
                    return await PerformAuthorizationFlow();

                using var httpClient = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Post, "https://id.twitch.tv/oauth2/token")
                {
                    Content = new StringContent($"grant_type=refresh_token&refresh_token={token.RefreshToken}&client_id={client_id}&client_secret={client_secret}", Encoding.UTF8, "application/x-www-form-urlencoded")
                };

                var response = await httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(responseContent);
                    token.AccessToken = tokenResponse.access_token;
                    token.ExpiresAt = DateTime.Now.AddSeconds(tokenResponse.expires_in);
                    SaveTokenData(token);
                    Write("Twitch oauth - Token refreshed!", "info");
                    return token;
                }
                else
                {
                    Write($"Twitch oauth - Error updating token: {responseContent}", "info", LogLevel.Error);
                    return null;
                }
            }
            catch (Exception ex)
            {
                Write(ex);
                return null;
            }
        }

        [ConsoleSector("butterBror.Utils.Tools.TwitchToken", "GetAuthorizationCodeAsync")]
        private static async Task<string> GetAuthorizationCodeAsync(HttpListener listener)
        {
            Core.Statistics.FunctionsUsed.Add();
            try
            {
                Write("Twitch oauth - Getting auth data...", "info");
                var url = $"https://id.twitch.tv/oauth2/authorize?client_id={client_id}&redirect_uri={redirect_uri}&response_type=code&scope=user:manage:chat_color+chat:edit+chat:read";
                var psi = new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                };
                Process.Start(psi);

                var context = await listener.GetContextAsync();
                var request = context.Request;
                var code = GetCodeFromResponse(request.Url.Query);
                Write("Twitch oauth - Auth data getted", "info");

                return code;
            }
            catch (Exception ex)
            {
                Write(ex);
                return null;
            }
        }

        [ConsoleSector("butterBror.Utils.Tools.TwitchToken", "ExchangeCodeForTokenAsync")]
        private static async Task<TokenData> ExchangeCodeForTokenAsync(string code)
        {
            Core.Statistics.FunctionsUsed.Add();
            try
            {
                Write("Twitch oauth - Getting exchange code...", "info");
                using var httpClient = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Post, "https://id.twitch.tv/oauth2/token")
                {
                    Content = new StringContent($"client_id={client_id}&client_secret={client_secret}&redirect_uri={redirect_uri}&grant_type=authorization_code&code={code}", Encoding.UTF8, "application/x-www-form-urlencoded")
                };

                var response = await httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(responseContent);
                    Write("Twitch oauth - Getted exchange code!", "info");
                    return new TokenData { AccessToken = tokenResponse.access_token, ExpiresAt = DateTime.Now.AddSeconds(tokenResponse.expires_in), RefreshToken = tokenResponse.refresh_token };
                }
                else
                {
                    Write($"Twitch oauth - Error receiving exchange token: {responseContent}", "err");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Write(ex);
                return null;
            }
        }

        [ConsoleSector("butterBror.Utils.Tools.TwitchToken", "GetCodeFromResponse")]
        private static string GetCodeFromResponse(string query)
        {
            Core.Statistics.FunctionsUsed.Add();
            var queryParams = HttpUtility.ParseQueryString(query);
            return queryParams["code"];
        }

        [ConsoleSector("butterBror.Utils.Tools.TwitchToken", "LoadTokenData")]
        private static TokenData LoadTokenData()
        {
            Core.Statistics.FunctionsUsed.Add();
            try
            {
                if (FileUtil.FileExists(database_path))
                {
                    var json = FileUtil.GetFileContent(database_path);
                    if (string.IsNullOrEmpty(json)) return null;
                    var tokenData = JsonConvert.DeserializeObject<TokenData>(json);
                    Write("Twitch oauth - Token data loaded!", "info");
                    return tokenData?.ExpiresAt > DateTime.Now ? tokenData : null;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        [ConsoleSector("butterBror.Utils.Tools.TwitchToken", "SaveTokenData")]
        private static void SaveTokenData(TokenData tokenData)
        {
            Core.Statistics.FunctionsUsed.Add();
            try
            {
                FileUtil.SaveFileContent(database_path, JsonConvert.SerializeObject(tokenData));
            }
            catch (Exception ex)
            {
                Write(ex);
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
}
