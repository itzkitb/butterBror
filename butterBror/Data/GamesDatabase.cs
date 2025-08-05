using butterBror.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;

namespace butterBror.Data
{
    /// <summary>
    /// A thread-safe database manager for game-related data storage and operations.
    /// Provides functionality for tracking user statistics, leaderboards, and game currency across multiple platforms.
    /// </summary>
    public class GamesDatabase : SqlDatabaseBase
    {
        /// <summary>
        /// Model for leaderboard entries containing user identifier and associated value.
        /// Used for representing top players in various game statistics.
        /// </summary>
        public class LeaderboardEntry
        {
            public long UserId { get; set; }
            public int Value { get; set; }
        }

        /// <summary>
        /// Initializes a new instance of the GamesDatabase class with the specified database file path.
        /// </summary>
        /// <param name="dbPath">The path to the SQLite database file. Defaults to "Games.db" if not specified.</param>
        public GamesDatabase(string dbPath = "Games.db")
            : base(dbPath, true)
        {
            InitializeDatabase();
        }

        /// <summary>
        /// Initializes and configures the database schema by creating necessary tables and indexes for game data storage.
        /// This method ensures the database structure is properly set up for tracking game currencies and user statistics.
        /// </summary>
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
        /// Ensures a user record exists in the specified table, creating it with default values if necessary.
        /// This method is called before any data operations to guarantee the user has a valid entry.
        /// </summary>
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
        /// Retrieves user data from the specified table and column, initializing the user record if not found.
        /// Returns the current value for the requested statistic, or 0 if no record exists.
        /// </summary>
        /// <param name="tableName">The name of the table to query (Frogs or Cookies)</param>
        /// <param name="platform">The streaming platform (Twitch, YouTube, etc.)</param>
        /// <param name="userId">The unique identifier of the user</param>
        /// <param name="columnName">The column name containing the desired statistic</param>
        /// <returns>The current value of the requested statistic for the user</returns>
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
        /// Sets or updates user data in the specified table by incrementing the value in the target column.
        /// This method safely handles both existing and new user records through the EnsureUserExists call.
        /// </summary>
        /// <param name="tableName">The name of the table to update (Frogs or Cookies)</param>
        /// <param name="platform">The streaming platform (Twitch, YouTube, etc.)</param>
        /// <param name="userId">The unique identifier of the user</param>
        /// <param name="columnName">The column name to update</param>
        /// <param name="value">The amount to add to the current value</param>
        public void SetData(string tableName, PlatformsEnum platform, long userId, string columnName, object value)
        {
            ValidateTableName(tableName);
            ValidateColumnName(tableName, columnName);

            EnsureUserExists(tableName, platform, userId);

            string updateSql = $@"
                UPDATE {tableName} 
                SET {columnName} = {columnName} + @Value 
                WHERE Platform = @Platform AND UserID = @UserId";

            ExecuteNonQuery(updateSql, new[]
            {
                new SQLiteParameter("@Platform", platform.ToString().ToUpper()),
                new SQLiteParameter("@UserId", userId),
                new SQLiteParameter("@Value", value)
            });
        }

        /// <summary>
        /// Retrieves a leaderboard for the specified table and column, ordered by value in descending order.
        /// Only includes users with positive values in the requested statistic, limiting to the top performers.
        /// </summary>
        /// <param name="tableName">The name of the table to query (Frogs or Cookies)</param>
        /// <param name="platform">The streaming platform (Twitch, YouTube, etc.)</param>
        /// <param name="columnName">The column name used for sorting the leaderboard</param>
        /// <param name="limit">The maximum number of entries to return (default: 10)</param>
        /// <returns>A list of leaderboard entries sorted by value in descending order</returns>
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
        /// Validates that the specified table name is valid (either 'Frogs' or 'Cookies').
        /// Throws an exception if an invalid table name is provided.
        /// </summary>
        /// <param name="tableName">The name of the table to validate</param>
        /// <exception cref="ArgumentException">Thrown when an invalid table name is specified</exception>
        private void ValidateTableName(string tableName)
        {
            if (tableName != "Frogs" && tableName != "Cookies")
            {
                throw new ArgumentException("Invalid table name. Valid values: Frogs, Cookies", nameof(tableName));
            }
        }

        /// <summary>
        /// Validates that the specified column name is valid for the given table.
        /// Throws an exception if an invalid column name is provided for the table.
        /// </summary>
        /// <param name="tableName">The name of the table containing the column</param>
        /// <param name="columnName">The name of the column to validate</param>
        /// <exception cref="ArgumentException">Thrown when an invalid column name is specified</exception>
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