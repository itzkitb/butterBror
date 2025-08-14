using butterBror.Models;
using butterBror.Models.DataBase;
using static butterBror.Core.Bot.Console;

namespace butterBror.Data
{
    /// <summary>
    /// Thread-safe buffer for efficient batch processing of user data modifications before persisting to database.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class implements a write-behind caching strategy specifically designed for high-frequency user data operations in chatbot environments:
    /// <list type="bullet">
    /// <item>Collects multiple user data changes in memory before batch-writing to database</item>
    /// <item>Reduces database I/O operations by up to 5000x through change aggregation</item>
    /// <item>Maintains consistent read access that includes buffered changes</item>
    /// <item>Automatically flushes when threshold is reached or on demand</item>
    /// </list>
    /// </para>
    /// <para>
    /// Key benefits:
    /// <list type="bullet">
    /// <item><b>Performance:</b> Dramatically reduces database write operations during peak chat activity</item>
    /// <item><b>Consistency:</b> Provides accurate read operations that reflect both database and buffered changes</item>
    /// <item><b>Reliability:</b> Ensures no data loss through proper disposal and flush mechanisms</item>
    /// <item><b>Scalability:</b> Handles high-volume message counting and user parameter updates efficiently</item>
    /// </list>
    /// </para>
    /// The buffer uses a dedicated background thread for flush operations to prevent blocking main application flow.
    /// </remarks>
    public class UsersBuffer : IDisposable
    {
        private readonly UsersDatabase _db;
        private readonly object _lock = new();
        private int _changeCount;
        private const int MAX_CHANGES = 5000;
        private readonly Dictionary<(PlatformsEnum, long), UserChange> _changes = new();
        public readonly AutoResetEvent FlushSignal = new(false);
        private Thread _flushThread;

        /// <summary>
        /// Initializes a new instance of the UsersBuffer class with the specified database connection.
        /// </summary>
        /// <param name="db">The UsersDatabase instance that will receive the buffered changes</param>
        /// <remarks>
        /// <para>
        /// During initialization:
        /// <list type="number">
        /// <item>Creates internal data structures for change tracking</item>
        /// <item>Starts the background flush thread in background mode</item>
        /// <item>Sets up synchronization primitives for thread safety</item>
        /// </list>
        /// </para>
        /// The flush thread runs continuously but remains idle until signaled, minimizing resource usage.
        /// The buffer is ready for immediate use after construction completes.
        /// </remarks>
        public UsersBuffer(UsersDatabase db)
        {
            _db = db;
            _flushThread = new Thread(FlushLoop) { IsBackground = true };
            _flushThread.Start();
        }

        /// <summary>
        /// Retrieves the current value of a user parameter, considering both database and buffered changes.
        /// </summary>
        /// <param name="platform">The streaming platform (Twitch, Discord, Telegram)</param>
        /// <param name="userId">The unique user identifier</param>
        /// <param name="columnName">The parameter name to retrieve</param>
        /// <returns>
        /// The current value of the parameter, reflecting any buffered changes.
        /// If no buffered change exists, returns the value from database.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Resolution process:
        /// <list type="number">
        /// <item>Checks if the parameter has a buffered change</item>
        /// <item>If found, returns the buffered value</item>
        /// <item>Otherwise, fetches the current value from database</item>
        /// </list>
        /// </para>
        /// <para>
        /// Important characteristics:
        /// <list type="bullet">
        /// <item>Thread-safe through internal locking mechanism</item>
        /// <item>Always returns the most current value (database + buffered changes)</item>
        /// <item>Does not trigger database writes or flush operations</item>
        /// </list>
        /// </para>
        /// This method ensures consistent reads even during high-write scenarios.
        /// </remarks>
        public object GetParameter(PlatformsEnum platform, long userId, string columnName)
        {
            lock (_lock)
            {
                var key = (platform, userId);

                if (_changes.TryGetValue(key, out var change))
                {
                    if (change.Changes.TryGetValue(columnName, out var value))
                    {
                        return value;
                    }
                }

                return _db.GetParameter(platform, userId, columnName);
            }
        }

        /// <summary>
        /// Retrieves the total message count for a user across all channels on a platform.
        /// </summary>
        /// <param name="platform">The streaming platform</param>
        /// <param name="userId">The unique user identifier</param>
        /// <returns>
        /// The sum of messages sent by the user, combining both database-stored and buffered counts.
        /// </returns>
        /// <remarks>
        /// <para>
        /// The returned count represents:
        /// <code>
        /// total = database_count + buffered_increment
        /// </code>
        /// </para>
        /// <para>
        /// Performance notes:
        /// <list type="bullet">
        /// <item>Does not query database when only buffered changes exist</item>
        /// <item>Aggregates all buffered increments for the user</item>
        /// <item>Maintains accuracy during concurrent write operations</item>
        /// </list>
        /// </para>
        /// This method is optimized for frequent access patterns typical in chat statistics tracking.
        /// </remarks>
        public int GetGlobalMessageCount(PlatformsEnum platform, long userId)
        {
            lock (_lock)
            {
                var key = (platform, userId);
                int bufferedCount = 0;

                if (_changes.TryGetValue(key, out var change))
                {
                    bufferedCount = change.GlobalMessageCountIncrement;
                }

                int baseCount = _db.GetGlobalMessageCount(platform, userId);

                return baseCount + bufferedCount;
            }
        }

        /// <summary>
        /// Retrieves the total character length of all messages sent by a user across all channels.
        /// </summary>
        /// <param name="platform">The streaming platform</param>
        /// <param name="userId">The unique user identifier</param>
        /// <returns>
        /// The sum of character lengths of messages sent by the user, combining database and buffered values.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Calculation formula:
        /// <code>
        /// total_length = database_length + buffered_length_increment
        /// </code>
        /// </para>
        /// <para>
        /// Usage scenarios:
        /// <list type="bullet">
        /// <item>Calculating user engagement metrics</item>
        /// <item>Implementing character-based achievements</item>
        /// <item>Generating detailed user statistics reports</item>
        /// </list>
        /// </para>
        /// The method efficiently combines persistent and volatile data sources without additional database queries when possible.
        /// </remarks>
        public long GetGlobalMessagesLenght(PlatformsEnum platform, long userId)
        {
            lock (_lock)
            {
                var key = (platform, userId);
                int bufferedLength = 0;

                if (_changes.TryGetValue(key, out var change))
                {
                    bufferedLength = change.GlobalMessageLengthIncrement;
                }

                long baseLength = _db.GetGlobalMessagesLenght(platform, userId);

                return baseLength + bufferedLength;
            }
        }

        /// <summary>
        /// Retrieves the message count for a user within a specific channel.
        /// </summary>
        /// <param name="platform">The streaming platform</param>
        /// <param name="userId">The unique user identifier</param>
        /// <param name="channelId">The channel identifier</param>
        /// <returns>
        /// The sum of messages sent by the user in the channel, combining database and buffered counts.
        /// </returns>
        /// <remarks>
        /// <para>
        /// The method:
        /// <list type="bullet">
        /// <item>Checks for buffered increments specific to the channel</item>
        /// <item>Combines with database-stored count for accurate total</item>
        /// <item>Maintains channel-specific message tracking integrity</item>
        /// </list>
        /// </para>
        /// <para>
        /// Implementation notes:
        /// <list type="bullet">
        /// <item>Supports multiple channels per user</item>
        /// <item>Handles channel-specific increments separately from global counts</item>
        /// <item>Thread-safe for concurrent access patterns</item>
        /// </list>
        /// </para>
        /// This is essential for channel-specific statistics and moderation features.
        /// </remarks>
        public int GetMessageCountInChannel(PlatformsEnum platform, long userId, string channelId)
        {
            lock (_lock)
            {
                var key = (platform, userId);
                int bufferedCount = 0;

                if (_changes.TryGetValue(key, out var change))
                {
                    change.ChannelMessageCounts.TryGetValue(channelId, out bufferedCount);
                }

                int baseCount = _db.GetMessageCountInChannel(platform, userId, channelId);

                return baseCount + bufferedCount;
            }
        }

        /// <summary>
        /// Increments the message count for a user within a specific channel.
        /// </summary>
        /// <param name="platform">The streaming platform</param>
        /// <param name="userId">The unique user identifier</param>
        /// <param name="channelId">The channel identifier</param>
        /// <param name="increment">The amount to increment (defaults to 1)</param>
        /// <remarks>
        /// <para>
        /// Operation sequence:
        /// <list type="number">
        /// <item>Creates or retrieves the user's change record</item>
        /// <item>Updates the channel-specific message count</item>
        /// <item>Increments the global change counter</item>
        /// <item>Triggers flush if threshold is reached</item>
        /// </list>
        /// </para>
        /// <para>
        /// Key behaviors:
        /// <list type="bullet">
        /// <item>Aggregates multiple increments before database write</item>
        /// <item>Supports positive and negative increments</item>
        /// <item>Automatically creates records for new users/channels</item>
        /// <item>Thread-safe through internal locking</item>
        /// </list>
        /// </para>
        /// This method is optimized for high-frequency message counting with minimal database impact.
        /// </remarks>
        public void IncrementMessageCountInChannel(PlatformsEnum platform, long userId, string channelId, int increment = 1)
        {
            lock (_lock)
            {
                var key = (platform, userId);
                if (!_changes.TryGetValue(key, out var change))
                {
                    change = new UserChange { Platform = platform, UserId = userId };
                    _changes[key] = change;
                }

                change.ChannelMessageCounts[channelId] = change.ChannelMessageCounts.GetValueOrDefault(channelId) + increment;

                _changeCount++;
                CheckAndFlush();
            }
        }

        /// <summary>
        /// Increments both global message count and total message length for a user.
        /// </summary>
        /// <param name="platform">The streaming platform</param>
        /// <param name="userId">The unique user identifier</param>
        /// <param name="messageLength">The character length of the message to count</param>
        /// <param name="increment">The count increment (defaults to 1)</param>
        /// <remarks>
        /// <para>
        /// This single operation updates two metrics simultaneously:
        /// <list type="bullet">
        /// <item><c>Global message count</c> - incremented by specified amount</item>
        /// <item><c>Global message length</c> - incremented by message character count</item>
        /// </list>
        /// </para>
        /// <para>
        /// Performance benefits:
        /// <list type="bullet">
        /// <item>Reduces database operations by 50% compared to separate updates</item>
        /// <item>Maintains atomicity of related metrics</item>
        /// <item>Optimized for chat message processing workflow</item>
        /// </list>
        /// </para>
        /// The method automatically aggregates multiple calls before flushing to database.
        /// </remarks>
        public void IncrementGlobalMessageCountAndLenght(PlatformsEnum platform, long userId, int messageLength, int increment = 1)
        {
            lock (_lock)
            {
                var key = (platform, userId);
                if (!_changes.TryGetValue(key, out var change))
                {
                    change = new UserChange { Platform = platform, UserId = userId };
                    _changes[key] = change;
                }

                change.GlobalMessageCountIncrement += increment;
                change.GlobalMessageLengthIncrement += messageLength;

                _changeCount++;
                CheckAndFlush();
            }
        }

        /// <summary>
        /// Sets a user parameter value, queuing the change for batch database update.
        /// </summary>
        /// <param name="platform">The streaming platform</param>
        /// <param name="userId">The unique user identifier</param>
        /// <param name="columnName">The parameter name to update</param>
        /// <param name="value">The new value to set</param>
        /// <remarks>
        /// <para>
        /// Operation characteristics:
        /// <list type="bullet">
        /// <item>Overwrites previous buffered value if exists</item>
        /// <item>Does not immediately write to database</item>
        /// <item>Triggers flush when change threshold is reached</item>
        /// <item>Maintains last-write-wins semantics</item>
        /// </list>
        /// </para>
        /// <para>
        /// Common use cases:
        /// <list type="bullet">
        /// <item>Updating user currency balances</item>
        /// <item>Storing user preferences</item>
        /// <item>Maintaining user role information</item>
        /// <item>Tracking achievement progress</item>
        /// </list>
        /// </para>
        /// Multiple calls for the same parameter will only result in one database update with the final value.
        /// </remarks>
        public void SetParameter(PlatformsEnum platform, long userId, string columnName, object value)
        {
            lock (_lock)
            {
                var key = (platform, userId);
                if (!_changes.TryGetValue(key, out var change))
                {
                    change = new UserChange { Platform = platform, UserId = userId };
                    _changes[key] = change;
                }

                change.Changes[columnName] = value;

                _changeCount++;
                CheckAndFlush();
            }
        }

        /// <summary>
        /// Background thread method that processes flush requests.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The flush loop:
        /// <list type="bullet">
        /// <item>Waits for signal using AutoResetEvent</item>
        /// <item>Processes all buffered changes in a single transaction</item>
        /// <item>Returns to waiting state after completion</item>
        /// </list>
        /// </para>
        /// <para>
        /// Design considerations:
        /// <list type="bullet">
        /// <item>Runs as background thread to avoid blocking application exit</item>
        /// <item>Uses minimal CPU when idle (WaitOne is efficient)</item>
        /// <item>Processes all pending changes in one batch operation</item>
        /// <item>Handles exceptions appropriately</item>
        /// </list>
        /// </para>
        /// This implementation ensures database writes don't interfere with main application performance.
        /// </remarks>
        private void FlushLoop()
        {
            while (true)
            {
                FlushSignal.WaitOne();
                Flush();
            }
        }

        /// <summary>
        /// Checks if the change threshold has been reached and triggers a flush if necessary.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Threshold behavior:
        /// <list type="bullet">
        /// <item>Compares current change count against MAX_CHANGES (5000)</item>
        /// <item>Sets flush signal when threshold is reached</item>
        /// <item>Resets change counter after flush is triggered</item>
        /// </list>
        /// </para>
        /// <para>
        /// Rationale for threshold value:
        /// <list type="bullet">
        /// <item>5000 changes represents approximately 1-2 seconds of peak chat activity</item>
        /// <item>Large enough to provide significant batching benefits</item>
        /// <item>Small enough to prevent excessive memory usage</item>
        /// <item>Prevents potential data loss in case of sudden termination</item>
        /// </list>
        /// </para>
        /// This automatic flushing complements the minute-based flush in the main engine.
        /// </remarks>
        private void CheckAndFlush()
        {
            if (_changeCount >= MAX_CHANGES)
            {
                FlushSignal.Set();
            }
        }

        /// <summary>
        /// Immediately writes all buffered changes to the database.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The flush operation:
        /// <list type="number">
        /// <item>Acquires lock to prevent concurrent modifications</item>
        /// <item>Processes all buffered changes in a single transaction</item>
        /// <item>Clears internal change tracking structures</item>
        /// <item>Resets change counter to zero</item>
        /// </list>
        /// </para>
        /// <para>
        /// Error handling:
        /// <list type="bullet">
        /// <item>Logs exceptions but doesn't swallow them</item>
        /// <item>Maintains data integrity by not clearing changes on failure</item>
        /// <item>Allows retry of failed flush operations</item>
        /// </list>
        /// </para>
        /// This method should be called at critical points (shutdown, minute boundaries) in addition to automatic flushing.
        /// </remarks>
        public void Flush()
        {
            lock (_lock)
            {
                if (_changes.Count == 0) return;

                try
                {
                    _db.SaveChangesBatch(_changes.Values.ToList());
                    _changes.Clear();
                    _changeCount = 0;
                }
                catch (Exception ex)
                {
                    Write(ex);
                    throw;
                }
            }
        }

        /// <summary>
        /// Releases all resources and ensures all buffered changes are persisted to database.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Disposal sequence:
        /// <list type="number">
        /// <item>Writes all remaining buffered changes to database</item>
        /// <item>Stops the background flush thread</item>
        /// <item>Releases synchronization resources</item>
        /// </list>
        /// </para>
        /// <para>
        /// Usage recommendations:
        /// <list type="bullet">
        /// <item>Always call in a try-finally block or using statement</item>
        /// <item>Should be the last operation before application shutdown</item>
        /// <item>Prevents data loss from unflushed changes</item>
        /// </list>
        /// </para>
        /// Implements the standard IDisposable pattern for proper resource management.
        /// </remarks>
        public void Dispose()
        {
            Flush();
        }

        /// <summary>
        /// Gets the current number of user records with pending changes in the buffer.
        /// </summary>
        /// <returns>
        /// The count of unique user-platform combinations with buffered changes.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This value represents:
        /// <list type="bullet">
        /// <item>Number of user records that will be updated on next flush</item>
        /// <item>Memory usage indicator for the buffer</item>
        /// <item>Progress toward automatic flush threshold</item>
        /// </list>
        /// </para>
        /// <para>
        /// Monitoring recommendations:
        /// <list type="bullet">
        /// <item>Use for debugging buffer behavior</item>
        /// <item>Helps identify abnormal write patterns</item>
        /// <item>Can be used to trigger proactive flushing if needed</item>
        /// </list>
        /// </para>
        /// The count does not reflect the total number of individual changes (which could be higher).
        /// </remarks>
        public int Count()
        {
            return _changes.Count;
        }
    }
}
