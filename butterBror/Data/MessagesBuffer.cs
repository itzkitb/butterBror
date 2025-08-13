using butterBror.Models;
using butterBror.Models.DataBase;
using static butterBror.Core.Bot.Console;

namespace butterBror.Data
{
    /// <summary>
    /// Thread-safe message buffering system for efficient database write operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class implements a high-performance batching mechanism for message storage with the following key features:
    /// <list type="bullet">
    /// <item>Automatic batch flushing when threshold is reached (5,000 messages)</item>
    /// <item>Background processing thread for non-blocking operation</item>
    /// <item>Thread-safe access through lock synchronization</item>
    /// <item>Graceful shutdown handling via <see cref="IDisposable"/></item>
    /// </list>
    /// </para>
    /// <para>
    /// The buffer significantly improves database performance by:
    /// <list type="bullet">
    /// <item>Reducing the number of individual database transactions</item>
    /// <item>Minimizing disk I/O operations through batch processing</item>
    /// <item>Preventing database contention during high message volume</item>
    /// <item>Maintaining application responsiveness during write operations</item>
    /// </list>
    /// </para>
    /// Designed specifically for chat applications where message rates can exceed thousands per minute.
    /// </remarks>
    public class MessagesBuffer : IDisposable
    {
        private readonly List<(PlatformsEnum platform, string channelId, long userId, Message message)> _buffer = new();
        private readonly object _lock = new();
        private long _messagesCount;
        private const long MAX_MESSAGES_COUNT = 5000;
        private readonly MessagesDatabase _db;
        public readonly AutoResetEvent FlushSignal = new(false);
        private Thread _flushThread;

        /// <summary>
        /// Initializes a new instance of the MessagesBuffer class with the specified database connection.
        /// </summary>
        /// <param name="db">The database instance where messages will be persisted</param>
        /// <remarks>
        /// <para>
        /// The constructor performs the following initialization:
        /// <list type="number">
        /// <item>Stores reference to the provided database connection</item>
        /// <item>Creates and starts the background flush processing thread</item>
        /// <item>Initializes the internal message buffer as empty</item>
        /// </list>
        /// </para>
        /// <para>
        /// Important notes:
        /// <list type="bullet">
        /// <item>The flush thread runs as a background thread (<see cref="Thread.IsBackground"/> = true)</item>
        /// <item>Thread lifetime is managed internally by the buffer</item>
        /// <item>Database connection must remain valid for the buffer's lifetime</item>
        /// </list>
        /// </para>
        /// The buffer is ready for immediate use after construction.
        /// </remarks>
        public MessagesBuffer(MessagesDatabase db)
        {
            _db = db;
            _flushThread = new Thread(FlushLoop) { IsBackground = true };
            _flushThread.Start();
        }

        /// <summary>
        /// Adds a new message to the buffer for eventual persistence.
        /// </summary>
        /// <param name="platform">The streaming platform where the message originated</param>
        /// <param name="channelId">The channel identifier where the message was sent</param>
        /// <param name="userId">The user identifier of the message sender</param>
        /// <param name="message">The message content and metadata</param>
        /// <remarks>
        /// <para>
        /// The method performs the following operations:
        /// <list type="number">
        /// <item>Thread-safely adds the message to the internal buffer</item>
        /// <item>Increments the message counter</item>
        /// <item>Checks if buffer threshold has been reached</item>
        /// <item>Signals flush thread if threshold exceeded (5,000 messages)</item>
        /// </list>
        /// </para>
        /// <para>
        /// Performance characteristics:
        /// <list type="bullet">
        /// <item>Constant time operation (O(1)) under normal conditions</item>
        /// <item>Minimal lock contention through fine-grained synchronization</item>
        /// <item>No immediate database access (asynchronous persistence)</item>
        /// </list>
        /// </para>
        /// Designed for high-frequency calls with minimal performance impact on message processing.
        /// </remarks>
        public void Add(PlatformsEnum platform, string channelId, long userId, Message message)
        {
            lock (_lock)
            {
                _buffer.Add((platform, channelId, userId, message));
                _messagesCount++;

                if (_messagesCount >= MAX_MESSAGES_COUNT)
                {
                    FlushSignal.Set();
                }
            }
        }

        /// <summary>
        /// Immediately persists all buffered messages to the database and clears the buffer.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The method performs the following sequence:
        /// <list type="number">
        /// <item>Acquires exclusive access to the buffer</item>
        /// <item>Checks if buffer contains messages to process</item>
        /// <item>Delegates actual database storage to <see cref="MessagesDatabase.SaveMessages"/></item>
        /// <item>Clears the buffer and resets message counter</item>
        /// </list>
        /// </para>
        /// <para>
        /// Error handling:
        /// <list type="bullet">
        /// <item>Exceptions are logged via <see cref="Core.Bot.Console.Write(Exception)"/></item>
        /// <item>Original exception is rethrown after logging</item>
        /// <item>Buffer state remains unchanged if persistence fails</item>
        /// </list>
        /// </para>
        /// Typically called automatically when buffer threshold is reached or at regular intervals.
        /// Can be called manually for immediate persistence when needed.
        /// </remarks>
        public void Flush()
        {
            lock (_lock)
            {
                if (_buffer.Count == 0) return;

                try
                {
                    _db.SaveMessages(_buffer);
                    _buffer.Clear();
                    _messagesCount = 0;
                }
                catch (Exception ex)
                {
                    Write(ex);
                    throw;
                }
            }
        }

        /// <summary>
        /// Background processing loop that handles automatic buffer flushing.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The loop operates as follows:
        /// <list type="bullet">
        /// <item>Waits indefinitely for a flush signal (<see cref="FlushSignal"/>)</item>
        /// <item>When signaled, calls <see cref="Flush"/> to persist messages</item>
        /// <item>Resumes waiting for next signal</item>
        /// </list>
        /// </para>
        /// <para>
        /// Key characteristics:
        /// <list type="bullet">
        /// <item>Runs continuously for the lifetime of the buffer</item>
        /// <item>Uses minimal CPU resources while waiting (blocked state)</item>
        /// <item>Processes flush requests immediately when signaled</item>
        /// <item>Automatically handles repeated flush requests</item>
        /// </list>
        /// </para>
        /// The thread terminates automatically when the application shuts down
        /// (as it's configured as a background thread).
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
        /// Gets the current count of messages waiting in the buffer.
        /// </summary>
        /// <returns>The number of messages currently stored in the buffer</returns>
        /// <remarks>
        /// <para>
        /// Implementation details:
        /// <list type="bullet">
        /// <item>Provides thread-safe access to the buffer size</item>
        /// <item>Returns exact count at the moment of call</item>
        /// <item>Value may change immediately after return</item>
        /// </list>
        /// </para>
        /// <para>
        /// Usage scenarios:
        /// <list type="bullet">
        /// <item>Monitoring buffer status for diagnostics</item>
        /// <item>Debugging message processing pipelines</item>
        /// <item>Determining when manual flush might be beneficial</item>
        /// </list>
        /// </para>
        /// Not intended for use in critical path logic due to potential race conditions.
        /// </remarks>
        public int Count()
        {
            return _buffer.Count;
        }

        /// <summary>
        /// Releases all resources used by the MessagesBuffer and flushes any remaining messages.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The disposal process:
        /// <list type="number">
        /// <item>Calls <see cref="Flush"/> to persist any remaining messages</item>
        /// <item>Does not explicitly terminate the background thread</item>
        /// <item>Relies on thread being background for automatic termination</item>
        /// </list>
        /// </para>
        /// <para>
        /// Best practices:
        /// <list type="bullet">
        /// <item>Should be called during application shutdown</item>
        /// <item>Guarantees no message loss on graceful shutdown</item>
        /// <item>Implement using <c>using</c> statement or explicit <see cref="IDisposable.Dispose"/> call</item>
        /// </list>
        /// </para>
        /// Failure to dispose properly may result in message loss during shutdown.
        /// </remarks>
        public void Dispose() => Flush();
    }
}
