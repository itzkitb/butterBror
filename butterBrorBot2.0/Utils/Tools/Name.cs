using butterBror.Utils.DataManagers;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static butterBror.Utils.Things.Console;

namespace butterBror.Utils.Tools
{
    public class Names
    {
        /// <summary>
        /// Getting a nickname from text
        /// </summary>
        [ConsoleSector("butterBror.Utils.Tools.Names", "GetUsernameFromText")]
        public static string GetUsernameFromText(string text)
        {
            Core.Statistics.FunctionsUsed.Add();
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
        /// Get user ID by nickname
        /// </summary>
        [ConsoleSector("butterBror.Utils.Tools.Names", "GetUserID")]
        public static string GetUserID(string user, Platforms platform, bool requestAPI = false)
        {
            Core.Statistics.FunctionsUsed.Add();

            string key = user.ToLowerInvariant();
            string dir = Path.Combine(Core.Bot.Pathes.Nick2ID, Platform.strings[(int)platform]);
            string filePath = Path.Combine(dir, key + ".txt");

            try
            {
                if (FileUtil.FileExists(filePath))
                    return FileUtil.GetFileContent(filePath);

                // Twitch API
                if (platform is Platforms.Twitch && requestAPI)
                {
                    if (string.IsNullOrEmpty(Core.Bot.TwitchClientId) || string.IsNullOrEmpty(Core.Bot.Tokens.Twitch.AccessToken))
                        return null;

                    using var client = new HttpClient();
                    client.DefaultRequestHeaders.Add("Client-ID", Core.Bot.TwitchClientId);
                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", Core.Bot.Tokens.Twitch.AccessToken);

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
                Write(ex);
            }

            // Not found
            return null;
        }

        /// <summary>
        /// Get username by ID
        /// </summary>
        [ConsoleSector("butterBror.Utils.Tools.Names", "GetUsername")]
        public static string GetUsername(string ID, Platforms platform, bool requestAPI = false)
        {
            Core.Statistics.FunctionsUsed.Add();

            string dir = Path.Combine(Core.Bot.Pathes.ID2Nick, Platform.strings[(int)platform]);
            string filePath = Path.Combine(dir, ID + ".txt");

            try
            {
                if (FileUtil.FileExists(filePath))
                    return FileUtil.GetFileContent(filePath);

                // API
                if (platform is Platforms.Twitch && requestAPI)
                {
                    if (string.IsNullOrEmpty(Core.Bot.TwitchClientId) ||
                        string.IsNullOrEmpty(Core.Bot.Tokens.Twitch.AccessToken))
                    {
                        return null;
                    }

                    using var client = new HttpClient();
                    client.DefaultRequestHeaders.Add("Client-ID", Core.Bot.TwitchClientId);
                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", Core.Bot.Tokens.Twitch.AccessToken);

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
                Write(ex);
            }

            return null;
        }

        /// <summary>
        /// Add invisible characters to text to avoid pinging chatters
        /// </summary>
        [ConsoleSector("butterBror.Utils.Tools.Names", "DontPing")]
        public static string DontPing(string username)
        {
            Core.Statistics.FunctionsUsed.Add();
            return string.Join("󠀀", username);
        }
    }
}
