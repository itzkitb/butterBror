using butterBror.Models;
using butterBror.Models.DataBase;
using butterBror.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
namespace butterBror.Data
{
    /// <summary>
    /// A thread-safe database manager for channel-related data storage and operations across multiple platforms.
    /// Provides functionality for command cooldown management, banned word filtering, and tracking first user messages.
    /// </summary>
    public class ChannelsDatabase : SqlDatabaseBase
    {
        /// <summary>
        /// Initializes a new instance of the ChannelsDatabase class with the specified database file path.
        /// </summary>
        /// <param name="dbPath">The path to the SQLite database file. Defaults to "Channels.db" if not specified.</param>
        public ChannelsDatabase(string dbPath = "Channels.db")
            : base(dbPath, true)
        {
            InitializeDatabase();
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
                        string channelsTableName = GetChannelsTableName(platform);
                        string firstMessagesTableName = GetFirstMessagesTableName(platform);

                        string createChannelsTable = $@"
                            CREATE TABLE IF NOT EXISTS [{channelsTableName}] (
                                ChannelID TEXT PRIMARY KEY,
                                CDDData TEXT DEFAULT '{{}}',
                                BanWords TEXT DEFAULT '{{""list"":[]}}'
                            );
                            CREATE INDEX IF NOT EXISTS idx_{channelsTableName}_channelid ON [{channelsTableName}](ChannelID);";

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
                        ExecuteNonQuery(createChannelsTable);
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

        /// <summary>
        /// Checks if a command is currently in cooldown period for the specified channel and platform.
        /// If the command is not in cooldown, updates the last use timestamp to the current time.
        /// </summary>
        /// <param name="platform">The streaming platform (Twitch, YouTube, etc.)</param>
        /// <param name="channelId">The unique identifier of the channel</param>
        /// <param name="commandName">The name of the command to check</param>
        /// <param name="cooldown">The cooldown duration in seconds</param>
        /// <returns>
        /// <c>true</c> if the command is currently in cooldown (not available for use);
        /// <c>false</c> if the command is available (and the cooldown timestamp has been updated)
        /// </returns>
        public bool IsCommandCooldown(PlatformsEnum platform, string channelId, string commandName, int cooldown)
        {
            string tableName = GetChannelsTableName(platform);
            string sql = $@"
                SELECT CDDData 
                FROM [{tableName}] 
                WHERE ChannelID = @ChannelId";
            string cddDataJson = ExecuteScalar<string>(sql, new[]
            {
                new SQLiteParameter("@ChannelId", channelId.ToString())
            });
            
            if (string.IsNullOrEmpty(cddDataJson))
            {
                InitializeChannel(platform, channelId);
                cddDataJson = "{}";
            }
            try
            {
                Dictionary<string, string> lastUses = Format.ParseStringDictionary(cddDataJson);
                DateTime now = DateTime.UtcNow;
                if (lastUses.TryGetValue(commandName, out string lastUse))
                {
                    DateTime lastUseTime = DateTime.Parse(lastUse, null, System.Globalization.DateTimeStyles.AdjustToUniversal);
                    if (now < lastUseTime.AddSeconds(cooldown))
                        return true;
                }

                lastUses[commandName] = now.ToString("o");
                string updateSql = $@"
                    UPDATE [{tableName}] 
                    SET CDDData = @CDDData 
                    WHERE ChannelID = @ChannelId";
                ExecuteNonQuery(updateSql, new[]
                {
                    new SQLiteParameter("@CDDData", Format.SerializeStringDictionary(lastUses)),
                    new SQLiteParameter("@ChannelId", channelId.ToString())
                });
                return false;
            }
            catch
            {
                string resetSql = $@"
                    UPDATE [{tableName}] 
                    SET CDDData = '{{}}' 
                    WHERE ChannelID = @ChannelId";
                ExecuteNonQuery(resetSql, new[]
                {
                    new SQLiteParameter("@ChannelId", channelId.ToString())
                });
                return false;
            }
        }

        /// <summary>
        /// Initializes a channel in the database with default values if it doesn't already exist.
        /// This ensures the channel has proper entries for command cooldown tracking and banned words.
        /// </summary>
        private void InitializeChannel(PlatformsEnum platform, string channelId)
        {
            string tableName = GetChannelsTableName(platform);
            string checkSql = $@"
                SELECT 1 
                FROM [{tableName}] 
                WHERE ChannelID = @ChannelId";
            bool exists = ExecuteScalar<object>(checkSql, new[]
            {
                new SQLiteParameter("@ChannelId", channelId.ToString())
            }) != null;
            if (!exists)
            {
                string insertSql = $@"
                    INSERT INTO [{tableName}] (ChannelID, CDDData, BanWords)
                    VALUES (@ChannelId, '{{}}', '{{""list"":[]}}')";
                ExecuteNonQuery(insertSql, new[]
                {
                    new SQLiteParameter("@ChannelId", channelId.ToString())
                });
            }
        }

        /// <summary>
        /// Retrieves the list of banned words for a specific channel on the given platform.
        /// If the channel doesn't exist in the database, it's initialized with default values before retrieval.
        /// </summary>
        /// <param name="platform">The streaming platform (Twitch, YouTube, etc.)</param>
        /// <param name="channelId">The unique identifier of the channel</param>
        /// <returns>A list of banned words for the channel, or an empty list if none are defined</returns>
        public List<string> GetBanWords(PlatformsEnum platform, string channelId)
        {
            InitializeChannel(platform, channelId);
            string tableName = GetChannelsTableName(platform);
            string sql = $@"
                SELECT BanWords 
                FROM [{tableName}] 
                WHERE ChannelID = @ChannelId";
            string banWordsJson = ExecuteScalar<string>(sql, new[]
            {
                new SQLiteParameter("@ChannelId", channelId.ToString())
            });
            if (string.IsNullOrEmpty(banWordsJson))
                return new List<string>();
            try
            {
                JObject banWordsData = JObject.Parse(banWordsJson);
                JToken listToken;
                if (banWordsData.TryGetValue("list", out listToken) && listToken is JArray listArray)
                {
                    return listArray.Select(token => token.ToString()).ToList();
                }
            }
            catch
            {
                ResetBanWords(platform, channelId);
            }
            return new List<string>();
        }

        /// <summary>
        /// Resets the banned words list for a channel to an empty state.
        /// This is typically used when there's a data corruption issue with the existing banned words data.
        /// </summary>
        private void ResetBanWords(PlatformsEnum platform, string channelId)
        {
            string tableName = GetChannelsTableName(platform);
            string updateSql = $@"
                UPDATE [{tableName}] 
                SET BanWords = '{{""list"":[]}}' 
                WHERE ChannelID = @ChannelId";
            ExecuteNonQuery(updateSql, new[]
            {
                new SQLiteParameter("@ChannelId", channelId.ToString())
            });
        }

        /// <summary>
        /// Sets the complete list of banned words for a specific channel on the given platform.
        /// This replaces any existing banned words with the new list provided.
        /// </summary>
        /// <param name="platform">The streaming platform (Twitch, YouTube, etc.)</param>
        /// <param name="channelId">The unique identifier of the channel</param>
        /// <param name="banWords">The new list of banned words to set for the channel</param>
        public void SetBanWords(PlatformsEnum platform, string channelId, List<string> banWords)
        {
            InitializeChannel(platform, channelId);
            JObject banWordsData = new JObject();
            banWordsData["list"] = new JArray(banWords);
            string tableName = GetChannelsTableName(platform);
            string updateSql = $@"
                UPDATE [{tableName}] 
                SET BanWords = @BanWords 
                WHERE ChannelID = @ChannelId";
            ExecuteNonQuery(updateSql, new[]
            {
                new SQLiteParameter("@BanWords", banWordsData.ToString(Formatting.None)),
                new SQLiteParameter("@ChannelId", channelId.ToString())
            });
        }

        /// <summary>
        /// Adds a new banned word to the channel's filter list if it doesn't already exist.
        /// Comparison is case-insensitive to prevent duplicate entries with different casing.
        /// </summary>
        /// <param name="platform">The streaming platform (Twitch, YouTube, etc.)</param>
        /// <param name="channelId">The unique identifier of the channel</param>
        /// <param name="banWord">The word or phrase to add to the banned words list</param>
        public void AddBanWord(PlatformsEnum platform, string channelId, string banWord)
        {
            List<string> currentBanWords = GetBanWords(platform, channelId);
            if (!currentBanWords.Contains(banWord, StringComparer.OrdinalIgnoreCase))
            {
                currentBanWords.Add(banWord);
                SetBanWords(platform, channelId, currentBanWords);
            }
        }

        /// <summary>
        /// Removes a banned word from the channel's filter list.
        /// The removal is case-insensitive to ensure the word is removed regardless of its original casing.
        /// </summary>
        /// <param name="platform">The streaming platform (Twitch, YouTube, etc.)</param>
        /// <param name="channelId">The unique identifier of the channel</param>
        /// <param name="banWord">The word or phrase to remove from the banned words list</param>
        public void RemoveBanWord(PlatformsEnum platform, string channelId, string banWord)
        {
            List<string> currentBanWords = GetBanWords(platform, channelId);
            currentBanWords.RemoveAll(w => w.Equals(banWord, StringComparison.OrdinalIgnoreCase));
            SetBanWords(platform, channelId, currentBanWords);
        }

        /// <summary>
        /// Retrieves the first message a user sent in a specific channel.
        /// This data is used for welcome messages or tracking user engagement history.
        /// </summary>
        /// <param name="platform">The streaming platform (Twitch, YouTube, etc.)</param>
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
        /// <param name="platform">The streaming platform (Twitch, YouTube, etc.)</param>
        /// <param name="channelId">The unique identifier of the channel</param>
        /// <param name="userId">The unique identifier of the user</param>
        /// <param name="message">The message object containing details to be stored</param>
        public void SaveFirstMessage(PlatformsEnum platform, string channelId, long userId, Message message)
        {
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
            ExecuteNonQuery(upsertSql, new[]
            {
                new SQLiteParameter("@ChannelId", channelId),
                new SQLiteParameter("@UserId", userId),
                new SQLiteParameter("@MessageDate", message.messageDate.ToString("o")),
                new SQLiteParameter("@MessageText", message.messageText ?? string.Empty),
                new SQLiteParameter("@IsMe", message.isMe ? 1 : 0),
                new SQLiteParameter("@IsModerator", message.isModerator ? 1 : 0),
                new SQLiteParameter("@IsSubscriber", message.isSubscriber ? 1 : 0),
                new SQLiteParameter("@IsPartner", message.isPartner ? 1 : 0),
                new SQLiteParameter("@IsStaff", message.isStaff ? 1 : 0),
                new SQLiteParameter("@IsTurbo", message.isTurbo ? 1 : 0),
                new SQLiteParameter("@IsVip", message.isVip ? 1 : 0)
            });
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