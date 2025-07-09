using butterBror.Utils.DataManagers;
using butterBror.Utils.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static butterBror.Utils.Bot.Console;

namespace butterBror.Utils.Tools
{
    /// <summary>
    /// Manages user data migrations and version tracking for user settings and preferences.
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

        /// <summary>
        /// Applies necessary data migrations to user data based on current version.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="username">The username of the user (used in log output).</param>
        /// <param name="platform">The platform context for the user data.</param>
        /// <remarks>
        /// Tracks applied migrations in _updated list and updates CAFUSV version after each successful migration.
        /// Logs migration progress and applied versions.
        /// </remarks>
        [ConsoleSector("butterBror.Utils.Tools.CAFUS", "Maintrance")]
        public void Maintrance(string userId, string username, Platforms platform)
        {
            Engine.Statistics.FunctionsUsed.Add();

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
                    Write($"@{username} CAFUS {string.Join(", ", _updated)} UPDATED", "cafus");
            }
            catch (Exception ex)
            {
                Write(ex);
            }
        }

        /// <summary>
        /// Migration handler for version 1.0 - Sets initial default user settings.
        /// </summary>
        /// <param name="uid">User ID for migration target.</param>
        /// <param name="p">Platform context for migration.</param>
        [ConsoleSector("butterBror.Utils.Tools.CAFUS", "Migrate0")]
        private static void Migrate0(string uid, Platforms p)
        {
            Engine.Statistics.FunctionsUsed.Add();
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

        /// <summary>
        /// Migration handler for version 1.1 - Adds bot developer status field.
        /// </summary>
        /// <param name="uid">User ID for migration target.</param>
        /// <param name="p">Platform context for migration.</param>
        [ConsoleSector("butterBror.Utils.Tools.CAFUS", "Migrate1")]
        private static void Migrate1(string uid, Platforms p)
        {
            Engine.Statistics.FunctionsUsed.Add();
            SaveIfMissing(uid, "isBotDev", false, p);
        }

        /// <summary>
        /// Migration handler for version 1.2 - Adds ban reason tracking and weather API usage limits.
        /// </summary>
        /// <param name="uid">User ID for migration target.</param>
        /// <param name="p">Platform context for migration.</param>
        [ConsoleSector("butterBror.Utils.Tools.CAFUS", "Migrate2")]
        private static void Migrate2(string uid, Platforms p)
        {
            Engine.Statistics.FunctionsUsed.Add();
            SaveIfMissing(uid, "banReason", "", p);
            SaveIfMissing(uid, "weatherAPIUsedTimes", 0, p);
            SaveIfMissing(uid, "weatherAPIResetDate", DateTime.UtcNow.AddDays(1), p);
        }

        /// <summary>
        /// Migration handler for version 1.3 - Adds fishing-related user preferences.
        /// </summary>
        /// <param name="uid">User ID for migration target.</param>
        /// <param name="p">Platform context for migration.</param>
        [ConsoleSector("butterBror.Utils.Tools.CAFUS", "Migrate3")]
        private static void Migrate3(string uid, Platforms p)
        {
            Engine.Statistics.FunctionsUsed.Add();
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

        /// <summary>
        /// Migration handler for version 1.4 - Initializes fish inventory with default items.
        /// </summary>
        /// <param name="uid">User ID for migration target.</param>
        /// <param name="p">Platform context for migration.</param>
        [ConsoleSector("butterBror.Utils.Tools.CAFUS", "Migrate4")]
        private static void Migrate4(string uid, Platforms p)
        {
            Engine.Statistics.FunctionsUsed.Add();
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

        /// <summary>
        /// Saves a value only if it doesn't already exist in user data.
        /// </summary>
        /// <param name="uid">User ID for target data.</param>
        /// <param name="key">Configuration key to check/set.</param>
        /// <param name="value">Default value to set if missing.</param>
        /// <param name="p">Platform context for user data.</param>
        [ConsoleSector("butterBror.Utils.Tools.CAFUS", "SaveIfMissing")]
        private static void SaveIfMissing(string uid, string key, object value, Platforms p)
        {
            Engine.Statistics.FunctionsUsed.Add();
            if (!UsersData.Contains(key, uid, p))
                UsersData.Save(uid, key, value, p);
        }

        /// <summary>
        /// Applies multiple default values to user data if missing.
        /// </summary>
        /// <param name="uid">User ID for target data.</param>
        /// <param name="p">Platform context for user data.</param>
        /// <param name="defaults">Dictionary of key-value pairs to apply as defaults.</param>
        [ConsoleSector("butterBror.Utils.Tools.CAFUS", "SaveDefaults")]
        private static void SaveDefaults(string uid, Platforms p, Dictionary<string, object> defaults)
        {
            foreach (var kv in defaults)
                SaveIfMissing(uid, kv.Key, kv.Value, p);
        }
    }
}
