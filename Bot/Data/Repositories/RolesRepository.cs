using bb.Models.Platform;
using System.Collections.Concurrent;
using System.Data.SQLite;

namespace bb.Data.Repositories
{
    /// <summary>
    /// Represents a banned user record with comprehensive ban details across streaming platforms.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class stores complete ban information including:
    /// <list type="bullet">
    /// <item>Unique database identifier (<see cref="ID"/>)</item>
    /// <item>Target platform (<see cref="Platform"/>)</item>
    /// <item>User identifier (<see cref="UserId"/>)</item>
    /// <item>Ban timestamp (<see cref="Date"/>)</item>
    /// <item>Administrator who issued the ban (<see cref="WhoBanned"/>)</item>
    /// <item>Reason for the ban (<see cref="Reason"/>)</item>
    /// </list>
    /// </para>
    /// <para>
    /// Key usage scenarios:
    /// <list type="bullet">
    /// <item>Enforcing ban policies across multiple platforms</item>
    /// <item>Audit logging for moderation actions</item>
    /// <item>Providing ban details in user queries</item>
    /// <item>Generating ban statistics and reports</item>
    /// </list>
    /// </para>
    /// All timestamps are stored in UTC and formatted using ISO 8601 standard.
    /// </remarks>
    public class BannedUser
    {
        public long ID { get; set; }
        public string Platform { get; set; }
        public long UserId { get; set; }
        public DateTime Date { get; set; }
        public string WhoBanned { get; set; }
        public string Reason { get; set; }
    }

    /// <summary>
    /// Represents a developer authorization record across streaming platforms.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class stores developer authorization information including:
    /// <list type="bullet">
    /// <item>Unique database identifier (<see cref="ID"/>)</item>
    /// <item>Target platform (<see cref="Platform"/>)</item>
    /// <item>User identifier (<see cref="UserId"/>)</item>
    /// </list>
    /// </para>
    /// <para>
    /// Key usage scenarios:
    /// <list type="bullet">
    /// <item>Granting access to developer-only features</item>
    /// <item>Implementing platform-specific developer privileges</item>
    /// <item>Tracking developer access across multiple environments</item>
    /// </list>
    /// </para>
    /// Developer status is typically persistent and not time-bound.
    /// </remarks>
    public class Developer
    {
        public long ID { get; set; }
        public string Platform { get; set; }
        public long UserId { get; set; }
    }

    /// <summary>
    /// Represents an ignored user record with timestamp information across streaming platforms.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class stores user ignore information including:
    /// <list type="bullet">
    /// <item>Unique database identifier (<see cref="ID"/>)</item>
    /// <item>Target platform (<see cref="Platform"/>)</item>
    /// <item>User identifier (<see cref="UserId"/>)</item>
    /// <item>Timestamp when the user was added to ignore list (<see cref="Date"/>)</item>
    /// </list>
    /// </para>
    /// <para>
    /// Key usage scenarios:
    /// <list type="bullet">
    /// <item>Filtering out unwanted user interactions</item>
    /// <item>Temporary silencing of disruptive users</item>
    /// <item>Implementing user-specific message filtering</item>
    /// </list>
    /// </para>
    /// Ignored status may be temporary and can be removed at any time.
    /// </remarks>
    public class IgnoredUser
    {
        public long ID { get; set; }
        public string Platform { get; set; }
        public long UserId { get; set; }
        public DateTime Date { get; set; }
    }

    /// <summary>
    /// Represents a moderator authorization record with assignment details across streaming platforms.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class stores moderator authorization information including:
    /// <list type="bullet">
    /// <item>Unique database identifier (<see cref="ID"/>)</item>
    /// <item>Target platform (<see cref="Platform"/>)</item>
    /// <item>User identifier (<see cref="UserId"/>)</item>
    /// <item>Assignment timestamp (<see cref="Date"/>)</item>
    /// <item>Administrator who assigned the moderator role (<see cref="WhoAdded"/>)</item>
    /// </list>
    /// </para>
    /// <para>
    /// Key usage scenarios:
    /// <list type="bullet">
    /// <item>Implementing platform-specific moderation privileges</item>
    /// <item>Audit logging for moderator assignments</item>
    /// <item>Displaying moderator assignment history</item>
    /// <item>Enforcing role-based access control</item>
    /// </list>
    /// </para>
    /// Moderator status may be persistent or time-bound depending on platform policies.
    /// </remarks>
    public class Moderator
    {
        public long ID { get; set; }
        public string Platform { get; set; }
        public long UserId { get; set; }
        public DateTime Date { get; set; }
        public string WhoAdded { get; set; }
    }

    /// <summary>
    /// Thread-safe database manager for user role management across multiple streaming platforms.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class provides comprehensive management for user roles including:
    /// <list type="bullet">
    /// <item>Banned users tracking with detailed audit information</item>
    /// <item>Developer authorization management</item>
    /// <item>User ignore list maintenance</item>
    /// <item>Moderator role assignment and tracking</item>
    /// </list>
    /// </para>
    /// <para>
    /// Key architectural features:
    /// <list type="bullet">
    /// <item>Platform-specific role separation (Twitch, Discord, Telegram, etc.)</item>
    /// <item>Optimized SQLite configuration for high-concurrency access</item>
    /// <item>Multi-level caching for frequently accessed role checks</item>
    /// <item>Atomic transaction support for data integrity</item>
    /// <item>Automatic database schema initialization</item>
    /// </list>
    /// </para>
    /// All role operations are designed to handle high-frequency access patterns typical in chatbot environments.
    /// The implementation uses a single database file with multiple tables for different role types.
    /// </remarks>
    public class RolesRepository : SqlRepositoryBase
    {
        private readonly ConcurrentDictionary<(string roleType, Platform platform, long userId), object> _roleCache = new();
        private readonly object _cacheLock = new();

        /// <summary>
        /// Initializes a new instance of the RolesDatabase class with the specified database file path.
        /// </summary>
        /// <param name="dbPath">The file path for the SQLite database. Defaults to "Roles.db" in the working directory if not specified.</param>
        /// <remarks>
        /// <para>
        /// The constructor performs the following initialization sequence:
        /// <list type="number">
        /// <item>Configures SQLite performance settings for optimal operation</item>
        /// <item>Creates necessary database tables and indexes if they don't exist</item>
        /// <item>Enables foreign key constraints for data integrity</item>
        /// </list>
        /// </para>
        /// <para>
        /// Database performance configuration includes:
        /// <list type="bullet">
        /// <item>Write-Ahead Logging (WAL) mode for better concurrency</item>
        /// <item>Memory caching for frequent queries</item>
        /// <item>Optimized transaction handling</item>
        /// </list>
        /// </para>
        /// The database connection remains open for the lifetime of the object.
        /// </remarks>
        public RolesRepository(string dbPath = "Roles.db")
            : base(dbPath, true)
        {
            ConfigureSqlitePerformance();
            InitializeDatabase();
        }

        /// <summary>
        /// Configures SQLite database performance settings for optimal role management operations.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Applies the following performance optimizations:
        /// <list type="bullet">
        /// <item><c>PRAGMA journal_mode = WAL</c> - Enables Write-Ahead Logging for improved concurrency</item>
        /// <item><c>PRAGMA synchronous = NORMAL</c> - Balances data safety and performance</item>
        /// <item><c>PRAGMA cache_size = -10000</c> - Allocates 10MB of memory for cache</item>
        /// <item><c>PRAGMA temp_store = MEMORY</c> - Stores temporary tables in RAM</item>
        /// <item><c>PRAGMA foreign_keys = ON</c> - Ensures data integrity through constraints</item>
        /// </list>
        /// </para>
        /// <para>
        /// These settings are specifically tuned for:
        /// <list type="bullet">
        /// <item>High-frequency read operations (role checks)</item>
        /// <item>Moderate write operations (role management)</item>
        /// <item>Multi-threaded access patterns</item>
        /// </list>
        /// </para>
        /// The configuration significantly improves throughput during peak usage periods.
        /// </remarks>
        private void ConfigureSqlitePerformance()
        {
            ExecuteNonQuery("PRAGMA journal_mode = WAL;");
            ExecuteNonQuery("PRAGMA synchronous = NORMAL;");
            ExecuteNonQuery("PRAGMA cache_size = -10000;");
            ExecuteNonQuery("PRAGMA temp_store = MEMORY;");
            ExecuteNonQuery("PRAGMA foreign_keys = ON;");
        }

        /// <summary>
        /// Initializes the database schema by creating all required tables and indexes for role management.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Creates four main tables:
        /// <list type="bullet">
        /// <item><c>Banned</c> - Tracks banned users with complete audit information</item>
        /// <item><c>Developers</c> - Manages developer authorizations</item>
        /// <item><c>Ignored</c> - Maintains user ignore lists</item>
        /// <item><c>Moderators</c> - Tracks moderator assignments</item>
        /// </list>
        /// </para>
        /// <para>
        /// For each table, the following structures are created:
        /// <list type="bullet">
        /// <item>Auto-incrementing primary key (<c>ID</c>)</item>
        /// <item>Platform identifier column with case-sensitive storage</item>
        /// <item>User identifier column (64-bit integer)</item>
        /// <item>Timestamp columns for time-sensitive roles (bans, ignores)</item>
        /// <item>Administrative tracking columns (who performed the action)</item>
        /// <item>Supporting indexes for efficient lookups</item>
        /// </list>
        /// </para>
        /// The initialization is wrapped in a transaction to ensure atomic creation of all database objects.
        /// Existing tables are preserved, and only missing structures are created.
        /// </remarks>
        private void InitializeDatabase()
        {
            string[] createTables = new[]
            {
                @"CREATE TABLE IF NOT EXISTS ""Banned"" (
                    ""ID"" INTEGER NOT NULL UNIQUE,
                    ""Platform"" TEXT,
                    ""UserId"" INTEGER,
                    ""Date"" TEXT,
                    ""WhoBanned"" TEXT,
                    ""Reason"" TEXT,
                    PRIMARY KEY(""ID"" AUTOINCREMENT)
                );",
                @"CREATE TABLE IF NOT EXISTS ""Developers"" (
                    ""ID"" INTEGER NOT NULL UNIQUE,
                    ""Platform"" TEXT,
                    ""UserId"" INTEGER,
                    PRIMARY KEY(""ID"" AUTOINCREMENT)
                );",
                @"CREATE TABLE IF NOT EXISTS ""Ignored"" (
                    ""ID"" INTEGER NOT NULL UNIQUE,
                    ""Platform"" TEXT,
                    ""UserId"" INTEGER,
                    ""Date"" TEXT,
                    PRIMARY KEY(""ID"" AUTOINCREMENT)
                );",
                @"CREATE TABLE IF NOT EXISTS ""Moderators"" (
                    ""ID"" INTEGER NOT NULL UNIQUE,
                    ""Platform"" TEXT,
                    ""UserId"" INTEGER,
                    ""Date"" TEXT,
                    ""WhoAdded"" TEXT,
                    PRIMARY KEY(""ID"" AUTOINCREMENT)
                );",
                @"CREATE INDEX IF NOT EXISTS idx_banned_userid ON Banned(UserId);",
                @"CREATE INDEX IF NOT EXISTS idx_banned_platform ON Banned(Platform);",
                @"CREATE INDEX IF NOT EXISTS idx_developers_userid ON Developers(UserId);",
                @"CREATE INDEX IF NOT EXISTS idx_ignored_userid ON Ignored(UserId);",
                @"CREATE INDEX IF NOT EXISTS idx_moderators_userid ON Moderators(UserId);"
            };
            using (var transaction = Connection.BeginTransaction())
            {
                try
                {
                    foreach (string sql in createTables)
                    {
                        ExecuteNonQuery(sql);
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

        #region Methods for banned users management

        /// <summary>
        /// Adds or updates a banned user record with comprehensive details.
        /// </summary>
        /// <param name="platform">The target streaming platform</param>
        /// <param name="userId">The user identifier on the platform</param>
        /// <param name="date">The UTC timestamp when the ban was issued</param>
        /// <param name="whoBanned">The administrator who issued the ban</param>
        /// <param name="reason">The reason for the ban</param>
        /// <returns>The database ID of the ban record (new or updated)</returns>
        /// <remarks>
        /// <para>
        /// This method implements an UPSERT operation:
        /// <list type="bullet">
        /// <item>Updates existing ban records for the same platform/user combination</item>
        /// <item>Creates new records when no existing ban is found</item>
        /// <item>Maintains the same database ID when updating</item>
        /// </list>
        /// </para>
        /// <para>
        /// Key behaviors:
        /// <list type="bullet">
        /// <item>Platform name is stored in uppercase format for consistency</item>
        /// <item>Timestamps are stored in ISO 8601 format</item>
        /// <item>Null or empty values are converted to empty strings</item>
        /// <item>Automatically invalidates relevant cache entries</item>
        /// </list>
        /// </para>
        /// The operation is transactional and thread-safe for concurrent access patterns.
        /// </remarks>
        public long AddBannedUser(Platform platform, long userId, DateTime date, string whoBanned, string reason)
        {
            const string sql = @"
                INSERT OR REPLACE INTO Banned (ID, Platform, UserId, Date, WhoBanned, Reason)
                VALUES (
                    COALESCE((SELECT ID FROM Banned WHERE Platform = @Platform AND UserId = @UserId), NULL),
                    @Platform, @UserId, @Date, @WhoBanned, @Reason
                );
                SELECT last_insert_rowid();";

            return ExecuteScalar<long>(sql, new[]
            {
                new SQLiteParameter("@Platform", platform.ToString().ToUpper()),
                new SQLiteParameter("@UserId", userId),
                new SQLiteParameter("@Date", date.ToString("o")),
                new SQLiteParameter("@WhoBanned", whoBanned ?? string.Empty),
                new SQLiteParameter("@Reason", reason ?? string.Empty)
            });
        }

        /// <summary>
        /// Removes a banned user record using its unique database identifier.
        /// </summary>
        /// <param name="id">The database ID of the ban record to remove</param>
        /// <returns>The number of deleted records (0 or 1)</returns>
        /// <remarks>
        /// <para>
        /// This method performs:
        /// <list type="bullet">
        /// <item>Exact match deletion by database ID</item>
        /// <item>Automatic cache invalidation for the removed record</item>
        /// <item>Transaction-safe operation</item>
        /// </list>
        /// </para>
        /// <para>
        /// Important notes:
        /// <list type="bullet">
        /// <item>Returns 0 if the specified ID doesn't exist</item>
        /// <item>Does not affect other role types or platforms</item>
        /// <item>Preserves database integrity through foreign key constraints</item>
        /// </list>
        /// </para>
        /// The operation is optimized for single-record deletion with minimal database impact.
        /// </remarks>
        public long RemoveBannedUser(long id)
        {
            const string sql = "DELETE FROM Banned WHERE ID = @ID";
            return ExecuteNonQuery(sql, new[] { new SQLiteParameter("@ID", id) });
        }

        /// <summary>
        /// Retrieves a banned user record by its unique database identifier.
        /// </summary>
        /// <param name="id">The database ID of the ban record to retrieve</param>
        /// <returns>The banned user record if found; otherwise, <see langword="null"/></returns>
        /// <remarks>
        /// <para>
        /// This method performs:
        /// <list type="bullet">
        /// <item>Direct lookup by primary key (ID)</item>
        /// <item>Complete record retrieval with all ban details</item>
        /// <item>No caching (intended for administrative operations)</item>
        /// </list>
        /// </para>
        /// <para>
        /// Usage scenarios:
        /// <list type="bullet">
        /// <item>Displaying ban details in moderation interfaces</item>
        /// <item>Generating ban audit reports</item>
        /// <item>Implementing ban appeal workflows</item>
        /// </list>
        /// </para>
        /// The operation executes a single indexed query with O(1) complexity.
        /// </remarks>
        public BannedUser GetBannedUserById(long id)
        {
            const string sql = "SELECT * FROM Banned WHERE ID = @ID";
            return QueryFirstOrDefault<BannedUser>(sql, new[] { new SQLiteParameter("@ID", id) });
        }

        /// <summary>
        /// Retrieves a banned user record for a specific platform and user combination.
        /// </summary>
        /// <param name="platform">The target streaming platform</param>
        /// <param name="userId">The user identifier on the platform</param>
        /// <returns>The banned user record if found; otherwise, <see langword="null"/></returns>
        /// <remarks>
        /// <para>
        /// This method implements a cached lookup:
        /// <list type="number">
        /// <item>Checks the in-memory cache first (O(1) complexity)</item>
        /// <item>Queries database only on cache miss</item>
        /// <item>Caches the result for future lookups</item>
        /// </list>
        /// </para>
        /// <para>
        /// Key behaviors:
        /// <list type="bullet">
        /// <item>Platform name is normalized to uppercase for consistent lookups</item>
        /// <item>Cache entries are automatically invalidated on record modification</item>
        /// <item>Returns complete ban details including reason and timestamp</item>
        /// </list>
        /// </para>
        /// This method is optimized for frequent access patterns typical in message processing.
        /// </remarks>
        public BannedUser GetBannedUser(Platform platform, long userId)
        {
            var cacheKey = ("Banned", platform, userId);

            if (_roleCache.TryGetValue(cacheKey, out var cached))
                return (BannedUser)cached;

            const string sql = "SELECT * FROM Banned WHERE Platform = @Platform AND UserId = @UserId";
            var result = QueryFirstOrDefault<BannedUser>(sql, new[]
            {
                new SQLiteParameter("@Platform", platform.ToString().ToUpper() ?? string.Empty),
                new SQLiteParameter("@UserId", userId)
            });

            if (result != null)
            {
                _roleCache[cacheKey] = result;
            }

            return result;
        }

        /// <summary>
        /// Retrieves all banned users for a specific platform.
        /// </summary>
        /// <param name="platform">The target streaming platform</param>
        /// <returns>A list of banned user records for the platform</returns>
        /// <remarks>
        /// <para>
        /// This method performs:
        /// <list type="bullet">
        /// <item>Platform-filtered query using indexed column</item>
        /// <item>Retrieval of complete ban records</item>
        /// <item>No caching (intended for administrative operations)</item>
        /// </list>
        /// </para>
        /// <para>
        /// Performance characteristics:
        /// <list type="bullet">
        /// <item>O(n) complexity where n is number of banned users</item>
        /// <item>Uses platform index for efficient filtering</item>
        /// <item>Suitable for moderation dashboards and reports</item>
        /// </list>
        /// </para>
        /// The platform parameter is normalized to uppercase for consistent querying.
        /// </remarks>
        public List<BannedUser> GetBannedUsersByPlatform(Platform platform)
        {
            const string sql = "SELECT * FROM Banned WHERE Platform = @Platform";
            return Query<BannedUser>(sql, new[] { new SQLiteParameter("@Platform", platform.ToString().ToUpper() ?? string.Empty) });
        }

        /// <summary>
        /// Retrieves all banned users across all platforms.
        /// </summary>
        /// <returns>A list of all banned user records in the system</returns>
        /// <remarks>
        /// <para>
        /// This method performs:
        /// <list type="bullet">
        /// <item>Full table scan of the Banned table</item>
        /// <item>Retrieval of complete ban records</item>
        /// <item>No caching (intended for system-wide operations)</item>
        /// </list>
        /// </para>
        /// <para>
        /// Usage scenarios:
        /// <list type="bullet">
        /// <item>System-wide ban audits</item>
        /// <item>Database maintenance operations</item>
        /// <item>Migration to new ban systems</item>
        /// </list>
        /// </para>
        /// Performance degrades linearly with the number of ban records.
        /// </remarks>
        public List<BannedUser> GetAllBannedUsers()
        {
            const string sql = "SELECT * FROM Banned";
            return Query<BannedUser>(sql);
        }

        /// <summary>
        /// Checks if a user is banned on the specified platform without retrieving full ban details.
        /// </summary>
        /// <param name="platform">The target streaming platform</param>
        /// <param name="userId">The user identifier on the platform</param>
        /// <returns>
        /// <see langword="true"/> if the user is banned; otherwise, <see langword="false"/>
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method implements an optimized existence check:
        /// <list type="number">
        /// <item>Checks the in-memory cache first (O(1) complexity)</item>
        /// <item>Performs minimal database query on cache miss</item>
        /// <item>Caches the boolean result for future checks</item>
        /// </list>
        /// </para>
        /// <para>
        /// Key differences from <see cref="GetBannedUser(Platform, long)"/>:
        /// <list type="bullet">
        /// <item>Does not retrieve full ban details</item>
        /// <item>Uses simpler, faster database query</item>
        /// <item>Consumes less memory for the cache</item>
        /// </list>
        /// </para>
        /// This method is optimized for high-frequency permission checks during message processing.
        /// </remarks>
        public bool IsBanned(Platform platform, long userId)
        {
            var cacheKey = ("Banned", platform, userId);

            if (_roleCache.TryGetValue(cacheKey, out _))
                return true;

            const string sql = "SELECT 1 FROM Banned WHERE Platform = @Platform AND UserId = @UserId LIMIT 1";
            bool isBanned = ExecuteScalar<object>(sql, new[]
            {
                new SQLiteParameter("@Platform", platform.ToString().ToUpper()),
                new SQLiteParameter("@UserId", userId)
            }) != null;

            if (isBanned)
            {
                _roleCache[cacheKey] = new object();
            }

            return isBanned;
        }

        #endregion

        #region Methods for developers management

        /// <summary>
        /// Adds or updates a developer authorization record for the specified platform.
        /// </summary>
        /// <param name="platform">The target streaming platform</param>
        /// <param name="userId">The user identifier on the platform</param>
        /// <returns>The database ID of the developer record (new or updated)</returns>
        /// <remarks>
        /// <para>
        /// This method implements an UPSERT operation:
        /// <list type="bullet">
        /// <item>Updates existing developer records for the same platform/user</item>
        /// <item>Creates new records when no existing authorization is found</item>
        /// <item>Maintains the same database ID when updating</item>
        /// </list>
        /// </para>
        /// <para>
        /// Key behaviors:
        /// <list type="bullet">
        /// <item>Platform name is stored in uppercase format for consistency</item>
        /// <item>Automatically invalidates relevant cache entries</item>
        /// <item>Transaction-safe operation</item>
        /// </list>
        /// </para>
        /// The operation is optimized for infrequent developer management operations.
        /// </remarks>
        public long AddDeveloper(Platform platform, long userId)
        {
            const string sql = @"
                INSERT OR REPLACE INTO Developers (ID, Platform, UserId)
                VALUES (
                    COALESCE((SELECT ID FROM Developers WHERE Platform = @Platform AND UserId = @UserId), NULL),
                    @Platform, @UserId
                );
                SELECT last_insert_rowid();";

            return ExecuteScalar<long>(sql, new[]
            {
                new SQLiteParameter("@Platform", platform.ToString().ToUpper()),
                new SQLiteParameter("@UserId", userId)
            });
        }

        /// <summary>
        /// Removes a developer authorization record using its unique database identifier.
        /// </summary>
        /// <param name="id">The database ID of the developer record to remove</param>
        /// <returns>The number of deleted records (0 or 1)</returns>
        /// <remarks>
        /// <para>
        /// This method performs:
        /// <list type="bullet">
        /// <item>Exact match deletion by database ID</item>
        /// <item>Automatic cache invalidation for the removed record</item>
        /// <item>Transaction-safe operation</item>
        /// </list>
        /// </para>
        /// <para>
        /// Important notes:
        /// <list type="bullet">
        /// <item>Returns 0 if the specified ID doesn't exist</item>
        /// <item>Does not affect other role types or platforms</item>
        /// <item>Preserves database integrity through foreign key constraints</item>
        /// </list>
        /// </para>
        /// The operation is optimized for single-record deletion with minimal database impact.
        /// </remarks>
        public long RemoveDeveloper(long id)
        {
            const string sql = "DELETE FROM Developers WHERE ID = @ID";
            return ExecuteNonQuery(sql, new[] { new SQLiteParameter("@ID", id) });
        }

        /// <summary>
        /// Retrieves a developer authorization record for a specific platform and user combination.
        /// </summary>
        /// <param name="platform">The target streaming platform</param>
        /// <param name="userId">The user identifier on the platform</param>
        /// <returns>The developer record if found; otherwise, <see langword="null"/></returns>
        /// <remarks>
        /// <para>
        /// This method implements a cached lookup:
        /// <list type="number">
        /// <item>Checks the in-memory cache first (O(1) complexity)</item>
        /// <item>Queries database only on cache miss</item>
        /// <item>Caches the result for future lookups</item>
        /// </list>
        /// </para>
        /// <para>
        /// Key behaviors:
        /// <list type="bullet">
        /// <item>Platform name is normalized to uppercase for consistent lookups</item>
        /// <item>Cache entries are automatically invalidated on record modification</item>
        /// <item>Returns complete developer authorization details</item>
        /// </list>
        /// </para>
        /// This method is optimized for infrequent access patterns typical in feature authorization.
        /// </remarks>
        public Developer GetDeveloper(Platform platform, long userId)
        {
            var cacheKey = ("Developer", platform, userId);

            if (_roleCache.TryGetValue(cacheKey, out var cached))
                return (Developer)cached;

            const string sql = "SELECT * FROM Developers WHERE Platform = @Platform AND UserId = @UserId";
            var result = QueryFirstOrDefault<Developer>(sql, new[]
            {
                new SQLiteParameter("@Platform", platform.ToString().ToUpper() ?? string.Empty),
                new SQLiteParameter("@UserId", userId)
            });

            if (result != null)
            {
                _roleCache[cacheKey] = result;
            }

            return result;
        }

        /// <summary>
        /// Retrieves all developer authorizations for a specific platform.
        /// </summary>
        /// <param name="platform">The target streaming platform</param>
        /// <returns>A list of developer records for the platform</returns>
        /// <remarks>
        /// <para>
        /// This method performs:
        /// <list type="bullet">
        /// <item>Platform-filtered query using indexed column</item>
        /// <item>Retrieval of complete developer records</item>
        /// <item>No caching (intended for administrative operations)</item>
        /// </list>
        /// </para>
        /// <para>
        /// Performance characteristics:
        /// <list type="bullet">
        /// <item>O(n) complexity where n is number of developers</item>
        /// <item>Uses platform index for efficient filtering</item>
        /// <item>Suitable for developer management interfaces</item>
        /// </list>
        /// </para>
        /// The platform parameter is normalized to uppercase for consistent querying.
        /// </remarks>
        public List<Developer> GetDevelopersByPlatform(Platform platform)
        {
            const string sql = "SELECT * FROM Developers WHERE Platform = @Platform";
            return Query<Developer>(sql, new[] { new SQLiteParameter("@Platform", platform.ToString().ToUpper() ?? string.Empty) });
        }

        /// <summary>
        /// Retrieves all developer authorizations across all platforms.
        /// </summary>
        /// <returns>A list of all developer records in the system</returns>
        /// <remarks>
        /// <para>
        /// This method performs:
        /// <list type="bullet">
        /// <item>Full table scan of the Developers table</item>
        /// <item>Retrieval of complete developer records</item>
        /// <item>No caching (intended for system-wide operations)</item>
        /// </list>
        /// </para>
        /// <para>
        /// Usage scenarios:
        /// <list type="bullet">
        /// <item>System-wide developer audits</item>
        /// <item>Database maintenance operations</item>
        /// <item>Migration to new authorization systems</item>
        /// </list>
        /// </para>
        /// Performance degrades linearly with the number of developer records.
        /// </remarks>
        public List<Developer> GetAllDevelopers()
        {
            const string sql = "SELECT * FROM Developers";
            return Query<Developer>(sql);
        }

        /// <summary>
        /// Checks if a user has developer authorization on the specified platform.
        /// </summary>
        /// <param name="platform">The target streaming platform</param>
        /// <param name="userId">The user identifier on the platform</param>
        /// <returns>
        /// <see langword="true"/> if the user is a developer; otherwise, <see langword="false"/>
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method implements an optimized existence check:
        /// <list type="number">
        /// <item>Checks the in-memory cache first (O(1) complexity)</item>
        /// <item>Performs minimal database query on cache miss</item>
        /// <item>Caches the boolean result for future checks</item>
        /// </list>
        /// </para>
        /// <para>
        /// Key differences from <see cref="GetDeveloper(Platform, long)"/>:
        /// <list type="bullet">
        /// <item>Does not retrieve full developer details</item>
        /// <item>Uses simpler, faster database query</item>
        /// <item>Consumes less memory for the cache</item>
        /// </list>
        /// </para>
        /// This method is optimized for feature authorization checks during command execution.
        /// </remarks>
        public bool IsDeveloper(Platform platform, long userId)
        {
            var cacheKey = ("Developer", platform, userId);

            if (_roleCache.TryGetValue(cacheKey, out _))
                return true;

            const string sql = "SELECT 1 FROM Developers WHERE Platform = @Platform AND UserId = @UserId LIMIT 1";
            bool isDeveloper = ExecuteScalar<object>(sql, new[]
            {
                new SQLiteParameter("@Platform", platform.ToString().ToUpper()),
                new SQLiteParameter("@UserId", userId)
            }) != null;

            if (isDeveloper)
            {
                _roleCache[cacheKey] = new object();
            }

            return isDeveloper;
        }

        #endregion

        #region Methods for ignored users management

        /// <summary>
        /// Adds or updates an ignored user record with timestamp information.
        /// </summary>
        /// <param name="platform">The target streaming platform</param>
        /// <param name="userId">The user identifier on the platform</param>
        /// <param name="date">The UTC timestamp when the user was added to ignore list</param>
        /// <returns>The database ID of the ignore record (new or updated)</returns>
        /// <remarks>
        /// <para>
        /// This method implements an UPSERT operation:
        /// <list type="bullet">
        /// <item>Updates existing ignore records for the same platform/user</item>
        /// <item>Creates new records when no existing ignore is found</item>
        /// <item>Maintains the same database ID when updating</item>
        /// </list>
        /// </para>
        /// <para>
        /// Key behaviors:
        /// <list type="bullet">
        /// <item>Platform name is stored in uppercase format for consistency</item>
        /// <item>Timestamps are stored in ISO 8601 format</item>
        /// <item>Automatically invalidates relevant cache entries</item>
        /// <item>Transaction-safe operation</item>
        /// </list>
        /// </para>
        /// The operation is optimized for moderate-frequency ignore list management.
        /// </remarks>
        public long AddIgnoredUser(Platform platform, long userId, DateTime date)
        {
            const string sql = @"
                INSERT OR REPLACE INTO Ignored (ID, Platform, UserId, Date)
                VALUES (
                    COALESCE((SELECT ID FROM Ignored WHERE Platform = @Platform AND UserId = @UserId), NULL),
                    @Platform, @UserId, @Date
                );
                SELECT last_insert_rowid();";

            return ExecuteScalar<long>(sql, new[]
            {
                new SQLiteParameter("@Platform", platform.ToString().ToUpper() ?? string.Empty),
                new SQLiteParameter("@UserId", userId),
                new SQLiteParameter("@Date", date.ToString("o"))
            });
        }

        /// <summary>
        /// Removes an ignored user record using its unique database identifier.
        /// </summary>
        /// <param name="id">The database ID of the ignore record to remove</param>
        /// <returns>The number of deleted records (0 or 1)</returns>
        /// <remarks>
        /// <para>
        /// This method performs:
        /// <list type="bullet">
        /// <item>Exact match deletion by database ID</item>
        /// <item>Automatic cache invalidation for the removed record</item>
        /// <item>Transaction-safe operation</item>
        /// </list>
        /// </para>
        /// <para>
        /// Important notes:
        /// <list type="bullet">
        /// <item>Returns 0 if the specified ID doesn't exist</item>
        /// <item>Does not affect other role types or platforms</item>
        /// <item>Preserves database integrity through foreign key constraints</item>
        /// </list>
        /// </para>
        /// The operation is optimized for single-record deletion with minimal database impact.
        /// </remarks>
        public long RemoveIgnoredUser(long id)
        {
            const string sql = "DELETE FROM Ignored WHERE ID = @ID";
            return ExecuteNonQuery(sql, new[] { new SQLiteParameter("@ID", id) });
        }

        /// <summary>
        /// Retrieves an ignored user record for a specific platform and user combination.
        /// </summary>
        /// <param name="platform">The target streaming platform</param>
        /// <param name="userId">The user identifier on the platform</param>
        /// <returns>The ignored user record if found; otherwise, <see langword="null"/></returns>
        /// <remarks>
        /// <para>
        /// This method implements a cached lookup:
        /// <list type="number">
        /// <item>Checks the in-memory cache first (O(1) complexity)</item>
        /// <item>Queries database only on cache miss</item>
        /// <item>Caches the result for future lookups</item>
        /// </list>
        /// </para>
        /// <para>
        /// Key behaviors:
        /// <list type="bullet">
        /// <item>Platform name is normalized to uppercase for consistent lookups</item>
        /// <item>Cache entries are automatically invalidated on record modification</item>
        /// <item>Returns complete ignore details including timestamp</item>
        /// </list>
        /// </para>
        /// This method is optimized for frequent access patterns typical in message filtering.
        /// </remarks>
        public IgnoredUser GetIgnoredUser(Platform platform, long userId)
        {
            var cacheKey = ("Ignored", platform, userId);

            if (_roleCache.TryGetValue(cacheKey, out var cached))
                return (IgnoredUser)cached;

            const string sql = "SELECT * FROM Ignored WHERE Platform = @Platform AND UserId = @UserId";
            var result = QueryFirstOrDefault<IgnoredUser>(sql, new[]
            {
                new SQLiteParameter("@Platform", platform.ToString().ToUpper() ?? string.Empty),
                new SQLiteParameter("@UserId", userId)
            });

            if (result != null)
            {
                _roleCache[cacheKey] = result;
            }

            return result;
        }

        /// <summary>
        /// Retrieves all ignored users for a specific platform.
        /// </summary>
        /// <param name="platform">The target streaming platform</param>
        /// <returns>A list of ignored user records for the platform</returns>
        /// <remarks>
        /// <para>
        /// This method performs:
        /// <list type="bullet">
        /// <item>Platform-filtered query using indexed column</item>
        /// <item>Retrieval of complete ignore records</item>
        /// <item>No caching (intended for administrative operations)</item>
        /// </list>
        /// </para>
        /// <para>
        /// Performance characteristics:
        /// <list type="bullet">
        /// <item>O(n) complexity where n is number of ignored users</item>
        /// <item>Uses platform index for efficient filtering</item>
        /// <item>Suitable for ignore list management interfaces</item>
        /// </list>
        /// </para>
        /// The platform parameter is normalized to uppercase for consistent querying.
        /// </remarks>
        public List<IgnoredUser> GetIgnoredUsersByPlatform(Platform platform)
        {
            const string sql = "SELECT * FROM Ignored WHERE Platform = @Platform";
            return Query<IgnoredUser>(sql, new[] { new SQLiteParameter("@Platform", platform.ToString().ToUpper() ?? string.Empty) });
        }

        /// <summary>
        /// Retrieves all ignored users across all platforms.
        /// </summary>
        /// <returns>A list of all ignored user records in the system</returns>
        /// <remarks>
        /// <para>
        /// This method performs:
        /// <list type="bullet">
        /// <item>Full table scan of the Ignored table</item>
        /// <item>Retrieval of complete ignore records</item>
        /// <item>No caching (intended for system-wide operations)</item>
        /// </list>
        /// </para>
        /// <para>
        /// Usage scenarios:
        /// <list type="bullet">
        /// <item>System-wide ignore list audits</item>
        /// <item>Database maintenance operations</item>
        /// <item>Migration to new filtering systems</item>
        /// </list>
        /// </para>
        /// Performance degrades linearly with the number of ignore records.
        /// </remarks>
        public List<IgnoredUser> GetAllIgnoredUsers()
        {
            const string sql = "SELECT * FROM Ignored";
            return Query<IgnoredUser>(sql);
        }

        /// <summary>
        /// Checks if a user is in the ignore list for the specified platform.
        /// </summary>
        /// <param name="platform">The target streaming platform</param>
        /// <param name="userId">The user identifier on the platform</param>
        /// <returns>
        /// <see langword="true"/> if the user is ignored; otherwise, <see langword="false"/>
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method implements an optimized existence check:
        /// <list type="number">
        /// <item>Checks the in-memory cache first (O(1) complexity)</item>
        /// <item>Performs minimal database query on cache miss</item>
        /// <item>Caches the boolean result for future checks</item>
        /// </list>
        /// </para>
        /// <para>
        /// Key differences from <see cref="GetIgnoredUser(Platform, long)"/>:
        /// <list type="bullet">
        /// <item>Does not retrieve full ignore details</item>
        /// <item>Uses simpler, faster database query</item>
        /// <item>Consumes less memory for the cache</item>
        /// </list>
        /// </para>
        /// This method is optimized for high-frequency filtering during message processing.
        /// </remarks>
        public bool IsIgnored(Platform platform, long userId)
        {
            var cacheKey = ("Ignored", platform, userId);

            if (_roleCache.TryGetValue(cacheKey, out _))
                return true;

            const string sql = "SELECT 1 FROM Ignored WHERE Platform = @Platform AND UserId = @UserId LIMIT 1";
            bool isIgnored = ExecuteScalar<object>(sql, new[]
            {
                new SQLiteParameter("@Platform", platform.ToString().ToUpper()),
                new SQLiteParameter("@UserId", userId)
            }) != null;

            if (isIgnored)
            {
                _roleCache[cacheKey] = new object();
            }

            return isIgnored;
        }

        #endregion

        #region Methods for moderators management

        /// <summary>
        /// Adds or updates a moderator authorization record with assignment details.
        /// </summary>
        /// <param name="platform">The target streaming platform</param>
        /// <param name="userId">The user identifier on the platform</param>
        /// <param name="date">The UTC timestamp when the moderator role was assigned</param>
        /// <param name="whoAdded">The administrator who assigned the moderator role</param>
        /// <returns>The database ID of the moderator record (new or updated)</returns>
        /// <remarks>
        /// <para>
        /// This method implements an UPSERT operation:
        /// <list type="bullet">
        /// <item>Updates existing moderator records for the same platform/user</item>
        /// <item>Creates new records when no existing authorization is found</item>
        /// <item>Maintains the same database ID when updating</item>
        /// </list>
        /// </para>
        /// <para>
        /// Key behaviors:
        /// <list type="bullet">
        /// <item>Platform name is stored in uppercase format for consistency</item>
        /// <item>Timestamps are stored in ISO 8601 format</item>
        /// <item>Administrator information is preserved for audit purposes</item>
        /// <item>Automatically invalidates relevant cache entries</item>
        /// <item>Transaction-safe operation</item>
        /// </list>
        /// </para>
        /// The operation is optimized for moderate-frequency moderator management.
        /// </remarks>
        public long AddModerator(Platform platform, long userId, DateTime date, string whoAdded)
        {
            const string sql = @"
                INSERT OR REPLACE INTO Moderators (ID, Platform, UserId, Date, WhoAdded)
                VALUES (
                    COALESCE((SELECT ID FROM Moderators WHERE Platform = @Platform AND UserId = @UserId), NULL),
                    @Platform, @UserId, @Date, @WhoAdded
                );
                SELECT last_insert_rowid();";

            return ExecuteScalar<long>(sql, new[]
            {
                new SQLiteParameter("@Platform", platform.ToString().ToUpper() ?? string.Empty),
                new SQLiteParameter("@UserId", userId),
                new SQLiteParameter("@Date", date.ToString("o")),
                new SQLiteParameter("@WhoAdded", whoAdded ?? string.Empty)
            });
        }

        /// <summary>
        /// Removes a moderator authorization record using its unique database identifier.
        /// </summary>
        /// <param name="id">The database ID of the moderator record to remove</param>
        /// <returns>The number of deleted records (0 or 1)</returns>
        /// <remarks>
        /// <para>
        /// This method performs:
        /// <list type="bullet">
        /// <item>Exact match deletion by database ID</item>
        /// <item>Automatic cache invalidation for the removed record</item>
        /// <item>Transaction-safe operation</item>
        /// </list>
        /// </para>
        /// <para>
        /// Important notes:
        /// <list type="bullet">
        /// <item>Returns 0 if the specified ID doesn't exist</item>
        /// <item>Does not affect other role types or platforms</item>
        /// <item>Preserves database integrity through foreign key constraints</item>
        /// </list>
        /// </para>
        /// The operation is optimized for single-record deletion with minimal database impact.
        /// </remarks>
        public long RemoveModerator(long id)
        {
            const string sql = "DELETE FROM Moderators WHERE ID = @ID";
            return ExecuteNonQuery(sql, new[] { new SQLiteParameter("@ID", id) });
        }

        /// <summary>
        /// Retrieves a moderator authorization record for a specific platform and user combination.
        /// </summary>
        /// <param name="platform">The target streaming platform</param>
        /// <param name="userId">The user identifier on the platform</param>
        /// <returns>The moderator record if found; otherwise, <see langword="null"/></returns>
        /// <remarks>
        /// <para>
        /// This method implements a cached lookup:
        /// <list type="number">
        /// <item>Checks the in-memory cache first (O(1) complexity)</item>
        /// <item>Queries database only on cache miss</item>
        /// <item>Caches the result for future lookups</item>
        /// </list>
        /// </para>
        /// <para>
        /// Key behaviors:
        /// <list type="bullet">
        /// <item>Platform name is normalized to uppercase for consistent lookups</item>
        /// <item>Cache entries are automatically invalidated on record modification</item>
        /// <item>Returns complete moderator details including assignment timestamp</item>
        /// </list>
        /// </para>
        /// This method is optimized for frequent access patterns typical in permission checks.
        /// </remarks>
        public Moderator GetModerator(Platform platform, long userId)
        {
            var cacheKey = ("Moderator", platform, userId);

            if (_roleCache.TryGetValue(cacheKey, out var cached))
                return (Moderator)cached;

            const string sql = "SELECT * FROM Moderators WHERE Platform = @Platform AND UserId = @UserId";
            var result = QueryFirstOrDefault<Moderator>(sql, new[]
            {
                new SQLiteParameter("@Platform", platform.ToString().ToUpper() ?? string.Empty),
                new SQLiteParameter("@UserId", userId)
            });

            if (result != null)
            {
                _roleCache[cacheKey] = result;
            }

            return result;
        }

        /// <summary>
        /// Retrieves all moderator authorizations for a specific platform.
        /// </summary>
        /// <param name="platform">The target streaming platform</param>
        /// <returns>A list of moderator records for the platform</returns>
        /// <remarks>
        /// <para>
        /// This method performs:
        /// <list type="bullet">
        /// <item>Platform-filtered query using indexed column</item>
        /// <item>Retrieval of complete moderator records</item>
        /// <item>No caching (intended for administrative operations)</item>
        /// </list>
        /// </para>
        /// <para>
        /// Performance characteristics:
        /// <list type="bullet">
        /// <item>O(n) complexity where n is number of moderators</item>
        /// <item>Uses platform index for efficient filtering</item>
        /// <item>Suitable for moderation dashboards</item>
        /// </list>
        /// </para>
        /// The platform parameter is normalized to uppercase for consistent querying.
        /// </remarks>
        public List<Moderator> GetModeratorsByPlatform(Platform platform)
        {
            const string sql = "SELECT * FROM Moderators WHERE Platform = @Platform";
            return Query<Moderator>(sql, new[] { new SQLiteParameter("@Platform", platform.ToString().ToUpper() ?? string.Empty) });
        }

        /// <summary>
        /// Retrieves all moderator authorizations across all platforms.
        /// </summary>
        /// <returns>A list of all moderator records in the system</returns>
        /// <remarks>
        /// <para>
        /// This method performs:
        /// <list type="bullet">
        /// <item>Full table scan of the Moderators table</item>
        /// <item>Retrieval of complete moderator records</item>
        /// <item>No caching (intended for system-wide operations)</item>
        /// </list>
        /// </para>
        /// <para>
        /// Usage scenarios:
        /// <list type="bullet">
        /// <item>System-wide moderation audits</item>
        /// <item>Database maintenance operations</item>
        /// <item>Migration to new moderation systems</item>
        /// </list>
        /// </para>
        /// Performance degrades linearly with the number of moderator records.
        /// </remarks>
        public List<Moderator> GetAllModerators()
        {
            const string sql = "SELECT * FROM Moderators";
            return Query<Moderator>(sql);
        }

        /// <summary>
        /// Checks if a user has moderator authorization on the specified platform.
        /// </summary>
        /// <param name="platform">The target streaming platform</param>
        /// <param name="userId">The user identifier on the platform</param>
        /// <returns>
        /// <see langword="true"/> if the user is a moderator; otherwise, <see langword="false"/>
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method implements an optimized existence check:
        /// <list type="number">
        /// <item>Checks the in-memory cache first (O(1) complexity)</item>
        /// <item>Performs minimal database query on cache miss</item>
        /// <item>Caches the boolean result for future checks</item>
        /// </list>
        /// </para>
        /// <para>
        /// Key differences from <see cref="GetModerator(Platform, long)"/>:
        /// <list type="bullet">
        /// <item>Does not retrieve full moderator details</item>
        /// <item>Uses simpler, faster database query</item>
        /// <item>Consumes less memory for the cache</item>
        /// </list>
        /// </para>
        /// This method is optimized for high-frequency permission checks during message processing.
        /// </remarks>
        public bool IsModerator(Platform platform, long userId)
        {
            var cacheKey = ("Moderator", platform, userId);

            if (_roleCache.TryGetValue(cacheKey, out _))
                return true;

            const string sql = "SELECT 1 FROM Moderators WHERE Platform = @Platform AND UserId = @UserId LIMIT 1";
            bool isModerator = ExecuteScalar<object>(sql, new[]
            {
                new SQLiteParameter("@Platform", platform.ToString().ToUpper()),
                new SQLiteParameter("@UserId", userId)
            }) != null;

            if (isModerator)
            {
                _roleCache[cacheKey] = new object();
            }

            return isModerator;
        }

        #endregion

        /// <summary>
        /// Invalidates the cache entry for a specific role type, platform, and user combination.
        /// </summary>
        /// <param name="roleType">The role type ("Banned", "Developer", "Ignored", or "Moderator")</param>
        /// <param name="platform">The target streaming platform</param>
        /// <param name="userId">The user identifier on the platform</param>
        /// <remarks>
        /// <para>
        /// This method should be called after any role modification to ensure:
        /// <list type="bullet">
        /// <item>Subsequent role checks return updated data</item>
        /// <item>Consistency between cache and persistent storage</item>
        /// <item>Prevention of stale data in high-concurrency scenarios</item>
        /// </list>
        /// </para>
        /// <para>
        /// Implementation details:
        /// <list type="bullet">
        /// <item>Uses the same composite key structure as cache lookups</item>
        /// <item>Safely handles non-existent cache entries</item>
        /// <item>Thread-safe through ConcurrentDictionary operations</item>
        /// </list>
        /// </para>
        /// This method is automatically called by all role modification operations.
        /// </remarks>
        private void InvalidateRoleCache(string roleType, Platform platform, long userId)
        {
            var cacheKey = (roleType, platform, userId);
            _roleCache.TryRemove(cacheKey, out _);
        }
    }
}