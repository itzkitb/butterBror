namespace bb.Core.Services
{
    /// <summary>
    /// Manages hierarchical file/directory paths for the application with automatic path formatting and update propagation.
    /// </summary>
    public class PathService
    {
        public PathService(string root)
        {
            Root = root;
            General = Path.Combine(root, "ButterBror/");

            UpdatePaths();
        }

        /// <summary>
        /// Gets or sets the general root directory path used for shared resources.
        /// </summary>
        public string Root { get; }

        /// <summary>
        /// Gets or sets the main working directory path (triggers path updates when changed).
        /// </summary>
        public string General { get; }

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
            ChannelsDatabase = Format(Path.Combine(General, "Channels.db"));
            GamesDatabase = Format(Path.Combine(General, "Games.db"));
            UsersDatabase = Format(Path.Combine(General, "Users.db"));
            MessagesDatabase = Format(Path.Combine(General, "Messages.db"));
            RolesDatabase = Format(Path.Combine(General, "Roles.db"));
            Settings = Format(Path.Combine(General, "Settings.json"));
            Translations = Format(Path.Combine(General, "Localization/"));
            TranslateDefault = Format(Path.Combine(Translations, "Default"));
            TranslateCustom = Format(Path.Combine(Translations, "Custom"));
            BlacklistWords = Format(Path.Combine(General, "BlockedWords.json"));
            APIUses = Format(Path.Combine(General, "API.json"));
            Logs = Format(Path.Combine(General, "Logs", $"{timestamp}.log"));
            Cache = Format(Path.Combine(General, "Location.cache"));
            Currency = Format(Path.Combine(General, "Currency.json"));
            SevenTVCache = Format(Path.Combine(General, "SevenTvCache.json"));
            Reserve = Format(Path.Combine(Root, "ButterBrorReserves/"));
        }

        /// <summary>
        /// Formats a path string by normalizing slashes (Windows-style).
        /// </summary>
        /// <param name="input">The raw path string to format.</param>
        /// <returns>A path with normalized Windows-style slashes.</returns>
        public static string Format(string input)
        {
            return Path.GetFullPath(input)
                .Replace("/", Path.DirectorySeparatorChar.ToString())
                .Replace("\\", Path.DirectorySeparatorChar.ToString());
        }
    }
}
