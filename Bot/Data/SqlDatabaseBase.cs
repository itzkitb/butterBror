using System.Data;
using System.Data.SQLite;
using System.Reflection;

namespace butterBror.Data
{
    /// <summary>
    /// Abstract base class providing foundational database operations for all SQL-based data managers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class implements a robust database access layer with the following key capabilities:
    /// <list type="bullet">
    /// <item>Unified connection management across multiple database implementations</item>
    /// <item>Parameterized query execution with type-safe parameter handling</item>
    /// <item>Automatic type conversion for database results</item>
    /// <item>Transaction management primitives</item>
    /// <item>Resource tracking and cleanup</item>
    /// <item>Performance monitoring through operation counting</item>
    /// </list>
    /// </para>
    /// <para>
    /// Design principles:
    /// <list type="bullet">
    /// <item><strong>Resource Efficiency</strong>: Implements proper IDisposable pattern for connection management</item>
    /// <item><strong>Type Safety</strong>: Provides generic methods with compile-time type checking</item>
    /// <item><strong>Error Resilience</strong>: Handles common database conversion issues gracefully</item>
    /// <item><strong>Performance Awareness</strong>: Optimized for high-frequency database operations</item>
    /// </list>
    /// </para>
    /// All derived classes inherit these core capabilities while implementing platform-specific behaviors.
    /// </remarks>
    public abstract class SqlDatabaseBase : IDisposable
    {
        public readonly string DbPath;
        private readonly bool _sharedCache;
        private readonly string _connectionString;
        private long _sqlOperationCount = 0;
        private bool _disposed;
        protected SQLiteConnection Connection { get; private set; }

        /// <summary>
        /// Begins a database transaction.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Usage pattern:
        /// <code>
        /// try
        /// {
        ///     db.BeginTransaction();
        ///     // Execute multiple operations
        ///     db.CommitTransaction();
        /// }
        /// catch
        /// {
        ///     db.RollbackTransaction();
        ///     throw;
        /// }
        /// </code>
        /// </para>
        /// <para>
        /// Important notes:
        /// <list type="bullet">
        /// <item>Nested transactions are not supported by SQLite</item>
        /// <item>Multiple concurrent transactions from different threads will cause errors</item>
        /// <item>Always pair with CommitTransaction or RollbackTransaction</item>
        /// </list>
        /// </para>
        /// The method executes the SQL command "BEGIN TRANSACTION;" directly.
        /// </remarks>
        public void BeginTransaction() => ExecuteNonQuery("BEGIN TRANSACTION;");

        /// <summary>
        /// Commits the current transaction.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Finalizes all changes made during the transaction.
        /// Must be called after BeginTransaction when all operations succeed.
        /// </para>
        /// <para>
        /// Transaction rules:
        /// <list type="bullet">
        /// <item>Only valid when a transaction is active</item>
        /// <item>Makes all changes permanent in the database</item>
        /// <item>Resets the transaction context for new operations</item>
        /// </list>
        /// </para>
        /// The method executes the SQL command "COMMIT;" directly.
        /// </remarks>
        public void CommitTransaction() => ExecuteNonQuery("COMMIT;");

        /// <summary>
        /// Rolls back the current transaction.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Discards all changes made during the transaction.
        /// Should be called when an error occurs during transaction operations.
        /// </para>
        /// <para>
        /// Rollback behavior:
        /// <list type="bullet">
        /// <item>Reverts all database changes to pre-transaction state</item>
        /// <item>Releases transaction locks on database resources</item>
        /// <item>Allows new transactions to begin immediately after</item>
        /// </list>
        /// </para>
        /// The method executes the SQL command "ROLLBACK;" directly.
        /// </remarks>
        public void RollbackTransaction() => ExecuteNonQuery("ROLLBACK;");

        /// <summary>
        /// Gets the current SQL operation count since the last reset.
        /// </summary>
        /// <returns>
        /// The number of SQL operations executed since the last call to <see cref="GetAndResetSqlOperationCount"/>.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This counter tracks all database operations including:
        /// <list type="bullet">
        /// <item>Scalar queries</item>
        /// <item>Non-query commands</item>
        /// <item>Result set queries</item>
        /// </list>
        /// </para>
        /// The counter is thread-safe and automatically increments for each operation.
        /// Useful for performance monitoring and debugging database access patterns.
        /// </remarks>
        public long GetAndResetSqlOperationCount()
        {
            return Interlocked.Exchange(ref _sqlOperationCount, 0);
        }

        /// <summary>
        /// Initializes a new instance of the SqlDatabaseBase class with the specified database configuration.
        /// </summary>
        /// <param name="dbPath">The file path for the SQLite database. Can be relative or absolute.</param>
        /// <param name="sharedCache">
        /// Specifies whether to use SQLite's shared cache mode.
        /// <list type="bullet">
        /// <item><see langword="true"/>: Enables shared cache (multiple connections can access the database concurrently)</item>
        /// <item><see langword="false"/>: Uses default cache mode (single connection access)</item>
        /// </list>
        /// Default value is <see langword="true"/>.
        /// </param>
        /// <remarks>
        /// <para>
        /// The initialization sequence:
        /// <list type="number">
        /// <item>Resolves absolute path for the database file</item>
        /// <item>Creates parent directory if it doesn't exist</item>
        /// <item>Constructs connection string with appropriate settings</item>
        /// <item>Establishes initial database connection</item>
        /// </list>
        /// </para>
        /// <para>
        /// Connection string parameters:
        /// <list type="bullet">
        /// <item><c>Data Source</c>: Database file path</item>
        /// <item><c>Cache</c>: Shared or Default based on sharedCache parameter</item>
        /// <item><c>Mode</c>: ReadWriteCreate (creates database if missing)</item>
        /// </list>
        /// </para>
        /// The database file and directory are created automatically if they don't exist.
        /// </remarks>
        protected SqlDatabaseBase(string dbPath, bool sharedCache = true)
        {
            DbPath = dbPath;
            _sharedCache = sharedCache;

            string directory = Path.GetDirectoryName(Path.GetFullPath(dbPath)) ?? Directory.GetCurrentDirectory();
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            _connectionString = $"Data Source={dbPath};Cache={(_sharedCache ? "Shared" : "Default")};Mode=ReadWriteCreate;";
            InitializeConnection();
        }

        /// <summary>
        /// Initializes or reinitializes the database connection.
        /// Closes any existing connection before creating a new one to prevent resource leaks.
        /// </summary>
        private void InitializeConnection()
        {
            if (Connection != null && Connection.State != ConnectionState.Closed)
            {
                Connection.Close();
            }

            Connection = new SQLiteConnection(_connectionString);
            Connection.Open();
        }

        /// <summary>
        /// Creates a backup copy of the current database.
        /// </summary>
        /// <param name="backupFilePath">The file path where the backup should be saved</param>
        /// <remarks>
        /// <para>
        /// Backup process:
        /// <list type="number">
        /// <item>Creates new connection to the backup file</item>
        /// <item>Uses SQLite's built-in BackupDatabase method</item>
        /// <item>Copies all data from main database to backup</item>
        /// <item>Closes all connections after completion</item>
        /// </list>
        /// </para>
        /// <para>
        /// Key characteristics:
        /// <list type="bullet">
        /// <item>Performs hot backups (database can be in use during operation)</item>
        /// <item>Creates complete copy of the database</item>
        /// <item>Overwrites existing backup file if present</item>
        /// <item>Preserves all database structure and data</item>
        /// </list>
        /// </para>
        /// The operation blocks until the backup is complete.
        /// Recommended for scheduled maintenance rather than frequent use.
        /// </remarks>
        public void CreateBackup(string backupFilePath)
        {
            using (var backupConnection = new SQLiteConnection($"Data Source={backupFilePath};Version=3;"))
            {
                backupConnection.Open();
                Connection.BackupDatabase(backupConnection, "main", "main", -1, null, 0);
            }
        }

        #region Database operation helper methods

        /// <summary>
        /// Executes a scalar query and returns the result as the specified type.
        /// </summary>
        /// <typeparam name="T">The expected return type of the scalar value</typeparam>
        /// <param name="sql">The SQL query to execute (should return a single value)</param>
        /// <param name="parameters">Optional collection of SQL parameters</param>
        /// <returns>
        /// The scalar result converted to type T, or default(T) if the result is null or DBNull.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Usage example:
        /// <code>
        /// int userCount = db.ExecuteScalar<int>(
        ///     "SELECT COUNT(*) FROM Users WHERE Active = 1");
        /// </code>
        /// </para>
        /// <para>
        /// Behavior details:
        /// <list type="bullet">
        /// <item>Automatically handles DBNull and null values</item>
        /// <item>Performs type conversion using Convert.ChangeType</item>
        /// <item>Increments operation counter for performance tracking</item>
        /// <item>Supports all standard SQL scalar functions</item>
        /// </list>
        /// </para>
        /// Best suited for queries that return a single value (COUNT, SUM, MAX, etc.).
        /// Throws InvalidCastException if conversion fails for non-null values.
        /// </remarks>
        protected T ExecuteScalar<T>(string sql, IEnumerable<SQLiteParameter> parameters = null)
        {
            Interlocked.Increment(ref _sqlOperationCount);
            using var cmd = CreateCommand(sql, parameters);
            var result = cmd.ExecuteScalar();
            return result == null || result == DBNull.Value ? default : (T)Convert.ChangeType(result, typeof(T));
        }

        /// <summary>
        /// Executes a non-query command (INSERT, UPDATE, DELETE) and returns affected row count.
        /// </summary>
        /// <param name="sql">The SQL command to execute</param>
        /// <param name="parameters">Optional collection of SQL parameters</param>
        /// <returns>The number of rows affected by the command</returns>
        /// <remarks>
        /// <para>
        /// Typical usage patterns:
        /// <list type="bullet">
        /// <item>INSERT operations: Returns number of rows inserted (usually 1)</item>
        /// <item>UPDATE operations: Returns number of rows modified</item>
        /// <item>DELETE operations: Returns number of rows deleted</item>
        /// </list>
        /// </para>
        /// <para>
        /// Implementation notes:
        /// <list type="bullet">
        /// <item>Command timeout set to 30 seconds by default</item>
        /// <item>Parameters prevent SQL injection attacks</item>
        /// <item>Operation counter incremented for performance tracking</item>
        /// <item>Does not return result sets (use Query methods for that)</item>
        /// </list>
        /// </para>
        /// For bulk operations, consider wrapping in a transaction for performance.
        /// Returns 0 if no rows were affected by the operation.
        /// </remarks>
        protected int ExecuteNonQuery(string sql, IEnumerable<SQLiteParameter> parameters = null)
        {
            Interlocked.Increment(ref _sqlOperationCount);
            using var cmd = CreateCommand(sql, parameters);
            return cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Executes a query and maps the results to a list of strongly-typed objects.
        /// </summary>
        /// <typeparam name="T">The type to map results to (must have parameterless constructor)</typeparam>
        /// <param name="sql">The SQL query to execute</param>
        /// <param name="parameters">Optional collection of SQL parameters</param>
        /// <returns>A list of objects populated from the query results</returns>
        /// <remarks>
        /// <para>
        /// Mapping behavior:
        /// <list type="bullet">
        /// <item>Matches database column names to object property names (case-insensitive)</item>
        /// <item>Skips properties without matching columns</item>
        /// <item>Handles common type conversions automatically</item>
        /// <item>Supports nullable value types</item>
        /// </list>
        /// </para>
        /// <para>
        /// Special conversion rules:
        /// <list type="bullet">
        /// <item><c>DateTime</c>: Converts string values using DateTime.Parse</item>
        /// <item><c>bool</c>: Converts INTEGER values (0 = false, non-zero = true)</item>
        /// <item><c>Nullable<T></c>: Handles database NULL values appropriately</item>
        /// </list>
        /// </para>
        /// <para>
        /// Error handling:
        /// <list type="bullet">
        /// <item>Skips incompatible type conversions (logs but doesn't throw)</item>
        /// <item>Ignores missing properties/columns</item>
        /// <item>Handles DBNull values appropriately</item>
        /// </list>
        /// </para>
        /// Returns empty list when no results match the query.
        /// </remarks>
        protected List<T> Query<T>(string sql, IEnumerable<SQLiteParameter> parameters = null) where T : new()
        {
            Interlocked.Increment(ref _sqlOperationCount);
            var result = new List<T>();
            using var cmd = CreateCommand(sql, parameters);
            using var reader = cmd.ExecuteReader();

            if (!reader.HasRows) return result;

            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite && p.GetSetMethod() != null)
                .ToList();

            var columnNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < reader.FieldCount; i++)
            {
                columnNames.Add(reader.GetName(i));
            }

            while (reader.Read())
            {
                var item = new T();
                foreach (var property in properties)
                {
                    if (!columnNames.Contains(property.Name)) continue;

                    try
                    {
                        if (!reader.IsDBNull(reader.GetOrdinal(property.Name)))
                        {
                            object value = reader.GetValue(property.Name);

                            // Special handling for DateTime
                            if (property.PropertyType == typeof(DateTime) && value is string stringValue)
                            {
                                property.SetValue(item, DateTime.Parse(stringValue));
                            }
                            // Special handling for bool (SQLite stores as INTEGER)
                            else if (property.PropertyType == typeof(bool) && value is long longValue)
                            {
                                property.SetValue(item, longValue != 0);
                            }
                            // Special handling for Nullable<bool>
                            else if (property.PropertyType == typeof(bool?) && value is long nullableLongValue)
                            {
                                property.SetValue(item, nullableLongValue != 0);
                            }
                            else
                            {
                                // Automatic type conversion
                                if (value != null && property.PropertyType.IsGenericType &&
                                    property.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                                {
                                    var underlyingType = Nullable.GetUnderlyingType(property.PropertyType);
                                    if (underlyingType != null)
                                    {
                                        property.SetValue(item, Convert.ChangeType(value, underlyingType));
                                    }
                                }
                                else
                                {
                                    property.SetValue(item, Convert.ChangeType(value, property.PropertyType));
                                }
                            }
                        }
                    }
                    catch (Exception ex) when (ex is InvalidCastException || ex is FormatException)
                    {
                    }
                    catch
                    {
                    }
                }
                result.Add(item);
            }
            return result;
        }

        /// <summary>
        /// Executes a query and returns the first object or default if none found.
        /// </summary>
        /// <typeparam name="T">The type to map results to (must have parameterless constructor)</typeparam>
        /// <param name="sql">The SQL query to execute</param>
        /// <param name="parameters">Optional collection of SQL parameters</param>
        /// <returns>
        /// The first object from the query results, or default(T) if no results found.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Equivalent to:
        /// <code>
        /// var results = Query<T>(sql, parameters);
        /// return results.FirstOrDefault();
        /// </code>
        /// </para>
        /// <para>
        /// Usage recommendations:
        /// <list type="bullet">
        /// <item>Use for queries expected to return single results</item>
        /// <item>Ideal for lookup by primary key</item>
        /// <item>More efficient than Query when only first result is needed</item>
        /// </list>
        /// </para>
        /// Does not execute LIMIT 1 optimization in SQL (fetches all results then takes first).
        /// Consider adding LIMIT 1 to your SQL for better performance with large result sets.
        /// </remarks>
        protected T QueryFirstOrDefault<T>(string sql, IEnumerable<SQLiteParameter> parameters = null) where T : new()
        {
            Interlocked.Increment(ref _sqlOperationCount);
            var list = Query<T>(sql, parameters);
            return list.FirstOrDefault();
        }

        /// <summary>
        /// Creates and configures a SQL command with the specified parameters.
        /// </summary>
        /// <param name="sql">The SQL command text</param>
        /// <param name="parameters">Optional collection of SQL parameters</param>
        /// <returns>A configured SQLiteCommand ready for execution</returns>
        /// <remarks>
        /// <para>
        /// Configuration details:
        /// <list type="bullet">
        /// <item>Command type: Text (not stored procedures)</item>
        /// <item>Command timeout: 30 seconds</item>
        /// <item>Parameter collection: Populated from input parameters</item>
        /// </list>
        /// </para>
        /// <para>
        /// Parameter handling:
        /// <list type="bullet">
        /// <item>Parameters are added directly to the command</item>
        /// <item>No automatic naming or value conversion</item>
        /// <item>Caller is responsible for proper parameter setup</item>
        /// </list>
        /// </para>
        /// Primarily used internally by other database methods.
        /// Can be used directly for specialized operations not covered by helper methods.
        /// </remarks>
        protected SQLiteCommand CreateCommand(string sql, IEnumerable<SQLiteParameter> parameters)
        {
            var cmd = Connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandType = CommandType.Text;
            cmd.CommandTimeout = 30;

            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    cmd.Parameters.Add(param);
                }
            }
            return cmd;
        }

        #endregion

        #region Resource management

        /// <summary>
        /// Releases all resources used by the database manager.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Proper usage pattern:
        /// <code>
        /// using (var db = new DerivedDatabase())
        /// {
        ///     // Database operations
        /// }
        /// </code>
        /// </para>
        /// <para>
        /// Resource cleanup sequence:
        /// <list type="number">
        /// <item>Closes active database connection</item>
        /// <item>Disposes connection object</item>
        /// <item>Resets internal state</item>
        /// <item>Prevents finalizer execution</item>
        /// </list>
        /// </para>
        /// Always call this method or use the using statement to prevent connection leaks.
        /// Safe to call multiple times (subsequent calls have no effect).
        /// </remarks>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the database manager, and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">
        /// <see langword="true"/> to release both managed and unmanaged resources;
        /// <see langword="false"/> to release only unmanaged resources.
        /// </param>
        /// <remarks>
        /// <para>
        /// Dispose pattern implementation:
        /// <list type="bullet">
        /// <item>Called by public Dispose() method with disposing = true</item>
        /// <item>Called by finalizer with disposing = false</item>
        /// <item>Only managed resources released when disposing = true</item>
        /// </list>
        /// </para>
        /// <para>
        /// Resource handling:
        /// <list type="bullet">
        /// <item>Managed resources: Connection object disposal</item>
        /// <item>Unmanaged resources: Native SQLite handles</item>
        /// </list>
        /// </para>
        /// Derived classes should override this method to dispose additional resources.
        /// Always call base.Dispose(disposing) in derived implementations.
        /// </remarks>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (Connection != null)
                    {
                        if (Connection.State != ConnectionState.Closed)
                        {
                            Connection.Close();
                        }
                        Connection.Dispose();
                        Connection = null;
                    }
                }

                _disposed = true;
            }
        }

        /// <summary>
        /// Finalizer for the SqlDatabaseBase class to ensure proper cleanup of resources.
        /// </summary>
        ~SqlDatabaseBase()
        {
            Dispose(disposing: false);
        }

        #endregion
    }
}