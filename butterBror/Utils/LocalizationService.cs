using butterBror.Data;
using butterBror.Models;
using DankDB;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TwitchLib.Client.Models;
using TwitchLib.EventSub.Core.SubscriptionTypes.Channel;
using static butterBror.Core.Bot.Console;

namespace butterBror.Utils
{
    /// <summary>
    /// Manages multilingual translations with support for default and custom overrides per channel/platform.
    /// </summary>
    public class LocalizationService
    {
        private static Dictionary<string, Dictionary<string, string>> _translations = new();
        private static Dictionary<string, Dictionary<string, Dictionary<string, string>>> _customTranslations = new();
        private static readonly Dictionary<string, Func<long, string>> _pluralRules = new Dictionary<string, Func<long, string>>
        {
            { "en-US", n => n == 1 ? "one" : "other" },
            { "ru-RU", n =>
                n % 10 == 1 && n % 100 != 11 ? "one" :
                n % 10 >= 2 && n % 10 <= 4 && (n % 100 < 10 || n % 100 >= 20) ? "few" : "many" }
        };

        public static string GetPluralString(string userLang, string key, string channelId, PlatformsEnum platform, long number, params object[] args)
        {
            try
            {
                if (!_translations.ContainsKey(userLang))
                    _translations[userLang] = LoadTranslations(userLang);

                if (!_customTranslations.ContainsKey(channelId))
                    _customTranslations[channelId] = new();

                if (!_customTranslations[channelId].ContainsKey(userLang))
                    _customTranslations[channelId][userLang] = LoadCustomTranslations(userLang, channelId, platform);

                string pluralForm = GetPluralForm(userLang, number);
                string pluralKey = $"{key}:{pluralForm}";

                var custom = _customTranslations[channelId][userLang];
                if (custom.TryGetValue(pluralKey, out var pluralCustomValue))
                {
                    return string.Format(pluralCustomValue, args);
                }

                if (_translations[userLang].TryGetValue(pluralKey, out var pluralDefaultValue))
                {
                    return string.Format(pluralDefaultValue, args);
                }

                Write($"Plural translate \"{pluralKey}\" in lang \"{userLang}\" was not found!", "info", Core.Bot.Console.LogLevel.Warning);
                return key;
            }
            catch (Exception ex)
            {
                Write(ex);
                return null;
            }
        }

        /// <summary>
        /// Determines plural form for a given number using language rules.
        /// </summary>
        private static string GetPluralForm(string fullLanguage, long number)
        {
            if (_pluralRules.TryGetValue(fullLanguage, out var rule))
            {
                return rule(number);
            }
            return "other"; // Fallback
        }

        /// <summary>
        /// Retrieves a localized translation with optional parameter replacements.
        /// </summary>
        /// <param name="userLang">Language code (e.g., "ru-RU", "en")</param>
        /// <param name="key">Translation key path (e.g., "command:ping")</param>
        /// <param name="channelId">Channel/Room identifier for custom translations</param>
        /// <param name="platform">Target platform context</param>
        /// <param name="replacements">Optional dictionary of %key% replacements</param>
        /// <returns>Localized string or key if not found</returns>
        /// <remarks>
        /// Checks in order: 
        /// 1. Custom translations for channel/platform
        /// 2. Default language translations
        /// Returns null if error occurs during retrieval
        /// </remarks>
        
        public static string GetString(string userLang, string key, string channelId, PlatformsEnum platform)
        {
            try
            {
                if (userLang == null || key == null || channelId == null || platform == null)
                    return "🚨 Something went wrong...";

                if (!_translations.ContainsKey(userLang))
                    _translations[userLang] = LoadTranslations(userLang);

                if (!_customTranslations.ContainsKey(channelId))
                    _customTranslations[channelId] = new();

                if (!_customTranslations[channelId].ContainsKey(userLang))
                    _customTranslations[channelId][userLang] = LoadCustomTranslations(userLang, channelId, platform);

                var custom = _customTranslations[channelId][userLang];
                if (custom.TryGetValue(key, out var customValue))
                {
                    return customValue;
                }

                if (_translations[userLang].TryGetValue(key, out var defaultVal))
                {
                    return defaultVal;
                }

                Write($"Translate \"{key}\" in lang \"{userLang}\" was not found!", "info", Core.Bot.Console.LogLevel.Warning);
                return key;
            }
            catch (Exception ex)
            {
                Write(ex);
                return null;
            }
        }

        /// <summary>
        /// Retrieves a localized translation with optional parameter replacements.
        /// </summary>
        /// <param name="userLang">Language code (e.g., "ru-RU", "en")</param>
        /// <param name="key">Translation key path (e.g., "command:ping")</param>
        /// <param name="channelId">Channel/Room identifier for custom translations</param>
        /// <param name="platform">Target platform context</param>
        /// <param name="replacements">Optional dictionary of %key% replacements</param>
        /// <returns>Localized string or key if not found</returns>
        /// <remarks>
        /// Checks in order: 
        /// 1. Custom translations for channel/platform
        /// 2. Default language translations
        /// Returns null if error occurs during retrieval
        /// </remarks>
        public static string GetString(string userLang, string key, string channelId, PlatformsEnum platform, params object[] args)
        {
            try
            {
                if (!_translations.ContainsKey(userLang))
                    _translations[userLang] = LoadTranslations(userLang);

                if (!_customTranslations.ContainsKey(channelId))
                    _customTranslations[channelId] = new();

                if (!_customTranslations[channelId].ContainsKey(userLang))
                    _customTranslations[channelId][userLang] = LoadCustomTranslations(userLang, channelId, platform);

                var custom = _customTranslations[channelId][userLang];
                if (custom.TryGetValue(key, out var customValue))
                {
                    if (args is not null) customValue = string.Format(customValue, args);
                    return customValue;
                }

                if (_translations[userLang].TryGetValue(key, out var defaultVal))
                {
                    if (args is not null) defaultVal = string.Format(defaultVal, args);
                    return defaultVal;
                }

                Write($"Translate \"{key}\" in lang \"{userLang}\" was not found!", "info", Core.Bot.Console.LogLevel.Warning);
                return key;
            }
            catch (Exception ex)
            {
                Write(ex);
                return null;
            }
        }

        /// <summary>
        /// Sets or updates a custom translation for a specific channel.
        /// </summary>
        /// <param name="key">Translation key to set</param>
        /// <param name="value">Translation value</param>
        /// <param name="channel">Channel identifier</param>
        /// <param name="lang">Language code</param>
        /// <param name="platform">Target platform</param>
        /// <returns>True if successful, false otherwise</returns>
        /// <remarks>
        /// - Creates necessary directories if missing
        /// - Updates both memory cache and persistent storage
        /// - Returns false if file operations fail
        /// </remarks>
        
        public static bool SetCustomTranslation(string key, string value, string channel, string lang, PlatformsEnum platform)
        {
            try
            {
                string path = $"{Engine.Bot.Pathes.TranslateCustom}{PlatformsPathName.strings[(int)platform]}/{channel}/";
                Directory.CreateDirectory(path);

                var content = FileUtil.FileExists($"{path}{lang}.json")
                    ? Manager.Get<Dictionary<string, string>>($"{path}{lang}.json", "translations")
                    : new Dictionary<string, string>();

                content[key] = value;
                _customTranslations[channel][lang] = content;

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

        /// <summary>
        /// Deletes a custom translation from channel-specific storage.
        /// </summary>
        /// <param name="key">Translation key to delete</param>
        /// <param name="channel">Channel identifier</param>
        /// <param name="lang">Language code</param>
        /// <param name="platform">Target platform</param>
        /// <returns>True if successfully deleted, false otherwise</returns>
        /// <remarks>
        /// - Only affects custom translations
        /// - Returns false if translation doesn't exist or operation fails
        /// </remarks>
        
        public static bool DeleteCustomTranslation(string key, string channel, string lang, PlatformsEnum platform)
        {
            try
            {
                string path = $"{Engine.Bot.Pathes.TranslateCustom}{PlatformsPathName.strings[(int)platform]}/{channel}/";
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

        /// <summary>
        /// Loads default translations for a specific language.
        /// </summary>
        /// <param name="userLang">Language code to load</param>
        /// <returns>Dictionary of translation keys and values</returns>
        /// <remarks>
        /// Uses Manager.Get to load from JSON files.
        /// Returns empty dictionary if file is missing or invalid.
        /// </remarks>
        
        private static Dictionary<string, string> LoadTranslations(string userLang)
        {
            return Manager.Get<Dictionary<string, string>>(
                $"{Engine.Bot.Pathes.TranslateDefault}{userLang}.json",
                "translations"
            ) ?? new Dictionary<string, string>();
        }

        /// <summary>
        /// Loads custom translations for a specific channel and language.
        /// </summary>
        /// <param name="userLang">Language code to load</param>
        /// <param name="channel">Channel identifier</param>
        /// <param name="platform">Target platform</param>
        /// <returns>Dictionary of translation keys and values</returns>
        /// <remarks>
        /// Uses Manager.Get to load from channel-specific JSON files.
        /// Returns empty dictionary if file is missing or invalid.
        /// </remarks>
        
        private static Dictionary<string, string> LoadCustomTranslations(string userLang, string channel, PlatformsEnum platform)
        {
            return Manager.Get<Dictionary<string, string>>(
                $"{Engine.Bot.Pathes.TranslateCustom}{channel}/{PlatformsPathName.strings[(int)platform]}/{userLang}.json",
                "translations"
            ) ?? new Dictionary<string, string>();
        }

        /// <summary>
        /// Checks if a Russian translation exists for a specific key.
        /// </summary>
        /// <param name="key">Translation key to check</param>
        /// <returns>True if Russian translation exists, false otherwise</returns>
        /// <remarks>
        /// Always checks against Russian ("ru") translations first.
        /// Loads Russian dictionary if not already loaded.
        /// </remarks>
        
        public static bool TranslateContains(string key)
        {
            if (!_translations.ContainsKey("ru"))
            {
                _translations["ru"] = LoadTranslations("ru");
            }

            return _translations["ru"].ContainsKey(key);
        }

        /// <summary>
        /// Forces refresh of translation dictionaries for specified language/channel.
        /// </summary>
        /// <param name="userLang">Language code to refresh</param>
        /// <param name="channel">Channel identifier</param>
        /// <param name="platform">Target platform</param>
        /// <returns>True if translations were successfully reloaded</returns>
        /// <remarks>
        /// - Clears existing translations from cache
        /// - Reloads translations from disk
        /// - Returns false if default translations fail to reload
        /// </remarks>
        
        public static bool UpdateTranslation(string userLang, string channel, PlatformsEnum platform)
        {
            try
            {
                if (_translations.ContainsKey(userLang))
                {
                    _translations[userLang].Clear();
                }
                if (_customTranslations.ContainsKey(channel))
                {
                    if (_customTranslations[channel].ContainsKey(userLang))
                    {
                        _customTranslations[channel][userLang].Clear();
                    }
                }

                _translations[userLang] = LoadTranslations(userLang);
                _customTranslations[channel][userLang] = LoadCustomTranslations(userLang, channel, platform);

                if (_translations[userLang].Count > 0)
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
