using butterBror.Core.Bot.SQLColumnNames;
using butterBror.Models;
using butterBror.Models.DataBase;
using System.Data;
using System.Data.SQLite;
using System.Text;

namespace butterBror.Data
{
    /// <summary>
    /// A thread-safe database manager for storing and retrieving chat messages across multiple platforms and channels.
    /// Implements automatic message history management with configurable retention limits to prevent database bloat.
    /// </summary>
    public class MessagesDatabase : SqlDatabaseBase
    {
        private const int DEFAULT_MAX_MESSAGES_PER_CHANNEL = 2500000;
        private const int DEFAULT_MESSAGES_TO_DELETE_AT_ONCE = 1000;
        private readonly int _maxMessagesPerChannel;
        private readonly int _messagesToDeleteAtOnce;

        /// <summary>
        /// Initializes a new instance of the MessagesDatabase class with the specified database file path and message retention parameters.
        /// </summary>
        /// <param name="dbPath">The path to the SQLite database file. Defaults to "Messages.db" if not specified.</param>
        /// <param name="maxMessagesPerChannel">The maximum number of messages to retain per channel before automatic cleanup begins. Defaults to 2,500,000.</param>
        /// <param name="messagesToDeleteAtOnce">The number of messages to delete in a single cleanup operation. Defaults to 1,000.</param>
        public MessagesDatabase(
            string dbPath = "Messages.db",
            int maxMessagesPerChannel = DEFAULT_MAX_MESSAGES_PER_CHANNEL,
            int messagesToDeleteAtOnce = DEFAULT_MESSAGES_TO_DELETE_AT_ONCE)
            : base(dbPath, true)
        {
            ConfigureSqlitePerformance();
            _maxMessagesPerChannel = maxMessagesPerChannel;
            _messagesToDeleteAtOnce = messagesToDeleteAtOnce;
        }

        private void ConfigureSqlitePerformance()
        {
            ExecuteNonQuery("PRAGMA journal_mode = WAL;");
            ExecuteNonQuery("PRAGMA synchronous = NORMAL;");
            ExecuteNonQuery("PRAGMA cache_size = -200000;");
            ExecuteNonQuery("PRAGMA temp_store = MEMORY;");
        }

        /// <summary>
        /// Retrieves a user's message from the specified channel by index from the most recent message.
        /// </summary>
        /// <param name="platform">The streaming platform (Twitch, Discord, etc.)</param>
        /// <param name="channelId">The unique identifier of the channel</param>
        /// <param name="userId">The unique identifier of the user</param>
        /// <param name="indexFromLast">The index of the message counting backward from the most recent (0 = most recent message)</param>
        /// <returns>The message object if found; otherwise, <c>null</c></returns>
        public Message GetMessage(PlatformsEnum platform, string channelId, long userId, int indexFromLast)
        {
            string tableName = GetTableName(platform, channelId);
            string sql = $@"
                SELECT * FROM [{tableName}]
                WHERE UserID = @UserId
                ORDER BY ID DESC
                LIMIT 1 OFFSET @IndexFromLast";
            return QueryFirstOrDefault<Message>(sql, new[]
            {
                new SQLiteParameter("@UserId", userId),
                new SQLiteParameter("@IndexFromLast", indexFromLast)
            });
        }

        /// <summary>
        /// Saves a user's message to the channel's message history and manages database size by automatically removing the oldest messages when retention limits are exceeded.
        /// </summary>
        /// <param name="platform">The streaming platform (Twitch, Discord, etc.)</param>
        /// <param name="channelId">The unique identifier of the channel</param>
        /// <param name="userId">The unique identifier of the user</param>
        /// <param name="message">The message object to store in the database</param>
        public void SaveMessages(List<(PlatformsEnum platform, string channelId, long userId, Message message)> messages)
        {
            if (messages.Count == 0) return;

            var messagesByChannel = messages
                .GroupBy(m => (m.platform, m.channelId))
                .ToList();

            BeginTransaction();
            try
            {
                foreach (var channelGroup in messagesByChannel)
                {
                    var (platform, channelId) = channelGroup.Key;
                    string tableName = GetTableName(platform, channelId);
                    EnsureTableExists(tableName);

                    if (channelGroup.Count() > 500)
                    {
                        BatchInsertMessages(tableName, channelGroup);
                    }
                    else
                    {
                        PreparedInsertMessages(tableName, channelGroup);
                    }

                    int totalCount = GetTotalMessageCount(platform, channelId);
                    if (totalCount > _maxMessagesPerChannel)
                    {
                        int messagesToDelete = Math.Min(totalCount - _maxMessagesPerChannel + 1,
                                                      _messagesToDeleteAtOnce);
                        string deleteSql = $@"
                    DELETE FROM [{tableName}] 
                    ORDER BY ID ASC 
                    LIMIT {messagesToDelete};";
                        ExecuteNonQuery(deleteSql);
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

        private void PreparedInsertMessages(string tableName, IEnumerable<(PlatformsEnum platform, string channelId, long userId, Message message)> messages)
        {
            string sql = $@"
        INSERT INTO [{tableName}] (
            UserID, MessageDate, MessageText,
            IsMe, IsModerator, IsSubscriber, IsPartner, IsStaff, IsTurbo, IsVip
        ) VALUES (
            @UserID, @MessageDate, @MessageText,
            @IsMe, @IsModerator, @IsSubscriber, @IsPartner, @IsStaff, @IsTurbo, @IsVip
        );";

            using var cmd = CreateCommand(sql, null);

            var userIdParam = cmd.Parameters.Add("@UserID", DbType.Int64);
            var messageDateParam = cmd.Parameters.Add("@MessageDate", DbType.String);
            var messageTextParam = cmd.Parameters.Add("@MessageText", DbType.String);
            var isMeParam = cmd.Parameters.Add("@IsMe", DbType.Int32);
            var isModeratorParam = cmd.Parameters.Add("@IsModerator", DbType.Int32);
            var isSubscriberParam = cmd.Parameters.Add("@IsSubscriber", DbType.Int32);
            var isPartnerParam = cmd.Parameters.Add("@IsPartner", DbType.Int32);
            var isStaffParam = cmd.Parameters.Add("@IsStaff", DbType.Int32);
            var isTurboParam = cmd.Parameters.Add("@IsTurbo", DbType.Int32);
            var isVipParam = cmd.Parameters.Add("@IsVip", DbType.Int32);

            foreach (var (_, _, userId, message) in messages)
            {
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

        private void BatchInsertMessages(string tableName, IEnumerable<(PlatformsEnum platform, string channelId, long userId, Message message)> messages)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"INSERT INTO [{tableName}] (UserID, MessageDate, MessageText, IsMe, IsModerator, IsSubscriber, IsPartner, IsStaff, IsTurbo, IsVip) VALUES ");

            var valuesList = new List<string>();
            var allParams = new List<SQLiteParameter>();

            int i = 0;
            foreach (var (_, _, userId, message) in messages)
            {
                string paramPrefix = $"p{i}_";

                valuesList.Add($"(@{paramPrefix}UserID, @{paramPrefix}MessageDate, @{paramPrefix}MessageText, " +
                              $"@{paramPrefix}IsMe, @{paramPrefix}IsModerator, @{paramPrefix}IsSubscriber, " +
                              $"@{paramPrefix}IsPartner, @{paramPrefix}IsStaff, @{paramPrefix}IsTurbo, @{paramPrefix}IsVip)");

                allParams.Add(new SQLiteParameter($"@{paramPrefix}UserID", userId));
                allParams.Add(new SQLiteParameter($"@{paramPrefix}MessageDate", message.messageDate.ToString("o")));
                allParams.Add(new SQLiteParameter($"@{paramPrefix}MessageText", message.messageText ?? string.Empty));
                allParams.Add(new SQLiteParameter($"@{paramPrefix}IsMe", message.isMe ? 1 : 0));
                allParams.Add(new SQLiteParameter($"@{paramPrefix}IsModerator", message.isModerator ? 1 : 0));
                allParams.Add(new SQLiteParameter($"@{paramPrefix}IsSubscriber", message.isSubscriber ? 1 : 0));
                allParams.Add(new SQLiteParameter($"@{paramPrefix}IsPartner", message.isPartner ? 1 : 0));
                allParams.Add(new SQLiteParameter($"@{paramPrefix}IsStaff", message.isStaff ? 1 : 0));
                allParams.Add(new SQLiteParameter($"@{paramPrefix}IsTurbo", message.isTurbo ? 1 : 0));
                allParams.Add(new SQLiteParameter($"@{paramPrefix}IsVip", message.isVip ? 1 : 0));

                i++;
            }

            sb.AppendLine(string.Join(",", valuesList));

            ExecuteNonQuery(sb.ToString(), allParams);
        }

        /// <summary>
        /// Retrieves the count of messages sent by a specific user in the specified channel.
        /// </summary>
        /// <param name="platform">The streaming platform (Twitch, Discord, etc.)</param>
        /// <param name="channelId">The unique identifier of the channel</param>
        /// <param name="userId">The unique identifier of the user</param>
        /// <returns>The total number of messages sent by the user in the channel</returns>
        public int GetMessageCountInChat(PlatformsEnum platform, string channelId, long userId)
        {
            string tableName = GetTableName(platform, channelId);
            string sql = $@"
                SELECT COUNT(*) 
                FROM [{tableName}]
                WHERE UserID = @UserId";
            return ExecuteScalar<int>(sql, new[] { new SQLiteParameter("@UserId", userId) });
        }

        /// <summary>
        /// Retrieves the total number of messages stored for the specified channel.
        /// </summary>
        /// <param name="platform">The streaming platform (Twitch, Discord, etc.)</param>
        /// <param name="channelId">The unique identifier of the channel</param>
        /// <returns>The total message count for the channel</returns>
        public int GetTotalMessageCount(PlatformsEnum platform, string channelId)
        {
            string tableName = GetTableName(platform, channelId);
            string sql = $@"
                SELECT MAX(ID) - MIN(ID) + 1 
                FROM [{tableName}]";
            return ExecuteScalar<int>(sql);
        }

        /// <summary>
        /// Ensures the required message table exists for the specified channel, creating it with appropriate structure and indexes if necessary.
        /// </summary>
        /// <param name="tableName">The fully qualified table name to verify or create</param>
        private void EnsureTableExists(string tableName)
        {
            string createTableSql = $@"
                CREATE TABLE IF NOT EXISTS [{tableName}] (
                    ID INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserID INTEGER NOT NULL,
                    MessageDate TEXT,
                    MessageText TEXT,
                    IsMe INTEGER,
                    IsModerator INTEGER,
                    IsSubscriber INTEGER,
                    IsPartner INTEGER,
                    IsStaff INTEGER,
                    IsTurbo INTEGER,
                    IsVip INTEGER
                );
                CREATE INDEX IF NOT EXISTS idx_{tableName}_userid ON [{tableName}](UserID);";
            ExecuteNonQuery(createTableSql);
        }

        /// <summary>
        /// Generates a database table name for the specified platform and channel combination.
        /// The table name follows the format: PLATFORM_CHANNELID (e.g., TWITCH_123456789).
        /// </summary>
        /// <param name="platform">The streaming platform (Twitch, Discord, etc.)</param>
        /// <param name="channelId">The unique identifier of the channel</param>
        /// <returns>The generated table name</returns>
        private string GetTableName(PlatformsEnum platform, string channelId)
        {
            string platformName = platform.ToString().ToUpper();
            return $"{platformName}_{channelId}";
        }
    }
}