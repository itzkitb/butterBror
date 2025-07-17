using butterBror.Data;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Web;
using static butterBror.Core.Bot.Console;

namespace butterBror.Core.Bot
{
    /// <summary>
    /// Manages Twitch OAuth2 token acquisition, refresh, and storage operations.
    /// </summary>
    public class TwitchToken
    {
        private static string _clientId;
        private static string _clientSecret;
        private static string _redirectURL;
        private static string _databasePath;

        /// <summary>
        /// Initializes a new instance of the TwitchToken class with application credentials.
        /// </summary>
        /// <param name="clientId">Twitch application client ID</param>
        /// <param name="clientSecret">Twitch application client secret</param>
        /// <param name="databasePath">Path to store token data persistently</param>
        public TwitchToken(string clientId, string clientSecret, string databasePath)
        {
            _clientId = clientId;
            _clientSecret = clientSecret;
            _redirectURL = "http://localhost:12121/";
            _databasePath = databasePath;
        }

        /// <summary>
        /// Asynchronously retrieves a valid Twitch API token, refreshing if necessary.
        /// </summary>
        /// <returns>A valid TokenData object or null on failure</returns>
        [ConsoleSector("butterBror.Utils.Tools.TwitchToken", "GetTokenAsync")]
        public static async Task<TokenData> GetTokenAsync()
        {
            Engine.Statistics.FunctionsUsed.Add();
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

        /// <summary>
        /// Initiates the Twitch OAuth2 authorization flow to obtain a new token.
        /// </summary>
        /// <param name="listener">HttpListener instance for handling the redirect</param>
        /// <returns>A new TokenData object or null on failure</returns>
        [ConsoleSector("butterBror.Utils.Tools.TwitchToken", "PerformAuthorizationFlow")]
        private static async Task<TokenData> PerformAuthorizationFlow()
        {
            Engine.Statistics.FunctionsUsed.Add();
            try
            {
                using var listener = new HttpListener();
                listener.Prefixes.Add(_redirectURL);
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

        /// <summary>
        /// Refreshes an existing Twitch API token using its refresh token.
        /// </summary>
        /// <param name="token">The token to refresh</param>
        /// <returns>An updated TokenData object or null on failure</returns>
        [ConsoleSector("butterBror.Utils.Tools.TwitchToken", "RefreshAccessToken")]
        public static async Task<TokenData> RefreshAccessToken(TokenData token)
        {
            Engine.Statistics.FunctionsUsed.Add();
            try
            {
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

        /// <summary>
        /// Gets the authorization code from Twitch's OAuth2 response.
        /// </summary>
        /// <param name="listener">HttpListener instance to receive the response</param>
        /// <returns>The authorization code string or null on failure</returns>
        [ConsoleSector("butterBror.Utils.Tools.TwitchToken", "GetAuthorizationCodeAsync")]
        private static async Task<string> GetAuthorizationCodeAsync(HttpListener listener)
        {
            Engine.Statistics.FunctionsUsed.Add();
            try
            {
                Write("Twitch oauth - Getting auth data...", "info");
                var url = $"https://id.twitch.tv/oauth2/authorize?client_id={_clientId}&redirect_uri={_redirectURL}&response_type=code&scope=user:manage:chat_color+chat:edit+chat:read";
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

        /// <summary>
        /// Exchanges an authorization code for a Twitch API token.
        /// </summary>
        /// <param name="code">The authorization code to exchange</param>
        /// <returns>A new TokenData object or null on failure</returns>
        [ConsoleSector("butterBror.Utils.Tools.TwitchToken", "ExchangeCodeForTokenAsync")]
        private static async Task<TokenData> ExchangeCodeForTokenAsync(string code)
        {
            Engine.Statistics.FunctionsUsed.Add();
            try
            {
                Write("Twitch oauth - Getting exchange code...", "info");
                using var httpClient = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Post, "https://id.twitch.tv/oauth2/token")
                {
                    Content = new StringContent($"client_id={_clientId}&client_secret={_clientSecret}&redirect_uri={_redirectURL}&grant_type=authorization_code&code={code}", Encoding.UTF8, "application/x-www-form-urlencoded")
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

        /// <summary>
        /// Extracts the authorization code from the HTTP query string.
        /// </summary>
        /// <param name="query">The HTTP query string</param>
        /// <returns>The authorization code or null if not found</returns>
        [ConsoleSector("butterBror.Utils.Tools.TwitchToken", "GetCodeFromResponse")]
        private static string GetCodeFromResponse(string query)
        {
            Engine.Statistics.FunctionsUsed.Add();
            var queryParams = HttpUtility.ParseQueryString(query);
            return queryParams["code"];
        }

        /// <summary>
        /// Loads stored token data from persistent storage.
        /// </summary>
        /// <returns>The loaded TokenData or null if loading failed</returns>
        [ConsoleSector("butterBror.Utils.Tools.TwitchToken", "LoadTokenData")]
        private static TokenData LoadTokenData()
        {
            Engine.Statistics.FunctionsUsed.Add();
            try
            {
                if (FileUtil.FileExists(_databasePath))
                {
                    var json = FileUtil.GetFileContent(_databasePath);
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

        /// <summary>
        /// Saves token data to persistent storage.
        /// </summary>
        /// <param name="tokenData">The token data to save</param>
        [ConsoleSector("butterBror.Utils.Tools.TwitchToken", "SaveTokenData")]
        private static void SaveTokenData(TokenData tokenData)
        {
            Engine.Statistics.FunctionsUsed.Add();
            try
            {
                FileUtil.SaveFileContent(_databasePath, JsonConvert.SerializeObject(tokenData));
            }
            catch (Exception ex)
            {
                Write(ex);
            }
        }

        /// <summary>
        /// Represents the Twitch API token response format.
        /// </summary>
        private class TokenResponse
        {
            public string access_token { get; set; }
            public int expires_in { get; set; }
            public string refresh_token { get; set; }
        }

        /// <summary>
        /// Contains Twitch authentication token data with expiration tracking.
        /// </summary>
        public class TokenData
        {
            public string AccessToken { get; set; }
            public DateTime ExpiresAt { get; set; }
            public string RefreshToken { get; set; }
        }
    }
}
