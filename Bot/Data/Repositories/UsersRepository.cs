using bb.Core.Bot;
using bb.Data.Entities;
using bb.Models.Platform;
using bb.Models.Users;
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
        private readonly ConcurrentDictionary<(Platform platform, string username), long> _usernameCache = new();

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
            foreach (Platform platform in Enum.GetValues(typeof(Platform)))
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
                foreach (Platform platform in Enum.GetValues(typeof(Platform)))
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
        /// Initializes the database schema by creating all required tables and indexes for user data management.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Creates a single unified user table instead of platform-specific tables.
        /// </para>
        /// <para>
        /// The new schema includes:
        /// <list type="bullet">
        /// <item>Single table for all users across all platforms</item>
        /// <item>Platform column to distinguish users by platform</item>
        /// <item>Decimal balance type for precise financial calculations</item>
        /// <item>Enum-based columns for roles and languages</item>
        /// <item>Renamed columns to follow consistent naming conventions</item>
        /// </list>
        /// </para>
        /// <para>
        /// The initialization is wrapped in a transaction to ensure atomic creation of all database objects.
        /// </para>
        /// </remarks>
        private void InitializeDatabase()
        {
            using (var transaction = Connection.BeginTransaction())
            {
                try
                {
                    string createTableSql = @"
                CREATE TABLE IF NOT EXISTS Users (
                    ID INTEGER NOT NULL,
                    Platform INTEGER NOT NULL,
                    FirstMessage TEXT,
                    FirstSeen TEXT,
                    FirstChannel TEXT,
                    LastMessage TEXT,
                    LastSeen TEXT,
                    LastChannel TEXT,
                    Balance DECIMAL(18, 2) DEFAULT 0.0,
                    Role INTEGER DEFAULT 2, -- Default: Public (2)
                    IsAfk INTEGER DEFAULT 0,
                    AfkMessage TEXT DEFAULT '',
                    AfkType TEXT DEFAULT '',
                    AfkStartTime TEXT DEFAULT '',
                    Reminders TEXT DEFAULT '[]',
                    LastCookie TEXT DEFAULT '',
                    GiftedCookies INTEGER DEFAULT 0,
                    EatedCookies INTEGER DEFAULT 0,
                    BuyedCookies INTEGER DEFAULT 0,
                    Location TEXT DEFAULT '',
                    Longitude TEXT DEFAULT '',
                    Latitude TEXT DEFAULT '',
                    Language INTEGER DEFAULT 1, -- Default: EnUs (1)
                    AfkResume TEXT DEFAULT '',
                    AfkResumeCount INTEGER DEFAULT 0,
                    LastUse TEXT DEFAULT '[]',
                    AiHistory TEXT DEFAULT '[]',
                    WeatherResultLocations TEXT DEFAULT '[]',
                    TotalMessages INTEGER DEFAULT 1,
                    TotalMessagesLength INTEGER DEFAULT 0,
                    LastHourlyReward TEXT DEFAULT '2000-01-01T00:00:00.0000000Z',
                    LastDailyReward TEXT DEFAULT '2000-01-01T00:00:00.0000000Z',
                    LastWeeklyReward TEXT DEFAULT '2000-01-01T00:00:00.0000000Z',
                    LastMonthlyReward TEXT DEFAULT '2000-01-01T00:00:00.0000000Z',
                    LastYearlyReward TEXT DEFAULT '2000-01-01T00:00:00.0000000Z',
                    BanReason TEXT DEFAULT '',
                    PRIMARY KEY (Platform, ID)
                );
                CREATE INDEX IF NOT EXISTS idx_users_id ON Users(ID);
                CREATE INDEX IF NOT EXISTS idx_users_platform ON Users(Platform);";

                    string createChannelCountsTable = @"
                CREATE TABLE IF NOT EXISTS ChannelCounts (
                    UserID INTEGER NOT NULL,
                    Platform INTEGER NOT NULL,
                    ChannelID TEXT NOT NULL,
                    MessageCount INTEGER DEFAULT 0,
                    PRIMARY KEY (UserID, Platform, ChannelID)
                );
                CREATE INDEX IF NOT EXISTS idx_channelcounts_userid ON ChannelCounts(UserID);
                CREATE INDEX IF NOT EXISTS idx_channelcounts_platform ON ChannelCounts(Platform);
                CREATE INDEX IF NOT EXISTS idx_channelcounts_channelid ON ChannelCounts(ChannelID);";

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

                    ExecuteNonQuery(createTableSql);
                    ExecuteNonQuery(createChannelCountsTable);
                    ExecuteNonQuery(createMappingTableSql);
                    transaction.Commit();

                    MigrateOldData();
                }
                catch (Exception ex)
                {
                    Logger.Write(ex);
                    transaction.Rollback();
                    throw;
                }
            }
        }

        /// <summary>
        /// Migrates data from old platform-specific tables to the new unified structure.
        /// </summary>
        private void MigrateOldData()
        {
            BeginTransaction();
            try
            {
                foreach (Platform platform in Enum.GetValues(typeof(Platform)))
                {
                    string oldTableName = platform.ToString().ToUpper();

                    string checkTableSql = $@"
                SELECT COUNT(*) 
                FROM sqlite_master 
                WHERE type='table' AND name='{oldTableName}'";

                    if (ExecuteScalar<int>(checkTableSql) == 0)
                        continue;

                    string migrateUsersSql = $@"
INSERT INTO Users (ID, Platform, FirstMessage, FirstSeen, FirstChannel,
                    LastMessage, LastSeen, LastChannel, Balance,
                    IsAfk, AfkMessage, AfkType, AfkStartTime, Reminders,
                    LastCookie, GiftedCookies, EatedCookies, BuyedCookies,
                    Location, Longitude, Latitude, Language, AfkResume,
                    AfkResumeCount, LastUse, AiHistory, WeatherResultLocations,
                    TotalMessages, TotalMessagesLength, LastHourlyReward,
                    LastDailyReward, LastWeeklyReward, LastMonthlyReward,
                    LastYearlyReward, BanReason)
SELECT ID, {((int)platform)}, FirstMessage, FirstSeen, FirstChannel,
       LastMessage, LastSeen, LastChannel, Balance,
       IsAFK, AFKText, AFKType, AFKStart, Reminders,
       LastCookie, GiftedCookies, EatedCookies, BuyedCookies,
       Location, Longitude, Latitude,
       CASE
           WHEN Language = 'en-US' THEN {(int)Language.EnUs}
           WHEN Language = 'ru-RU' THEN {(int)Language.RuRu}
           ELSE {(int)Language.EnUs}
       END,
       AFKResume,
       AFKResumeTimes, LastUse, GPTHistory, WeatherResultLocations,
       TotalMessages, TotalMessagesLength, LastHourlyReward,
       LastDailyReward, LastWeeklyReward, LastMonthlyReward,
       LastYearlyReward, ''
FROM [{oldTableName}]";

                    ExecuteNonQuery(migrateUsersSql);

                    string channelCountsTable = $"{oldTableName}_ChannelCounts";
                    string checkChannelCountsTableSql = $@"
                SELECT COUNT(*) 
                FROM sqlite_master 
                WHERE type='table' AND name='{channelCountsTable}'";

                    if (ExecuteScalar<int>(checkChannelCountsTableSql) > 0)
                    {
                        string migrateChannelCountsSql = $@"
                    INSERT INTO ChannelCounts (UserID, Platform, ChannelID, MessageCount)
                    SELECT UserID, {((int)platform)}, ChannelID, MessageCount
                    FROM [{channelCountsTable}]";

                        ExecuteNonQuery(migrateChannelCountsSql);
                    }

                    string migrateUsernameMappingsSql = $@"
                INSERT INTO UsernameMapping (Platform, UserID, Username)
                SELECT '{platform}', UserID, Username
                FROM UsernameMapping
                WHERE Platform = '{platform.ToString().ToUpper()}'";

                    ExecuteNonQuery(migrateUsernameMappingsSql);

                    string dropOldTableSql = $"DROP TABLE IF EXISTS [{oldTableName}]";
                    ExecuteNonQuery(dropOldTableSql);

                    string dropChannelCountsTableSql = $"DROP TABLE IF EXISTS [{channelCountsTable}]";
                    ExecuteNonQuery(dropChannelCountsTableSql);
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
        private void MigratePlatformData(Platform platform)
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
                    Core.Bot.Logger.Write(ex);
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
        /// Checks if a user exists in the database.
        /// </summary>
        /// <param name="platform">The streaming platform (Twitch, Discord, Telegram, etc.)</param>
        /// <param name="userId">The unique numeric identifier of the user</param>
        /// <returns>
        /// <see langword="true"/> if the user exists in the database;
        /// <see langword="false"/> if the user has not been registered
        /// </returns>
        public bool CheckUserExists(Platform platform, long userId)
        {
            string sql = "SELECT 1 FROM Users WHERE Platform = @Platform AND ID = @UserId LIMIT 1";
            return ExecuteScalar<object>(sql, new[]
            {
        new SQLiteParameter("@Platform", (int)platform),
        new SQLiteParameter("@UserId", userId)
    }) != null;
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
        public bool RegisterNewUser(Platform platform, long userId, Language initialLanguage = Language.EnUs, string initialMessage = "", string initialChannel = "")
        {
            if (CheckUserExists(platform, userId))
            {
                return false;
            }

            DateTime now = DateTime.UtcNow;
            string sql = @"
        INSERT INTO Users (
            ID, Platform, FirstSeen, FirstMessage, FirstChannel, 
            LastSeen, LastMessage, LastChannel,
            Balance, Role, IsAfk, AfkMessage, AfkType, AfkStartTime,
            Reminders, LastCookie, GiftedCookies, EatedCookies, BuyedCookies, Location,
            Longitude, Latitude, Language, AfkResume, AfkResumeCount, LastUse, 
            AiHistory, WeatherResultLocations,
            TotalMessages, TotalMessagesLength
        ) VALUES (
            @ID, @Platform, @FirstSeen, @FirstMessage, @FirstChannel, 
            @LastSeen, @LastMessage, @LastChannel,
            0.0, 2, 0, '', '', '',
            '[]', '', 0, 0, 0, '',
            '', '', 1, '', 0, '[]', 
            '[]', '[]',
            1, @TotalMessagesLength
        );";

            ExecuteNonQuery(sql, new[]
            {
        new SQLiteParameter("@ID", userId),
        new SQLiteParameter("@Platform", (int)platform),
        new SQLiteParameter("@FirstSeen", now.ToString("o")),
        new SQLiteParameter("@LastSeen", now.ToString("o")),
        new SQLiteParameter("@Language", (int)Language.EnUs),
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
        public bool AddUsernameMapping(Platform platform, long userId, string username)
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
        public long? GetUserIdByUsername(Platform platform, string username)
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
        public string GetUsernameByUserId(Platform platform, long userId)
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
        public int RemoveUsernameMapping(Platform platform, long userId)
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
        public List<string> GetAllUsernames(Platform platform, long userId)
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
        public object GetParameter(Platform platform, long userId, string columnName)
        {
            if (!CheckUserExists(platform, userId))
            {
                return null;
            }

            ValidateColumnName(columnName);
            string sql = $"SELECT {columnName} FROM Users WHERE Platform = @Platform AND ID = @UserId";
            return ExecuteScalar<object>(sql, new[]
            {
        new SQLiteParameter("@Platform", (int)platform),
        new SQLiteParameter("@UserId", userId)
    });
        }

        /// <summary>
        /// Sets the value of a specific user parameter.
        /// </summary>
        /// <param name="platform">The streaming platform</param>
        /// <param name="userId">The user ID whose parameter should be updated</param>
        /// <param name="columnName">The name of the parameter/column to update</param>
        /// <param name="value">The new value for the parameter</param>
        public void SetParameter(Platform platform, long userId, string columnName, object value)
        {
            if (!CheckUserExists(platform, userId))
            {
                return;
            }

            ValidateColumnName(columnName);
            string sql = $"UPDATE Users SET {columnName} = @Value WHERE Platform = @Platform AND ID = @UserId";
            ExecuteNonQuery(sql, new[]
            {
        new SQLiteParameter("@Value", value ?? DBNull.Value),
        new SQLiteParameter("@Platform", (int)platform),
        new SQLiteParameter("@UserId", userId)
    });
        }

        /// <summary>
        /// Retrieves the total message count across all channels for a user.
        /// </summary>
        /// <param name="platform">The streaming platform</param>
        /// <param name="userId">The user ID whose message count should be retrieved</param>
        /// <returns>The total number of messages sent by the user</returns>
        public int GetGlobalMessageCount(Platform platform, long userId)
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
        public long GetGlobalMessagesLenght(Platform platform, long userId)
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
        public int GetMessageCountInChannel(Platform platform, long userId, string channelId)
        {
            string sql = @"
        SELECT MessageCount 
        FROM ChannelCounts 
        WHERE UserID = @UserId AND Platform = @Platform AND ChannelID = @ChannelId";

            var result = ExecuteScalar<int>(sql, new[]
            {
        new SQLiteParameter("@UserId", userId),
        new SQLiteParameter("@Platform", (int)platform),
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
        public void SetMessageCountInChannel(Platform platform, long userId, string channelId, int count)
        {
            string sql = @"
        INSERT OR REPLACE INTO ChannelCounts (UserID, Platform, ChannelID, MessageCount)
        VALUES (@UserId, @Platform, @ChannelId, @Count)";

            ExecuteNonQuery(sql, new[]
            {
        new SQLiteParameter("@UserId", userId),
        new SQLiteParameter("@Platform", (int)platform),
        new SQLiteParameter("@ChannelId", channelId),
        new SQLiteParameter("@Count", count)
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
        public int IncrementMessageCountInChannel(Platform platform, long userId, string channelId, int increment = 1)
        {
            string sql = @"
        INSERT INTO ChannelCounts (UserID, Platform, ChannelID, MessageCount)
        VALUES (@UserId, @Platform, @ChannelId, @Increment)
        ON CONFLICT(UserID, Platform, ChannelID) DO UPDATE SET
        MessageCount = MessageCount + @Increment;";

            ExecuteNonQuery(sql, new[]
            {
        new SQLiteParameter("@UserId", userId),
        new SQLiteParameter("@Platform", (int)platform),
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
        public int IncrementGlobalMessageCountAndLenght(Platform platform, long userId, int messageLength, int increment = 1)
        {
            int currentCount = GetGlobalMessageCount(platform, userId);
            long messagesLength = GetGlobalMessagesLenght(platform, userId);
            int newCount = currentCount + increment;
            long newLength = messagesLength + messageLength;

            SetParameter(platform, userId, "TotalMessages", newCount);
            SetParameter(platform, userId, "TotalMessagesLength", newLength);

            return newCount;
        }

        /// <summary>
        /// Retrieves the total number of users across all platforms with non-empty FirstSeen value.
        /// </summary>
        /// <returns>The total count of users with FirstSeen value set</returns>
        public int GetTotalUsers()
        {
            string sql = "SELECT COUNT(*) FROM Users WHERE FirstSeen IS NOT NULL AND FirstSeen != ''";
            return ExecuteScalar<int>(sql);
        }

        /// <summary>
        /// Retrieves the total balance across all users.
        /// </summary>
        /// <returns>A decimal value representing the total balance with decimal precision</returns>
        public decimal GetTotalBalance()
        {
            string sql = "SELECT COALESCE(SUM(Balance), 0) FROM Users";
            return ExecuteScalar<decimal>(sql);
        }

        /// <summary>
        /// Validates that a column name is safe and exists in the database schema.
        /// </summary>
        /// <param name="columnName">The column name to validate</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the column name contains invalid characters or doesn't exist in the schema
        /// </exception>
        private void ValidateColumnName(string columnName)
        {
            if (!Regex.IsMatch(columnName, @"^[a-zA-Z0-9_]+$"))
            {
                throw new ArgumentException("Invalid column name", nameof(columnName));
            }

            string[] validColumns = {
        "ID", "Platform", "FirstMessage", "FirstSeen", "FirstChannel", "LastMessage", "LastSeen", "LastChannel",
        "Balance", "Role", "IsAfk", "AfkMessage", "AfkType", "AfkStartTime", "Reminders", "LastCookie",
        "GiftedCookies", "EatedCookies", "BuyedCookies", "Location", "Longitude", "Latitude", "Language",
        "AfkResume", "AfkResumeCount", "LastUse", "AiHistory", "WeatherResultLocations",
        "TotalMessages", "TotalMessagesLength", "LastHourlyReward", "LastDailyReward", "LastWeeklyReward",
        "LastMonthlyReward", "LastYearlyReward", "BanReason"
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
        public void SaveChangesBatch(List<UserChange> changes)
        {
            if (changes.Count == 0) return;

            BeginTransaction();
            try
            {
                // Process global message count and length updates
                var globalChanges = changes
                    .Where(c => c.GlobalMessageCountIncrement != 0 || c.GlobalMessageLengthIncrement != 0)
                    .ToList();

                if (globalChanges.Count > 0)
                {
                    foreach (var change in globalChanges)
                    {
                        string updateSql = @"
                    UPDATE Users 
                    SET TotalMessages = TotalMessages + @CountIncrement, 
                        TotalMessagesLength = TotalMessagesLength + @LengthIncrement
                    WHERE Platform = @Platform AND ID = @UserId";

                        ExecuteNonQuery(updateSql, new[]
                        {
                    new SQLiteParameter("@CountIncrement", change.GlobalMessageCountIncrement),
                    new SQLiteParameter("@LengthIncrement", change.GlobalMessageLengthIncrement),
                    new SQLiteParameter("@Platform", (int)change.Platform),
                    new SQLiteParameter("@UserId", change.UserId)
                });
                    }
                }

                // Process channel message count updates
                var channelChanges = changes
                    .SelectMany(c => c.ChannelMessageCounts.Select(cc =>
                        new { c.Platform, c.UserId, ChannelId = cc.Key, Increment = cc.Value }))
                    .ToList();

                if (channelChanges.Count > 0)
                {
                    const int MAX_PARAMS = 999;
                    const int PARAMS_PER_ROW = 4; // UserID, Platform, ChannelId, Increment
                    int batchSize = MAX_PARAMS / PARAMS_PER_ROW;
                    batchSize = Math.Min(batchSize, 300);

                    for (int i = 0; i < channelChanges.Count; i += batchSize)
                    {
                        var batch = channelChanges.Skip(i).Take(batchSize).ToList();
                        var sb = new StringBuilder();
                        sb.AppendLine("INSERT INTO ChannelCounts (UserID, Platform, ChannelID, MessageCount) VALUES ");
                        var values = new List<string>();
                        var parameters = new List<SQLiteParameter>();

                        for (int j = 0; j < batch.Count; j++)
                        {
                            var change = batch[j];
                            string paramPrefix = $"p{j}_";
                            values.Add($"(@{paramPrefix}UserID, @{paramPrefix}Platform, @{paramPrefix}ChannelId, @{paramPrefix}Increment)");

                            parameters.Add(new SQLiteParameter($"@{paramPrefix}UserID", change.UserId));
                            parameters.Add(new SQLiteParameter($"@{paramPrefix}Platform", (int)change.Platform));
                            parameters.Add(new SQLiteParameter($"@{paramPrefix}ChannelId", change.ChannelId));
                            parameters.Add(new SQLiteParameter($"@{paramPrefix}Increment", change.Increment));
                        }

                        sb.AppendLine(string.Join(",", values));
                        sb.AppendLine("ON CONFLICT(UserID, Platform, ChannelID) DO UPDATE SET MessageCount = MessageCount + excluded.MessageCount;");

                        ExecuteNonQuery(sb.ToString(), parameters.ToArray());
                    }
                }

                // Process parameter updates
                var paramChanges = changes
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

                        parameters.Add(new SQLiteParameter("@Platform", (int)change.Platform));
                        parameters.Add(new SQLiteParameter("@UserId", change.UserId));

                        string updateSql = $@"
                    UPDATE Users 
                    SET {string.Join(", ", setClauses)}
                    WHERE Platform = @Platform AND ID = @UserId";

                        ExecuteNonQuery(updateSql, parameters.ToArray());
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
        private string GetTableName(Platform platform)
        {
            return platform.ToString().ToUpper();
        }
    }
}