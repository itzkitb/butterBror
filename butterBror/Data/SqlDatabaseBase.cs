using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace butterBror.Data
{
    /// <summary>
    /// An abstract base class for all SQL database managers.
    /// Provides common functionality for database connection management and query execution across various database implementations.
    /// </summary>
    public abstract class SqlDatabaseBase : IDisposable
    {
        private readonly string _dbPath;
        private readonly bool _sharedCache;
        private readonly string _connectionString;
        private bool _disposed;
        protected SQLiteConnection Connection { get; private set; }

        /// <summary>
        /// Initializes a new instance of the SqlDatabaseBase class with the specified database file path and connection options.
        /// Creates the database directory if it doesn't exist and establishes a connection to the database.
        /// </summary>
        /// <param name="dbPath">The path to the database file</param>
        /// <param name="sharedCache">Whether to use shared cache mode for the database connection (default: true)</param>
        protected SqlDatabaseBase(string dbPath, bool sharedCache = true)
        {
            _dbPath = dbPath;
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

        #region Database operation helper methods

        /// <summary>
        /// Executes a scalar query and returns the result as the specified type.
        /// Handles null and DBNull values by returning the default value for the type.
        /// </summary>
        /// <typeparam name="T">The type to which the result should be converted</typeparam>
        /// <param name="sql">The SQL query to execute</param>
        /// <param name="parameters">Optional parameters for the query</param>
        /// <returns>The scalar result converted to the specified type, or default value if null or DBNull</returns>
        protected T ExecuteScalar<T>(string sql, IEnumerable<SQLiteParameter> parameters = null)
        {
            using var cmd = CreateCommand(sql, parameters);
            var result = cmd.ExecuteScalar();
            return result == null || result == DBNull.Value ? default : (T)Convert.ChangeType(result, typeof(T));
        }

        /// <summary>
        /// Executes a non-query command (INSERT, UPDATE, DELETE) and returns the number of affected rows.
        /// </summary>
        /// <param name="sql">The SQL command to execute</param>
        /// <param name="parameters">Optional parameters for the command</param>
        /// <returns>The number of rows affected by the command</returns>
        protected int ExecuteNonQuery(string sql, IEnumerable<SQLiteParameter> parameters = null)
        {
            using var cmd = CreateCommand(sql, parameters);
            return cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Executes a query and returns a list of objects of the specified type.
        /// Automatically maps database columns to object properties based on name matching, with special handling for common type conversions.
        /// </summary>
        /// <typeparam name="T">The type of objects to return (must have a parameterless constructor)</typeparam>
        /// <param name="sql">The SQL query to execute</param>
        /// <param name="parameters">Optional parameters for the query</param>
        /// <returns>A list of objects populated with query results</returns>
        protected List<T> Query<T>(string sql, IEnumerable<SQLiteParameter> parameters = null) where T : new()
        {
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
        /// Executes a query and returns the first object of the specified type or default if none found.
        /// </summary>
        /// <typeparam name="T">The type of object to return (must have a parameterless constructor)</typeparam>
        /// <param name="sql">The SQL query to execute</param>
        /// <param name="parameters">Optional parameters for the query</param>
        /// <returns>The first object matching the query or default if none found</returns>
        protected T QueryFirstOrDefault<T>(string sql, IEnumerable<SQLiteParameter> parameters = null) where T : new()
        {
            var list = Query<T>(sql, parameters);
            return list.FirstOrDefault();
        }

        /// <summary>
        /// Creates and configures a SQL command with the specified query and parameters.
        /// Sets default command timeout to 30 seconds.
        /// </summary>
        /// <param name="sql">The SQL command text</param>
        /// <param name="parameters">Optional parameters for the command</param>
        /// <returns>A configured SQLiteCommand object ready for execution</returns>
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
        /// Releases all resources used by the SqlDatabaseBase instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the SqlDatabaseBase and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources</param>
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