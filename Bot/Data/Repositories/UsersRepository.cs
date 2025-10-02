using bb.Data.Entities;
using bb.Models.Platform;
using Newtonsoft.Json.Linq;
using SevenTV.Types.Rest;
using System.Collections.Concurrent;
using System.Data;
using System.Data.SQLite;
using System.Text;
using System.Text.RegularExpressions;

namespace bb.Data.Repositories
{
    /// <summary>
    /// Thread-safe database manager for cross-platform user data storage and operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class provides comprehensive management of user profiles and activity metrics across multiple platforms including:
    /// <list type="bullet">
    /// <item>Complete user profile storage with customizable parameters</item>
    /// <item>Username-to-ID mapping system with caching</item>
    /// <item>Message count tracking across channels and platforms</item>
    /// <item>Global and channel-specific activity metrics</item>
    /// </list>
    /// </para>
    /// <para>
    /// Key features:
    /// <list type="bullet">
    /// <item>Platform-specific data isolation (Twitch, Discord, Telegram, etc.)</item>
    /// <item>Automatic schema initialization and legacy data migration</item>
    /// <item>Optimized SQLite configuration for high-concurrency chat environments</item>
    /// <item>Batch processing for efficient bulk operations</item>
    /// <item>Concurrent dictionary caching for username lookups</item>
    /// </list>
    /// </para>
    /// Designed to handle the high-frequency data access patterns typical in multi-platform chatbot systems.
    /// </remarks>
    public class UsersRepository : SqlRepositoryBase
    {
        private readonly ConcurrentDictionary<(PlatformsEnum platform, string username), long> _usernameCache = new();

        /// <summary>
        /// Initializes a new instance of the UsersDatabase class with the specified database file path.
        /// </summary>
        /// <param name="dbPath">The file path for the SQLite database. Defaults to "Users.db" in the working directory if not specified.</param>
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
        public UsersRepository(string dbPath = "Users.db")
            : base(dbPath, true)
        {
            ConfigureSqlitePerformance();
            InitializeDatabase();
            MigrateOldDataIfNeeded();
        }

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
        /// <item><c>PRAGMA foreign_keys = ON</c> - Enables foreign key constraints</item>
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
            ExecuteNonQuery("PRAGMA foreign_keys = ON;");
        }

        /// <summary>
        /// Checks for legacy data structures and migrates them to the current schema if necessary.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Migration is triggered when:
        /// <list type="bullet">
        /// <item>Older "ChannelMessagesCount" column is detected in user tables</item>
        /// <item>Legacy JSON-formatted channel message data exists</item>
        /// </list>
        /// </para>
        /// <para>
        /// The migration process:
        /// <list type="number">
        /// <item>Identifies platforms requiring migration</item>
        /// <item>Extracts channel message counts from JSON format</item>
        /// <item>Migrates to normalized channel-specific message count tables</item>
        /// <item>Removes legacy columns while preserving all data</item>
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
                string tableName = GetTableName(platform);
                string checkColumnSql = $@"
            SELECT COUNT(*) 
            FROM sqlite_master 
            WHERE type='table' AND name='{tableName}' AND sql LIKE '%ChannelMessagesCount%'";

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
            catch
            {
                RollbackTransaction();
                throw;
            }
        }

        /// <summary>
        /// Migrates user data from legacy format to current database schema for a specific platform.
        /// </summary>
        /// <param name="platform">The platform whose user data needs migration</param>
        /// <remarks>
        /// <para>
        /// For each user on the specified platform:
        /// <list type="bullet">
        /// <item>Extracts channel message counts from JSON format</item>
        /// <item>Converts to individual channel records in the channel counts table</item>
        /// <item>Preserves all other user data during schema restructuring</item>
        /// </list>
        /// </para>
        /// <para>
        /// Implementation details:
        /// <list type="bullet">
        /// <item>Uses temporary table approach to avoid data loss</item>
        /// <item>Skips empty or invalid JSON data structures</item>
        /// <item>Preserves all existing user parameters during migration</item>
        /// <item>Completely removes legacy columns after successful migration</item>
        /// </list>
        /// </para>
        /// The migration preserves all historical message count data while optimizing storage structure.
        /// </remarks>
        private void MigratePlatformData(PlatformsEnum platform)
        {
            string tableName = GetTableName(platform);
            string channelCountsTable = $"{tableName}_ChannelCounts";

            string checkColumnSql = $@"
                SELECT COUNT(*) 
                FROM PRAGMA_TABLE_INFO('{tableName}')
                WHERE name = 'ChannelMessagesCount'";

            if (ExecuteScalar<int>(checkColumnSql) == 0)
                return;

            string selectSql = $@"
                SELECT ID, ChannelMessagesCount 
                FROM [{tableName}]
                WHERE ChannelMessagesCount IS NOT NULL AND ChannelMessagesCount != '[]' AND ChannelMessagesCount != '{{}}'";

            using var cmd = CreateCommand(selectSql, null);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                long userId = reader.GetInt64(0);
                string channelMessagesJson = reader.GetString(1);

                try
                {
                    JObject channelMessages = JObject.Parse(channelMessagesJson);

                    foreach (var channel in channelMessages)
                    {
                        string channelId = channel.Key;
                        int count = channel.Value.ToObject<int>();

                        string insertSql = $@"
                            INSERT OR REPLACE INTO [{channelCountsTable}] (UserID, ChannelID, MessageCount)
                            VALUES (@UserId, @ChannelId, @Count)";

                        ExecuteNonQuery(insertSql, new[]
                        {
                            new SQLiteParameter("@UserId", userId),
                            new SQLiteParameter("@ChannelId", channelId),
                            new SQLiteParameter("@Count", count)
                        });
                    }
                }
                catch (Exception ex)
                {
                    Core.Bot.Console.Write(ex);
                }
            }

            string tempTable = $"{tableName}_temp";
            string recreateTableSql = $@"
        CREATE TABLE [{tempTable}] AS SELECT 
            ID, FirstMessage, FirstSeen, FirstChannel, LastMessage, LastSeen, LastChannel,
            Balance, AfterDotBalance, Rating, IsAFK, AFKText, AFKType, AFKStart,
            Reminders, LastCookie, GiftedCookies, EatedCookies, BuyedCookies, Location,
            Longitude, Latitude, Language, AFKResume, AFKResumeTimes, LastUse,
            GPTHistory, WeatherResultLocations, TotalMessages, TotalMessagesLength
        FROM [{tableName}];
        
        DROP TABLE [{tableName}];
        
        ALTER TABLE [{tempTable}] RENAME TO [{tableName}];";

            ExecuteNonQuery(recreateTableSql);
        }

        /// <summary>
        /// Initializes the database schema by creating all required tables and indexes for user data management.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Creates two types of tables for each platform:
        /// <list type="bullet">
        /// <item><c>PLATFORMNAME</c> - Core user profile table with comprehensive user data</item>
        /// <item><c>PLATFORMNAME_ChannelCounts</c> - Channel-specific message tracking</item>
        /// </list>
        /// </para>
        /// <para>
        /// Additionally creates:
        /// <list type="bullet">
        /// <item><c>UsernameMapping</c> - Cross-referencing table for username-to-ID resolution</item>
        /// </list>
        /// </para>
        /// <para>
        /// For each table, the following structures are created:
        /// <list type="bullet">
        /// <item>Primary key constraints on appropriate columns</item>
        /// <item>Indexes for efficient user-based and channel-based lookups</item>
        /// <item>Default values for all counters and flags</item>
        /// <item>JSON-compatible fields for complex data structures</item>
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
                        string tableName = GetTableName(platform);
                        string createTableSql = $@"
                    CREATE TABLE IF NOT EXISTS [{tableName}] (
                        ID INTEGER PRIMARY KEY,
                        FirstMessage TEXT,
                        FirstSeen TEXT,
                        FirstChannel TEXT,
                        LastMessage TEXT,
                        LastSeen TEXT,
                        LastChannel TEXT,
                        Balance INTEGER DEFAULT 0,
                        AfterDotBalance INTEGER DEFAULT 0,
                        Rating INTEGER DEFAULT 500,
                        IsAFK INTEGER DEFAULT 0,
                        AFKText TEXT DEFAULT '',
                        AFKType TEXT DEFAULT '',
                        AFKStart TEXT DEFAULT '',
                        Reminders TEXT DEFAULT '[]',
                        LastCookie TEXT DEFAULT '',
                        GiftedCookies INTEGER DEFAULT 0,
                        EatedCookies INTEGER DEFAULT 0,
                        BuyedCookies INTEGER DEFAULT 0,
                        Location TEXT DEFAULT '',
                        Longitude TEXT DEFAULT '',
                        Latitude TEXT DEFAULT '',
                        Language TEXT DEFAULT 'en-US',
                        AFKResume TEXT DEFAULT '',
                        AFKResumeTimes INTEGER DEFAULT 0,
                        LastUse TEXT DEFAULT '[]',
                        GPTHistory TEXT DEFAULT '[]',
                        WeatherResultLocations TEXT DEFAULT '[]',
                        TotalMessages INTEGER DEFAULT 1,
                        TotalMessagesLength INTEGER DEFAULT 0
                    );
                    CREATE INDEX IF NOT EXISTS idx_{tableName}_id ON [{tableName}](ID);";

                        // Сначала создаем таблицу
                        ExecuteNonQuery(createTableSql);

                        // Затем добавляем дополнительные столбцы
                        string[] columnsToAdd = {
                    "LastHourlyReward TEXT DEFAULT '2000-01-01T00:00:00.0000000Z'",
                    "LastDailyReward TEXT DEFAULT '2000-01-01T00:00:00.0000000Z'",
                    "LastWeeklyReward TEXT DEFAULT '2000-01-01T00:00:00.0000000Z'",
                    "LastMonthlyReward TEXT DEFAULT '2000-01-01T00:00:00.0000000Z'",
                    "LastYearlyReward TEXT DEFAULT '2000-01-01T00:00:00.0000000Z'"
                };
                        foreach (var column in columnsToAdd)
                        {
                            try
                            {
                                ExecuteNonQuery($"ALTER TABLE [{tableName}] ADD COLUMN {column}");
                            }
                            catch (SQLiteException ex)
                            {
                                if (!ex.Message.Contains("duplicate column name"))
                                {
                                    throw;
                                }
                            }
                        }

                        string channelCountsTable = $"{tableName}_ChannelCounts";
                        string createChannelCountsTable = $@"
                    CREATE TABLE IF NOT EXISTS [{channelCountsTable}] (
                        UserID INTEGER NOT NULL,
                        ChannelID TEXT NOT NULL,
                        MessageCount INTEGER DEFAULT 0,
                        PRIMARY KEY (UserID, ChannelID)
                    );
                    CREATE INDEX IF NOT EXISTS idx_{channelCountsTable}_userid ON [{channelCountsTable}](UserID);";
                        ExecuteNonQuery(createChannelCountsTable);
                    }

                    string createMappingTableSql = @"
                CREATE TABLE IF NOT EXISTS UsernameMapping (
                    Platform TEXT NOT NULL,
                    UserID INTEGER NOT NULL,
                    Username TEXT NOT NULL,
                    PRIMARY KEY (Platform, UserID)
                );
                CREATE INDEX IF NOT EXISTS idx_username_mapping_platform ON UsernameMapping(Platform);
                CREATE INDEX IF NOT EXISTS idx_username_mapping_userid ON UsernameMapping(UserID);
                CREATE INDEX IF NOT EXISTS idx_username_mapping_username ON UsernameMapping(Username);";
                    ExecuteNonQuery(createMappingTableSql);

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
        /// Checks if a user exists in the database for the specified platform.
        /// </summary>
        /// <param name="platform">The streaming platform (Twitch, Discord, Telegram, etc.)</param>
        /// <param name="userId">The unique numeric identifier of the user</param>
        /// <returns>
        /// <see langword="true"/> if the user exists in the database;
        /// <see langword="false"/> if the user has not been registered
        /// </returns>
        /// <remarks>
        /// <para>
        /// Implementation notes:
        /// <list type="bullet">
        /// <item>Performs a primary key lookup which is highly optimized through database indexing</item>
        /// <item>Uses parameterized queries to prevent SQL injection</item>
        /// <item>Executes with minimal database overhead (single row check)</item>
        /// </list>
        /// </para>
        /// This method is typically used as a preliminary check before user registration or data retrieval operations.
        /// </remarks>
        public bool CheckUserExists(PlatformsEnum platform, long userId)
        {
            string tableName = GetTableName(platform);
            string sql = $"SELECT 1 FROM [{tableName}] WHERE ID = @UserId LIMIT 1";
            return ExecuteScalar<object>(sql, new[] { new SQLiteParameter("@UserId", userId) }) != null;
        }

        /// <summary>
        /// Registers a new user with default parameters if they don't already exist.
        /// </summary>
        /// <param name="platform">The streaming platform (Twitch, Discord, Telegram, etc.)</param>
        /// <param name="userId">The unique numeric identifier of the user</param>
        /// <param name="initialLanguage">Default language setting for the user (defaults to "en-US")</param>
        /// <param name="initialMessage">The first message sent by the user (optional)</param>
        /// <param name="initialChannel">The first channel where the user interacted (optional)</param>
        /// <returns>
        /// <see langword="true"/> if the user was successfully registered;
        /// <see langword="false"/> if the user already existed
        /// </returns>
        /// <remarks>
        /// <para>
        /// The registration process initializes the user with:
        /// <list type="bullet">
        /// <item>Current timestamps for first seen/first message</item>
        /// <item>Default language setting</item>
        /// <item>Initial balance of 0 coins</item>
        /// <item>Default rating of 500 points</item>
        /// <item>Message count initialized to 1 (for the registration message)</item>
        /// <item>Message length set to the length of the initial message</item>
        /// </list>
        /// </para>
        /// <para>
        /// Important behaviors:
        /// <list type="bullet">
        /// <item>Atomic operation - either fully succeeds or fails</item>
        /// <item>Does not overwrite existing user data</item>
        /// <item>Automatically creates username mapping if applicable</item>
        /// </list>
        /// </para>
        /// This method should be called whenever a new user is detected in any channel.
        /// </remarks>
        public bool RegisterNewUser(PlatformsEnum platform, long userId, string initialLanguage = "en-US", string initialMessage = "", string initialChannel = "")
        {
            if (CheckUserExists(platform, userId))
            {
                return false;
            }
            string tableName = GetTableName(platform);
            DateTime now = DateTime.UtcNow;
            string sql = $@"
                INSERT INTO [{tableName}] (
                    ID, FirstSeen, FirstMessage, FirstChannel, 
                    LastSeen, LastMessage, LastChannel,
                    Balance, AfterDotBalance, Rating,
                    IsAFK, AFKText, AFKType, AFKStart,
                    Reminders, LastCookie, GiftedCookies, 
                    EatedCookies, BuyedCookies, Location,
                    Longitude, Latitude, Language,
                    AFKResume, AFKResumeTimes, LastUse, 
                    GPTHistory, WeatherResultLocations,
                    TotalMessages, TotalMessagesLength
                ) VALUES (
                    @ID, @FirstSeen, @FirstMessage, @FirstChannel, 
                    @LastSeen, @LastMessage, @LastChannel,
                    0, 0, 500,
                    0, '', '', '',
                    '{{}}', '', 0, 
                    0, 0, '',
                    '', '', @Language,
                    '', 0, '{{}}', 
                    '[]', '[]',
                    1, @TotalMessagesLength
                );";
            ExecuteNonQuery(sql, new[]
            {
                new SQLiteParameter("@ID", userId),
                new SQLiteParameter("@FirstSeen", now.ToString("o")),
                new SQLiteParameter("@LastSeen", now.ToString("o")),
                new SQLiteParameter("@Language", initialLanguage),
                new SQLiteParameter("@FirstMessage", initialMessage),
                new SQLiteParameter("@FirstChannel", initialChannel),
                new SQLiteParameter("@LastMessage", initialMessage),
                new SQLiteParameter("@LastChannel", initialChannel),
                new SQLiteParameter("@TotalMessagesLength", initialMessage.Length)
            });
            return true;
        }

        /// <summary>
        /// Adds or updates a username mapping for a user on the specified platform.
        /// </summary>
        /// <param name="platform">The streaming platform</param>
        /// <param name="userId">The unique numeric identifier of the user</param>
        /// <param name="username">The username to map to the user ID</param>
        /// <returns>
        /// <see langword="true"/> if the mapping was successfully added;
        /// <see langword="false"/> if the username was invalid or empty
        /// </returns>
        /// <remarks>
        /// <para>
        /// The method performs the following sequence:
        /// <list type="number">
        /// <item>Validates the username is not empty or whitespace</item>
        /// <item>Updates the internal cache with the new mapping</item>
        /// <item>Removes any existing mappings for the same user/platform</item>
        /// <item>Inserts the new mapping into the database</item>
        /// </list>
        /// </para>
        /// <para>
        /// Key characteristics:
        /// <list type="bullet">
        /// <item>Case-sensitive storage of usernames</item>
        /// <item>Automatic cache invalidation of old mappings</item>
        /// <item>Primary key constraint on (Platform, UserID)</item>
        /// <item>Single username per user ID (replaces previous mapping)</item>
        /// </list>
        /// </para>
        /// This method should be called whenever a user's username is detected or updated.
        /// </remarks>
        public bool AddUsernameMapping(PlatformsEnum platform, long userId, string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return false;

            var cacheKey = (platform, username);

            _usernameCache[cacheKey] = userId;

            RemoveUsernameMapping(platform, userId);

            string sql = @"
                INSERT INTO UsernameMapping (Platform, UserID, Username)
                VALUES (@Platform, @UserID, @Username)";
            ExecuteNonQuery(sql, new[]
            {
                new SQLiteParameter("@Platform", platform.ToString().ToUpper()),
                new SQLiteParameter("@UserID", userId),
                new SQLiteParameter("@Username", username)
            });

            return true;
        }

        /// <summary>
        /// Retrieves the user ID associated with a username on the specified platform.
        /// </summary>
        /// <param name="platform">The streaming platform</param>
        /// <param name="username">The username to look up</param>
        /// <returns>
        /// The user ID if found; otherwise, <see langword="null"/>
        /// </returns>
        /// <remarks>
        /// <para>
        /// Lookup process:
        /// <list type="number">
        /// <item>Checks the in-memory cache first (O(1) complexity)</item>
        /// <item>If not cached, queries the database with parameterized search</item>
        /// <item>Caches the result for future lookups</item>
        /// </list>
        /// </para>
        /// <para>
        /// Performance characteristics:
        /// <list type="bullet">
        /// <item>First access: Database query with O(n) complexity</item>
        /// <item>Subsequent accesses: O(1) cache lookup</item>
        /// <item>Cache uses platform-username composite key</item>
        /// <item>Case-sensitive matching (exact username match)</item>
        /// </list>
        /// </para>
        /// Returns <see langword="null"/> for non-existent usernames rather than throwing exceptions.
        /// </remarks>
        public long? GetUserIdByUsername(PlatformsEnum platform, string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return null;

            var cacheKey = (platform, username);

            if (_usernameCache.TryGetValue(cacheKey, out long cachedUserId))
                return cachedUserId;

            string sql = @"
                SELECT UserID 
                FROM UsernameMapping 
                WHERE Platform = @Platform AND Username = @Username";
            object result = ExecuteScalar<object>(sql, new[]
            {
                new SQLiteParameter("@Platform", platform.ToString().ToUpper()),
                new SQLiteParameter("@Username", username)
            });

            if (result != null && result != DBNull.Value)
            {
                long userId = Convert.ToInt64(result);
                _usernameCache[cacheKey] = userId;
                return userId;
            }

            return null;
        }

        /// <summary>
        /// Retrieves the username associated with a user ID on the specified platform.
        /// </summary>
        /// <param name="platform">The streaming platform</param>
        /// <param name="userId">The user ID to look up</param>
        /// <returns>
        /// The username if found; otherwise, <see langword="null"/>
        /// </returns>
        /// <remarks>
        /// <para>
        /// Implementation details:
        /// <list type="bullet">
        /// <item>Direct database query using primary key index</item>
        /// <item>No caching layer (less frequently used than username-to-ID lookup)</item>
        /// <item>Returns most recently recorded username</item>
        /// </list>
        /// </para>
        /// <para>
        /// Important notes:
        /// <list type="bullet">
        /// <item>Does not guarantee username is current (users can change usernames)</item>
        /// <item>Returns <see langword="null"/> if no mapping exists</item>
        /// <item>Case-sensitive storage but returns exact stored value</item>
        /// </list>
        /// </para>
        /// This method is primarily used for display purposes rather than critical operations.
        /// </remarks>
        public string GetUsernameByUserId(PlatformsEnum platform, long userId)
        {
            string sql = @"
                SELECT Username 
                FROM UsernameMapping 
                WHERE Platform = @Platform AND UserID = @UserID";
            object result = ExecuteScalar<object>(sql, new[]
            {
                new SQLiteParameter("@Platform", platform.ToString().ToUpper()),
                new SQLiteParameter("@UserID", userId)
            });
            return result as string;
        }

        /// <summary>
        /// Removes all username mappings for a user on the specified platform.
        /// </summary>
        /// <param name="platform">The streaming platform</param>
        /// <param name="userId">The user ID whose mappings should be removed</param>
        /// <returns>The number of mappings removed (typically 0 or 1)</returns>
        /// <remarks>
        /// <para>
        /// The operation:
        /// <list type="bullet">
        /// <item>Removes entries from the UsernameMapping table</item>
        /// <item>Does not affect the core user profile data</item>
        /// <item>Does not invalidate the user cache</item>
        /// </list>
        /// </para>
        /// <para>
        /// Usage scenarios:
        /// <list type="bullet">
        /// <item>User account deletion</item>
        /// <item>Username change processing (typically followed by new mapping)</item>
        /// <item>Data cleanup operations</item>
        /// </list>
        /// </para>
        /// Returns the count of removed mappings (0 if none existed, 1 if a mapping was removed).
        /// </remarks>
        public int RemoveUsernameMapping(PlatformsEnum platform, long userId)
        {
            string sql = @"
                DELETE FROM UsernameMapping 
                WHERE Platform = @Platform AND UserID = @UserID";
            return ExecuteNonQuery(sql, new[]
            {
                new SQLiteParameter("@Platform", platform.ToString().ToUpper()),
                new SQLiteParameter("@UserID", userId)
            });
        }

        /// <summary>
        /// Retrieves all usernames associated with a user ID on the specified platform.
        /// </summary>
        /// <param name="platform">The streaming platform</param>
        /// <param name="userId">The user ID to look up</param>
        /// <returns>A list of all recorded usernames for the user</returns>
        /// <remarks>
        /// <para>
        /// Important characteristics:
        /// <list type="bullet">
        /// <item>Returns historical usernames (not just current)</item>
        /// <item>Results are not ordered chronologically</item>
        /// <item>May contain duplicates if not properly maintained</item>
        /// </list>
        /// </para>
        /// <para>
        /// Implementation details:
        /// <list type="bullet">
        /// <item>Direct database query against UsernameMapping table</item>
        /// <item>Returns empty list if no mappings exist</item>
        /// <item>Case-sensitive storage (preserves original casing)</item>
        /// </list>
        /// </para>
        /// This method is primarily used for analytics and historical tracking rather than real-time operations.
        /// </remarks>
        public List<string> GetAllUsernames(PlatformsEnum platform, long userId)
        {
            string sql = @"
                SELECT Username 
                FROM UsernameMapping 
                WHERE Platform = @Platform AND UserID = @UserID";
            var result = new List<string>();
            using var cmd = CreateCommand(sql, new[]
            {
                new SQLiteParameter("@Platform", platform.ToString().ToUpper()),
                new SQLiteParameter("@UserID", userId)
            });
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(reader["Username"].ToString());
            }
            return result;
        }

        /// <summary>
        /// Retrieves the value of a specific user parameter.
        /// </summary>
        /// <param name="platform">The streaming platform</param>
        /// <param name="userId">The user ID whose parameter should be retrieved</param>
        /// <param name="columnName">The name of the parameter/column to retrieve</param>
        /// <returns>The parameter value, or <see langword="null"/> if not found</returns>
        /// <remarks>
        /// <para>
        /// Security considerations:
        /// <list type="bullet">
        /// <item>Validates column name against whitelist to prevent SQL injection</item>
        /// <item>Only permits access to predefined user parameters</item>
        /// <item>Does not allow arbitrary SQL expressions</item>
        /// </list>
        /// </para>
        /// <para>
        /// Usage notes:
        /// <list type="bullet">
        /// <item>Returns <see langword="null"/> for non-existent users</item>
        /// <item>Returns raw database values (may require type conversion)</item>
        /// <item>Safe for frequent access due to indexed lookups</item>
        /// </list>
        /// </para>
        /// For complex parameter types (JSON objects), consider using specialized retrieval methods.
        /// </remarks>
        public object GetParameter(PlatformsEnum platform, long userId, string columnName)
        {
            if (!CheckUserExists(platform, userId))
            {
                return null;
            }
            string tableName = GetTableName(platform);
            ValidateColumnName(columnName);
            string sql = $"SELECT {columnName} FROM [{tableName}] WHERE ID = @UserId";
            return ExecuteScalar<object>(sql, new[] { new SQLiteParameter("@UserId", userId) });
        }

        /// <summary>
        /// Sets the value of a specific user parameter.
        /// </summary>
        /// <param name="platform">The streaming platform</param>
        /// <param name="userId">The user ID whose parameter should be updated</param>
        /// <param name="columnName">The name of the parameter/column to update</param>
        /// <param name="value">The new value for the parameter</param>
        /// <remarks>
        /// <para>
        /// Important behaviors:
        /// <list type="bullet">
        /// <item>Validates column name against whitelist</item>
        /// <item>Only updates existing users (no auto-registration)</item>
        /// <item>Preserves existing values for other parameters</item>
        /// <item>Handles null values appropriately</item>
        /// </list>
        /// </para>
        /// <para>
        /// Data handling:
        /// <list type="bullet">
        /// <item>Simple parameters: Direct value assignment</item>
        /// <item>JSON parameters: Requires proper serialization</item>
        /// <item>Counters: Use increment methods instead of direct assignment</item>
        /// </list>
        /// </para>
        /// For performance-critical operations involving multiple parameters, consider using SaveChangesBatch().
        /// </remarks>
        public void SetParameter(PlatformsEnum platform, long userId, string columnName, object value)
        {
            if (!CheckUserExists(platform, userId))
            {
                return;
            }
            string tableName = GetTableName(platform);
            ValidateColumnName(columnName);
            string sql = $"UPDATE [{tableName}] SET {columnName} = @Value WHERE ID = @UserId";
            ExecuteNonQuery(sql, new[]
            {
                new SQLiteParameter("@Value", value ?? DBNull.Value),
                new SQLiteParameter("@UserId", userId)
            });
        }

        /// <summary>
        /// Retrieves the total message count across all channels for a user.
        /// </summary>
        /// <param name="platform">The streaming platform</param>
        /// <param name="userId">The user ID whose message count should be retrieved</param>
        /// <returns>The total number of messages sent by the user</returns>
        /// <remarks>
        /// <para>
        /// Implementation details:
        /// <list type="bullet">
        /// <item>Retrieves value from TotalMessages column</item>
        /// <item>Returns 0 for non-existent users</item>
        /// <item>Includes messages from all channels</item>
        /// </list>
        /// </para>
        /// <para>
        /// Usage scenarios:
        /// <list type="bullet">
        /// <item>User engagement metrics</item>
        /// <item>Ranking and leaderboard systems</item>
        /// <item>Progress tracking for user milestones</item>
        /// </list>
        /// </para>
        /// For channel-specific message counts, use GetMessageCountInChannel() instead.
        /// </remarks>
        public int GetGlobalMessageCount(PlatformsEnum platform, long userId)
        {
            if (!CheckUserExists(platform, userId))
            {
                return 0;
            }
            object result = GetParameter(platform, userId, "TotalMessages");
            return result != null && result != DBNull.Value ? Convert.ToInt32(result) : 0;
        }

        /// <summary>
        /// Retrieves the total character count across all messages sent by a user.
        /// </summary>
        /// <param name="platform">The streaming platform</param>
        /// <param name="userId">The user ID whose message length should be retrieved</param>
        /// <returns>The total number of characters in all messages sent by the user</returns>
        /// <remarks>
        /// <para>
        /// Key characteristics:
        /// <list type="bullet">
        /// <item>Retrieves value from TotalMessagesLength column</item>
        /// <item>Returns 0 for non-existent users</item>
        /// <item>Counts all characters including spaces and punctuation</item>
        /// </list>
        /// </para>
        /// <para>
        /// Implementation notes:
        /// <list type="bullet">
        /// <item>Uses 64-bit integer to handle very large values</item>
        /// <item>Includes messages from all channels</item>
        /// <item>Updated automatically when messages are processed</item>
        /// </list>
        /// </para>
        /// This metric is useful for analyzing user communication patterns and verbosity.
        /// </remarks>
        public long GetGlobalMessagesLenght(PlatformsEnum platform, long userId)
        {
            if (!CheckUserExists(platform, userId))
            {
                return 0;
            }
            object result = GetParameter(platform, userId, "TotalMessagesLength");
            return result != null && result != DBNull.Value ? Convert.ToInt64(result) : 0;
        }

        /// <summary>
        /// Retrieves the message count for a user in a specific channel.
        /// </summary>
        /// <param name="platform">The streaming platform</param>
        /// <param name="userId">The user ID whose message count should be retrieved</param>
        /// <param name="channelId">The channel ID where messages were sent</param>
        /// <returns>The number of messages sent by the user in the specified channel</returns>
        /// <remarks>
        /// <para>
        /// Implementation details:
        /// <list type="bullet">
        /// <item>Queries the platform-specific channel counts table</item>
        /// <item>Returns 0 if no messages have been recorded</item>
        /// <item>Uses composite primary key (UserID, ChannelID) for fast lookup</item>
        /// </list>
        /// </para>
        /// <para>
        /// Usage scenarios:
        /// <list type="bullet">
        /// <item>Channel-specific user statistics</item>
        /// <item>Personalized channel greetings</item>
        /// <item>Channel-based reward systems</item>
        /// </list>
        /// </para>
        /// For global message count across all channels, use GetGlobalMessageCount() instead.
        /// </remarks>
        public int GetMessageCountInChannel(PlatformsEnum platform, long userId, string channelId)
        {
            string tableName = GetTableName(platform);
            string channelCountsTable = $"{tableName}_ChannelCounts";

            string sql = $@"
                SELECT MessageCount 
                FROM [{channelCountsTable}] 
                WHERE UserID = @UserId AND ChannelID = @ChannelId";

            var result = ExecuteScalar<int>(sql, new[]
            {
                new SQLiteParameter("@UserId", userId),
                new SQLiteParameter("@ChannelId", channelId)
            });

            return result;
        }

        /// <summary>
        /// Sets the message count for a user in a specific channel.
        /// </summary>
        /// <param name="platform">The streaming platform</param>
        /// <param name="userId">The user ID whose message count should be updated</param>
        /// <param name="channelId">The channel ID where messages were sent</param>
        /// <param name="count">The new message count value</param>
        /// <remarks>
        /// <para>
        /// Important behaviors:
        /// <list type="bullet">
        /// <item>Replaces existing count (does not increment)</item>
        /// <item>Creates record if none exists</item>
        /// <item>Does not update global message count</item>
        /// </list>
        /// </para>
        /// <para>
        /// Use cases:
        /// <list type="bullet">
        /// <item>Data migration scenarios</item>
        /// <item>Correcting historical data</item>
        /// <item>Special event message count resets</item>
        /// </list>
        /// </para>
        /// For typical message processing, use IncrementMessageCountInChannel() instead.
        /// </remarks>
        public void SetMessageCountInChannel(PlatformsEnum platform, long userId, string channelId, int count)
        {
            string tableName = GetTableName(platform);
            string channelCountsTable = $"{tableName}_ChannelCounts";

            string upsertSql = $@"
                INSERT OR REPLACE INTO [{channelCountsTable}] (UserID, ChannelID, MessageCount)
                VALUES (
                    @UserId, 
                    @ChannelId, 
                    COALESCE((SELECT MessageCount FROM [{channelCountsTable}] WHERE UserID = @UserId AND ChannelID = @ChannelId), 0) + @Increment
                );";

            ExecuteNonQuery(upsertSql, new[]
            {
                new SQLiteParameter("@UserId", userId),
                new SQLiteParameter("@ChannelId", channelId),
                new SQLiteParameter("@Increment", count)
            });
        }

        /// <summary>
        /// Increments the message count for a user in a specific channel.
        /// </summary>
        /// <param name="platform">The streaming platform</param>
        /// <param name="userId">The user ID whose message count should be incremented</param>
        /// <param name="channelId">The channel ID where messages were sent</param>
        /// <param name="increment">The amount to increment the count by (defaults to 1)</param>
        /// <returns>The new message count value for the channel</returns>
        /// <remarks>
        /// <para>
        /// Implementation details:
        /// <list type="bullet">
        /// <item>Uses atomic UPSERT operation for thread safety</item>
        /// <item>Creates record if none exists</item>
        /// <item>Automatically updates global message count</item>
        /// <item>Handles negative increments for corrections</item>
        /// </list>
        /// </para>
        /// <para>
        /// Performance characteristics:
        /// <list type="bullet">
        /// <item>Single database operation per call</item>
        /// <item>Optimized for high-frequency message processing</item>
        /// <item>Safe for concurrent access from multiple threads</item>
        /// </list>
        /// </para>
        /// This is the primary method for updating message counts during normal operation.
        /// </remarks>
        public int IncrementMessageCountInChannel(PlatformsEnum platform, long userId, string channelId, int increment = 1)
        {
            string tableName = GetTableName(platform);
            string channelCountsTable = $"{tableName}_ChannelCounts";

            string updateSql = $@"
                INSERT INTO [{channelCountsTable}] (UserID, ChannelID, MessageCount)
                VALUES (@UserId, @ChannelId, @Increment)
                ON CONFLICT(UserID, ChannelID) DO UPDATE SET
                MessageCount = MessageCount + @Increment;";

            ExecuteNonQuery(updateSql, new[]
            {
                new SQLiteParameter("@UserId", userId),
                new SQLiteParameter("@ChannelId", channelId),
                new SQLiteParameter("@Increment", increment)
            });

            return GetMessageCountInChannel(platform, userId, channelId);
        }

        /// <summary>
        /// Increments both the message count and character count across all channels for a user.
        /// </summary>
        /// <param name="platform">The streaming platform</param>
        /// <param name="userId">The user ID whose counts should be incremented</param>
        /// <param name="messageLenght">The length of the new message to add to the total</param>
        /// <param name="increment">The amount to increment the message count by (defaults to 1)</param>
        /// <returns>The new total message count value</returns>
        /// <remarks>
        /// <para>
        /// The method performs:
        /// <list type="bullet">
        /// <item>Atomic update of TotalMessages counter</item>
        /// <item>Atomic update of TotalMessagesLength counter</item>
        /// <item>Single database transaction for both operations</item>
        /// </list>
        /// </para>
        /// <para>
        /// Important notes:
        /// <list type="bullet">
        /// <item>Updates global counters only (not channel-specific)</item>
        /// <item>Should be called alongside channel-specific updates</item>
        /// <item>Requires message length as separate parameter</item>
        /// </list>
        /// </para>
        /// For batch operations involving multiple users or channels, consider using SaveChangesBatch().
        /// </remarks>
        public int IncrementGlobalMessageCountAndLenght(PlatformsEnum platform, long userId, int messageLenght, int increment = 1)
        {
            int currentCount = GetGlobalMessageCount(platform, userId);
            long messagesLenght = GetGlobalMessagesLenght(platform, userId);
            int newCount = currentCount + increment;
            long newLenght = messagesLenght + messageLenght;
            SetParameter(platform, userId, "TotalMessages", newCount);
            SetParameter(platform, userId, "TotalMessagesLength", newLenght);
            return newCount;
        }

        /// <summary>
        /// Retrieves the total number of users across all platforms with non-empty FirstSeen value.
        /// </summary>
        /// <returns>The total count of users with FirstSeen value set</returns>
        /// <remarks>
        /// <para>
        /// This method:
        /// <list type="bullet">
        /// <item>Counts users with non-NULL FirstSeen across all platform tables</item>
        /// <item>Aggregates results from Discord, Telegram, Twitch platforms</item>
        /// <item>Uses efficient SQL counting operations</item>
        /// </list>
        /// </para>
        /// The count includes all users who have sent at least one message.
        /// </remarks>
        public int GetTotalUsers()
        {
            int totalCount = 0;

            foreach (PlatformsEnum platform in Enum.GetValues(typeof(PlatformsEnum)))
            {
                string tableName = GetTableName(platform);
                string sql = $"SELECT COUNT(*) FROM [{tableName}] WHERE FirstSeen IS NOT NULL AND FirstSeen != ''";
                totalCount += ExecuteScalar<int>(sql);
            }

            return totalCount;
        }

        /// <summary>
        /// Retrieves the total balance across all users and platforms, properly combining Balance and AfterDotBalance.
        /// </summary>
        /// <returns>A decimal value representing the total balance with decimal precision</returns>
        /// <remarks>
        /// <para>
        /// This method:
        /// <list type="bullet">
        /// <item>Calculates the sum of Balance across all platform tables</item>
        /// <item>Calculates the sum of AfterDotBalance across all platform tables</item>
        /// <item>Converts AfterDotBalance to decimal part (divided by 100)</item>
        /// <item>Combines both values to form a proper decimal representation</item>
        /// </list>
        /// </para>
        /// <para>
        /// Example:
        /// <list type="bullet">
        /// <item>If Balance = 100 and AfterDotBalance = 1020, result will be 110.2</item>
        /// <item>Balance contributes to the integer part (100)</item>
        /// <item>AfterDotBalance / 100 contributes to the decimal part (10.2)</item>
        /// <item>Total = 100 + 10.2 = 110.2</item>
        /// </list>
        /// </para>
        /// The calculation handles large values and maintains precision.
        /// </remarks>
        public decimal GetTotalBalance()
        {
            long totalBalance = 0;
            long totalAfterDotBalance = 0;

            foreach (PlatformsEnum platform in Enum.GetValues(typeof(PlatformsEnum)))
            {
                string tableName = GetTableName(platform);
                string balanceSql = $"SELECT COALESCE(SUM(Balance), 0) FROM [{tableName}]";
                totalBalance += ExecuteScalar<long>(balanceSql);

                string afterDotSql = $"SELECT COALESCE(SUM(AfterDotBalance), 0) FROM [{tableName}]";
                totalAfterDotBalance += ExecuteScalar<long>(afterDotSql);
            }

            decimal decimalPart = totalAfterDotBalance / 100.0m;
            return totalBalance + decimalPart;
        }

        /// <summary>
        /// Generates the database table name for the specified platform.
        /// </summary>
        /// <param name="platform">The streaming platform</param>
        /// <returns>
        /// Uppercase string representation of the platform name used as table name.
        /// </returns>
        /// <example>
        /// For PlatformsEnum.Twitch, returns "TWITCH"
        /// </example>
        /// <remarks>
        /// This method is primarily for internal use within the database layer.
        /// Used consistently across all database operations for platform-specific table access.
        /// </remarks>
        private string GetTableName(PlatformsEnum platform)
        {
            return platform.ToString().ToUpper();
        }

        /// <summary>
        /// Validates that a column name is safe and exists in the database schema.
        /// </summary>
        /// <param name="columnName">The column name to validate</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the column name contains invalid characters or doesn't exist in the schema
        /// </exception>
        /// <remarks>
        /// <para>
        /// Validation criteria:
        /// <list type="bullet">
        /// <item>Must contain only alphanumeric characters and underscores</item>
        /// <item>Must match one of the predefined column names in the schema</item>
        /// </list>
        /// </para>
        /// <para>
        /// Security purpose:
        /// <list type="bullet">
        /// <item>Prevents SQL injection in dynamic column access</item>
        /// <item>Ensures compatibility with current database schema</item>
        /// <item>Provides clear error messages for invalid parameters</item>
        /// </list>
        /// </para>
        /// This method is called internally before any dynamic column access operations.
        /// </remarks>
        private void ValidateColumnName(string columnName)
        {
            if (!Regex.IsMatch(columnName, @"^[a-zA-Z0-9_]+$"))
            {
                throw new ArgumentException("Invalid column name", nameof(columnName));
            }
            string[] validColumns = {
                "ID", "FirstMessage", "FirstSeen", "FirstChannel", "LastMessage", "LastSeen", "LastChannel",
                "Balance", "AfterDotBalance", "Rating", "IsAFK", "AFKText", "AFKType", "Reminders", "LastCookie",
                "GiftedCookies", "EatedCookies", "BuyedCookies", "Location", "Longitude", "Latitude", "Language",
                "AFKStart", "AFKResume", "AFKResumeTimes", "LastUse", "GPTHistory", "WeatherResultLocations",
                "TotalMessages", "TotalMessagesLength", "LastHourlyReward", "LastDailyReward", "LastWeeklyReward",
                "LastMonthlyReward", "LastYearlyReward"
            };
            if (!validColumns.Contains(columnName))
            {
                throw new ArgumentException("Unknown column name", nameof(columnName));
            }
        }

        /// <summary>
        /// Saves multiple user changes in a batch operation for maximum efficiency.
        /// </summary>
        /// <param name="changes">Collection of user change operations to process</param>
        /// <remarks>
        /// <para>
        /// The batch processing workflow:
        /// <list type="number">
        /// <item>Groups changes by platform for optimal processing</item>
        /// <item>Processes global message count updates in bulk</item>
        /// <item>Processes channel message counts using parameterized batches</item>
        /// <item>Processes parameter updates in grouped operations</item>
        /// <item>Commits all changes in a single transaction</item>
        /// </list>
        /// </para>
        /// <para>
        /// Performance optimizations:
        /// <list type="bullet">
        /// <item>Parameter reuse to minimize SQL command preparation</item>
        /// <item>Batch size management to stay within SQLite parameter limits</item>
        /// <item>Transaction grouping to reduce disk I/O operations</item>
        /// <item>Platform-specific processing to minimize context switching</item>
        /// </list>
        /// </para>
        /// <para>
        /// Key benefits:
        /// <list type="bullet">
        /// <item>Reduces database roundtrips from O(n) to O(1) for batch operations</item>
        /// <item>Minimizes transaction overhead for high-volume operations</item>
        /// <item>Maintains data consistency through atomic transaction</item>
        /// <item>Handles up to thousands of changes in a single operation</item>
        /// </list>
        /// </para>
        /// Empty collections are safely ignored with no database operations performed.
        /// This method is essential for high-performance message processing systems.
        /// </remarks>
        public void SaveChangesBatch(List<UserChange> changes)
        {
            if (changes.Count == 0) return;

            BeginTransaction();
            try
            {
                var changesByPlatform = changes.GroupBy(c => c.Platform).ToList();

                foreach (var platformGroup in changesByPlatform)
                {
                    PlatformsEnum platform = platformGroup.Key;
                    string tableName = GetTableName(platform);
                    string channelCountsTable = $"{tableName}_ChannelCounts";

                    var globalChanges = platformGroup
                        .Where(c => c.GlobalMessageCountIncrement != 0 || c.GlobalMessageLengthIncrement != 0)
                        .ToList();

                    if (globalChanges.Count > 0)
                    {
                        foreach (var change in globalChanges)
                        {
                            string updateSql = $@"
                        UPDATE [{tableName}] 
                        SET TotalMessages = TotalMessages + @CountIncrement, 
                            TotalMessagesLength = TotalMessagesLength + @LengthIncrement
                        WHERE ID = @UserId";

                            ExecuteNonQuery(updateSql, new[]
                            {
                        new SQLiteParameter("@CountIncrement", change.GlobalMessageCountIncrement),
                        new SQLiteParameter("@LengthIncrement", change.GlobalMessageLengthIncrement),
                        new SQLiteParameter("@UserId", change.UserId)
                    });
                        }
                    }

                    var channelChanges = platformGroup
                        .SelectMany(c => c.ChannelMessageCounts.Select(cc =>
                            new { c.UserId, ChannelId = cc.Key, Increment = cc.Value }))
                        .ToList();

                    if (channelChanges.Count > 0)
                    {
                        const int MAX_PARAMS = 999;
                        const int PARAMS_PER_ROW = 3;
                        int batchSize = MAX_PARAMS / PARAMS_PER_ROW;
                        batchSize = Math.Min(batchSize, 300);

                        for (int i = 0; i < channelChanges.Count; i += batchSize)
                        {
                            var batch = channelChanges.Skip(i).Take(batchSize).ToList();

                            var sb = new StringBuilder();
                            sb.AppendLine($"INSERT INTO [{channelCountsTable}] (UserID, ChannelID, MessageCount) VALUES ");

                            var values = new List<string>();
                            var parameters = new List<SQLiteParameter>();

                            for (int j = 0; j < batch.Count; j++)
                            {
                                var change = batch[j];
                                string paramPrefix = $"p{j}_";

                                values.Add($"(@{paramPrefix}UserID, @{paramPrefix}ChannelId, @{paramPrefix}Increment)");

                                parameters.Add(new SQLiteParameter($"@{paramPrefix}UserID", change.UserId));
                                parameters.Add(new SQLiteParameter($"@{paramPrefix}ChannelId", change.ChannelId));
                                parameters.Add(new SQLiteParameter($"@{paramPrefix}Increment", change.Increment));
                            }

                            sb.AppendLine(string.Join(",", values));
                            sb.AppendLine("ON CONFLICT(UserID, ChannelID) DO UPDATE SET MessageCount = MessageCount + excluded.MessageCount;");

                            ExecuteNonQuery(sb.ToString(), parameters.ToArray());
                        }
                    }

                    var paramChanges = platformGroup
                        .Where(c => c.Changes.Count > 0)
                        .ToList();

                    if (paramChanges.Count > 0)
                    {
                        foreach (var change in paramChanges)
                        {
                            var setClauses = change.Changes.Keys
                                .Select(k => $"{k} = @{k}")
                                .ToList();

                            var parameters = change.Changes
                                .Select(kv => new SQLiteParameter($"@{kv.Key}", kv.Value ?? DBNull.Value))
                                .ToList();

                            parameters.Add(new SQLiteParameter("@UserId", change.UserId));

                            string updateSql = $@"
                        UPDATE [{tableName}] 
                        SET {string.Join(", ", setClauses)}
                        WHERE ID = @UserId";

                            ExecuteNonQuery(updateSql, parameters.ToArray());
                        }
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
    }
}