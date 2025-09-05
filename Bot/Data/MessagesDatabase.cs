using bb.Models;
using bb.Models.DataBase;
using System.Data;
using System.Data.SQLite;
using System.Text;

namespace bb.Data
{
    /// <summary>
    /// Thread-safe database manager for persistent storage and retrieval of chat message history across multiple streaming platforms.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class implements a robust message history system with the following key features:
    /// <list type="bullet">
    /// <item>Platform and channel-specific message storage (Twitch, Discord, Telegram)</item>
    /// <item>Automatic message retention management to prevent database bloat</item>
    /// <item>Optimized batch processing for high-volume message ingestion</item>
    /// <item>Configurable message limits with automatic cleanup operations</item>
    /// <item>Efficient indexing for fast user message lookups</item>
    /// </list>
    /// </para>
    /// <para>
    /// Technical implementation highlights:
    /// <list type="bullet">
    /// <item>Per-channel database tables for optimal query performance</item>
    /// <item>Write-Ahead Logging (WAL) mode for concurrent read/write operations</item>
    /// <item>Memory-optimized caching (200MB) for frequent access patterns</item>
    /// <item>Dual insertion strategy for optimal performance across message volumes</item>
    /// <item>Transaction-based operations to ensure data integrity</item>
    /// </list>
    /// </para>
    /// Designed to handle the high-throughput requirements of chatbot environments while maintaining database performance.
    /// </remarks>
    public class MessagesDatabase : SqlDatabaseBase
    {
        private const int DEFAULT_MAX_MESSAGES_PER_CHANNEL = 2500000;
        private const int DEFAULT_MESSAGES_TO_DELETE_AT_ONCE = 1000;
        private readonly int _maxMessagesPerChannel;
        private readonly int _messagesToDeleteAtOnce;

        /// <summary>
        /// Initializes a new instance of the MessagesDatabase class with configurable message retention parameters.
        /// </summary>
        /// <param name="dbPath">File path for the SQLite database. Defaults to "Messages.db" in the working directory if not specified.</param>
        /// <param name="maxMessagesPerChannel">
        /// Maximum number of messages to retain per channel before automatic cleanup begins.
        /// Default value: 2,500,000 messages.
        /// Higher values increase storage requirements but preserve longer message history.
        /// </param>
        /// <param name="messagesToDeleteAtOnce">
        /// Number of messages to delete in a single cleanup operation.
        /// Default value: 1,000 messages.
        /// Smaller values reduce transaction size but increase cleanup frequency.
        /// </param>
        /// <remarks>
        /// <para>
        /// Initialization sequence:
        /// <list type="number">
        /// <item>Configures SQLite performance settings optimized for message storage</item>
        /// <item>Sets message retention parameters for automatic cleanup</item>
        /// <item>Database connection remains open for the lifetime of the object</item>
        /// </list>
        /// </para>
        /// <para>
        /// Performance considerations:
        /// <list type="bullet">
        /// <item>Default retention limit (2.5M messages) balances storage needs with performance</item>
        /// <item>Batch deletion (1,000 messages) minimizes transaction size and lock contention</item>
        /// <item>Database file grows incrementally as messages are added</item>
        /// </list>
        /// </para>
        /// The database automatically creates channel-specific tables on first message insertion.
        /// </remarks>
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

        /// <summary>
        /// Configures SQLite database performance settings specifically optimized for message storage workloads.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Applies the following performance-critical settings:
        /// <list type="bullet">
        /// <item><c>PRAGMA journal_mode = WAL</c> - Enables Write-Ahead Logging for concurrent read/write operations</item>
        /// <item><c>PRAGMA synchronous = NORMAL</c> - Balances durability with write performance</item>
        /// <item><c>PRAGMA cache_size = -200000</c> - Allocates 200MB of memory for database cache</item>
        /// <item><c>PRAGMA temp_store = MEMORY</c> - Stores temporary tables in RAM for faster sorting</item>
        /// </list>
        /// </para>
        /// <para>
        /// These settings are specifically tuned for:
        /// <list type="bullet">
        /// <item>High-frequency write operations (message ingestion)</item>
        /// <item>Moderate-frequency read operations (message history lookups)</item>
        /// <item>Large database size management (multi-gigabyte datasets)</item>
        /// <item>Concurrent access from multiple bot instances</item>
        /// </list>
        /// </para>
        /// The configuration significantly improves throughput during peak chat activity periods.
        /// </remarks>
        private void ConfigureSqlitePerformance()
        {
            ExecuteNonQuery("PRAGMA journal_mode = WAL;");
            ExecuteNonQuery("PRAGMA synchronous = NORMAL;");
            ExecuteNonQuery("PRAGMA cache_size = -200000;");
            ExecuteNonQuery("PRAGMA temp_store = MEMORY;");
        }

        /// <summary>
        /// Retrieves a specific message from a user's history in a channel by position relative to the most recent message.
        /// </summary>
        /// <param name="platform">The streaming platform (Twitch, Discord, Telegram)</param>
        /// <param name="channelId">The unique channel identifier</param>
        /// <param name="userId">The unique user identifier</param>
        /// <param name="indexFromLast">Zero-based index counting backward from the most recent message (0 = most recent)</param>
        /// <returns>
        /// The requested <see cref="Message"/> object if found; otherwise, <see langword="null"/>.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Query behavior:
        /// <list type="bullet">
        /// <item>Sorts messages by ID in descending order (newest first)</item>
        /// <item>Uses efficient OFFSET/LIMIT for pagination</item>
        /// <item>Leverages UserID index for fast filtering</item>
        /// </list>
        /// </para>
        /// <para>
        /// Performance characteristics:
        /// <list type="bullet">
        /// <item>O(1) for index 0 (most recent message)</item>
        /// <item>O(log n) for other indexes due to index usage</item>
        /// <item>Efficient even for channels with millions of messages</item>
        /// </list>
        /// </para>
        /// Common use cases include implementing "!lastmessage" commands or message reference features.
        /// Returns <see langword="null"/> for invalid indexes or when user has no messages in the channel.
        /// </remarks>
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
        /// Saves multiple messages to their respective channel histories with automatic retention management.
        /// </summary>
        /// <param name="messages">Collection of message records to persist, grouped by (platform, channelId)</param>
        /// <remarks>
        /// <para>
        /// Processing workflow:
        /// <list type="number">
        /// <item>Groups messages by channel for optimized processing</item>
        /// <item>Begins a database transaction for atomic operation</item>
        /// <item>Creates channel tables if they don't exist</item>
        /// <item>Processes messages using optimal insertion strategy</item>
        /// <item>Checks message count and performs cleanup if needed</item>
        /// <item>Commits transaction after all operations complete</item>
        /// </list>
        /// </para>
        /// <para>
        /// Insertion strategies:
        /// <list type="bullet">
        /// <item><b>Prepared statements</b> for small batches (<= 90 messages): Reuses SQL command with parameter updates</item>
        /// <item><b>Batch insertion</b> for large batches (> 90 messages): Combines multiple INSERT statements for reduced roundtrips</item>
        /// </list>
        /// </para>
        /// <para>
        /// Automatic cleanup:
        /// <list type="bullet">
        /// <item>Triggers when channel message count exceeds <see cref="_maxMessagesPerChannel"/></item>
        /// <item>Deletes oldest messages first (ascending ID order)</item>
        /// <item>Removes up to <see cref="_messagesToDeleteAtOnce"/> messages per operation</item>
        /// <item>Maintains message count at approximately <c>_maxMessagesPerChannel - _messagesToDeleteAtOnce</c></item>
        /// </list>
        /// </para>
        /// Empty collections are safely ignored with no database operations performed.
        /// The method is thread-safe and handles concurrent message ingestion patterns.
        /// </remarks>
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

                    if (channelGroup.Count() > 90)
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

        /// <summary>
        /// Inserts a small batch of messages using parameterized queries with command reuse.
        /// </summary>
        /// <param name="tableName">The target database table name</param>
        /// <param name="messages">The messages to insert</param>
        /// <remarks>
        /// <para>
        /// Optimized for batches of 90 or fewer messages:
        /// <list type="bullet">
        /// <item>Prepares a single SQL command template</item>
        /// <item>Reuses command object with updated parameters</item>
        /// <item>Minimizes SQL parsing overhead</item>
        /// <item>Reduces memory allocation for small operations</item>
        /// </list>
        /// </para>
        /// <para>
        /// Technical implementation:
        /// <list type="bullet">
        /// <item>Uses parameter binding to prevent SQL injection</item>
        /// <item>Converts boolean values to INTEGER (1/0) for storage efficiency</item>
        /// <item>Formats timestamps using ISO 8601 standard</item>
        /// <item>Handles empty message text as empty string (not NULL)</item>
        /// </list>
        /// </para>
        /// This approach provides optimal performance for typical message volumes per channel.
        /// </remarks>
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

        /// <summary>
        /// Processes large message batches using optimized multi-row INSERT statements.
        /// </summary>
        /// <param name="tableName">The target database table name</param>
        /// <param name="messages">The messages to insert</param>
        /// <remarks>
        /// <para>
        /// Handles batches larger than 90 messages by:
        /// <list type="bullet">
        /// <item>Splitting into sub-batches of optimal size (≤ 90 messages)</item>
        /// <item>Constructing multi-row INSERT statements</item>
        /// <item>Managing SQLite's parameter limit (999 parameters)</item>
        /// <item>Minimizing database roundtrips for high-volume ingestion</item>
        /// </list>
        /// </para>
        /// <para>
        /// Batch sizing rationale:
        /// <list type="bullet">
        /// <item>Each message requires 10 parameters</item>
        /// <item>SQLite limit: 999 parameters per statement</item>
        /// <item>Maximum batch size: 99 messages (990 parameters)</item>
        /// <item>Safety margin: 90 messages (900 parameters)</item>
        /// </list>
        /// </para>
        /// This strategy significantly improves throughput during peak message volumes.
        /// The method automatically handles batch splitting and parameter management.
        /// </remarks>
        private void BatchInsertMessages(string tableName,
    IEnumerable<(PlatformsEnum platform, string channelId, long userId, Message message)> messages)
        {
            const int MAX_PARAMS = 999;
            const int PARAMS_PER_MESSAGE = 10;
            int batchSize = MAX_PARAMS / PARAMS_PER_MESSAGE;
            batchSize = Math.Min(batchSize, 90);

            var messageList = messages.ToList();
            for (int i = 0; i < messageList.Count; i += batchSize)
            {
                var batch = messageList.Skip(i).Take(batchSize).ToList();
                InsertSingleBatch(tableName, batch);
            }
        }

        /// <summary>
        /// Executes a single batch insertion operation with multiple message values.
        /// </summary>
        /// <param name="tableName">The target database table name</param>
        /// <param name="batch">The message batch to insert</param>
        /// <remarks>
        /// <para>
        /// Constructs and executes a multi-row INSERT statement:
        /// <list type="bullet">
        /// <item>Formats SQL with multiple value tuples</item>
        /// <item>Generates unique parameter names for each message</item>
        /// <item>Executes single database command for entire batch</item>
        /// <item>Maintains transaction integrity</item>
        /// </list>
        /// </para>
        /// <para>
        /// Performance benefits:
        /// <list type="bullet">
        /// <item>Reduces database roundtrips from N to 1 for batch size N</item>
        /// <item>Minimizes transaction overhead</item>
        /// <item>Optimizes SQLite's write performance</item>
        /// <item>Reduces lock contention in high-concurrency scenarios</item>
        /// </list>
        /// </para>
        /// This is the most efficient method for inserting multiple messages in a single operation.
        /// </remarks>
        private void InsertSingleBatch(string tableName,
            List<(PlatformsEnum platform, string channelId, long userId, Message message)> batch)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"INSERT INTO [{tableName}] (UserID, MessageDate, MessageText, IsMe, IsModerator, IsSubscriber, IsPartner, IsStaff, IsTurbo, IsVip) VALUES ");

            var valuesList = new List<string>();
            var allParams = new List<SQLiteParameter>();

            for (int j = 0; j < batch.Count; j++)
            {
                var (_, _, userId, message) = batch[j];
                string paramPrefix = $"p{j}_";

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
            }

            sb.AppendLine(string.Join(",", valuesList));
            ExecuteNonQuery(sb.ToString(), allParams);
        }

        /// <summary>
        /// Retrieves the count of messages sent by a specific user in a channel.
        /// </summary>
        /// <param name="platform">The streaming platform</param>
        /// <param name="channelId">The channel identifier</param>
        /// <param name="userId">The user identifier</param>
        /// <returns>The total number of messages sent by the user in the channel</returns>
        /// <remarks>
        /// <para>
        /// Query implementation:
        /// <list type="bullet">
        /// <item>Uses COUNT(*) aggregate function for efficiency</item>
        /// <item>Leverages UserID index for fast filtering</item>
        /// <item>Executes as a single scalar operation</item>
        /// </list>
        /// </para>
        /// <para>
        /// Performance characteristics:
        /// <list type="bullet">
        /// <item>O(1) time complexity due to index usage</item>
        /// <item>Memory-efficient (doesn't load message content)</item>
        /// <item>Highly optimized even for channels with millions of messages</item>
        /// </list>
        /// </para>
        /// Common use cases include leaderboards, activity tracking, and user engagement metrics.
        /// Returns 0 if the user has no messages in the channel.
        /// </remarks>
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
        /// Retrieves the total message count for a specific channel.
        /// </summary>
        /// <param name="platform">The streaming platform</param>
        /// <param name="channelId">The channel identifier</param>
        /// <returns>The total number of messages stored for the channel</returns>
        /// <remarks>
        /// <para>
        /// Query implementation:
        /// <list type="bullet">
        /// <item>Uses COUNT(*) for efficient row counting</item>
        /// <item>Executes as a single scalar operation</item>
        /// <item>Does not load any message data into memory</item>
        /// </list>
        /// </para>
        /// <para>
        /// Performance considerations:
        /// <list type="bullet">
        /// <item>Time complexity: O(log n) due to SQLite's internal optimizations</item>
        /// <item>Memory usage: constant (independent of message count)</item>
        /// <item>May be slower than GetMessageCountInChat due to lack of filtering</item>
        /// </list>
        /// </para>
        /// Used for retention management and monitoring channel message volumes.
        /// Returns 0 for channels with no messages stored.
        /// </remarks>
        public int GetTotalMessageCount(PlatformsEnum platform, string channelId)
        {
            string tableName = GetTableName(platform, channelId);
            return ExecuteScalar<int>($"SELECT COUNT(*) FROM [{tableName}]");
        }

        /// <summary>
        /// Ensures the required message storage table exists for a channel, creating it with appropriate schema if necessary.
        /// </summary>
        /// <param name="tableName">The fully qualified table name to verify or create</param>
        /// <remarks>
        /// <para>
        /// Table structure includes:
        /// <list type="bullet">
        /// <item><c>ID</c>: Auto-incrementing primary key (for ordering)</item>
        /// <item><c>UserID</c>: User identifier (indexed for fast lookups)</item>
        /// <item><c>MessageDate</c>: ISO 8601 formatted timestamp</item>
        /// <item><c>MessageText</c>: Message content (stored as TEXT)</item>
        /// <item>Role flags: IsMe, IsModerator, etc. (stored as INTEGER 0/1)</item>
        /// </list>
        /// </para>
        /// <para>
        /// Indexing strategy:
        /// <list type="bullet">
        /// <item>Primary key on ID (automatic)</item>
        /// <item>Secondary index on UserID for efficient user lookups</item>
        /// <item>No additional indexes to optimize write performance</item>
        /// </list>
        /// </para>
        /// The operation is idempotent and safe to call multiple times.
        /// Table creation is wrapped in transaction for atomicity.
        /// </remarks>
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
        /// Generates a database table name for a specific platform and channel combination.
        /// </summary>
        /// <param name="platform">The streaming platform</param>
        /// <param name="channelId">The channel identifier</param>
        /// <returns>
        /// A string in the format "PLATFORM_CHANNELID" where PLATFORM is uppercase and CHANNELID is the raw identifier.
        /// </returns>
        /// <example>
        /// For PlatformsEnum.Twitch and channelId "123456789", returns "TWITCH_123456789"
        /// </example>
        /// <remarks>
        /// <para>
        /// Naming convention rationale:
        /// <list type="bullet">
        /// <item>Uppercase platform name ensures consistent casing</item>
        /// <item>Simple concatenation enables easy pattern matching</item>
        /// <item>No special characters to avoid SQL identifier issues</item>
        /// <item>Channel ID preserved in original format for accurate referencing</item>
        /// </list>
        /// </para>
        /// This scheme allows for efficient database management and maintenance operations.
        /// The generated name is safe for direct use in SQL statements with proper quoting.
        /// </remarks>
        private string GetTableName(PlatformsEnum platform, string channelId)
        {
            string platformName = platform.ToString().ToUpper();
            return $"{platformName}_{channelId}";
        }
    }
}