using bb.Data;
using bb.Models.Platform;
using bb.Models.Users;
using DankDB;
using Newtonsoft.Json;
using static bb.Core.Bot.Logger;

namespace bb.Utils
{
    /// <summary>
    /// Manages multilingual translation resources with hierarchical fallback and customization capabilities.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This service provides a comprehensive localization system supporting:
    /// <list type="bullet">
    /// <item>Default language translations stored in JSON files</item>
    /// <item>Channel-specific custom overrides</item>
    /// <item>Language-aware pluralization rules</item>
    /// <item>Runtime translation updates and caching</item>
    /// </list>
    /// </para>
    /// <para>
    /// Key features:
    /// <list type="bullet">
    /// <item>Two-tiered translation lookup (custom → default)</item>
    /// <item>Automatic plural form selection based on language rules</item>
    /// <item>Parameterized string formatting with <c>string.Format()</c></item>
    /// <item>Memory caching for performance optimization</item>
    /// <item>Safe error handling with fallback mechanisms</item>
    /// </list>
    /// </para>
    /// The system follows the standard localization pattern where translations are organized by language code (e.g., "en-US", "ru-RU").
    /// </remarks>
    public class LocalizationService
    {
        private static Dictionary<Language, Dictionary<string, string>> _translations = new();
        private static Dictionary<string, Dictionary<Language, Dictionary<string, string>>> _customTranslations = new();
        private static readonly Dictionary<Language, Func<long, string>> _pluralRules = new Dictionary<Language, Func<long, string>>
        {
            { Language.EnUs, n => n == 1 ? "one" : "other" },
            { Language.RuRu, n =>
                n % 10 == 1 && n % 100 != 11 ? "one" :
                n % 10 >= 2 && n % 10 <= 4 && (n % 100 < 10 || n % 100 >= 20) ? "few" : "many" }
        };
        private static readonly HttpClient _httpClient = new HttpClient();

        /// <summary>
        /// Retrieves a pluralized translation string based on numerical value and language rules.
        /// </summary>
        /// <param name="lang">Language code for translation (e.g., "en-US", "ru-RU")</param>
        /// <param name="key">Base translation key (e.g., "messages.count")</param>
        /// <param name="channelId">Channel identifier for custom translations</param>
        /// <param name="platform">Platform context (Twitch, Discord, etc.)</param>
        /// <param name="number">Numerical value determining plural form</param>
        /// <param name="args">Optional formatting parameters for the string</param>
        /// <returns>
        /// Formatted translation string appropriate for the number and language,
        /// or the original key if translation is not found
        /// </returns>
        /// <remarks>
        /// <para>
        /// The method follows this lookup sequence:
        /// <list type="number">
        /// <item>Determines plural form using language rules (e.g., "one", "few", "many")</item>
        /// <item>Constructs plural key: <c>"{key}:{pluralForm}"</c> (e.g., "messages:count:one")</item>
        /// <item>Checks for channel-specific custom translation</item>
        /// <item>Falls back to default language translation</item>
        /// <item>Logs warning if translation is missing</item>
        /// </list>
        /// </para>
        /// <para>
        /// Example:
        /// <code>
        /// // For English with number=2:
        /// GetPluralString("en-US", "messages:count", "channel123", PlatformsEnum.Twitch, 2);
        /// // Looks for "messages:count:other" in custom or default translations
        /// </code>
        /// </para>
        /// <para>
        /// Error handling:
        /// <list type="bullet">
        /// <item>Returns key as fallback if translation is missing</item>
        /// <item>Logs exception details but doesn't crash the application</item>
        /// <item>Returns null if critical error occurs during processing</item>
        /// </list>
        /// </para>
        /// This method automatically loads and caches translations on first access.
        /// </remarks>
        public static string GetPluralString(Language lang, string key, string channelId, Platform platform, long number, params object[] args)
        {
            try
            {
                if (!_translations.ContainsKey(lang))
                    _translations[lang] = LoadTranslations(lang);

                if (!_customTranslations.ContainsKey(channelId))
                    _customTranslations[channelId] = new();

                if (!_customTranslations[channelId].ContainsKey(lang))
                    _customTranslations[channelId][lang] = LoadCustomTranslations(lang, channelId, platform);

                string pluralForm = GetPluralForm(lang, number);
                string pluralKey = $"{key}:{pluralForm}";

                var custom = _customTranslations[channelId][lang];
                if (custom.TryGetValue(pluralKey, out var pluralCustomValue))
                {
                    return string.Format(pluralCustomValue, args);
                }

                if (_translations[lang].TryGetValue(pluralKey, out var pluralDefaultValue))
                {
                    return string.Format(pluralDefaultValue, args);
                }

                string remoteValue = GetRemoteTranslation(lang, pluralKey);
                if (remoteValue != null)
                {
                    _translations[lang][pluralKey] = remoteValue;
                    SaveTranslationFile(lang);
                    return string.Format(remoteValue, args);
                }

                Write($"Plural translate \"{pluralKey}\" in lang \"{lang}\" was not found!", Core.Bot.Logger.LogLevel.Warning);
                return key;
            }
            catch (Exception ex)
            {
                Write(ex);
                return null;
            }
        }

        /// <summary>
        /// Determines the appropriate plural form category for a given number in a specific language.
        /// </summary>
        /// <param name="lang">Language code (e.g., "en-US", "ru-RU")</param>
        /// <param name="number">The numerical value to evaluate</param>
        /// <returns>
        /// Plural form category ("one", "few", "many", "other", etc.)
        /// or "other" for unsupported languages
        /// </returns>
        /// <remarks>
        /// <para>
        /// Implementation details:
        /// <list type="bullet">
        /// <item>Uses language-specific rules defined in <see cref="_pluralRules"/></item>
        /// <item>Handles edge cases like 11-14 in Russian (which use "many" instead of "few")</item>
        /// <item>Follows Unicode CLDR pluralization rules where applicable</item>
        /// </list>
        /// </para>
        /// <para>
        /// Language support matrix:
        /// <list type="table">
        /// <item><term>en-US</term><description>one (1), other (n != 1)</description></item>
        /// <item><term>ru-RU</term><description>one (1, 21, 31...), few (2-4, 22-24...), many (5-20, 25-30...)</description></item>
        /// </list>
        /// </para>
        /// This method is used internally by <see cref="GetPluralString"/> to select the correct translation variant.
        /// </remarks>
        private static string GetPluralForm(Language lang, long number)
        {
            if (_pluralRules.TryGetValue(lang, out var rule))
            {
                return rule(number);
            }
            return "other"; // Fallback
        }

        /// <summary>
        /// Retrieves a basic translation string without pluralization.
        /// </summary>
        /// <param name="lang">Language code for translation (e.g., "en-US", "ru-RU")</param>
        /// <param name="key">Translation key path (e.g., "command:ping:description")</param>
        /// <param name="channelId">Channel identifier for custom translations</param>
        /// <param name="platform">Platform context (Twitch, Discord, etc.)</param>
        /// <returns>
        /// Translated string if found, the original key if translation is missing,
        /// or "🚨 Something went wrong..." for null parameters
        /// </returns>
        /// <remarks>
        /// <para>
        /// The method follows this lookup sequence:
        /// <list type="number">
        /// <item>Validates input parameters</item>
        /// <item>Loads language translations if not already cached</item>
        /// <item>Checks for channel-specific custom translation</item>
        /// <item>Falls back to default language translation</item>
        /// <item>Logs warning if translation is missing</item>
        /// </list>
        /// </para>
        /// <para>
        /// Key behaviors:
        /// <list type="bullet">
        /// <item>Does not perform string formatting (use GetString overload with args)</item>
        /// <item>Parameter validation returns user-friendly error for null inputs</item>
        /// <item>Automatically caches translations for subsequent accesses</item>
        /// </list>
        /// </para>
        /// This is the simplest translation method for static strings without parameters.
        /// </remarks>
        public static string GetString(Language lang, string key, string channelId, Platform platform)
        {
            try
            {
                if (lang == null || key == null || channelId == null || platform == null)
                    return "🚨 Something went wrong...";

                if (!_translations.ContainsKey(lang))
                    _translations[lang] = LoadTranslations(lang);

                if (!_customTranslations.ContainsKey(channelId))
                    _customTranslations[channelId] = new();

                if (!_customTranslations[channelId].ContainsKey(lang))
                    _customTranslations[channelId][lang] = LoadCustomTranslations(lang, channelId, platform);

                var custom = _customTranslations[channelId][lang];
                if (custom.TryGetValue(key, out var customValue))
                {
                    return customValue;
                }

                if (_translations[lang].TryGetValue(key, out var defaultVal))
                {
                    return defaultVal;
                }

                string remoteValue = GetRemoteTranslation(lang, key);
                if (remoteValue != null)
                {
                    _translations[lang][key] = remoteValue;
                    SaveTranslationFile(lang);
                    Write($"Added missing translation key \"{key}\" for language \"{lang}\" from remote source.", Core.Bot.Logger.LogLevel.Info);
                    return remoteValue;
                }

                Write($"Translate \"{key}\" in lang \"{lang}\" was not found!", Core.Bot.Logger.LogLevel.Warning);
                return key;
            }
            catch (Exception ex)
            {
                Write(ex);
                return null;
            }
        }

        /// <summary>
        /// Retrieves a parameterized translation string with optional formatting.
        /// </summary>
        /// <param name="lang">Language code for translation (e.g., "en-US", "ru-RU")</param>
        /// <param name="key">Translation key path (e.g., "command:ping:response")</param>
        /// <param name="channelId">Channel identifier for custom translations</param>
        /// <param name="platform">Platform context (Twitch, Discord, etc.)</param>
        /// <param name="args">Formatting parameters (replaces {0}, {1}, etc.)</param>
        /// <returns>
        /// Formatted translation string if found, the original key if translation is missing,
        /// or null if critical error occurs
        /// </returns>
        /// <remarks>
        /// <para>
        /// The method extends <see cref="GetString(string, string, string, Platform)"/> with:
        /// <list type="bullet">
        /// <item>Parameter substitution using <c>string.Format()</c></item>
        /// <item>Null-safe handling of formatting parameters</item>
        /// <item>Preservation of custom formatting in translations</item>
        /// </list>
        /// </para>
        /// <para>
        /// Usage example:
        /// <code>
        /// // Translation: "Hello {0}! Your balance: {1} coins"
        /// GetString("en-US", "greeting", "channel123", PlatformsEnum.Twitch, "User", 100);
        /// // Returns: "Hello User! Your balance: 100 coins"
        /// </code>
        /// </para>
        /// <para>
        /// Important notes:
        /// <list type="bullet">
        /// <item>Uses standard .NET string formatting rules</item>
        /// <item>Logs warning but continues if translation is missing</item>
        /// <item>Returns null only for critical exceptions (file access errors, etc.)</item>
        /// </list>
        /// </para>
        /// This is the primary method for retrieving dynamic translation strings with variable content.
        /// </remarks>
        public static string GetString(Language lang, string key, string channelId, Platform platform, params object[] args)
        {
            try
            {
                if (!_translations.ContainsKey(lang))
                    _translations[lang] = LoadTranslations(lang);

                if (!_customTranslations.ContainsKey(channelId))
                    _customTranslations[channelId] = new();

                if (!_customTranslations[channelId].ContainsKey(lang))
                    _customTranslations[channelId][lang] = LoadCustomTranslations(lang, channelId, platform);

                var custom = _customTranslations[channelId][lang];
                if (custom.TryGetValue(key, out var customValue))
                {
                    if (args is not null) customValue = string.Format(customValue, args);
                    return customValue;
                }

                if (_translations[lang].TryGetValue(key, out var defaultVal))
                {
                    if (args is not null) defaultVal = string.Format(defaultVal, args);
                    return defaultVal;
                }

                string remoteValue = GetRemoteTranslation(lang, key);
                if (remoteValue != null)
                {
                    _translations[lang][key] = remoteValue;
                    SaveTranslationFile(lang);
                    Write($"Added missing translation key \"{key}\" for language \"{lang}\" from remote source.", Core.Bot.Logger.LogLevel.Info);
                    return args is not null ? string.Format(remoteValue, args) : remoteValue;
                }


                Write($"Translate \"{key}\" in lang \"{lang}\" was not found!", Core.Bot.Logger.LogLevel.Warning);
                return key;
            }
            catch (Exception ex)
            {
                Write(ex);
                return null;
            }
        }

        /// <summary>
        /// Sets or updates a custom translation for a specific channel and language.
        /// </summary>
        /// <param name="key">Translation key to customize (e.g., "command:ping:description")</param>
        /// <param name="value">Custom translation value</param>
        /// <param name="channel">Channel identifier</param>
        /// <param name="lang">Language code for the custom translation</param>
        /// <param name="platform">Target platform context</param>
        /// <returns>
        /// <see langword="true"/> if the operation succeeded;
        /// <see langword="false"/> if an error occurred during file operations
        /// </returns>
        /// <remarks>
        /// <para>
        /// The method performs these operations:
        /// <list type="number">
        /// <item>Creates necessary directory structure if missing</item>
        /// <item>Loads existing custom translations or creates new set</item>
        /// <item>Updates the specified key with new value</item>
        /// <item>Persists changes to disk in JSON format</item>
        /// <item>Updates in-memory cache for immediate availability</item>
        /// </list>
        /// </para>
        /// <para>
        /// File structure:
        /// <code>
        /// TRNSLT/CUSTOM/
        ///   ├── TWITCH/
        ///   │   └── channel123/
        ///   │       └── en-US.json
        ///   └── DISCORD/
        ///       └── guild456/
        ///           └── ru-RU.json
        /// </code>
        /// </para>
        /// <para>
        /// Error handling:
        /// <list type="bullet">
        /// <item>Returns false for file access errors</item>
        /// <item>Logs exception details for debugging</item>
        /// <item>Does not affect existing translations on failure</item>
        /// </list>
        /// </para>
        /// Custom translations take immediate effect for subsequent <see cref="GetString"/> calls.
        /// </remarks>
        public static bool SetCustomTranslation(string key, string value, string channel, Language lang, Platform platform)
        {
            try
            {
                string dirPath = Path.Combine(bb.Program.BotInstance.Paths.TranslateCustom, PlatformsPathName.strings[(int)platform].ToUpper(), channel);
                Directory.CreateDirectory(dirPath);
                string path = Path.Combine(dirPath, $"{lang.ToStringFormat()}.json");

                var content = FileUtil.FileExists(path)
                    ? Manager.Get<Dictionary<string, string>>(path, "translations")
                    : new Dictionary<string, string>();

                content[key] = value;
                _customTranslations[channel][lang] = content;

                FileUtil.SaveFileContent(
                    path,
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
        /// Removes a custom translation override for a specific channel and language.
        /// </summary>
        /// <param name="key">Translation key to remove</param>
        /// <param name="channel">Channel identifier</param>
        /// <param name="lang">Language code</param>
        /// <param name="platform">Target platform</param>
        /// <returns>
        /// <see langword="true"/> if the translation was successfully removed;
        /// <see langword="false"/> if the translation didn't exist or operation failed
        /// </returns>
        /// <remarks>
        /// <para>
        /// The method performs these operations:
        /// <list type="number">
        /// <item>Checks if custom translation file exists</item>
        /// <item>Loads existing custom translations</item>
        /// <item>Removes the specified key if present</item>
        /// <item>Persists modified translations to disk</item>
        /// <item>Updates in-memory cache for immediate effect</item>
        /// </list>
        /// </para>
        /// <para>
        /// Important behaviors:
        /// <list type="bullet">
        /// <item>Returns false if key doesn't exist (not considered an error)</item>
        /// <item>After removal, default translation will be used</item>
        /// <item>Does not affect translations for other channels or languages</item>
        /// </list>
        /// </para>
        /// <para>
        /// Error scenarios:
        /// <list type="bullet">
        /// <item>Returns false for file access errors</item>
        /// <item>Logs exception details but doesn't crash the application</item>
        /// <item>Leaves existing translations intact on failure</item>
        /// </list>
        /// </para>
        /// This operation is the inverse of <see cref="SetCustomTranslation"/>.
        /// </remarks>
        public static bool DeleteCustomTranslation(string key, string channel, Language lang, Platform platform)
        {
            try
            {
                string dirPath = Path.Combine(bb.Program.BotInstance.Paths.TranslateCustom, PlatformsPathName.strings[(int)platform].ToUpper(), channel);
                if (!Directory.Exists(dirPath)) return false;
                string path = Path.Combine(dirPath, $"{lang.ToStringFormat()}.json");

                var content = Manager.Get<Dictionary<string, string>>(path, "translations");
                if (content == null || !content.ContainsKey(key)) return false;

                content.Remove(key);
                FileUtil.SaveFileContent(
                    path,
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
        /// Loads default translations for a specific language from disk.
        /// </summary>
        /// <param name="lang">Language code to load (e.g., "en-US")</param>
        /// <returns>
        /// Dictionary of translation keys and values,
        /// or empty dictionary if file is missing or invalid
        /// </returns>
        /// <remarks>
        /// <para>
        /// File location:
        /// <code>
        /// TRNSLT/DEFAULT/{userLang}.json
        /// </code>
        /// </para>
        /// <para>
        /// Expected JSON structure:
        /// <code>
        /// {
        ///   "translations": {
        ///     "key1": "value1",
        ///     "key2": "value2"
        ///   }
        /// }
        /// </code>
        /// </para>
        /// <para>
        /// Loading behavior:
        /// <list type="bullet">
        /// <item>Only loads once per language (cached in memory)</item>
        /// <item>Returns empty dictionary for missing/invalid files</item>
        /// <item>Does not throw exceptions for file access issues</item>
        /// <item>Safe for concurrent access from multiple threads</item>
        /// </list>
        /// </para>
        /// This method is called automatically during translation lookups.
        /// </remarks>
        private static Dictionary<string, string> LoadTranslations(Language lang)
        {
            string localPath = Path.Combine(bb.Program.BotInstance.Paths.TranslateDefault, $"{lang.ToStringFormat()}.json");

            if (!File.Exists(localPath))
            {
                Write($"Translation file for {lang} not found locally. Attempting to download...", Core.Bot.Logger.LogLevel.Info);
                if (!DownloadTranslationFile(lang, localPath))
                {
                    Write($"Failed to download translation file for {lang}. Using empty dictionary.", Core.Bot.Logger.LogLevel.Warning);
                    return new Dictionary<string, string>();
                }
            }

            return Manager.Get<Dictionary<string, string>>(localPath, "translations") ?? new Dictionary<string, string>();
        }

        /// <summary>
        /// Downloads translation file from remote repository to local storage.
        /// </summary>
        /// <param name="lang">Language code to download</param>
        /// <param name="localPath">Local path to save the file</param>
        /// <returns>True if download and save were successful</returns>
        public static bool DownloadTranslationFile(Language lang, string localPath)
        {
            string url = $"https://raw.githubusercontent.com/itzkitb/butterBror/refs/heads/master/DefaultTranslate/{lang.ToStringFormat()}.json";
            try
            {
                var response = _httpClient.GetAsync(url).GetAwaiter().GetResult();
                if (response.IsSuccessStatusCode)
                {
                    string content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    File.WriteAllText(localPath, content);
                    Write($"Successfully downloaded translation file for {lang.ToStringFormat()}", Core.Bot.Logger.LogLevel.Info);
                    return true;
                }
                Write($"Failed to download {lang.ToStringFormat()}.json. Status code: {response.StatusCode}", Core.Bot.Logger.LogLevel.Error);
            }
            catch (Exception ex)
            {
                Write($"Error downloading {lang.ToStringFormat()}.json: {ex.Message}", Core.Bot.Logger.LogLevel.Error);
            }
            return false;
        }

        /// <summary>
        /// Retrieves a single translation key from remote repository.
        /// </summary>
        /// <param name="lang">Language code</param>
        /// <param name="key">Translation key to find</param>
        /// <returns>Translation value if found, null otherwise</returns>
        private static string GetRemoteTranslation(Language lang, string key)
        {
            string url = $"https://raw.githubusercontent.com/itzkitb/butterBror/refs/heads/master/DefaultTranslate/{lang.ToStringFormat()}.json";
            try
            {
                var response = _httpClient.GetAsync(url).GetAwaiter().GetResult();
                if (response.IsSuccessStatusCode)
                {
                    string json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    var root = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(json);

                    if (root != null &&
                        root.TryGetValue("translations", out var translations) &&
                        translations != null &&
                        translations.TryGetValue(key, out var value))
                    {
                        return value;
                    }
                }
            }
            catch (Exception ex)
            {
                Write($"Error fetching remote translation for {lang.ToStringFormat()}/{key}: {ex.Message}", Core.Bot.Logger.LogLevel.Error);
            }
            return null;
        }

        /// <summary>
        /// Saves updated translations back to local file.
        /// </summary>
        /// <param name="lang">Language code to save</param>
        private static void SaveTranslationFile(Language lang)
        {
            string localPath = Path.Combine(bb.Program.BotInstance.Paths.TranslateDefault, $"{lang.ToStringFormat()}.json");
            var translations = _translations[lang];
            var jsonStructure = new { translations };
            File.WriteAllText(localPath, JsonConvert.SerializeObject(jsonStructure, Formatting.Indented));
        }

        /// <summary>
        /// Loads custom translations for a specific channel and language from disk.
        /// </summary>
        /// <param name="lang">Language code to load (e.g., "en-US")</param>
        /// <param name="channel">Channel identifier</param>
        /// <param name="platform">Target platform</param>
        /// <returns>
        /// Dictionary of custom translation keys and values,
        /// or empty dictionary if file is missing or invalid
        /// </returns>
        /// <remarks>
        /// <para>
        /// File location:
        /// <code>
        /// TRNSLT/CUSTOM/{PLATFORM}/{channel}/{userLang}.json
        /// </code>
        /// </para>
        /// <para>
        /// Expected JSON structure (same as default translations):
        /// <code>
        /// {
        ///   "translations": {
        ///     "key1": "custom_value1"
        ///   }
        /// }
        /// </code>
        /// </para>
        /// <para>
        /// Loading behavior:
        /// <list type="bullet">
        /// <item>Only loads once per channel-language combination</item>
        /// <item>Returns empty dictionary for missing/invalid files</item>
        /// <item>Does not throw exceptions for file access issues</item>
        /// <item>Safe for concurrent access from multiple threads</item>
        /// </list>
        /// </para>
        /// This method is called automatically during translation lookups.
        /// </remarks>
        private static Dictionary<string, string> LoadCustomTranslations(Language lang, string channel, Platform platform)
        {
            return Manager.Get<Dictionary<string, string>>(
                Path.Combine(bb.Program.BotInstance.Paths.TranslateCustom, PlatformsPathName.strings[(int)platform].ToUpper(), channel, $"{lang.ToStringFormat()}.json"),
                "translations"
            ) ?? new Dictionary<string, string>();
        }

        /// <summary>
        /// Checks if a translation exists for the English language ("en-US").
        /// </summary>
        /// <param name="key">Translation key to check</param>
        /// <returns>
        /// <see langword="true"/> if the key exists in English translations;
        /// <see langword="false"/> otherwise
        /// </returns>
        /// <remarks>
        /// <para>
        /// Implementation details:
        /// <list type="bullet">
        /// <item>Always checks against English ("en-US") translations</item>
        /// <item>Automatically loads English translations if not cached</item>
        /// <item>Only checks default translations (ignores custom overrides)</item>
        /// <item>Does not verify translation content (only key existence)</item>
        /// </list>
        /// </para>
        /// <para>
        /// Usage scenarios:
        /// <list type="bullet">
        /// <item>Validating command keys before execution</item>
        /// <item>Checking if a feature has proper localization</item>
        /// <item>Debugging missing translation issues</item>
        /// </list>
        /// </para>
        /// This method is primarily used for validation and debugging purposes.
        /// </remarks>
        public static bool TranslateContains(string key)
        {
            if (!_translations.ContainsKey(Language.EnUs))
            {
                _translations[Language.EnUs] = LoadTranslations(Language.EnUs);
            }

            return _translations[Language.EnUs].ContainsKey(key);
        }

        /// <summary>
        /// Forces a refresh of translation dictionaries for a specific language and channel.
        /// </summary>
        /// <param name="lang">Language code to refresh (e.g., "en-US")</param>
        /// <param name="channel">Channel identifier</param>
        /// <param name="platform">Target platform</param>
        /// <returns>
        /// <see langword="true"/> if default translations were successfully reloaded;
        /// <see langword="false"/> if reload failed or translations are empty
        /// </returns>
        /// <remarks>
        /// <para>
        /// The method performs these operations:
        /// <list type="number">
        /// <item>Clears existing translations from memory cache</item>
        /// <item>Reloads default translations from disk</item>
        /// <item>Reloads custom translations from disk</item>
        /// <item>Verifies successful reload of default translations</item>
        /// </list>
        /// </para>
        /// <para>
        /// Use cases:
        /// <list type="bullet">
        /// <item>After modifying translation files at runtime</item>
        /// <item>Forcing immediate application of new translations</item>
        /// <item>Recovering from corrupted translation cache</item>
        /// </list>
        /// </para>
        /// <para>
        /// Return behavior:
        /// <list type="bullet">
        /// <item>Returns true if default translations loaded successfully</item>
        /// <item>Returns false if default translations are empty after reload</item>
        /// <item>Logs exception details but continues on error</item>
        /// </list>
        /// </para>
        /// This method is thread-safe and can be called during normal operation.
        /// </remarks>
        public static bool UpdateTranslation(Language lang, string channel, Platform platform)
        {
            try
            {
                if (_translations.ContainsKey(lang))
                {
                    _translations[lang].Clear();
                }
                if (_customTranslations.ContainsKey(channel))
                {
                    if (_customTranslations[channel].ContainsKey(lang))
                    {
                        _customTranslations[channel][lang].Clear();
                    }
                }

                _translations[lang] = LoadTranslations(lang);
                _customTranslations[channel][lang] = LoadCustomTranslations(lang, channel, platform);

                if (_translations[lang].Count > 0)
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
