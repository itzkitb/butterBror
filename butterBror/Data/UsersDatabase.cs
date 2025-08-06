using butterBror.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text.RegularExpressions;

namespace butterBror.Data
{
    /// <summary>
    /// A thread-safe database manager for user data storage and operations across multiple platforms.
    /// Provides functionality for tracking user statistics, preferences, and activity history.
    /// </summary>
    public class UsersDatabase : SqlDatabaseBase
    {
        private readonly ConcurrentDictionary<(PlatformsEnum platform, string username), long> _usernameCache = new();

        /// <summary>
        /// Initializes a new instance of the UsersDatabase class with the specified database file path.
        /// </summary>
        /// <param name="dbPath">The path to the SQLite database file. Defaults to "Users.db" if not specified.</param>
        public UsersDatabase(string dbPath = "Users.db")
            : base(dbPath, true)
        {
            ConfigureSqlitePerformance();
            InitializeDatabase();
            MigrateOldDataIfNeeded();
        }

        private void ConfigureSqlitePerformance()
        {
            ExecuteNonQuery("PRAGMA journal_mode = WAL;");
            ExecuteNonQuery("PRAGMA synchronous = NORMAL;");
            ExecuteNonQuery("PRAGMA cache_size = -500000;");
            ExecuteNonQuery("PRAGMA temp_store = MEMORY;");
            ExecuteNonQuery("PRAGMA foreign_keys = ON;");
        }

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
        /// Initializes and configures the database schema by creating necessary tables and indexes for user data storage.
        /// This method ensures the database structure is properly set up for tracking user profiles and activity metrics.
        /// </summary>
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

                        // Новая таблица для счетчиков сообщений по каналам
                        string channelCountsTable = $"{tableName}_ChannelCounts";
                        string createChannelCountsTable = $@"
                    CREATE TABLE IF NOT EXISTS [{channelCountsTable}] (
                        UserID INTEGER NOT NULL,
                        ChannelID TEXT NOT NULL,
                        MessageCount INTEGER DEFAULT 0,
                        PRIMARY KEY (UserID, ChannelID)
                    );
                    CREATE INDEX IF NOT EXISTS idx_{channelCountsTable}_userid ON [{channelCountsTable}](UserID);";

                        ExecuteNonQuery(createTableSql);
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
        /// <param name="platform">The streaming platform (Twitch, YouTube, etc.)</param>
        /// <param name="userId">The unique identifier of the user</param>
        /// <returns><c>true</c> if the user exists in the database; otherwise, <c>false</c></returns>
        public bool CheckUserExists(PlatformsEnum platform, long userId)
        {
            string tableName = GetTableName(platform);
            string sql = $"SELECT 1 FROM [{tableName}] WHERE ID = @UserId LIMIT 1";
            return ExecuteScalar<object>(sql, new[] { new SQLiteParameter("@UserId", userId) }) != null;
        }

        /// <summary>
        /// Registers a new user with default parameters if they don't already exist.
        /// </summary>
        /// <param name="platform">The streaming platform (Twitch, YouTube, etc.)</param>
        /// <param name="userId">The unique identifier of the user</param>
        /// <param name="initialLanguage">The default language setting for the user (defaults to "en-US")</param>
        /// <param name="initialMessage">The first message sent by the user (optional)</param>
        /// <param name="initialChannel">The first channel where the user interacted (optional)</param>
        /// <returns><c>true</c> if the user was successfully registered; <c>false</c> if the user already exists</returns>
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
                    TotalMessages, TotalMessagesLength, ChannelMessagesCount
                ) VALUES (
                    @ID, @FirstSeen, @FirstMessage, @FirstChannel, 
                    @LastSeen, @LastMessage, @LastChannel,
                    0, 0, 500,
                    0, '', '', '',
                    '[]', '', 0, 
                    0, 0, '',
                    '', '', @Language,
                    '', 0, '[]', 
                    '[]', '[]',
                    1, @TotalMessagesLength, '[]'
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
        /// This method removes any existing mappings before adding the new one.
        /// </summary>
        /// <param name="platform">The streaming platform (Twitch, YouTube, etc.)</param>
        /// <param name="userId">The unique identifier of the user</param>
        /// <param name="username">The username to map to the user ID</param>
        /// <returns><c>true</c> if the mapping was successfully added; otherwise, <c>false</c></returns>
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
        /// <param name="platform">The streaming platform (Twitch, YouTube, etc.)</param>
        /// <param name="username">The username to look up</param>
        /// <returns>The user ID if found; otherwise, <c>null</c></returns>
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
        /// <param name="platform">The streaming platform (Twitch, YouTube, etc.)</param>
        /// <param name="userId">The user ID to look up</param>
        /// <returns>The username if found; otherwise, <c>null</c></returns>
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
        /// <param name="platform">The streaming platform (Twitch, YouTube, etc.)</param>
        /// <param name="userId">The user ID whose mappings should be removed</param>
        /// <returns>The number of mappings removed</returns>
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
        /// <param name="platform">The streaming platform (Twitch, YouTube, etc.)</param>
        /// <param name="userId">The user ID to look up</param>
        /// <returns>A list of usernames associated with the user ID</returns>
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
        /// <param name="platform">The streaming platform (Twitch, YouTube, etc.)</param>
        /// <param name="userId">The user ID whose parameter should be retrieved</param>
        /// <param name="columnName">The name of the parameter/column to retrieve</param>
        /// <returns>The value of the parameter, or <c>null</c> if not found</returns>
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
        /// <param name="platform">The streaming platform (Twitch, YouTube, etc.)</param>
        /// <param name="userId">The user ID whose parameter should be updated</param>
        /// <param name="columnName">The name of the parameter/column to update</param>
        /// <param name="value">The new value for the parameter</param>
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
        /// <param name="platform">The streaming platform (Twitch, YouTube, etc.)</param>
        /// <param name="userId">The user ID whose message count should be retrieved</param>
        /// <returns>The total number of messages sent by the user</returns>
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
        /// Retrieves the message count for a user in a specific channel.
        /// </summary>
        /// <param name="platform">The streaming platform (Twitch, YouTube, etc.)</param>
        /// <param name="userId">The user ID whose message count should be retrieved</param>
        /// <param name="channelId">The channel ID where messages were sent</param>
        /// <returns>The number of messages sent by the user in the specified channel</returns>
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
        /// <param name="platform">The streaming platform (Twitch, YouTube, etc.)</param>
        /// <param name="userId">The user ID whose message count should be updated</param>
        /// <param name="channelId">The channel ID where messages were sent</param>
        /// <param name="count">The new message count value</param>
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
        /// <param name="platform">The streaming platform (Twitch, YouTube, etc.)</param>
        /// <param name="userId">The user ID whose message count should be incremented</param>
        /// <param name="channelId">The channel ID where messages were sent</param>
        /// <param name="increment">The amount to increment the count by (defaults to 1)</param>
        /// <returns>The new message count value for the channel</returns>
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
        /// Increments the total message count across all channels for a user.
        /// </summary>
        /// <param name="platform">The streaming platform (Twitch, YouTube, etc.)</param>
        /// <param name="userId">The user ID whose message count should be incremented</param>
        /// <param name="increment">The amount to increment the count by (defaults to 1)</param>
        /// <returns>The new total message count value</returns>
        public int IncrementGlobalMessageCount(PlatformsEnum platform, long userId, int increment = 1)
        {
            int currentCount = GetGlobalMessageCount(platform, userId);
            int newCount = currentCount + increment;
            SetParameter(platform, userId, "TotalMessages", newCount);
            return newCount;
        }

        /// <summary>
        /// Generates the database table name for the specified platform.
        /// The table name follows the format: PLATFORM_NAME (all uppercase).
        /// </summary>
        /// <param name="platform">The streaming platform (Twitch, YouTube, etc.)</param>
        /// <returns>The generated table name</returns>
        private string GetTableName(PlatformsEnum platform)
        {
            return platform.ToString().ToUpper();
        }

        /// <summary>
        /// Validates that a column name is safe and exists in the database schema.
        /// Throws an exception if the column name is invalid or doesn't exist.
        /// </summary>
        /// <param name="columnName">The column name to validate</param>
        /// <exception cref="ArgumentException">Thrown when an invalid column name is specified</exception>
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
                "TotalMessages", "TotalMessagesLength", "ChannelMessagesCount"
            };
            if (!validColumns.Contains(columnName))
            {
                throw new ArgumentException("Unknown column name", nameof(columnName));
            }
        }
    }
}