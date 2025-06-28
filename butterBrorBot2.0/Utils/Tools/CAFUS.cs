using butterBror.Utils.DataManagers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static butterBror.Utils.Things.Console;

namespace butterBror.Utils.Tools
{
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

        [ConsoleSector("butterBror.Utils.Tools.CAFUS", "Maintrance")]
        public void Maintrance(string userId, string username, Platforms platform)
        {
            Core.Statistics.FunctionsUsed.Add();

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

        [ConsoleSector("butterBror.Utils.Tools.CAFUS", "Migrate0")]
        private static void Migrate0(string uid, Platforms p)
        {
            Core.Statistics.FunctionsUsed.Add();
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

        [ConsoleSector("butterBror.Utils.Tools.CAFUS", "Migrate1")]
        private static void Migrate1(string uid, Platforms p)
        {
            Core.Statistics.FunctionsUsed.Add();
            SaveIfMissing(uid, "isBotDev", false, p);
        }

        [ConsoleSector("butterBror.Utils.Tools.CAFUS", "Migrate2")]
        private static void Migrate2(string uid, Platforms p)
        {
            Core.Statistics.FunctionsUsed.Add();
            SaveIfMissing(uid, "banReason", "", p);
            SaveIfMissing(uid, "weatherAPIUsedTimes", 0, p);
            SaveIfMissing(uid, "weatherAPIResetDate", DateTime.UtcNow.AddDays(1), p);
        }

        [ConsoleSector("butterBror.Utils.Tools.CAFUS", "Migrate3")]
        private static void Migrate3(string uid, Platforms p)
        {
            Core.Statistics.FunctionsUsed.Add();
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

        [ConsoleSector("butterBror.Utils.Tools.CAFUS", "Migrate4")]
        private static void Migrate4(string uid, Platforms p)
        {
            Core.Statistics.FunctionsUsed.Add();
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

        [ConsoleSector("butterBror.Utils.Tools.CAFUS", "SaveIfMissing")]
        private static void SaveIfMissing(string uid, string key, object value, Platforms p)
        {
            Core.Statistics.FunctionsUsed.Add();
            if (!UsersData.Contains(key, uid, p))
                UsersData.Save(uid, key, value, p);
        }

        [ConsoleSector("butterBror.Utils.Tools.CAFUS", "SaveDefaults")]
        private static void SaveDefaults(string uid, Platforms p, Dictionary<string, object> defaults)
        {
            foreach (var kv in defaults)
                SaveIfMissing(uid, kv.Key, kv.Value, p);
        }
    }
}
