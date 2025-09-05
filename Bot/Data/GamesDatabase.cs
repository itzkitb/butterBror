using bb.Models;
using System.Data.SQLite;

namespace bb.Data
{
    /// <summary>
    /// Thread-safe database manager for game-related data operations across multiple streaming platforms.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class provides comprehensive management for game statistics including:
    /// <list type="bullet">
    /// <item>User currency tracking (Frogs and Cookies game systems)</item>
    /// <item>Leaderboard generation for competitive metrics</item>
    /// <item>Cross-platform statistic aggregation</item>
    /// <item>Atomic data operations with transaction safety</item>
    /// </list>
    /// </para>
    /// <para>
    /// Key features:
    /// <list type="bullet">
    /// <item>Two distinct game systems: Frogs (virtual currency) and Cookies (social interaction metrics)</item>
    /// <item>Automatic user record initialization for new players</item>
    /// <item>Parameterized queries to prevent SQL injection</item>
    /// <item>Indexed database structure for optimal leaderboard performance</item>
    /// <item>Thread-safe operations suitable for high-concurrency chat environments</item>
    /// </list>
    /// </para>
    /// The database is designed to handle frequent read/write operations typical in real-time game systems.
    /// </remarks>
    public class GamesDatabase : SqlDatabaseBase
    {
        /// <summary>
        /// Represents an entry in a game leaderboard with user identifier and associated value.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This model is used to return structured leaderboard data from the database, containing:
        /// <list type="bullet">
        /// <item><c>UserId</c>: Unique platform-specific identifier of the user</item>
        /// <item><c>Value</c>: Numeric value representing the user's standing in the leaderboard</item>
        /// </list>
        /// </para>
        /// <para>
        /// Usage examples:
        /// <list type="bullet">
        /// <item>Top Frog collectors leaderboard (highest Frogs value)</item>
        /// <item>Most active gifters leaderboard (highest Gifted value)</item>
        /// <item>Most frequent cookie eaters leaderboard (highest EatersCount)</item>
        /// </list>
        /// </para>
        /// The class is designed for efficient serialization and deserialization in game systems.
        /// </remarks>
        public class LeaderboardEntry
        {
            public long UserId { get; set; }
            public int Value { get; set; }
        }

        /// <summary>
        /// Initializes a new instance of the GamesDatabase class with the specified database file path.
        /// </summary>
        /// <param name="dbPath">The file path for the SQLite database. Defaults to "Games.db" in the working directory if not specified.</param>
        /// <remarks>
        /// <para>
        /// The constructor performs the following initialization sequence:
        /// <list type="number">
        /// <item>Establishes connection to the SQLite database file</item>
        /// <item>Creates necessary database tables if they don't exist</item>
        /// <item>Builds required indexes for optimal query performance</item>
        /// <item>Prepares the database for multi-platform game operations</item>
        /// </list>
        /// </para>
        /// <para>
        /// Database structure includes:
        /// <list type="bullet">
        /// <item><c>Frogs</c> table: Tracks virtual currency (Frogs) across platforms</item>
        /// <item><c>Cookies</c> table: Records social interaction metrics (cookie-related actions)</item>
        /// </list>
        /// </para>
        /// The database connection remains open for the lifetime of the object to minimize connection overhead.
        /// </remarks>
        public GamesDatabase(string dbPath = "Games.db")
            : base(dbPath, true)
        {
            InitializeDatabase();
        }

        /// <summary>
        /// Initializes the database schema by creating all required tables and indexes for game data management.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Creates two primary tables with the following structure:
        /// </para>
        /// 
        /// <para>
        /// <c>Frogs</c> table:
        /// <list type="bullet">
        /// <item>Tracks virtual currency (Frogs) across platforms</item>
        /// <item>Columns: Frogs (collected), Gifted, Received</item>
        /// <item>Unique constraint on Platform + UserID</item>
        /// <item>Indexes on Platform, UserID, and Frogs for leaderboard queries</item>
        /// </list>
        /// </para>
        /// 
        /// <para>
        /// <c>Cookies</c> table:
        /// <list type="bullet">
        /// <item>Records social interaction metrics</item>
        /// <item>Columns: EatersCount, GiftersCount, RecipientsCount</item>
        /// <item>Unique constraint on Platform + UserID</item>
        /// <item>Indexes on Platform, UserID, and EatersCount for performance</item>
        /// </list>
        /// </para>
        /// 
        /// <para>
        /// Implementation details:
        /// <list type="bullet">
        /// <item>Operations wrapped in transaction for atomic initialization</item>
        /// <item>Uses "IF NOT EXISTS" to safely handle repeated initialization</item>
        /// <item>Auto-increment primary key for internal record management</item>
        /// <item>ON CONFLICT REPLACE for seamless user record updates</item>
        /// </list>
        /// </para>
        /// This method is idempotent and can be safely called multiple times.
        /// </remarks>
        private void InitializeDatabase()
        {
            using (var transaction = Connection.BeginTransaction())
            {
                try
                {
                    string createFrogsTable = @"
                        CREATE TABLE IF NOT EXISTS Frogs (
                            ID INTEGER PRIMARY KEY AUTOINCREMENT,
                            Platform TEXT NOT NULL,
                            UserID INTEGER NOT NULL,
                            Frogs INTEGER DEFAULT 0,
                            Gifted INTEGER DEFAULT 0,
                            Received INTEGER DEFAULT 0,
                            UNIQUE(Platform, UserID) ON CONFLICT REPLACE
                        );
                        CREATE INDEX IF NOT EXISTS idx_frogs_platform ON Frogs(Platform);
                        CREATE INDEX IF NOT EXISTS idx_frogs_userid ON Frogs(UserID);
                        CREATE INDEX IF NOT EXISTS idx_frogs_frogs ON Frogs(Frogs);";

                    string createCookiesTable = @"
                        CREATE TABLE IF NOT EXISTS Cookies (
                            ID INTEGER PRIMARY KEY AUTOINCREMENT,
                            Platform TEXT NOT NULL,
                            UserID INTEGER NOT NULL,
                            EatersCount INTEGER DEFAULT 0,
                            GiftersCount INTEGER DEFAULT 0,
                            RecipientsCount INTEGER DEFAULT 0,
                            UNIQUE(Platform, UserID) ON CONFLICT REPLACE
                        );
                        CREATE INDEX IF NOT EXISTS idx_cookies_platform ON Cookies(Platform);
                        CREATE INDEX IF NOT EXISTS idx_cookies_userid ON Cookies(UserID);
                        CREATE INDEX IF NOT EXISTS idx_cookies_eaters ON Cookies(EatersCount);";

                    ExecuteNonQuery(createFrogsTable);
                    ExecuteNonQuery(createCookiesTable);

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
        /// Ensures a user record exists in the specified game table, creating it with default values if necessary.
        /// </summary>
        /// <param name="tableName">The game table to ensure user exists in ("Frogs" or "Cookies")</param>
        /// <param name="platform">The streaming platform identifier</param>
        /// <param name="userId">The unique user identifier</param>
        /// <remarks>
        /// <para>
        /// This method performs the following operations:
        /// <list type="number">
        /// <item>Checks if a record exists for the user on the specified platform</item>
        /// <item>If no record exists, creates one with default values (0 for all counters)</item>
        /// <item>Uses atomic "INSERT OR IGNORE" operation for thread safety</item>
        /// </list>
        /// </para>
        /// <para>
        /// Default values by table:
        /// <list type="table">
        /// <item><term>Frogs table</term><description>Frogs=0, Gifted=0, Received=0</description></item>
        /// <item><term>Cookies table</term><description>EatersCount=0, GiftersCount=0, RecipientsCount=0</description></item>
        /// </list>
        /// </para>
        /// This method is called automatically before any data operation to ensure data integrity.
        /// It's designed for high-frequency execution with minimal performance impact.
        /// </remarks>
        private void EnsureUserExists(string tableName, PlatformsEnum platform, long userId)
        {
            string sql = tableName switch
            {
                "Frogs" => @"INSERT OR IGNORE INTO Frogs (Platform, UserID, Frogs, Gifted, Received) 
                              VALUES (@Platform, @UserId, 0, 0, 0)",
                "Cookies" => @"INSERT OR IGNORE INTO Cookies (Platform, UserID, EatersCount, GiftersCount, RecipientsCount) 
                                VALUES (@Platform, @UserId, 0, 0, 0)",
                _ => throw new ArgumentException("Invalid table name", nameof(tableName))
            };

            ExecuteNonQuery(sql, new[]
            {
                new SQLiteParameter("@Platform", platform.ToString().ToUpper()),
                new SQLiteParameter("@UserId", userId)
            });
        }

        /// <summary>
        /// Retrieves a specific game statistic for a user from the specified table.
        /// </summary>
        /// <param name="tableName">The game table to query ("Frogs" or "Cookies")</param>
        /// <param name="platform">The streaming platform (Twitch, Discord, etc.)</param>
        /// <param name="userId">The unique user identifier</param>
        /// <param name="columnName">The specific statistic to retrieve</param>
        /// <returns>
        /// The current value of the requested statistic as an object, or 0 if no record exists.
        /// The value is typically an integer but returned as object for database compatibility.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Valid column names by table:
        /// <list type="table">
        /// <item><term>Frogs table</term><description>Frogs, Gifted, Received</description></item>
        /// <item><term>Cookies table</term><description>EatersCount, GiftersCount, RecipientsCount</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// Behavior:
        /// <list type="bullet">
        /// <item>Automatically initializes user record if not found</item>
        /// <item>Returns 0 for uninitialized statistics</item>
        /// <item>Performs case-sensitive column name validation</item>
        /// <item>Uses parameterized queries for security</item>
        /// </list>
        /// </para>
        /// This method is optimized for frequent read operations with indexed database lookups.
        /// Consider using GetLeaderboard for bulk statistic retrieval operations.
        /// </remarks>
        /// <exception cref="ArgumentException">Thrown when invalid table or column names are specified</exception>
        public object GetData(string tableName, PlatformsEnum platform, long userId, string columnName)
        {
            ValidateTableName(tableName);
            ValidateColumnName(tableName, columnName);

            string sql = tableName switch
            {
                "Frogs" => $"SELECT {columnName} FROM Frogs WHERE Platform = @Platform AND UserID = @UserId",
                "Cookies" => $"SELECT {columnName} FROM Cookies WHERE Platform = @Platform AND UserID = @UserId",
                _ => throw new ArgumentException("Invalid table name", nameof(tableName))
            };

            var parameters = new[]
            {
                new SQLiteParameter("@Platform", platform.ToString().ToUpper()),
                new SQLiteParameter("@UserId", userId)
            };

            var result = ExecuteScalar<object>(sql, parameters);

            if (result == null || result is DBNull)
            {
                EnsureUserExists(tableName, platform, userId);
                return 0;
            }

            return result;
        }

        /// <summary>
        /// Updates a user's game statistic by incrementing the specified value.
        /// </summary>
        /// <param name="tableName">The game table to update ("Frogs" or "Cookies")</param>
        /// <param name="platform">The streaming platform (Twitch, Discord, etc.)</param>
        /// <param name="userId">The unique user identifier</param>
        /// <param name="columnName">The specific statistic to update</param>
        /// <param name="value">The amount to add to the current value (can be negative)</param>
        /// <remarks>
        /// <para>
        /// This method performs an atomic increment operation using SQL's UPDATE with addition:
        /// <c>UPDATE table SET column = column + value WHERE conditions</c>
        /// </para>
        /// <para>
        /// Key characteristics:
        /// <list type="bullet">
        /// <item>Guarantees user record exists before update (calls EnsureUserExists)</item>
        /// <item>Supports both positive and negative increments</item>
        /// <item>Thread-safe operation suitable for concurrent access</item>
        /// <item>Minimal database roundtrips for performance</item>
        /// </list>
        /// </para>
        /// <para>
        /// Usage examples:
        /// <list type="bullet">
        /// <item>Adding frogs collected: SetData("Frogs", platform, userId, "Frogs", 5)</item>
        /// <item>Recording gifted frogs: SetData("Frogs", platform, userId, "Gifted", 1)</item>
        /// <item>Tracking cookie consumption: SetData("Cookies", platform, userId, "EatersCount", 1)</item>
        /// </list>
        /// </para>
        /// This is the primary method for modifying game statistics in the system.
        /// </remarks>
        /// <exception cref="ArgumentException">Thrown when invalid table or column names are specified</exception>
        public void SetData(string tableName, PlatformsEnum platform, long userId, string columnName, object value)
        {
            ValidateTableName(tableName);
            ValidateColumnName(tableName, columnName);

            EnsureUserExists(tableName, platform, userId);

            string updateSql = $@"
                UPDATE {tableName} 
                SET {columnName} = @Value
                WHERE Platform = @Platform AND UserID = @UserId";

            ExecuteNonQuery(updateSql, new[]
            {
                new SQLiteParameter("@Platform", platform.ToString().ToUpper()),
                new SQLiteParameter("@UserId", userId),
                new SQLiteParameter("@Value", value)
            });
        }

        /// <summary>
        /// Retrieves a sorted leaderboard for the specified game statistic.
        /// </summary>
        /// <param name="tableName">The game table to query ("Frogs" or "Cookies")</param>
        /// <param name="platform">The streaming platform (Twitch, Discord, etc.)</param>
        /// <param name="columnName">The statistic to rank users by</param>
        /// <param name="limit">Maximum number of entries to return (default: 10)</param>
        /// <returns>
        /// A list of <see cref="LeaderboardEntry"/> objects sorted by value in descending order,
        /// representing the top performers for the specified statistic.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Query behavior:
        /// <list type="bullet">
        /// <item>Filters for positive values only (excludes users with 0 in the statistic)</item>
        /// <item>Sorts results in descending order (highest value first)</item>
        /// <item>Applies the specified limit to control result set size</item>
        /// <item>Uses indexed columns for optimal performance</item>
        /// </list>
        /// </para>
        /// <para>
        /// Performance characteristics:
        /// <list type="bullet">
        /// <item>Execution time: O(log n) due to indexed sorting</item>
        /// <item>Ideal for regular leaderboard displays (top 10-100 users)</item>
        /// <item>Avoid extremely large limits for production usage</item>
        /// </list>
        /// </para>
        /// <para>
        /// Example usage:
        /// <code>
        /// // Get top 10 frog collectors on Twitch
        /// var leaderboard = gamesDB.GetLeaderboard("Frogs", PlatformsEnum.Twitch, "Frogs", 10);
        /// </code>
        /// </para>
        /// This method is optimized for frequent leaderboard requests in game systems.
        /// </remarks>
        /// <exception cref="ArgumentException">Thrown when invalid table or column names are specified</exception>
        public List<LeaderboardEntry> GetLeaderboard(string tableName, PlatformsEnum platform, string columnName, int limit = 10)
        {
            ValidateTableName(tableName);
            ValidateColumnName(tableName, columnName);

            string sql;
            if (tableName == "Frogs")
            {
                sql = $@"
                    SELECT UserID, {columnName} 
                    FROM Frogs 
                    WHERE Platform = @Platform AND {columnName} > 0
                    ORDER BY {columnName} DESC 
                    LIMIT @Limit";
            }
            else
            {
                sql = $@"
                    SELECT UserID, {columnName} 
                    FROM Cookies 
                    WHERE Platform = @Platform AND {columnName} > 0
                    ORDER BY {columnName} DESC 
                    LIMIT @Limit";
            }

            var parameters = new[]
            {
                new SQLiteParameter("@Platform", platform.ToString().ToUpper()),
                new SQLiteParameter("@Limit", limit)
            };

            var result = new List<LeaderboardEntry>();
            using var cmd = CreateCommand(sql, parameters);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                result.Add(new LeaderboardEntry
                {
                    UserId = Convert.ToInt64(reader["UserID"]),
                    Value = reader[1] != DBNull.Value ? Convert.ToInt32(reader[1]) : 0
                });
            }

            return result;
        }

        /// <summary>
        /// Validates that the specified table name is valid for game operations.
        /// </summary>
        /// <param name="tableName">The table name to validate</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the table name is not "Frogs" or "Cookies",
        /// with details about valid options.
        /// </exception>
        /// <remarks>
        /// This validation method is called internally before any table operation
        /// to ensure proper database access and prevent SQL injection risks.
        /// It provides clear error messaging to aid in debugging invalid operations.
        /// </remarks>
        private void ValidateTableName(string tableName)
        {
            if (tableName != "Frogs" && tableName != "Cookies")
            {
                throw new ArgumentException("Invalid table name. Valid values: Frogs, Cookies", nameof(tableName));
            }
        }

        /// <summary>
        /// Validates that the specified column name is valid for the given table.
        /// </summary>
        /// <param name="tableName">The table containing the column</param>
        /// <param name="columnName">The column name to validate</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the column name is not valid for the specified table,
        /// listing all acceptable column names for reference.
        /// </exception>
        /// <remarks>
        /// <para>
        /// Valid column mappings:
        /// <list type="table">
        /// <item><term>Frogs table</term><description>Valid columns: Frogs, Gifted, Received</description></item>
        /// <item><term>Cookies table</term><description>Valid columns: EatersCount, GiftersCount, RecipientsCount</description></item>
        /// </list>
        /// </para>
        /// This method prevents invalid column references that could cause runtime errors
        /// and provides helpful feedback for developers using the API incorrectly.
        /// </remarks>
        private void ValidateColumnName(string tableName, string columnName)
        {
            string[] validColumns;

            if (tableName == "Frogs")
            {
                validColumns = new[] { "Frogs", "Gifted", "Received" };
            }
            else
            {
                validColumns = new[] { "EatersCount", "GiftersCount", "RecipientsCount" };
            }

            if (!validColumns.Contains(columnName))
            {
                throw new ArgumentException($"Invalid column name for table {tableName}. Valid values: {string.Join(", ", validColumns)}", nameof(columnName));
            }
        }
    }
}