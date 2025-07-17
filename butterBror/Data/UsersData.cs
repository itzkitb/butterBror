using butterBror.Models;
using DankDB;
using Newtonsoft.Json.Linq;
using static butterBror.Core.Bot.Console;

namespace butterBror.Data
{
    /// <summary>
    /// Manages user data persistence and retrieval operations across different platforms.
    /// </summary>
    public class UsersData
    {
        private static readonly string _directory = Engine.Bot.Pathes.Users;

        /// <summary>
        /// Retrieves a user-specific value of type T from persistent storage.
        /// </summary>
        /// <typeparam name="T">The type of data to retrieve.</typeparam>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="paramName">The name of the parameter to retrieve.</param>
        /// <param name="platform">The platform (Twitch/Discord) associated with the user.</param>
        /// <returns>The retrieved value or default(T) if operation fails.</returns>
        [ConsoleSector("butterBror.Utils.DataManagers.UsersData", "Get")]
        public static T Get<T>(string userId, string paramName, PlatformsEnum platform)
        {
            Engine.Statistics.FunctionsUsed.Add();
            try
            {
                return Manager.Get<T>(GetUserFilePath(userId, platform), paramName);
            }
            catch (Exception ex)
            {
                Write(ex);
                return default;
            }
        }

        /// <summary>
        /// Saves a user-specific value to persistent storage with optional backup.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="paramName">The name of the parameter to save.</param>
        /// <param name="value">The value to persist.</param>
        /// <param name="platform">The platform (Twitch/Discord) associated with the user.</param>
        [ConsoleSector("butterBror.Utils.DataManagers.UsersData", "Save")]
        public static void Save(string userId, string paramName, object value, PlatformsEnum platform)
        {
            Engine.Statistics.FunctionsUsed.Add();
            try
            {
                string path = GetUserFilePath(userId, platform);
                if (FileUtil.FileExists(path)) FileUtil.CreateBackup(path);
                SafeManager.Save(path, paramName, value);
            }
            catch (Exception ex)
            {
                Write(ex);
            }
        }

        /// <summary>
        /// Checks if a user parameter exists in persistent storage.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="paramName">The name of the parameter to check.</param>
        /// <param name="platform">The platform (Twitch/Discord) associated with the user.</param>
        /// <returns>True if parameter exists; otherwise, false.</returns>
        [ConsoleSector("butterBror.Utils.DataManagers.UsersData", "Contains")]
        public static bool Contains(string userId, string paramName, PlatformsEnum platform)
        {
            Engine.Statistics.FunctionsUsed.Add();
            try
            {
                return Manager.Get<dynamic>(GetUserFilePath(userId, platform), paramName) is not null;
            }
            catch (Exception ex)
            {
                Write(ex);
                return false;
            }
        }

        /// <summary>
        /// Registers a new user with default values in the system.
        /// </summary>
        /// <param name="userId">The unique identifier of the user to register.</param>
        /// <param name="firstMessage">The user's initial message text.</param>
        /// <param name="platform">The platform (Twitch/Discord) where the user registered.</param>
        [ConsoleSector("butterBror.Utils.DataManagers.UsersData", "Register")]
        public static void Register(string userId, string firstMessage, PlatformsEnum platform)
        {
            Engine.Statistics.FunctionsUsed.Add();
            string path = GetUserFilePath(userId, platform);
            SafeManager.Save(path, "firstSeen", DateTime.UtcNow, false);
            SafeManager.Save(path, "firstMessage", firstMessage, false);
            SafeManager.Save(path, "lastSeenMessage", firstMessage, false);
            SafeManager.Save(path, "lastSeen", DateTime.UtcNow, false);
            SafeManager.Save(path, "floatBalance", 0, false);
            SafeManager.Save(path, "balance", 0, false);
            SafeManager.Save(path, "totalMessages", 0, false);
            SafeManager.Save(path, "miningVideocards", new JArray(), false);
            SafeManager.Save(path, "miningProcessors", new JArray(), false);
            SafeManager.Save(path, "lastMiningClear", DateTime.UtcNow, false);
            SafeManager.Save(path, "isBotModerator", false, false);
            SafeManager.Save(path, "isBanned", false, false);
            SafeManager.Save(path, "isIgnored", false, false);
            SafeManager.Save(path, "rating", 500, false);
            SafeManager.Save(path, "inventory", new JArray(), false);
            SafeManager.Save(path, "warningLvl", 3, false);
            SafeManager.Save(path, "isVip", false, false);
            SafeManager.Save(path, "isAfk", false, false);
            SafeManager.Save(path, "afkText", string.Empty, false);
            SafeManager.Save(path, "afkType", string.Empty, false);
            SafeManager.Save(path, "reminders", new JObject(), false);
            SafeManager.Save(path, "lastCookieEat", DateTime.UtcNow.AddDays(-1), false);
            SafeManager.Save(path, "giftedCookies", 0, false);
            SafeManager.Save(path, "eatedCookies", 0, false);
            SafeManager.Save(path, "buyedCookies", 0, false);
            SafeManager.Save(path, "userPlace", string.Empty, false);
            SafeManager.Save(path, "userLon", "0", false);
            SafeManager.Save(path, "userLat", "0", false);
            SafeManager.Save(path, "language", "ru", false);
            SafeManager.Save(path, "afkTime", DateTime.UtcNow);
        }

        /// <summary>
        /// Constructs the file path for a user's data file based on platform.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="platform">The platform (Twitch/Discord) associated with the user.</param>
        /// <returns>The full file path for the user's data file.</returns>
        [ConsoleSector("butterBror.Utils.DataManagers.UsersData", "GetUserFilePath")]
        private static string GetUserFilePath(string userId, PlatformsEnum platform)
        {
            Engine.Statistics.FunctionsUsed.Add();
            return Path.Combine(_directory, $"{PlatformsPathName.strings[(int)platform]}/{userId}.json");
        }
    }
}
