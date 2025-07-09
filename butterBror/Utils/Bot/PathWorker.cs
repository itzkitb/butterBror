using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static butterBror.Engine.Statistics;

namespace butterBror.Utils.Bot
{
    /// <summary>
    /// Manages hierarchical file/directory paths for the application with automatic path formatting and update propagation.
    /// </summary>
    public class PathWorker
    {
        /// <summary>
        /// Gets or sets the general root directory path used for shared resources.
        /// </summary>
        public string General { get; set; } = string.Empty;

        private string _main_path;

        /// <summary>
        /// Gets or sets the main working directory path (triggers path updates when changed).
        /// </summary>
        public string Main
        {
            get => _main_path;
            set
            {
                _main_path = Format(value);
                UpdatePaths();
            }
        }

        /// <summary>
        /// Gets the path to the channels directory.
        /// </summary>
        public string Channels { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the path to the users database directory.
        /// </summary>
        public string Users { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the path to the nickname conversion data directory.
        /// </summary>
        public string NicknamesData { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the path to the nickname-to-ID mapping directory.
        /// </summary>
        public string Nick2ID { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the path to the ID-to-nickname mapping directory.
        /// </summary>
        public string ID2Nick { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the path to the settings configuration file.
        /// </summary>
        public string Settings { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the path to the cookies storage file.
        /// </summary>
        public string Cookies { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the path to the translations directory.
        /// </summary>
        public string Translations { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the path to the default translations directory.
        /// </summary>
        public string TranslateDefault { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the path to the custom translations directory.
        /// </summary>
        public string TranslateCustom { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the path to the blacklist words file.
        /// </summary>
        public string BlacklistWords { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the path to the blacklist replacements file.
        /// </summary>
        public string BlacklistReplacements { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the path to the API usage statistics file.
        /// </summary>
        public string APIUses { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the path to the current log file (with timestamp).
        /// </summary>
        public string Logs { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the path to the error log file.
        /// </summary>
        public string Errors { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the path to the local cache file.
        /// </summary>
        public string Cache { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the path to the currency data file.
        /// </summary>
        public string Currency { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the path to the 7TV cache file.
        /// </summary>
        public string SevenTVCache { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the path to the reserve directory for backup operations.
        /// </summary>
        public string Reserve { get; private set; } = string.Empty;

        /// <summary>
        /// Updates all derived paths based on the current Main directory.
        /// </summary>
        public void UpdatePaths()
        {
            FunctionsUsed.Add();

            Channels = Format(Path.Combine(Main, "CHNLS/"));
            Users = Format(Path.Combine(Main, "USERSDB/"));
            NicknamesData = Format(Path.Combine(Main, "CONVRT/"));
            Nick2ID = Format(Path.Combine(NicknamesData, "N2I/"));
            ID2Nick = Format(Path.Combine(NicknamesData, "I2N/"));
            Settings = Format(Path.Combine(Main, "SETTINGS.json"));
            Cookies = Format(Path.Combine(Main, "COOKIES.MDS"));
            Translations = Format(Path.Combine(Main, "TRNSLT/"));
            TranslateDefault = Format(Path.Combine(Translations, "DEFAULT/"));
            TranslateCustom = Format(Path.Combine(Translations, "CUSTOM/"));
            BlacklistWords = Format(Path.Combine(Main, "BNWORDS.txt"));
            BlacklistReplacements = Format(Path.Combine(Main, "BNWORDSREP.txt"));
            APIUses = Format(Path.Combine(Main, "API.json"));
            Logs = Format(Path.Combine(Main, "LOGS", $"{DateTime.UtcNow.ToString("dd_MM_yyyy HH.mm.ss")}.log"));
            Errors = Format(Path.Combine(Main, "ERRORS.log"));
            Cache = Format(Path.Combine(Main, "LOC.cache"));
            Currency = Format(Path.Combine(Main, "CURR.json"));
            SevenTVCache = Format(Path.Combine(Main, "7TV.json"));
            Reserve = Format(Path.Combine(General, "butterbror_reserves/", $"{DateTime.UtcNow.ToString("dd_MM_yyyy")}/"));
        }

        /// <summary>
        /// Formats a path string by normalizing slashes (Windows-style).
        /// </summary>
        /// <param name="input">The raw path string to format.</param>
        /// <returns>A path with normalized Windows-style slashes.</returns>
        public string Format(string input)
        {
            return input.Replace("/", "\\");
        }
    }
}
