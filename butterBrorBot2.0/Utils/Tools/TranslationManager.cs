using butterBror.Utils.DataManagers;
using butterBror.Utils;
using DankDB;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static butterBror.Utils.Things.Console;

namespace butterBror.Utils.Tools
{
    public class TranslationManager
    {
        private static Dictionary<string, Dictionary<string, string>> translations = new();
        private static Dictionary<string, Dictionary<string, Dictionary<string, string>>> customTranslations = new();

        [ConsoleSector("butterBror.Utils.Tools.TranslationManager", "GetTranslation")]
        public static string GetTranslation(string userLang, string key, string channel_id, Platforms platform, Dictionary<string, string> replacements = null)
        {
            Core.Statistics.FunctionsUsed.Add();
            try
            {
                if (!translations.ContainsKey(userLang))
                    translations[userLang] = LoadTranslations(userLang);

                if (!customTranslations.ContainsKey(channel_id))
                    customTranslations[channel_id] = new();

                if (!customTranslations[channel_id].ContainsKey(userLang))
                    customTranslations[channel_id][userLang] = LoadCustomTranslations(userLang, channel_id, platform);

                var custom = customTranslations[channel_id][userLang];
                if (custom.TryGetValue(key, out var customValue))
                {
                    if (replacements is not null) customValue = Text.ArgumentsReplacement(customValue, replacements);
                    return customValue;
                }

                if (translations[userLang].TryGetValue(key, out var defaultVal))
                {
                    if (replacements is not null) defaultVal = Text.ArgumentsReplacement(defaultVal, replacements);
                    return defaultVal;
                }

                Write($"Translate \"{key}\" in lang \"{userLang}\" was not found!", "info", LogLevel.Warning);
                return key;
            }
            catch (Exception ex)
            {
                Write(ex);
                return null;
            }
        }

        [ConsoleSector("butterBror.Utils.Tools.TranslationManager", "SetCustomTranslation")]
        public static bool SetCustomTranslation(string key, string value, string channel, string lang, Platforms platform)
        {
            Core.Statistics.FunctionsUsed.Add();
            try
            {
                string path = $"{Core.Bot.Pathes.TranslateCustom}{Platform.strings[(int)platform]}/{channel}/";
                Directory.CreateDirectory(path);

                var content = FileUtil.FileExists($"{path}{lang}.json")
                    ? Manager.Get<Dictionary<string, string>>($"{path}{lang}.json", "translations")
                    : new Dictionary<string, string>();

                content[key] = value;
                customTranslations[channel][lang] = content;

                FileUtil.SaveFileContent(
                    $"{path}{lang}.json",
                    JsonConvert.SerializeObject(new { translations = content }, Formatting.Indented)
                );
                return true;
            }
            catch (Exception ex)
            {
                Write(ex);
                return false;
            }
        }

        [ConsoleSector("butterBror.Utils.Tools.TranslationManager", "DeleteCustomTranslation")]
        public static bool DeleteCustomTranslation(string key, string channel, string lang, Platforms platform)
        {
            Core.Statistics.FunctionsUsed.Add();
            try
            {
                string path = $"{Core.Bot.Pathes.TranslateCustom}{Platform.strings[(int)platform]}/{channel}/";
                if (!Directory.Exists(path)) return false;

                var content = Manager.Get<Dictionary<string, string>>($"{path}{lang}.json", "translations");
                if (content == null || !content.ContainsKey(key)) return false;

                content.Remove(key);
                FileUtil.SaveFileContent(
                    $"{path}{lang}.json",
                    JsonConvert.SerializeObject(new { translations = content }, Formatting.Indented)
                );
                return true;
            }
            catch (Exception ex)
            {
                Write(ex);
                return false;
            }
        }

        [ConsoleSector("butterBror.Utils.Tools.TranslationManager", "LoadTranslations")]
        private static Dictionary<string, string> LoadTranslations(string userLang)
        {
            Core.Statistics.FunctionsUsed.Add();
            return Manager.Get<Dictionary<string, string>>(
                $"{Core.Bot.Pathes.TranslateDefault}{userLang}.json",
                "translations"
            ) ?? new Dictionary<string, string>();
        }

        [ConsoleSector("butterBror.Utils.Tools.TranslationManager", "LoadCustomTranslations")]
        private static Dictionary<string, string> LoadCustomTranslations(string userLang, string channel, Platforms platform)
        {
            Core.Statistics.FunctionsUsed.Add();
            return Manager.Get<Dictionary<string, string>>(
                $"{Core.Bot.Pathes.TranslateCustom}{channel}/{Platform.strings[(int)platform]}/{userLang}.json",
                "translations"
            ) ?? new Dictionary<string, string>();
        }

        [ConsoleSector("butterBror.Utils.Tools.TranslationManager", "TranslateContains")]
        public static bool TranslateContains(string key)
        {
            Core.Statistics.FunctionsUsed.Add();
            if (!translations.ContainsKey("ru"))
            {
                translations["ru"] = LoadTranslations("ru");
            }

            return translations["ru"].ContainsKey(key);
        }

        [ConsoleSector("butterBror.Utils.Tools.TranslationManager", "UpdateTranslation")]
        public static bool UpdateTranslation(string userLang, string channel, Platforms platform)
        {
            Core.Statistics.FunctionsUsed.Add();
            try
            {
                if (translations.ContainsKey(userLang))
                {
                    translations[userLang].Clear();
                }
                if (customTranslations.ContainsKey(channel))
                {
                    if (customTranslations[channel].ContainsKey(userLang))
                    {
                        customTranslations[channel][userLang].Clear();
                    }
                }

                translations[userLang] = LoadTranslations(userLang);
                customTranslations[channel][userLang] = LoadCustomTranslations(userLang, channel, platform);

                if (translations[userLang].Count > 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                Write(ex);
                return false;
            }
        }
    }
}
