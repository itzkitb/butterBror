namespace butterBror.Core.Services
{
    /// <summary>
    /// Manages hierarchical file/directory paths for the application with automatic path formatting and update propagation.
    /// </summary>
    public class PathService
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
        /// Gets the path to the settings configuration file.
        /// </summary>
        public string Settings { get; private set; } = string.Empty;

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
        /// Gets the path to the reserve directory for backup operations.
        /// </summary>
        public string MessagesDatabase { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the path to the reserve directory for backup operations.
        /// </summary>
        public string ChannelsDatabase { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the path to the reserve directory for backup operations.
        /// </summary>
        public string GamesDatabase { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the path to the reserve directory for backup operations.
        /// </summary>
        public string UsersDatabase { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the path to the reserve directory for backup operations.
        /// </summary>
        public string RolesDatabase { get; private set; } = string.Empty;

        /// <summary>
        /// Updates all derived paths based on the current Main directory.
        /// </summary>
        public void UpdatePaths()
        {
            string timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            ChannelsDatabase = Path.Combine(Main, "Channels.db");
            GamesDatabase = Path.Combine(Main, "Games.db");
            UsersDatabase = Path.Combine(Main, "Users.db");
            MessagesDatabase = Path.Combine(Main, "Messages.db");
            RolesDatabase = Path.Combine(Main, "Roles.db");
            Settings = Path.Combine(Main, "SETTINGS.json");
            Translations = Path.Combine(Main, "TRNSLT/");
            TranslateDefault = Path.Combine(Translations, "DEFAULT");
            TranslateCustom = Path.Combine(Translations, "CUSTOM");
            BlacklistWords = Path.Combine(Main, "BNWORDS.txt");
            BlacklistReplacements = Path.Combine(Main, "BNWORDSREP.txt");
            APIUses = Path.Combine(Main, "API.json");
            Logs = Path.Combine(Main, "LOGS", $"{timestamp}.log");
            Cache = Path.Combine(Main, "LOC.cache");
            Currency = Path.Combine(Main, "CURR.json");
            SevenTVCache = Path.Combine(Main, "7TV.json");
            Reserve = Path.Combine(General, "bbReserves/");
        }

        /// <summary>
        /// Formats a path string by normalizing slashes (Windows-style).
        /// </summary>
        /// <param name="input">The raw path string to format.</param>
        /// <returns>A path with normalized Windows-style slashes.</returns>
        public string Format(string input)
        {
            return Path.GetFullPath(input)
                .Replace("/", Path.DirectorySeparatorChar.ToString())
                .Replace("\\", Path.DirectorySeparatorChar.ToString());
        }
    }
}
