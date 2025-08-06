using butterBror.Models;
using butterBror.Models.DataBase;
using butterBror.Utils;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Data.SQLite;

namespace butterBror.Data
{
    /// <summary>
    /// A thread-safe database manager for channel-related data storage and operations across multiple platforms.
    /// Provides functionality for command cooldown management, banned word filtering, and tracking first user messages.
    /// </summary>
    public class ChannelsDatabase : SqlDatabaseBase
    {
        private readonly Dictionary<(PlatformsEnum platform, string channelId), HashSet<string>> _banWordsCache = new();
        private readonly object _cacheLock = new object();

        /// <summary>
        /// Initializes a new instance of the ChannelsDatabase class with the specified database file path.
        /// </summary>
        /// <param name="dbPath">The path to the SQLite database file. Defaults to "Channels.db" if not specified.</param>
        public ChannelsDatabase(string dbPath = "Channels.db")
            : base(dbPath, true)
        {
            ConfigureSqlitePerformance();
            InitializeDatabase();
            MigrateOldDataIfNeeded();
        }

        private string GetCDDTableName(PlatformsEnum platform) => $"CommandCooldowns_{platform.ToString().ToUpper()}";
        private string GetBanWordsTableName(PlatformsEnum platform) => $"BanWords_{platform.ToString().ToUpper()}";

        private void ConfigureSqlitePerformance()
        {
            ExecuteNonQuery("PRAGMA journal_mode = WAL;");
            ExecuteNonQuery("PRAGMA synchronous = NORMAL;");
            ExecuteNonQuery("PRAGMA cache_size = -500000;");
            ExecuteNonQuery("PRAGMA temp_store = MEMORY;");
        }

        /// <summary>
        /// Initializes and configures the database schema by creating necessary tables and indexes for all supported platforms.
        /// This method ensures the database structure is properly set up for channel data storage, command cooldown tracking, and first message recording.
        /// </summary>
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

                        ExecuteNonQuery(createCDDTable);
                        ExecuteNonQuery(createBanWordsTable);
                        ExecuteNonQuery(createFirstMessagesTable);
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
                Core.Bot.Console.Write(ex);
                throw;
            }
        }

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
                        var lastUses = Format.ParseStringDictionary(cddData);
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
        /// Checks if a command is currently in cooldown period for the specified channel and platform.
        /// If the command is not in cooldown, updates the last use timestamp to the current time.
        /// </summary>
        /// <param name="platform">The streaming platform (Twitch, Discord, etc.)</param>
        /// <param name="channelId">The unique identifier of the channel</param>
        /// <param name="commandName">The name of the command to check</param>
        /// <param name="cooldown">The cooldown duration in seconds</param>
        /// <returns>
        /// <c>true</c> if the command is currently in cooldown (not available for use);
        /// <c>false</c> if the command is available (and the cooldown timestamp has been updated)
        /// </returns>
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
        /// Retrieves the list of banned words for a specific channel on the given platform.
        /// If the channel doesn't exist in the database, it's initialized with default values before retrieval.
        /// </summary>
        /// <param name="platform">The streaming platform (Twitch, Discord, etc.)</param>
        /// <param name="channelId">The unique identifier of the channel</param>
        /// <returns>A list of banned words for the channel, or an empty list if none are defined</returns>
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

        private void InvalidateBanWordsCache(PlatformsEnum platform, string channelId)
        {
            var key = (platform, channelId);
            lock (_cacheLock)
            {
                _banWordsCache.Remove(key);
            }
        }

        /// <summary>
        /// Sets the complete list of banned words for a specific channel on the given platform.
        /// This replaces any existing banned words with the new list provided.
        /// </summary>
        /// <param name="platform">The streaming platform (Twitch, Discord, etc.)</param>
        /// <param name="channelId">The unique identifier of the channel</param>
        /// <param name="banWords">The new list of banned words to set for the channel</param>
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
        /// Adds a new banned word to the channel's filter list if it doesn't already exist.
        /// Comparison is case-insensitive to prevent duplicate entries with different casing.
        /// </summary>
        /// <param name="platform">The streaming platform (Twitch, Discord, etc.)</param>
        /// <param name="channelId">The unique identifier of the channel</param>
        /// <param name="banWord">The word or phrase to add to the banned words list</param>
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
        /// Removes a banned word from the channel's filter list.
        /// The removal is case-insensitive to ensure the word is removed regardless of its original casing.
        /// </summary>
        /// <param name="platform">The streaming platform (Twitch, Discord, etc.)</param>
        /// <param name="channelId">The unique identifier of the channel</param>
        /// <param name="banWord">The word or phrase to remove from the banned words list</param>
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

        /// <summary>
        /// Retrieves the first message a user sent in a specific channel.
        /// This data is used for welcome messages or tracking user engagement history.
        /// </summary>
        /// <param name="platform">The streaming platform (Twitch, Discord, etc.)</param>
        /// <param name="channelId">The unique identifier of the channel</param>
        /// <param name="userId">The unique identifier of the user</param>
        /// <returns>The first message object sent by the user in the channel, or null if not found</returns>
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
        /// Saves or updates the first message record for a user in a specific channel.
        /// Uses an UPSERT operation to either create a new record or update an existing one.
        /// </summary>
        /// <param name="platform">The streaming platform (Twitch, Discord, etc.)</param>
        /// <param name="channelId">The unique identifier of the channel</param>
        /// <param name="userId">The unique identifier of the user</param>
        /// <param name="message">The message object containing details to be stored</param>
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
        /// Generates the database table name for channel data based on the specified platform.
        /// The table name follows the format: PLATFORM_NAME (all uppercase).
        /// </summary>
        private string GetChannelsTableName(PlatformsEnum platform)
        {
            return platform.ToString().ToUpper();
        }

        /// <summary>
        /// Generates the database table name for first message data based on the specified platform.
        /// The table name follows the format: FirstMessage_PLATFORM_NAME (all uppercase).
        /// </summary>
        private string GetFirstMessagesTableName(PlatformsEnum platform)
        {
            return $"FirstMessage_{platform.ToString().ToUpper()}";
        }
    }
}