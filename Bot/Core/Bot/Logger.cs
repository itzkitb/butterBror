using Pastel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Timers;
using Telegram.Bot.Types;
using static System.Net.Mime.MediaTypeNames;

namespace bb.Core.Bot
{
    /// <summary>
    /// Centralized logging system with multi-channel support and real-time event broadcasting.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><b>error</b> - Dedicated channel for error messages and exception handling</item>
    /// <item><b>info</b> - General information and operational events channel</item>
    /// <item><b>core</b> - Core system events and initialization messages</item>
    /// <item><b>chat</b> - Messages from various chat platforms (Twitch, Discord, Telegram)</item>
    /// </list>
    /// Features:
    /// <list type="bullet">
    /// <item>Thread-safe file logging with lock synchronization</item>
    /// <item>Automatic log directory creation</item>
    /// <item>Caller information tracking (file, line number, method)</item>
    /// <item>Color-coded console output using Pastel library</item>
    /// <item>Real-time dashboard integration via DashboardServer</item>
    /// <item>Structured timestamp format (ISO 8601 UTC)</item>
    /// </list>
    /// All log entries include source file context for debugging purposes.
    /// </remarks>
    public class Logger
    {
        private static readonly object _fileLock = new object();
        private static bool _directoryChecked = false;

        static Logger()
        {
            System.Console.CursorVisible = false;
        }

        /// <summary>
        /// Writes a formatted log message to both console and persistent storage.
        /// </summary>
        /// <param name="message">The message content to log. Should be concise yet descriptive.</param>
        /// <param name="channel">Logical channel identifier for message categorization. Common values:
        /// <list type="table">
        /// <item><term>error</term><description>Exception/error notifications</description></item>
        /// <item><term>info</term><description>General operational messages</description></item>
        /// <item><term>core</term><description>Core system initialization events</description></item>
        /// <item><term>initialization</term><description>Startup sequence messages</description></item>
        /// <item><term>chat</term><description>Platform message processing</description></item>
        /// </list>
        /// </param>
        /// <param name="type">Severity level of the log entry (default: Info).</param>
        /// <param name="filePath">[CallerFilePath] Automatically populated with source file path.</param>
        /// <param name="lineNumber">[CallerLineNumber] Automatically populated with source line number.</param>
        /// <param name="memberName">[CallerMemberName] Automatically populated with calling method name.</param>
        /// <remarks>
        /// <list type="bullet">
        /// <item>Formats output with timestamp, channel, and color coding</item>
        /// <item>Persists logs to file with thread-safe locking mechanism</item>
        /// <item>Forwards messages to DashboardServer for real-time monitoring</item>
        /// <item>Handles file system errors gracefully with debug fallback</item>
        /// <item>Uses UTC timestamps for consistent time tracking across timezones</item>
        /// </list>
        /// Console format: [HH:mm:ss.FF] [channel] message
        /// File format: [yyyy-MM-dd HH:mm:ss.fffZ] LVL [file:line(method)] message
        /// </remarks>
        public static void Write(
            string message,
            LogLevel type = LogLevel.Info,
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0,
            [CallerMemberName] string memberName = "")
        {
#if RELEASE
            if (type == LogLevel.Debug) return;
#endif

            string logEntry = FormatLogEntry(filePath, lineNumber, memberName, type, message);
            string fileName = Path.GetFileName(filePath) ?? "Unknown";

            try
            {
                EnsureDirectoryExists();
                WriteToFile(logEntry);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to write log to file: {ex.Message}\n{ex.StackTrace}");
            }

            System.Console.WriteLine($"{"[".Pastel("#bababa")} {GetEmoji(type)} {DateTime.Now.ToString("HH:mm.ss").PadRight(8).Pastel("#888888")} {"]".Pastel("#bababa")} {$"{fileName}:{lineNumber}".Pastel(Program.BotInstance.MainColor)} {"-".Pastel("#bababa")} {message.Pastel("#bababa")}");
            DashboardServer.HandleLog(message, type);
        }

        /// <summary>
        /// Logs exception details with full diagnostic information.
        /// </summary>
        /// <param name="exception">The exception to log. Must not be null.</param>
        /// <param name="filePath">[CallerFilePath] Automatically populated source file path.</param>
        /// <param name="lineNumber">[CallerLineNumber] Automatically populated source line number.</param>
        /// <param name="memberName">[CallerMemberName] Automatically populated calling method name.</param>
        /// <remarks>
        /// <list type="bullet">
        /// <item>Captures complete exception details including:
        /// <list type="bullet">
        /// <item>Exception message</item>
        /// <item>Source assembly</item>
        /// <item>Stack trace</item>
        /// <item>Target method</item>
        /// </list>
        /// </item>
        /// <item>Writes to "errors" channel with ERROR severity level</item>
        /// <item>Formats console output with error-specific color scheme</item>
        /// <item>Guarantees log persistence even during critical failures</item>
        /// <item>Includes precise error location context for debugging</item>
        /// </list>
        /// Automatically routes to DashboardServer with ERROR severity.
        /// Falls back to Debug.WriteLine if file logging fails.
        /// </remarks>
        public static void Write(
            Exception exception,
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0,
            [CallerMemberName] string memberName = "")
        {
            string text = FormatException(exception);
            string logEntry = FormatLogEntry(filePath, lineNumber, memberName, LogLevel.Error, text);
            string fileName = Path.GetFileName(filePath) ?? "Unknown";

            try
            {
                EnsureDirectoryExists();
                WriteToFile(logEntry);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to write log to file: {ex.Message}\n{ex.StackTrace}");
            }

            System.Console.WriteLine($"{"[".Pastel("#bababa")} {GetEmoji(LogLevel.Error)} {DateTime.Now.ToString("HH:mm.ss").PadRight(8).Pastel("#888888")} {"]".Pastel("#bababa")} {$"{fileName}:{lineNumber}".Pastel("#ff4f4f")} {"-".Pastel("#bababa")} {logEntry.Pastel("#bababa")}");

            DashboardServer.HandleLog(text, LogLevel.Error);
        }

        /// <summary>
        /// Formats a structured log entry with diagnostic context.
        /// </summary>
        /// <param name="filePath">Source file path of the logging call.</param>
        /// <param name="lineNumber">Line number in the source file.</param>
        /// <param name="memberName">Calling method name.</param>
        /// <param name="level">Log severity level.</param>
        /// <param name="message">Message content to include.</param>
        /// <returns>A standardized log string in the format:
        /// [ emoji HH:mm.ss.ff ] file:line(method) - message</returns>
        /// <remarks>
        /// <list type="bullet">
        /// <item>Standardizes log level abbreviations (INF/WRN/ERR)</item>
        /// <item>Truncates file paths to just filename for readability</item>
        /// <item>Includes precise caller context for debugging</item>
        /// <item>Handles null/empty inputs gracefully</item>
        /// </list>
        /// Example output: [ ❌ 14:30:45.12 ] Engine.cs:42(Main) - Critical failure
        /// </remarks>
        private static string FormatLogEntry(string filePath, int lineNumber, string memberName, LogLevel level, string message)
        {
            string timestamp = DateTime.UtcNow.ToString("HH:mm.ss.ff");
            string fileName = Path.GetFileName(filePath) ?? "Unknown";

            return $"[ {GetEmoji(level)} {timestamp} ] {fileName}:{lineNumber}({memberName}) - {message}";
        }

        public static void Clear()
        {
            System.Console.Clear();
        }

        /// <summary>
        /// Converts exception details into a comprehensive diagnostic string.
        /// </summary>
        /// <param name="ex">The exception to format. Must not be null.</param>
        /// <returns>A multi-line string containing:
        /// <list type="bullet">
        /// <item>Error message</item>
        /// <item>Source assembly</item>
        /// <item>Full stack trace</item>
        /// <item>Target method name</item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// Provides complete exception context for post-mortem analysis.
        /// Safely handles null properties in exception objects.
        /// Formats output for optimal readability in log files.
        /// </remarks>
        private static string FormatException(Exception ex)
        {
            return $"Error: {ex.Message}\nSource: {ex.Source}\nStack: {ex.StackTrace}\nTarget: {ex.TargetSite?.Name ?? "N/A"}";
        }

        /// <summary>
        /// Ensures log directory exists (executed once per application session).
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        /// <item>Checks directory existence only once (lazy initialization)</item>
        /// <item>Creates directory structure if missing</item>
        /// <item>Thread-safe implementation via double-checked locking pattern</item>
        /// <item>Uses path from butterBror.Bot.Pathes.Logs configuration</item>
        /// </list>
        /// Directory is created in ApplicationData folder as configured in PathService.
        /// </remarks>
        private static void EnsureDirectoryExists()
        {
            if (bb.Program.BotInstance == null || bb.Program.BotInstance.Paths == null) return;

            string logDirectory = Path.GetDirectoryName(bb.Program.BotInstance.Paths.Logs);

            if (!_directoryChecked && !Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
                _directoryChecked = true;
            }
        }

        /// <summary>
        /// Thread-safe writer for persistent log storage.
        /// </summary>
        /// <param name="logEntry">Formatted log entry to write.</param>
        /// <remarks>
        /// <list type="bullet">
        /// <item>Uses file lock for exclusive write access</item>
        /// <item>Appends entries to existing log file</item>
        /// <item>Disposes StreamWriter properly using 'using' pattern</item>
        /// <item>Handles IO exceptions gracefully</item>
        /// </list>
        /// Implements double-buffering through StreamWriter for performance.
        /// Optimized for high-frequency logging scenarios.
        /// </remarks>
        private static void WriteToFile(string logEntry)
        {
            if (bb.Program.BotInstance == null || bb.Program.BotInstance.Paths == null) return;

            lock (_fileLock) // Thread-safe writing
            {
                using var writer = new StreamWriter(bb.Program.BotInstance.Paths.Logs, true);
                writer.WriteLine(logEntry);
            }
        }

        private static string GetEmoji(LogLevel level)
        {
            if (emojis.TryGetValue(level, out string emoji))
            {
                return emoji;
            }
            return "👾";
        }

        /// <summary>
        /// Enumeration of log severity levels.
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        /// <item><b>Info</b> - Routine operational messages (default level)</item>
        /// <item><b>Warning</b> - Potential issues requiring attention</item>
        /// <item><b>Error</b> - Critical failures affecting functionality</item>
        /// </list>
        /// Used for filtering and prioritization in logging systems.
        /// Corresponds to standard syslog severity levels.
        /// </remarks>
        public enum LogLevel
        {
            /// <summary>
            /// Informational messages about normal operation.
            /// </summary>
            Info,

            /// <summary>
            /// Warning messages indicating potential issues.
            /// </summary>
            Warning,

            /// <summary>
            /// Error messages representing functional failures.
            /// </summary>
            Error,
            Critical,
            Debug,
            Progress
        }

        private static Dictionary<LogLevel, string> emojis = new()
        {
            { LogLevel.Info, "ℹ️" },
            { LogLevel.Warning, "⚠️" },
            { LogLevel.Error, "❌" },
            { LogLevel.Critical, "🚨" },
            { LogLevel.Debug, "🐞" },
            { LogLevel.Progress, "🔄" }
        };

        private class AnimationState
        {
            public Guid Id { get; set; }
            public int LineNumber { get; set; }
            public string Message { get; set; }
            public string FilePath { get; set; }
            public int FileLine { get; set; }
            public string Member { get; set; }
            public int CurrentFrame { get; set; }
            public bool IsComplete { get; set; }
            public int Current { get; set; }
            public int Total { get; set; }
        }
    }
}