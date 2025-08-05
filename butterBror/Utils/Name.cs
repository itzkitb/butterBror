using butterBror.Data;
using butterBror.Models;
using Microsoft.VisualStudio.Services.Organization.Client;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using static butterBror.Core.Bot.Console;

namespace butterBror.Utils
{
    /// <summary>
    /// Provides functionality for username and user ID lookups across platforms with caching capabilities.
    /// </summary>
    public class Names
    {
        /// <summary>
        /// Extracts the first mentioned username from text containing @mentions.
        /// </summary>
        /// <param name="text">The input text containing potential @mentions.</param>
        /// <returns>
        /// The first mentioned username with @ prefix, or empty string if no mention found.
        /// Returns null if an exception occurs during processing.
        /// </returns>
        /// <exception cref="Exception">All exceptions during execution are caught and logged internally.</exception>
        /// <remarks>
        /// Uses regex pattern "@(\w+)" to identify mentions.
        /// Returns empty string for text without any @mentions.
        /// </remarks>
        
        public static string GetUsernameFromText(string text)
        {
            Engine.Statistics.FunctionsUsed.Add();
            try
            {
                if (!text.Contains('@'))
                    return string.Empty;

                MatchCollection matches = Regex.Matches(text, @"@(\w+)");
                return " @" + matches.ElementAt(0).ToString().Replace("@", "");
            }
            catch (Exception ex)
            {
                Write(ex);
                return null;
            }
        }

        /// <summary>
        /// Retrieves user ID for a given username with platform-specific caching and API fallback.
        /// </summary>
        /// <param name="user">The username to look up.</param>
        /// <param name="platform">The target platform (Twitch/Discord/Telegram).</param>
        /// <param name="requestAPI">Flag indicating whether to use API lookup if cache is empty.</param>
        /// <returns>User ID as string, or null if not found.</returns>
        /// <remarks>
        /// - First checks local cache files for ID
        /// - For Twitch, uses Twitch API with Helix endpoint if requestAPI is true
        /// - Caches successful API results for future lookups
        /// - Handles empty/mismatched cache directories automatically
        /// </remarks>
        
        public static string GetUserID(string user, PlatformsEnum platform, bool requestAPI = false)
        {
            Engine.Statistics.FunctionsUsed.Add();

            string key = user.ToLowerInvariant();

            try
            {
                if (Engine.Bot.SQL.Users.GetUserIdByUsername(platform, key) is not null)
                    return Engine.Bot.SQL.Users.GetUserIdByUsername(platform, key).ToString();

                // Twitch API
                if (platform is PlatformsEnum.Twitch && requestAPI)
                {
                    if (string.IsNullOrEmpty(Engine.Bot.TwitchClientId) || string.IsNullOrEmpty(Engine.Bot.Tokens.Twitch.AccessToken))
                        return null;

                    using var client = new HttpClient();
                    client.DefaultRequestHeaders.Add("Client-ID", Engine.Bot.TwitchClientId);
                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", Engine.Bot.Tokens.Twitch.AccessToken);

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
                            Engine.Bot.SQL.Users.AddUsernameMapping(platform, Format.ToLong(id), key);
                            return id;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Write(ex);
            }

            // Not found
            return null;
        }

        /// <summary>
        /// Retrieves username for a given user ID with platform-specific caching and API fallback.
        /// </summary>
        /// <param name="ID">The user ID to look up.</param>
        /// <param name="platform">The target platform (Twitch/Discord/Telegram).</param>
        /// <param name="requestAPI">Flag indicating whether to use API lookup if cache is empty.</param>
        /// <returns>Username as string, or null if not found.</returns>
        /// <remarks>
        /// - First checks local cache files for username
        /// - For Twitch, uses Twitch API with Helix endpoint if requestAPI is true
        /// - Caches successful API results for future lookups
        /// - Handles empty/mismatched cache directories automatically
        /// </remarks>
        
        public static string GetUsername(string ID, PlatformsEnum platform, bool requestAPI = false)
        {
            Engine.Statistics.FunctionsUsed.Add();

            try
            {
                if (Engine.Bot.SQL.Users.GetUsernameByUserId(platform, Format.ToLong(ID)) is not null)
                    return Engine.Bot.SQL.Users.GetUsernameByUserId(platform, Format.ToLong(ID));

                // API
                if (platform is PlatformsEnum.Twitch && requestAPI)
                {
                    if (string.IsNullOrEmpty(Engine.Bot.TwitchClientId) ||
                        string.IsNullOrEmpty(Engine.Bot.Tokens.Twitch.AccessToken))
                    {
                        return null;
                    }

                    using var client = new HttpClient();
                    client.DefaultRequestHeaders.Add("Client-ID", Engine.Bot.TwitchClientId);
                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", Engine.Bot.Tokens.Twitch.AccessToken);

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
                            Engine.Bot.SQL.Users.AddUsernameMapping(platform, Format.ToLong(ID), login);
                            return login;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Write(ex);
            }

            return null;
        }

        /// <summary>
        /// Modifies a username to prevent accidental @mentions in chat messages.
        /// </summary>
        /// <param name="username">The original username to protect.</param>
        /// <returns>
        /// Modified username with invisible characters inserted between each character.
        /// Example: "User" → "U󠀀s󠀀e󠀀r"
        /// </returns>
        /// <remarks>
        /// Uses U+FF80 (Private Use Area) invisible character to break mention detection
        /// Useful for displaying usernames in chat without triggering notifications
        /// Preserves original username display while preventing accidental @mentions
        /// </remarks>
        
        public static string DontPing(string username)
        {
            Engine.Statistics.FunctionsUsed.Add();
            return string.Join("󠀀", username);
        }
    }
}
