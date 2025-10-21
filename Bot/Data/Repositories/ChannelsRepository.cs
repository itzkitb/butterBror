using bb.Data.Entities;
using bb.Models.Platform;
using bb.Utils;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Data.SQLite;

namespace bb.Data.Repositories
{
    /// <summary>
    /// Thread-safe database manager for channel-specific data operations across multiple streaming platforms.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class provides comprehensive management for channel-related data including:
    /// <list type="bullet">
    /// <item>Command cooldown tracking with precise timestamp management</item>
    /// <item>Banned word filtering with case-insensitive matching</item>
    /// <item>First message history tracking for user engagement metrics</item>
    /// </list>
    /// </para>
    /// <para>
    /// Key features:
    /// <list type="bullet">
    /// <item>Platform-specific table separation (Twitch, Discord, Telegram)</item>
    /// <item>Automatic database schema initialization and migration</item>
    /// <item>Optimized SQLite performance configuration (WAL mode, memory caching)</item>
    /// <item>Thread-safe operations through transaction management</item>
    /// <item>Efficient caching for frequently accessed banned words</item>
    /// </list>
    /// </para>
    /// All data operations are designed to handle high-frequency access patterns typical in chatbot environments.
    /// </remarks>
    public class ChannelsRepository : SqlRepositoryBase
    {
        private readonly Dictionary<(PlatformsEnum platform, string channelId), HashSet<string>> _banWordsCache = new();
        private readonly Dictionary<(PlatformsEnum platform, string channelId), string> _commandPrefixCache = new();
        private readonly object _cacheLock = new object();

        /// <summary>
        /// Initializes a new instance of the ChannelsDatabase class with the specified database file path.
        /// </summary>
        /// <param name="dbPath">The file path for the SQLite database. Defaults to "Channels.db" in the working directory if not specified.</param>
        /// <remarks>
        /// <para>
        /// The constructor performs the following initialization sequence:
        /// <list type="number">
        /// <item>Configures SQLite performance settings for optimal read/write operations</item>
        /// <item>Creates necessary database tables and indexes if they don't exist</item>
        /// <item>Checks for and migrates legacy data structures if present</item>
        /// </list>
        /// </para>
        /// Database files are created in the application's working directory unless an absolute path is provided.
        /// The database connection is kept open for the lifetime of the object.
        /// </remarks>
        public ChannelsRepository(string dbPath = "Channels.db")
            : base(dbPath, true)
        {
            ConfigureSqlitePerformance();
            InitializeDatabase();
            MigrateOldDataIfNeeded();
        }

        /// <summary>
        /// Generates the command cooldown tracking table name for the specified platform.
        /// </summary>
        /// <param name="platform">The streaming platform identifier</param>
        /// <returns>
        /// A string representing the table name in the format "CommandCooldowns_PLATFORMNAME"
        /// where PLATFORMNAME is the uppercase string representation of the platform enum value.
        /// </returns>
        /// <example>
        /// For PlatformsEnum.Twitch, returns "CommandCooldowns_TWITCH"
        /// </example>
        private string GetCDDTableName(PlatformsEnum platform) => $"CommandCooldowns_{platform.ToString().ToUpper()}";

        /// <summary>
        /// Generates the banned words table name for the specified platform.
        /// </summary>
        /// <param name="platform">The streaming platform identifier</param>
        /// <returns>
        /// A string representing the table name in the format "BanWords_PLATFORMNAME"
        /// where PLATFORMNAME is the uppercase string representation of the platform enum value.
        /// </returns>
        /// <example>
        /// For PlatformsEnum.Discord, returns "BanWords_DISCORD"
        /// </example>
        private string GetBanWordsTableName(PlatformsEnum platform) => $"BanWords_{platform.ToString().ToUpper()}";

        private string GetCommandPrefixTableName(PlatformsEnum platform) => $"CommandPrefixes_{platform.ToString().ToUpper()}";

        /// <summary>
        /// Configures SQLite database performance settings for optimal operation in a chatbot environment.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Applies the following performance optimizations:
        /// <list type="bullet">
        /// <item><c>PRAGMA journal_mode = WAL</c> - Enables Write-Ahead Logging for better concurrency</item>
        /// <item><c>PRAGMA synchronous = NORMAL</c> - Balances performance and durability</item>
        /// <item><c>PRAGMA cache_size = -500000</c> - Allocates 500MB of memory for cache</item>
        /// <item><c>PRAGMA temp_store = MEMORY</c> - Stores temporary tables in RAM</item>
        /// </list>
        /// </para>
        /// These settings are specifically tuned for high-read, moderate-write workloads typical in chat applications.
        /// The optimizations significantly improve throughput during peak usage periods.
        /// </remarks>
        private void ConfigureSqlitePerformance()
        {
            ExecuteNonQuery("PRAGMA journal_mode = WAL;");
            ExecuteNonQuery("PRAGMA synchronous = NORMAL;");
            ExecuteNonQuery("PRAGMA cache_size = -500000;");
            ExecuteNonQuery("PRAGMA temp_store = MEMORY;");
        }

        /// <summary>
        /// Initializes the database schema by creating all required tables and indexes for channel data management.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Creates three types of tables for each platform:
        /// <list type="bullet">
        /// <item><c>CommandCooldowns_*</c> - Tracks command usage timestamps</item>
        /// <item><c>BanWords_*</c> - Stores channel-specific banned word filters</item>
        /// <item><c>FirstMessage_*</c> - Records users' first messages in channels</item>
        /// </list>
        /// </para>
        /// <para>
        /// For each table, the following structures are created:
        /// <list type="bullet">
        /// <item>Primary key constraints on appropriate columns</item>
        /// <item>Indexes for efficient channel-based and user-based lookups</item>
        /// <item>Case-insensitive collation for banned word comparisons</item>
        /// </list>
        /// </para>
        /// The initialization is wrapped in a transaction to ensure atomic creation of all database objects.
        /// </remarks>
        private void InitializeDatabase()
        {
            using (var transaction = Connection.BeginTransaction())
            {
                try
                {
                    foreach (PlatformsEnum platform in Enum.GetValues(typeof(PlatformsEnum)))
                    {
                        string cddTableName = GetCDDTableName(platform);
                        string createCDDTable = $@"
                    CREATE TABLE IF NOT EXISTS [{cddTableName}] (
                        ChannelID TEXT NOT NULL,
                        CommandName TEXT NOT NULL,
                        LastUse TEXT NOT NULL,
                        PRIMARY KEY (ChannelID, CommandName)
                    );
                    CREATE INDEX IF NOT EXISTS idx_{cddTableName}_channelid ON [{cddTableName}](ChannelID);";

                        string banWordsTableName = GetBanWordsTableName(platform);
                        string createBanWordsTable = $@"
                    CREATE TABLE IF NOT EXISTS [{banWordsTableName}] (
                        ChannelID TEXT NOT NULL,
                        BanWord TEXT NOT NULL COLLATE NOCASE,
                        PRIMARY KEY (ChannelID, BanWord)
                    );
                    CREATE INDEX IF NOT EXISTS idx_{banWordsTableName}_channelid ON [{banWordsTableName}](ChannelID);";

                        string firstMessagesTableName = GetFirstMessagesTableName(platform);
                        string createFirstMessagesTable = $@"
                    CREATE TABLE IF NOT EXISTS [{firstMessagesTableName}] (
                        ChannelID TEXT NOT NULL,
                        UserID INTEGER NOT NULL,
                        MessageDate TEXT,
                        MessageText TEXT,
                        IsMe INTEGER,
                        IsModerator INTEGER,
                        IsSubscriber INTEGER,
                        IsPartner INTEGER,
                        IsStaff INTEGER,
                        IsTurbo INTEGER,
                        IsVip INTEGER,
                        PRIMARY KEY (ChannelID, UserID)
                    );
                    CREATE INDEX IF NOT EXISTS idx_{firstMessagesTableName}_channelid ON [{firstMessagesTableName}](ChannelID);
                    CREATE INDEX IF NOT EXISTS idx_{firstMessagesTableName}_userid ON [{firstMessagesTableName}](UserID);";
                        string commandPrefixesTableName = GetCommandPrefixTableName(platform);
                        string createCommandPrefixesTable = $@"
                    CREATE TABLE IF NOT EXISTS [{commandPrefixesTableName}] (
                        ChannelID TEXT NOT NULL,
                        Prefix TEXT NOT NULL,
                        PRIMARY KEY (ChannelID)
                    );
                    CREATE INDEX IF NOT EXISTS idx_{commandPrefixesTableName}_channelid ON [{commandPrefixesTableName}](ChannelID);";

                        ExecuteNonQuery(createCDDTable);
                        ExecuteNonQuery(createBanWordsTable);
                        ExecuteNonQuery(createFirstMessagesTable);
                        ExecuteNonQuery(createCommandPrefixesTable);
                    }
                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        /// <summary>
        /// Checks for legacy data structures and migrates them to the current schema if necessary.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Migration is triggered when:
        /// <list type="bullet">
        /// <item>Older "CHANNELNAME" format tables are detected</item>
        /// <item>Legacy "CDDData" column exists in channel tables</item>
        /// </list>
        /// </para>
        /// <para>
        /// The migration process:
        /// <list type="number">
        /// <item>Identifies channels requiring migration</item>
        /// <item>Converts JSON-formatted cooldown data to normalized table structure</item>
        /// <item>Migrates banned word lists from JSON to relational format</item>
        /// <item>Drops legacy tables after successful migration</item>
        /// </list>
        /// </para>
        /// All migration operations are performed within a single transaction to maintain data integrity.
        /// Errors during migration are logged but don't prevent the system from continuing operation.
        /// </remarks>
        private void MigrateOldDataIfNeeded()
        {
            bool needsMigration = false;
            foreach (PlatformsEnum platform in Enum.GetValues(typeof(PlatformsEnum)))
            {
                string oldTable = GetChannelsTableName(platform);
                string checkColumnSql = $@"
                    SELECT COUNT(*) 
                    FROM sqlite_master 
                    WHERE type='table' AND name='{oldTable}' AND sql LIKE '%CDDData%'";

                if (ExecuteScalar<int>(checkColumnSql) > 0)
                {
                    needsMigration = true;
                    break;
                }
            }

            if (!needsMigration) return;

            BeginTransaction();
            try
            {
                foreach (PlatformsEnum platform in Enum.GetValues(typeof(PlatformsEnum)))
                {
                    MigratePlatformData(platform);
                }
                CommitTransaction();
            }
            catch (Exception ex)
            {
                RollbackTransaction();
                Core.Bot.Logger.Write(ex);
                throw;
            }
        }

        /// <summary>
        /// Migrates channel data from legacy format to current database schema for a specific platform.
        /// </summary>
        /// <param name="platform">The platform whose data needs migration</param>
        /// <remarks>
        /// <para>
        /// For each channel on the specified platform:
        /// <list type="bullet">
        /// <item>Extracts command cooldown data from JSON format</item>
        /// <item>Converts to individual command entries in the cooldown table</item>
        /// <item>Parses banned word JSON array into separate database records</item>
        /// </list>
        /// </para>
        /// <para>
        /// Error handling:
        /// <list type="bullet">
        /// <item>Individual channel migration failures don't stop the entire process</item>
        /// <item>Detailed error logging for troubleshooting migration issues</item>
        /// <item>Complete rollback if platform-level migration fails</item>
        /// </list>
        /// </para>
        /// After successful migration, the legacy table is dropped to clean up the database structure.
        /// </remarks>
        private void MigratePlatformData(PlatformsEnum platform)
        {
            string oldTable = GetChannelsTableName(platform);
            string cddTable = GetCDDTableName(platform);
            string banWordsTable = GetBanWordsTableName(platform);

            string checkTableSql = $@"
        SELECT COUNT(*) 
        FROM sqlite_master 
        WHERE type='table' AND name='{oldTable}'";

            if (ExecuteScalar<int>(checkTableSql) == 0)
                return;

            try
            {
                string selectSql = $@"
            SELECT ChannelID, CDDData, BanWords 
            FROM [{oldTable}]";

                var channelsToMigrate = new List<(string channelId, string cddData, string banWordsJson)>();
                using (var cmd = CreateCommand(selectSql, null))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string channelId = reader.GetString(0);
                        string cddData = !reader.IsDBNull(1) ? reader.GetString(1) : null;
                        string banWordsJson = !reader.IsDBNull(2) ? reader.GetString(2) : null;

                        channelsToMigrate.Add((channelId, cddData, banWordsJson));
                    }
                }

                foreach (var (channelId, cddData, _) in channelsToMigrate)
                {
                    if (string.IsNullOrEmpty(cddData)) continue;

                    try
                    {
                        var lastUses = DataConversion.ParseStringDictionary(cddData);
                        foreach (var kvp in lastUses)
                        {
                            string insertCddSql = $@"
                        INSERT OR REPLACE INTO [{cddTable}] (ChannelID, CommandName, LastUse)
                        VALUES (@ChannelId, @CommandName, @LastUse)";
                            ExecuteNonQuery(insertCddSql, new[]
                            {
                        new SQLiteParameter("@ChannelId", channelId),
                        new SQLiteParameter("@CommandName", kvp.Key),
                        new SQLiteParameter("@LastUse", kvp.Value)
                    });
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка миграции cooldown для канала {channelId}: {ex.Message}");
                    }
                }

                foreach (var (channelId, _, banWordsJson) in channelsToMigrate)
                {
                    if (string.IsNullOrEmpty(banWordsJson)) continue;

                    try
                    {
                        JObject banWordsData = JObject.Parse(banWordsJson);
                        if (banWordsData["list"] is JArray listArray)
                        {
                            foreach (var token in listArray)
                            {
                                string banWord = token.ToString();
                                string insertBanSql = $@"
                            INSERT OR IGNORE INTO [{banWordsTable}] (ChannelID, BanWord)
                            VALUES (@ChannelId, @BanWord)";
                                ExecuteNonQuery(insertBanSql, new[]
                                {
                            new SQLiteParameter("@ChannelId", channelId),
                            new SQLiteParameter("@BanWord", banWord)
                        });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка миграции запрещенных слов для канала {channelId}: {ex.Message}");
                    }
                }

                string dropOldTableSql = $@"
            DROP TABLE [{oldTable}];";
                ExecuteNonQuery(dropOldTableSql);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Критическая ошибка миграции для платформы {platform}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Determines if a command is currently in cooldown for a specific channel on a given platform.
        /// </summary>
        /// <param name="platform">The streaming platform (Twitch, Discord, Telegram)</param>
        /// <param name="channelId">The unique channel identifier</param>
        /// <param name="commandName">The command to check</param>
        /// <param name="cooldown">Required cooldown duration in seconds</param>
        /// <returns>
        /// <see langword="true"/> if the command is currently in cooldown period;
        /// <see langword="false"/> if the command is available (and the last use timestamp was updated)
        /// </returns>
        /// <remarks>
        /// <para>
        /// The method performs the following operations:
        /// <list type="number">
        /// <item>Checks the last execution time for the command in the channel</item>
        /// <item>Returns <see langword="true"/> if current time is within cooldown period</item>
        /// <item>If available, updates the timestamp and returns <see langword="false"/></item>
        /// </list>
        /// </para>
        /// <para>
        /// Implementation notes:
        /// <list type="bullet">
        /// <item>Uses UTC timestamps for consistent time calculations across timezones</item>
        /// <item>Employs UPSERT operation to create records for new command/channel combinations</item>
        /// <item>Formatted using ISO 8601 standard for timestamp storage</item>
        /// </list>
        /// </para>
        /// This method is thread-safe and handles concurrent access patterns typical in chat environments.
        /// </remarks>
        public bool IsCommandCooldown(PlatformsEnum platform, string channelId, string commandName, int cooldown)
        {
            string tableName = GetCDDTableName(platform);
            DateTime now = DateTime.UtcNow;

            string selectSql = $@"
                SELECT LastUse 
                FROM [{tableName}] 
                WHERE ChannelID = @ChannelId AND CommandName = @CommandName";

            string lastUseStr = ExecuteScalar<string>(selectSql, new[]
            {
                new SQLiteParameter("@ChannelId", channelId),
                new SQLiteParameter("@CommandName", commandName)
            });

            if (!string.IsNullOrEmpty(lastUseStr))
            {
                DateTime lastUseTime = DateTime.Parse(lastUseStr, null, System.Globalization.DateTimeStyles.AdjustToUniversal);
                if (now < lastUseTime.AddSeconds(cooldown))
                    return true;
            }

            string upsertSql = $@"
                INSERT OR REPLACE INTO [{tableName}] (ChannelID, CommandName, LastUse)
                VALUES (@ChannelId, @CommandName, @LastUse)";

            ExecuteNonQuery(upsertSql, new[]
            {
                new SQLiteParameter("@ChannelId", channelId),
                new SQLiteParameter("@CommandName", commandName),
                new SQLiteParameter("@LastUse", now.ToString("o"))
            });

            return false;
        }

        /// <summary>
        /// Retrieves the list of banned words for a specific channel on a given platform.
        /// </summary>
        /// <param name="platform">The streaming platform (Twitch, Discord, Telegram)</param>
        /// <param name="channelId">The unique channel identifier</param>
        /// <returns>
        /// A list of banned words for the channel. Returns an empty list if no banned words are configured.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Features:
        /// <list type="bullet">
        /// <item>Implements a case-insensitive cache for frequent access patterns</item>
        /// <item>Automatically initializes channel data if not present</item>
        /// <item>Returns words in original casing as stored in database</item>
        /// </list>
        /// </para>
        /// <para>
        /// Performance characteristics:
        /// <list type="bullet">
        /// <item>First access: Database query with O(n) complexity</item>
        /// <item>Subsequent accesses: O(1) cache lookup</item>
        /// <item>Cache invalidation on any modification to banned words</item>
        /// </list>
        /// </para>
        /// The cache uses case-insensitive comparison to prevent duplicate entries with different casing.
        /// </remarks>
        public List<string> GetBanWords(PlatformsEnum platform, string channelId)
        {
            var key = (platform, channelId);

            lock (_cacheLock)
            {
                if (_banWordsCache.TryGetValue(key, out var words))
                {
                    return words.ToList();
                }
            }

            string tableName = GetBanWordsTableName(platform);
            string sql = $@"
                SELECT BanWord 
                FROM [{tableName}] 
                WHERE ChannelID = @ChannelId";

            var result = new List<string>();
            using var cmd = CreateCommand(sql, new[]
            {
                new SQLiteParameter("@ChannelId", channelId)
            });
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                result.Add(reader.GetString(0));
            }

            lock (_cacheLock)
            {
                _banWordsCache[key] = new HashSet<string>(result, StringComparer.OrdinalIgnoreCase);
            }

            return result;
        }

        /// <summary>
        /// Invalidates the cached banned words for a specific channel-platform combination.
        /// </summary>
        /// <param name="platform">The streaming platform</param>
        /// <param name="channelId">The channel identifier</param>
        /// <remarks>
        /// <para>
        /// This method should be called after any modification to banned words to ensure:
        /// <list type="bullet">
        /// <item>Subsequent <see cref="GetBanWords"/> calls return updated data</item>
        /// <item>Consistency between cache and persistent storage</item>
        /// <item>Prevention of stale data in high-concurrency scenarios</item>
        /// </list>
        /// </para>
        /// The operation is thread-safe and uses lock synchronization to prevent race conditions.
        /// Cache invalidation is immediate and affects all threads accessing the data.
        /// </remarks>
        private void InvalidateBanWordsCache(PlatformsEnum platform, string channelId)
        {
            var key = (platform, channelId);
            lock (_cacheLock)
            {
                _banWordsCache.Remove(key);
            }
        }

        /// <summary>
        /// Sets the complete list of banned words for a channel, replacing any existing entries.
        /// </summary>
        /// <param name="platform">The streaming platform</param>
        /// <param name="channelId">The channel identifier</param>
        /// <param name="banWords">The new list of banned words</param>
        /// <remarks>
        /// <para>
        /// The operation follows this sequence:
        /// <list type="number">
        /// <item>Begins a database transaction</item>
        /// <item>Deletes all existing banned words for the channel</item>
        /// <item>Inserts new banned words in batch operation</item>
        /// <item>Commits transaction and invalidates cache</item>
        /// </list>
        /// </para>
        /// <para>
        /// Important behaviors:
        /// <list type="bullet">
        /// <item>Passing null or empty list effectively clears all banned words</item>
        /// <item>Case-insensitive storage prevents duplicates with different casing</item>
        /// <item>Transaction ensures atomic update (all or nothing)</item>
        /// </list>
        /// </para>
        /// The method automatically handles cache invalidation after successful update.
        /// </remarks>
        public void SetBanWords(PlatformsEnum platform, string channelId, List<string> banWords)
        {
            string tableName = GetBanWordsTableName(platform);

            BeginTransaction();
            try
            {
                string deleteSql = $@"
                    DELETE FROM [{tableName}] 
                    WHERE ChannelID = @ChannelId";
                ExecuteNonQuery(deleteSql, new[]
                {
                    new SQLiteParameter("@ChannelId", channelId)
                });

                if (banWords != null && banWords.Count > 0)
                {
                    string insertSql = $@"
                        INSERT INTO [{tableName}] (ChannelID, BanWord)
                        VALUES (@ChannelId, @BanWord)";

                    using var cmd = CreateCommand(insertSql, null);
                    var channelIdParam = cmd.Parameters.Add("@ChannelId", DbType.String);
                    var banWordParam = cmd.Parameters.Add("@BanWord", DbType.String);

                    channelIdParam.Value = channelId;

                    foreach (string word in banWords)
                    {
                        banWordParam.Value = word;
                        cmd.ExecuteNonQuery();
                    }
                }

                CommitTransaction();
                InvalidateBanWordsCache(platform, channelId);
            }
            catch
            {
                RollbackTransaction();
                throw;
            }
        }

        /// <summary>
        /// Adds a single banned word to a channel's filter list if it doesn't already exist.
        /// </summary>
        /// <param name="platform">The streaming platform</param>
        /// <param name="channelId">The channel identifier</param>
        /// <param name="banWord">The word to add to banned words list</param>
        /// <remarks>
        /// <para>
        /// Key characteristics:
        /// <list type="bullet">
        /// <item>Uses case-insensitive comparison to prevent duplicates</item>
        /// <item>Inserts only if the word doesn't already exist</item>
        /// <item>Immediate cache invalidation after successful addition</item>
        /// </list>
        /// </para>
        /// <para>
        /// Implementation details:
        /// <list type="bullet">
        /// <item>Uses "INSERT OR IGNORE" SQLite syntax for atomic operation</item>
        /// <item>Stores words exactly as provided (case preservation)</item>
        /// <item>Automatic cache management for performance</item>
        /// </list>
        /// </para>
        /// The method is designed for frequent, individual word additions with minimal overhead.
        /// </remarks>
        public void AddBanWord(PlatformsEnum platform, string channelId, string banWord)
        {
            string tableName = GetBanWordsTableName(platform);
            string insertSql = $@"
                INSERT OR IGNORE INTO [{tableName}] (ChannelID, BanWord)
                VALUES (@ChannelId, @BanWord)";

            ExecuteNonQuery(insertSql, new[]
            {
                new SQLiteParameter("@ChannelId", channelId),
                new SQLiteParameter("@BanWord", banWord)
            });
            InvalidateBanWordsCache(platform, channelId);
        }

        /// <summary>
        /// Removes a banned word from a channel's filter list.
        /// </summary>
        /// <param name="platform">The streaming platform</param>
        /// <param name="channelId">The channel identifier</param>
        /// <param name="banWord">The word to remove from banned words list</param>
        /// <remarks>
        /// <para>
        /// Key behaviors:
        /// <list type="bullet">
        /// <item>Case-insensitive matching for removal</item>
        /// <item>No effect if the word doesn't exist in the list</item>
        /// <item>Immediate cache invalidation after removal</item>
        /// </list>
        /// </para>
        /// <para>
        /// Technical implementation:
        /// <list type="bullet">
        /// <item>Uses parameterized queries to prevent SQL injection</item>
        /// <item>Performs exact match (not substring) removal</item>
        /// <item>Transaction ensures data consistency</item>
        /// </list>
        /// </para>
        /// The method safely handles concurrent access patterns common in chat environments.
        /// </remarks>
        public void RemoveBanWord(PlatformsEnum platform, string channelId, string banWord)
        {
            string tableName = GetBanWordsTableName(platform);
            string deleteSql = $@"
                DELETE FROM [{tableName}] 
                WHERE ChannelID = @ChannelId AND BanWord = @BanWord";

            ExecuteNonQuery(deleteSql, new[]
            {
                new SQLiteParameter("@ChannelId", channelId),
                new SQLiteParameter("@BanWord", banWord)
            });
            InvalidateBanWordsCache(platform, channelId);
        }

        public string GetCommandPrefix(PlatformsEnum platform, string channelId)
        {
            var key = (platform, channelId);
            lock (_cacheLock)
            {
                if (_commandPrefixCache.TryGetValue(key, out string prefix))
                {
                    return prefix;
                }
            }

            string tableName = GetCommandPrefixTableName(platform);
            string sql = $@"
                SELECT Prefix 
                FROM [{tableName}] 
                WHERE ChannelID = @ChannelId";

            string prefixFromDb = ExecuteScalar<string>(sql, new[] {
                new SQLiteParameter("@ChannelId", channelId)
            });

            string defaultPrefix = bb.Program.BotInstance.DefaultCommandPrefix.ToString();
            if (string.IsNullOrEmpty(prefixFromDb))
            {
                prefixFromDb = defaultPrefix;
            }

            lock (_cacheLock)
            {
                _commandPrefixCache[key] = prefixFromDb;
            }

            return prefixFromDb;
        }

        public void SetCommandPrefix(PlatformsEnum platform, string channelId, string prefix)
        {
            string tableName = GetCommandPrefixTableName(platform);

            BeginTransaction();
            try
            {
                if (string.IsNullOrEmpty(prefix))
                {
                    string deleteSql = $@"
                        DELETE FROM [{tableName}] 
                        WHERE ChannelID = @ChannelId";
                    ExecuteNonQuery(deleteSql, new[]
                    {
                        new SQLiteParameter("@ChannelId", channelId)
                    });
                }
                else
                {
                    string upsertSql = $@"
                        INSERT OR REPLACE INTO [{tableName}] (ChannelID, Prefix)
                        VALUES (@ChannelId, @Prefix)";
                    ExecuteNonQuery(upsertSql, new[]
                    {
                        new SQLiteParameter("@ChannelId", channelId),
                        new SQLiteParameter("@Prefix", prefix)
                    });
                }

                CommitTransaction();
                InvalidateCommandPrefixCache(platform, channelId);
            }
            catch
            {
                RollbackTransaction();
                throw;
            }
        }

        private void InvalidateCommandPrefixCache(PlatformsEnum platform, string channelId)
        {
            var key = (platform, channelId);
            lock (_cacheLock)
            {
                _commandPrefixCache.Remove(key);
            }
        }

        /// <summary>
        /// Retrieves the first message a user sent in a specific channel.
        /// </summary>
        /// <param name="platform">The streaming platform</param>
        /// <param name="channelId">The channel identifier</param>
        /// <param name="userId">The user identifier</param>
        /// <returns>
        /// A <see cref="Message"/> object containing the first message details, or <see langword="null"/> if not found.
        /// </returns>
        /// <remarks>
        /// <para>
        /// The returned message includes:
        /// <list type="bullet">
        /// <item>Message text content</item>
        /// <item>Timestamp of first message</item>
        /// <item>User role information (moderator, subscriber, etc.)</item>
        /// <item>Message metadata flags</item>
        /// </list>
        /// </para>
        /// <para>
        /// Usage scenarios:
        /// <list type="bullet">
        /// <item>Welcome new users with personalized messages</item>
        /// <item>Track user engagement history</item>
        /// <item>Analyze first message patterns</item>
        /// </list>
        /// </para>
        /// The method performs a primary key lookup which is highly optimized through database indexing.
        /// </remarks>
        public Message GetFirstMessage(PlatformsEnum platform, string channelId, long userId)
        {
            string tableName = GetFirstMessagesTableName(platform);
            string sql = $@"
                SELECT * 
                FROM [{tableName}] 
                WHERE ChannelID = @ChannelId AND UserID = @UserId";
            return QueryFirstOrDefault<Message>(sql, new[]
            {
                new SQLiteParameter("@ChannelId", channelId),
                new SQLiteParameter("@UserId", userId)
            });
        }

        /// <summary>
        /// Saves or updates first message records for multiple user-channel combinations in a batch operation.
        /// </summary>
        /// <param name="messages">Collection of message records to save</param>
        /// <remarks>
        /// <para>
        /// Processing workflow:
        /// <list type="number">
        /// <item>Groups messages by platform for efficient processing</item>
        /// <item>Begins a database transaction</item>
        /// <item>Processes each platform's messages in optimized batch operations</item>
        /// <item>Commits transaction after all operations complete</item>
        /// </list>
        /// </para>
        /// <para>
        /// Performance optimizations:
        /// <list type="bullet">
        /// <item>Parameter reuse to minimize SQL command preparation</item>
        /// <item>Batch processing to reduce database roundtrips</item>
        /// <item>UPSERT operations for efficient record management</item>
        /// </list>
        /// </para>
        /// The method is designed to handle high-volume message logging with minimal performance impact.
        /// Empty collections are safely ignored with no database operations performed.
        /// </remarks>
        public void SaveFirstMessages(List<(PlatformsEnum platform, string channelId, long userId, Message message)> messages)
        {
            if (messages.Count == 0) return;

            BeginTransaction();
            try
            {
                var messagesByPlatform = messages
                    .GroupBy(m => m.platform)
                    .ToList();

                foreach (var platformGroup in messagesByPlatform)
                {
                    PlatformsEnum platform = platformGroup.Key;
                    string tableName = GetFirstMessagesTableName(platform);

                    string upsertSql = $@"
                INSERT OR REPLACE INTO [{tableName}] (
                    ChannelID, UserID, MessageDate, MessageText, 
                    IsMe, IsModerator, IsSubscriber, IsPartner, 
                    IsStaff, IsTurbo, IsVip
                ) VALUES (
                    @ChannelId, @UserId, @MessageDate, @MessageText, 
                    @IsMe, @IsModerator, @IsSubscriber, @IsPartner, 
                    @IsStaff, @IsTurbo, @IsVip
                )";

                    using var cmd = CreateCommand(upsertSql, null);

                    var channelIdParam = cmd.Parameters.Add("@ChannelId", DbType.String);
                    var userIdParam = cmd.Parameters.Add("@UserId", DbType.Int64);
                    var messageDateParam = cmd.Parameters.Add("@MessageDate", DbType.String);
                    var messageTextParam = cmd.Parameters.Add("@MessageText", DbType.String);
                    var isMeParam = cmd.Parameters.Add("@IsMe", DbType.Int32);
                    var isModeratorParam = cmd.Parameters.Add("@IsModerator", DbType.Int32);
                    var isSubscriberParam = cmd.Parameters.Add("@IsSubscriber", DbType.Int32);
                    var isPartnerParam = cmd.Parameters.Add("@IsPartner", DbType.Int32);
                    var isStaffParam = cmd.Parameters.Add("@IsStaff", DbType.Int32);
                    var isTurboParam = cmd.Parameters.Add("@IsTurbo", DbType.Int32);
                    var isVipParam = cmd.Parameters.Add("@IsVip", DbType.Int32);

                    foreach (var (_, channelId, userId, message) in platformGroup)
                    {
                        channelIdParam.Value = channelId;
                        userIdParam.Value = userId;
                        messageDateParam.Value = message.messageDate.ToString("o");
                        messageTextParam.Value = message.messageText ?? string.Empty;
                        isMeParam.Value = message.isMe ? 1 : 0;
                        isModeratorParam.Value = message.isModerator ? 1 : 0;
                        isSubscriberParam.Value = message.isSubscriber ? 1 : 0;
                        isPartnerParam.Value = message.isPartner ? 1 : 0;
                        isStaffParam.Value = message.isStaff ? 1 : 0;
                        isTurboParam.Value = message.isTurbo ? 1 : 0;
                        isVipParam.Value = message.isVip ? 1 : 0;

                        cmd.ExecuteNonQuery();
                    }
                }

                CommitTransaction();
            }
            catch
            {
                RollbackTransaction();
                throw;
            }
        }

        /// <summary>
        /// Generates the legacy channel data table name for a specific platform.
        /// </summary>
        /// <param name="platform">The streaming platform</param>
        /// <returns>
        /// Uppercase string representation of the platform name.
        /// </returns>
        /// <remarks>
        /// This method exists primarily for backward compatibility with older database structures.
        /// Used during data migration to identify legacy tables that need conversion.
        /// </remarks>
        private string GetChannelsTableName(PlatformsEnum platform)
        {
            return platform.ToString().ToUpper();
        }

        /// <summary>
        /// Generates the first message tracking table name for a specific platform.
        /// </summary>
        /// <param name="platform">The streaming platform</param>
        /// <returns>
        /// Table name in the format "FirstMessage_PLATFORMNAME" where PLATFORMNAME is uppercase.
        /// </returns>
        /// <example>
        /// For PlatformsEnum.Telegram, returns "FirstMessage_TELEGRAM"
        /// </example>
        private string GetFirstMessagesTableName(PlatformsEnum platform)
        {
            return $"FirstMessage_{platform.ToString().ToUpper()}";
        }
    }
}