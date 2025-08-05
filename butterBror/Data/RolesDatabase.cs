using butterBror.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace butterBror.Data
{
    /// <summary>
    /// Represents a banned user record with associated details including platform, ban date, administrator, and reason.
    /// </summary>
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
    /// Represents a developer record with platform and user identifier information.
    /// </summary>
    public class Developer
    {
        public long ID { get; set; }
        public string Platform { get; set; }
        public long UserId { get; set; }
    }

    /// <summary>
    /// Represents an ignored user record with platform and timestamp information.
    /// </summary>
    public class IgnoredUser
    {
        public long ID { get; set; }
        public string Platform { get; set; }
        public long UserId { get; set; }
        public DateTime Date { get; set; }
    }

    /// <summary>
    /// Represents a moderator record with platform, assignment date, and administrator information.
    /// </summary>
    public class Moderator
    {
        public long ID { get; set; }
        public string Platform { get; set; }
        public long UserId { get; set; }
        public DateTime Date { get; set; }
        public string WhoAdded { get; set; }
    }

    /// <summary>
    /// A thread-safe database manager for user role management across multiple platforms.
    /// Provides functionality for tracking banned users, developers, ignored users, and moderators.
    /// </summary>
    public class RolesDatabase : SqlDatabaseBase
    {
        /// <summary>
        /// Initializes a new instance of the RolesDatabase class with the specified database file path.
        /// </summary>
        /// <param name="dbPath">The path to the SQLite database file. Defaults to "Roles.db" if not specified.</param>
        public RolesDatabase(string dbPath = "Roles.db")
            : base(dbPath, true)
        {
            InitializeDatabase();
        }

        /// <summary>
        /// Initializes and configures the database schema by creating necessary tables and indexes for role management.
        /// This method ensures the database structure is properly set up for tracking user roles across multiple platforms.
        /// </summary>
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
        /// Adds a user to the banned list with specified details including platform, ban date, administrator, and reason.
        /// </summary>
        /// <param name="platform">The streaming platform (Twitch, YouTube, etc.)</param>
        /// <param name="userId">The unique identifier of the user</param>
        /// <param name="date">The date when the user was banned</param>
        /// <param name="whoBanned">The administrator who performed the ban</param>
        /// <param name="reason">The reason for the ban</param>
        /// <returns>The database ID of the newly created ban record</returns>
        public long AddBannedUser(PlatformsEnum platform, long userId, DateTime date, string whoBanned, string reason)
        {
            const string sql = @"
                INSERT INTO Banned (Platform, UserId, Date, WhoBanned, Reason)
                VALUES (@Platform, @UserId, @Date, @WhoBanned, @Reason);
                SELECT last_insert_rowid();";
            return ExecuteScalar<long>(sql, new[]
            {
                new SQLiteParameter("@Platform", Enum.GetName(platform).ToUpper() ?? string.Empty),
                new SQLiteParameter("@UserId", userId),
                new SQLiteParameter("@Date", date.ToString("o")),
                new SQLiteParameter("@WhoBanned", whoBanned ?? string.Empty),
                new SQLiteParameter("@Reason", reason ?? string.Empty)
            });
        }

        /// <summary>
        /// Removes a banned user record from the database using the specified record ID.
        /// </summary>
        /// <param name="id">The database ID of the ban record to remove</param>
        /// <returns>The number of deleted records (0 or 1)</returns>
        public long RemoveBannedUser(long id)
        {
            const string sql = "DELETE FROM Banned WHERE ID = @ID";
            return ExecuteNonQuery(sql, new[] { new SQLiteParameter("@ID", id) });
        }

        /// <summary>
        /// Retrieves a banned user record by its unique database identifier.
        /// </summary>
        /// <param name="id">The database ID of the ban record to retrieve</param>
        /// <returns>The banned user record if found; otherwise, <c>null</c></returns>
        public BannedUser GetBannedUserById(long id)
        {
            const string sql = "SELECT * FROM Banned WHERE ID = @ID";
            return QueryFirstOrDefault<BannedUser>(sql, new[] { new SQLiteParameter("@ID", id) });
        }

        /// <summary>
        /// Checks if a user is banned on the specified platform and returns their ban details if found.
        /// </summary>
        /// <param name="platform">The streaming platform (Twitch, YouTube, etc.)</param>
        /// <param name="userId">The unique identifier of the user</param>
        /// <returns>The banned user record if found; otherwise, <c>null</c></returns>
        public BannedUser GetBannedUser(PlatformsEnum platform, long userId)
        {
            const string sql = "SELECT * FROM Banned WHERE Platform = @Platform AND UserId = @UserId";
            return QueryFirstOrDefault<BannedUser>(sql, new[]
            {
                new SQLiteParameter("@Platform", platform.ToString().ToUpper() ?? string.Empty),
                new SQLiteParameter("@UserId", userId)
            });
        }

        /// <summary>
        /// Retrieves all banned users for the specified platform.
        /// </summary>
        /// <param name="platform">The streaming platform (Twitch, YouTube, etc.)</param>
        /// <returns>A list of banned user records for the platform</returns>
        public List<BannedUser> GetBannedUsersByPlatform(PlatformsEnum platform)
        {
            const string sql = "SELECT * FROM Banned WHERE Platform = @Platform";
            return Query<BannedUser>(sql, new[] { new SQLiteParameter("@Platform", platform.ToString().ToUpper() ?? string.Empty) });
        }

        /// <summary>
        /// Retrieves all banned users across all platforms.
        /// </summary>
        /// <returns>A list of all banned user records</returns>
        public List<BannedUser> GetAllBannedUsers()
        {
            const string sql = "SELECT * FROM Banned";
            return Query<BannedUser>(sql);
        }

        #endregion

        #region Methods for developers management

        /// <summary>
        /// Adds a user to the developer list for the specified platform.
        /// </summary>
        /// <param name="platform">The streaming platform (Twitch, YouTube, etc.)</param>
        /// <param name="userId">The unique identifier of the user</param>
        /// <returns>The database ID of the newly created developer record</returns>
        public long AddDeveloper(PlatformsEnum platform, long userId)
        {
            const string sql = @"
                INSERT INTO Developers (Platform, UserId)
                VALUES (@Platform, @UserId);
                SELECT last_insert_rowid();";
            return ExecuteScalar<long>(sql, new[]
            {
                new SQLiteParameter("@Platform", platform.ToString().ToUpper() ?? string.Empty),
                new SQLiteParameter("@UserId", userId)
            });
        }

        /// <summary>
        /// Removes a developer record from the database using the specified record ID.
        /// </summary>
        /// <param name="id">The database ID of the developer record to remove</param>
        /// <returns>The number of deleted records (0 or 1)</returns>
        public long RemoveDeveloper(long id)
        {
            const string sql = "DELETE FROM Developers WHERE ID = @ID";
            return ExecuteNonQuery(sql, new[] { new SQLiteParameter("@ID", id) });
        }

        /// <summary>
        /// Checks if a user is a developer on the specified platform and returns their developer record if found.
        /// </summary>
        /// <param name="platform">The streaming platform (Twitch, YouTube, etc.)</param>
        /// <param name="userId">The unique identifier of the user</param>
        /// <returns>The developer record if found; otherwise, <c>null</c></returns>
        public Developer GetDeveloper(PlatformsEnum platform, long userId)
        {
            const string sql = "SELECT * FROM Developers WHERE Platform = @Platform AND UserId = @UserId";
            return QueryFirstOrDefault<Developer>(sql, new[]
            {
                new SQLiteParameter("@Platform", platform.ToString().ToUpper() ?? string.Empty),
                new SQLiteParameter("@UserId", userId)
            });
        }

        /// <summary>
        /// Retrieves all developers for the specified platform.
        /// </summary>
        /// <param name="platform">The streaming platform (Twitch, YouTube, etc.)</param>
        /// <returns>A list of developer records for the platform</returns>
        public List<Developer> GetDevelopersByPlatform(PlatformsEnum platform)
        {
            const string sql = "SELECT * FROM Developers WHERE Platform = @Platform";
            return Query<Developer>(sql, new[] { new SQLiteParameter("@Platform", platform.ToString().ToUpper() ?? string.Empty) });
        }

        /// <summary>
        /// Retrieves all developers across all platforms.
        /// </summary>
        /// <returns>A list of all developer records</returns>
        public List<Developer> GetAllDevelopers()
        {
            const string sql = "SELECT * FROM Developers";
            return Query<Developer>(sql);
        }

        #endregion

        #region Methods for ignored users management

        /// <summary>
        /// Adds a user to the ignored list with the specified platform and timestamp.
        /// </summary>
        /// <param name="platform">The streaming platform (Twitch, YouTube, etc.)</param>
        /// <param name="userId">The unique identifier of the user</param>
        /// <param name="date">The date when the user was added to the ignore list</param>
        /// <returns>The database ID of the newly created ignore record</returns>
        public long AddIgnoredUser(PlatformsEnum platform, long userId, DateTime date)
        {
            const string sql = @"
                INSERT INTO Ignored (Platform, UserId, Date)
                VALUES (@Platform, @UserId, @Date);
                SELECT last_insert_rowid();";
            return ExecuteScalar<long>(sql, new[]
            {
                new SQLiteParameter("@Platform", platform.ToString().ToUpper() ?? string.Empty),
                new SQLiteParameter("@UserId", userId),
                new SQLiteParameter("@Date", date.ToString("o"))
            });
        }

        /// <summary>
        /// Removes an ignored user record from the database using the specified record ID.
        /// </summary>
        /// <param name="id">The database ID of the ignore record to remove</param>
        /// <returns>The number of deleted records (0 or 1)</returns>
        public long RemoveIgnoredUser(long id)
        {
            const string sql = "DELETE FROM Ignored WHERE ID = @ID";
            return ExecuteNonQuery(sql, new[] { new SQLiteParameter("@ID", id) });
        }

        /// <summary>
        /// Checks if a user is in the ignored list for the specified platform and returns their ignore record if found.
        /// </summary>
        /// <param name="platform">The streaming platform (Twitch, YouTube, etc.)</param>
        /// <param name="userId">The unique identifier of the user</param>
        /// <returns>The ignored user record if found; otherwise, <c>null</c></returns>
        public IgnoredUser GetIgnoredUser(PlatformsEnum platform, long userId)
        {
            const string sql = "SELECT * FROM Ignored WHERE Platform = @Platform AND UserId = @UserId";
            return QueryFirstOrDefault<IgnoredUser>(sql, new[]
            {
                new SQLiteParameter("@Platform", platform.ToString().ToUpper() ?? string.Empty),
                new SQLiteParameter("@UserId", userId)
            });
        }

        /// <summary>
        /// Retrieves all ignored users for the specified platform.
        /// </summary>
        /// <param name="platform">The streaming platform (Twitch, YouTube, etc.)</param>
        /// <returns>A list of ignored user records for the platform</returns>
        public List<IgnoredUser> GetIgnoredUsersByPlatform(PlatformsEnum platform)
        {
            const string sql = "SELECT * FROM Ignored WHERE Platform = @Platform";
            return Query<IgnoredUser>(sql, new[] { new SQLiteParameter("@Platform", platform.ToString().ToUpper() ?? string.Empty) });
        }

        /// <summary>
        /// Retrieves all ignored users across all platforms.
        /// </summary>
        /// <returns>A list of all ignored user records</returns>
        public List<IgnoredUser> GetAllIgnoredUsers()
        {
            const string sql = "SELECT * FROM Ignored";
            return Query<IgnoredUser>(sql);
        }

        #endregion

        #region Methods for moderators management

        /// <summary>
        /// Adds a user to the moderator list with specified platform, assignment date, and administrator information.
        /// </summary>
        /// <param name="platform">The streaming platform (Twitch, YouTube, etc.)</param>
        /// <param name="userId">The unique identifier of the user</param>
        /// <param name="date">The date when the user was assigned as moderator</param>
        /// <param name="whoAdded">The administrator who assigned the moderator role</param>
        /// <returns>The database ID of the newly created moderator record</returns>
        public long AddModerator(PlatformsEnum platform, long userId, DateTime date, string whoAdded)
        {
            const string sql = @"
                INSERT INTO Moderators (Platform, UserId, Date, WhoAdded)
                VALUES (@Platform, @UserId, @Date, @WhoAdded);
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
        /// Removes a moderator record from the database using the specified record ID.
        /// </summary>
        /// <param name="id">The database ID of the moderator record to remove</param>
        /// <returns>The number of deleted records (0 or 1)</returns>
        public long RemoveModerator(long id)
        {
            const string sql = "DELETE FROM Moderators WHERE ID = @ID";
            return ExecuteNonQuery(sql, new[] { new SQLiteParameter("@ID", id) });
        }

        /// <summary>
        /// Checks if a user is a moderator on the specified platform and returns their moderator record if found.
        /// </summary>
        /// <param name="platform">The streaming platform (Twitch, YouTube, etc.)</param>
        /// <param name="userId">The unique identifier of the user</param>
        /// <returns>The moderator record if found; otherwise, <c>null</c></returns>
        public Moderator GetModerator(PlatformsEnum platform, long userId)
        {
            const string sql = "SELECT * FROM Moderators WHERE Platform = @Platform AND UserId = @UserId";
            return QueryFirstOrDefault<Moderator>(sql, new[]
            {
                new SQLiteParameter("@Platform", platform.ToString().ToUpper() ?? string.Empty),
                new SQLiteParameter("@UserId", userId)
            });
        }

        /// <summary>
        /// Retrieves all moderators for the specified platform.
        /// </summary>
        /// <param name="platform">The streaming platform (Twitch, YouTube, etc.)</param>
        /// <returns>A list of moderator records for the platform</returns>
        public List<Moderator> GetModeratorsByPlatform(PlatformsEnum platform)
        {
            const string sql = "SELECT * FROM Moderators WHERE Platform = @Platform";
            return Query<Moderator>(sql, new[] { new SQLiteParameter("@Platform", platform.ToString().ToUpper() ?? string.Empty) });
        }

        /// <summary>
        /// Retrieves all moderators across all platforms.
        /// </summary>
        /// <returns>A list of all moderator records</returns>
        public List<Moderator> GetAllModerators()
        {
            const string sql = "SELECT * FROM Moderators";
            return Query<Moderator>(sql);
        }

        #endregion
    }
}