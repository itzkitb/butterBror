using Pastel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace butterBror.Core.Bot
{
    /* All channels:
     * error - Channel for receiving errors
     * info - Channel for receiving information and events
     * kernel - Events from the bot core
     * chat - Messages from various chats
     */

    /// <summary>
    /// Provides logging functionality with support for multiple log levels and event notifications.
    /// </summary>
    public class Console
    {
        private static readonly object _fileLock = new object();
        private static bool _directoryChecked = false;

        /// <summary>
        /// Writes a log message with specified level to the log file and raises the OnChatLine event.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="channel">The channel associated with the log message.</param>
        /// <param name="type">The log level (Info/Warning/Error).</param>
        public static void Write(
            string message,
            string channel,
            LogLevel type = LogLevel.Info,
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0,
            [CallerMemberName] string memberName = "")
        {
            string logEntry = FormatLogEntry(filePath, lineNumber, memberName, type, message);

            try
            {
                EnsureDirectoryExists();
                WriteToFile(logEntry);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to write log to file: {ex.Message}\n{ex.StackTrace}");
            }

            System.Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.FF").PadRight(11).Pastel("#666666")} [ {channel.Pastel("#ff7b42")} ] {message.Pastel("#bababa")}");
        }

        /// <summary>
        /// Writes an exception to the log file and raises the ErrorOccured event.
        /// </summary>
        /// <param name="exception">The exception to log.</param>
        public static void Write(
            Exception exception,
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0,
            [CallerMemberName] string memberName = "")
        {
            string text = FormatException(exception);
            string logEntry = FormatLogEntry(filePath, lineNumber, memberName, LogLevel.Error, text);

            try
            {
                EnsureDirectoryExists();
                WriteToFile(logEntry);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to write log to file: {ex.Message}\n{ex.StackTrace}");
            }

            System.Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.FF").PadRight(11).Pastel("#666666")} [ {"errors".Pastel("#ff4f4f")} ] {logEntry.Pastel("#bababa")}");
        }

        /// <summary>
        /// Formats a log entry with timestamp, sector, log level, and message.
        /// </summary>
        /// <param name="sector">The source sector/class from attributes.</param>
        /// <param name="level">The log severity level.</param>
        /// <param name="message">The message to log.</param>
        /// <returns>A formatted log string.</returns>
        private static string FormatLogEntry(string filePath, int lineNumber, string memberName, LogLevel level, string message)
        {
            string timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff") + "Z";

            string levelAbbr = level switch
            {
                LogLevel.Info => "INF",
                LogLevel.Warning => "WRN",
                LogLevel.Error => "ERR",
                _ => new string(level.ToString().ToUpperInvariant().Take(3).ToArray())
            };

            string fileName = Path.GetFileName(filePath) ?? "Unknown";

            return $"[{timestamp}] {levelAbbr} [{fileName}:{lineNumber}({memberName})] {message}";
        }

        /// <summary>
        /// Converts an exception into a detailed error string.
        /// </summary>
        /// <param name="exception">The exception to format.</param>
        /// <returns>A string containing exception details.</returns>
        private static string FormatException(Exception ex)
        {
            return $"Error: {ex.Message}\nSource: {ex.Source}\nStack: {ex.StackTrace}\nTarget: {ex.TargetSite?.Name ?? "N/A"}";
        }

        /// <summary>
        /// Ensures the log directory exists (once per session).
        /// </summary>
        private static void EnsureDirectoryExists()
        {
            string logDirectory = Path.GetDirectoryName(Engine.Bot.Pathes.Logs);

            if (!_directoryChecked && !Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
                _directoryChecked = true;
            }
        }

        /// <summary>
        /// Thread-safe file writer for log entries.
        /// </summary>
        /// <param name="logEntry">The formatted log entry to write.</param>
        private static void WriteToFile(string logEntry)
        {
            lock (_fileLock) // Thread-safe writing
            {
                using var writer = new StreamWriter(Engine.Bot.Pathes.Logs, true);
                writer.WriteLine(logEntry);
            }
        }

        public enum LogLevel
        {
            Info,
            Warning,
            Error
        }
    }
}
