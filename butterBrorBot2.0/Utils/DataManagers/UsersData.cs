using butterBror.Utils.DataManagers;
using DankDB;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static butterBror.Utils.Things.Console;

namespace butterBror.Utils.DataManagers
{
    public class UsersData
    {
        private static readonly string directory = Core.Bot.Pathes.Users;

        [ConsoleSector("butterBror.Utils.DataManagers.UsersData", "Get")]
        public static T Get<T>(string userId, string paramName, Platforms platform)
        {
            Core.Statistics.FunctionsUsed.Add();
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

        [ConsoleSector("butterBror.Utils.DataManagers.UsersData", "Save")]
        public static void Save(string userId, string paramName, object value, Platforms platform)
        {
            Core.Statistics.FunctionsUsed.Add();
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

        [ConsoleSector("butterBror.Utils.DataManagers.UsersData", "Contains")]
        public static bool Contains(string userId, string paramName, Platforms platform)
        {
            Core.Statistics.FunctionsUsed.Add();
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

        [ConsoleSector("butterBror.Utils.DataManagers.UsersData", "Register")]
        public static void Register(string userId, string firstMessage, Platforms platform)
        {
            Core.Statistics.FunctionsUsed.Add();
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

        [ConsoleSector("butterBror.Utils.DataManagers.UsersData", "GetUserFilePath")]
        private static string GetUserFilePath(string userId, Platforms platform)
        {
            Core.Statistics.FunctionsUsed.Add();
            return Path.Combine(directory, $"{Platform.strings[(int)platform]}/{userId}.json");
        }
    }
}
