using butterBror.Models;
using butterBror.Models.DataBase;
using System.Data;
using System.Data.SQLite;

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
            _maxMessagesPerChannel = maxMessagesPerChannel;
            _messagesToDeleteAtOnce = messagesToDeleteAtOnce;
        }

        /// <summary>
        /// Retrieves a user's message from the specified channel by index from the most recent message.
        /// </summary>
        /// <param name="platform">The streaming platform (Twitch, YouTube, etc.)</param>
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
        /// <param name="platform">The streaming platform (Twitch, YouTube, etc.)</param>
        /// <param name="channelId">The unique identifier of the channel</param>
        /// <param name="userId">The unique identifier of the user</param>
        /// <param name="message">The message object to store in the database</param>
        public void SaveMessage(PlatformsEnum platform, string channelId, long userId, Message message)
        {
            string tableName = GetTableName(platform, channelId);
            EnsureTableExists(tableName);
            string sql = $@"
                INSERT INTO [{tableName}] (
                    UserID, MessageDate, MessageText,
                    IsMe, IsModerator, IsSubscriber, IsPartner, IsStaff, IsTurbo, IsVip
                ) VALUES (
                    @UserID, @MessageDate, @MessageText,
                    @IsMe, @IsModerator, @IsSubscriber, @IsPartner, @IsStaff, @IsTurbo, @IsVip
                );";
            ExecuteNonQuery(sql, new[]
            {
                new SQLiteParameter("@UserID", userId),
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

            int totalCount = GetTotalMessageCount(platform, channelId);
            if (totalCount > _maxMessagesPerChannel)
            {
                int messagesToDelete = Math.Min(totalCount - _maxMessagesPerChannel + 1, _messagesToDeleteAtOnce);
                string deleteSql = $@"
                    DELETE FROM [{tableName}] 
                    WHERE ID IN (
                        SELECT ID FROM [{tableName}] ORDER BY ID ASC LIMIT {messagesToDelete}
                    );";
                ExecuteNonQuery(deleteSql);
            }
        }

        /// <summary>
        /// Retrieves the count of messages sent by a specific user in the specified channel.
        /// </summary>
        /// <param name="platform">The streaming platform (Twitch, YouTube, etc.)</param>
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
        /// <param name="platform">The streaming platform (Twitch, YouTube, etc.)</param>
        /// <param name="channelId">The unique identifier of the channel</param>
        /// <returns>The total message count for the channel</returns>
        public int GetTotalMessageCount(PlatformsEnum platform, string channelId)
        {
            string tableName = GetTableName(platform, channelId);
            string sql = $@"SELECT COUNT(*) FROM [{tableName}]";
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
        /// <param name="platform">The streaming platform (Twitch, YouTube, etc.)</param>
        /// <param name="channelId">The unique identifier of the channel</param>
        /// <returns>The generated table name</returns>
        private string GetTableName(PlatformsEnum platform, string channelId)
        {
            string platformName = platform.ToString().ToUpper();
            return $"{platformName}_{channelId}";
        }
    }
}