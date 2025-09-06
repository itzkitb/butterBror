using bb.Data;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Web;
using static bb.Core.Bot.Console;

namespace bb.Core.Bot
{
    /// <summary>
    /// Manages Twitch OAuth2 token lifecycle including acquisition, validation, refresh, and persistent storage.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item>Implements Authorization Code flow with PKCE (Proof Key for Code Exchange)</item>
    /// <item>Automatically handles token expiration and refresh</item>
    /// <item>Provides local persistence for token data</item>
    /// <item>Includes built-in HTTP server for authorization callback handling</item>
    /// </list>
    /// The class follows Twitch API authentication best practices and handles both initial authorization
    /// and token refresh operations transparently for the application.
    /// </remarks>
    public class TwitchToken
    {
        private static string _clientId;
        private static string _clientSecret;
        private static string _redirectURL;
        private static string _databasePath;

        /// <summary>
        /// Initializes a new instance of the TwitchToken class with application credentials.
        /// </summary>
        /// <param name="clientId">Twitch application client ID obtained from Twitch Developer Console</param>
        /// <param name="clientSecret">Twitch application client secret (keep confidential)</param>
        /// <param name="databasePath">File path for persistent token storage (typically JSON format)</param>
        /// <remarks>
        /// Configures the authorization flow with localhost redirect for secure token handling.
        /// The redirect URL is fixed to http://localhost:12121/ and must be registered in Twitch Developer Console.
        /// </remarks>
        public TwitchToken(string clientId, string clientSecret, string databasePath)
        {
            _clientId = clientId;
            _clientSecret = clientSecret;
            _redirectURL = "http://localhost:12121/";
            _databasePath = databasePath;
        }

        /// <summary>
        /// Asynchronously retrieves a valid Twitch API token, handling both initial authorization and refresh operations.
        /// </summary>
        /// <returns>
        /// A valid <see cref="TokenData"/> object with active access token, or null if authorization fails.
        /// Returns cached token if valid, initiates refresh flow for expired tokens, or starts full authorization when needed.
        /// </returns>
        /// <remarks>
        /// <list type="bullet">
        /// <item>First checks for valid cached token</item>
        /// <item>Attempts refresh using refresh token if available</item>
        /// <item>Initiates full authorization flow when no valid token exists</item>
        /// <item>Handles all token lifecycle management transparently</item>
        /// </list>
        /// This is the primary entry point for obtaining a valid Twitch API token.
        /// </remarks>
        public static async Task<TokenData> GetTokenAsync()
        {
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
        /// Initiates the Twitch OAuth2 authorization flow to obtain a new access token.
        /// </summary>
        /// <returns>
        /// A new <see cref="TokenData"/> object containing access token, refresh token, and expiration time,
        /// or null if authorization fails.
        /// </returns>
        /// <remarks>
        /// <list type="bullet">
        /// <item>Launches system default browser for user authentication</item>
        /// <item>Sets up local HTTP listener on port 12121 to capture authorization code</item>
        /// <item>Exchanges authorization code for access token</item>
        /// <item>Provides user-friendly completion page after authorization</item>
        /// </list>
        /// Requires the following Twitch scopes:
        /// <c>user:manage:chat_color, chat:edit, chat:read, moderator:read:chatters, user:manage:blocked_users</c>
        /// </remarks>
        private static async Task<TokenData> PerformAuthorizationFlow()
        {
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
        /// Refreshes an expired access token using its refresh token.
        /// </summary>
        /// <param name="token">The existing token data containing a valid refresh token</param>
        /// <returns>
        /// An updated <see cref="TokenData"/> object with new access token and expiration time,
        /// or null if refresh fails.
        /// </returns>
        /// <remarks>
        /// <list type="bullet">
        /// <item>Uses Twitch's token refresh endpoint (id.twitch.tv/oauth2/token)</item>
        /// <item>Updates expiration time based on new token's lifetime (typically 60 days)</item>
        /// <item>Preserves the same refresh token unless Twitch issues a new one</item>
        /// <item>Automatically falls back to full authorization if refresh fails</item>
        /// </list>
        /// This method should be called when API requests return 401 Unauthorized errors.
        /// </remarks>
        public static async Task<TokenData> RefreshAccessToken(TokenData token)
        {
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
        /// Retrieves the authorization code from Twitch's OAuth2 response via local HTTP server.
        /// </summary>
        /// <param name="listener">Configured HttpListener waiting for the authorization callback</param>
        /// <returns>The authorization code string, or null if extraction fails</returns>
        /// <remarks>
        /// <list type="bullet">
        /// <item>Opens user's default browser to Twitch authorization page</item>
        /// <item>Waits for the redirect response containing the authorization code</item>
        /// <item>Parses the query string to extract the 'code' parameter</item>
        /// <item>Closes the HTTP listener after successful code retrieval</item>
        /// </list>
        /// The authorization code is valid for a short period (typically 10 minutes).
        /// </remarks>
        private static async Task<string> GetAuthorizationCodeAsync(HttpListener listener)
        {
            try
            {
                Write("Twitch oauth - Getting auth data...", "info");
                var url = $"https://id.twitch.tv/oauth2/authorize?client_id={_clientId}&redirect_uri={_redirectURL}&response_type=code&scope=user:manage:chat_color+chat:edit+chat:read+moderator:read:chatters+user:manage:blocked_users";
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
        /// Exchanges an authorization code for a complete access token and refresh token.
        /// </summary>
        /// <param name="code">The authorization code obtained from GetAuthorizationCodeAsync</param>
        /// <returns>
        /// A populated <see cref="TokenData"/> object, or null if exchange fails.
        /// </returns>
        /// <remarks>
        /// <list type="bullet">
        /// <item>Sends POST request to Twitch token endpoint with authorization code</item>
        /// <item>Includes client credentials and redirect URI for validation</item>
        /// <item>Parses JSON response containing access token and refresh token</item>
        /// <item>Calculates token expiration time based on expires_in value</item>
        /// </list>
        /// This is the final step in the OAuth2 authorization flow.
        /// </remarks>
        private static async Task<TokenData> ExchangeCodeForTokenAsync(string code)
        {
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
        /// Extracts the authorization code from the HTTP query string in the redirect response.
        /// </summary>
        /// <param name="query">The query string portion of the redirect URL (starts with ?)</param>
        /// <returns>The authorization code value, or null if not present</returns>
        /// <remarks>
        /// Parses the query string using HttpUtility.ParseQueryString for proper URL decoding.
        /// Expected format: ?code=AUTHORIZATION_CODE&amp;scope=REQUESTED_SCOPES
        /// </remarks>
        private static string GetCodeFromResponse(string query)
        {
            var queryParams = HttpUtility.ParseQueryString(query);
            return queryParams["code"];
        }

        /// <summary>
        /// Loads stored token data from persistent storage.
        /// </summary>
        /// <returns>
        /// The deserialized <see cref="TokenData"/> object if valid and not expired,
        /// or null if loading fails or token is expired.
        /// </returns>
        /// <remarks>
        /// <list type="bullet">
        /// <item>Checks if token database file exists</item>
        /// <item>Reads and deserializes JSON content</item>
        /// <item>Validates token expiration time</item>
        /// <item>Returns null for expired tokens to trigger refresh flow</item>
        /// </list>
        /// This method handles the persistence layer for token data between application restarts.
        /// </remarks>
        private static TokenData LoadTokenData()
        {
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
        /// Saves token data to persistent storage for future use.
        /// </summary>
        /// <param name="tokenData">The token data to persist, including access token, refresh token, and expiration</param>
        /// <remarks>
        /// <list type="bullet">
        /// <item>Serializes token data to JSON format</item>
        /// <item>Writes to configured database path</item>
        /// <item>Handles file I/O exceptions gracefully</item>
        /// <item>Ensures sensitive token data is stored securely</item>
        /// </list>
        /// This method is called after successful token acquisition or refresh.
        /// </remarks>
        private static void SaveTokenData(TokenData tokenData)
        {
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
        /// Represents the response structure from Twitch's token endpoint.
        /// </summary>
        /// <remarks>
        /// Maps directly to Twitch API's JSON response format for token operations.
        /// Used internally for deserialization of token exchange responses.
        /// </remarks>
        private class TokenResponse
        {
            public string access_token { get; set; }
            public int expires_in { get; set; }
            public string refresh_token { get; set; }
        }

        /// <summary>
        /// Contains Twitch authentication token data with expiration tracking for application use.
        /// </summary>
        public class TokenData
        {
            public string AccessToken { get; set; }
            public DateTime ExpiresAt { get; set; }
            public string RefreshToken { get; set; }
        }
    }
}
