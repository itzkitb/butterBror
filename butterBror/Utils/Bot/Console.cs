using Pastel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace butterBror.Utils.Bot
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
        public static void Write(string message, string channel, LogLevel type = LogLevel.Info)
        {
            string sector = GetCallingMethodSector();
            string logEntry = FormatLogEntry(sector, type, message);

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
        public static void Write(Exception exception)
        {
            string sector = GetCallingMethodSector();
            string text = FormatException(exception);
            string logEntry = FormatLogEntry(sector, LogLevel.Error, text);

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
        private static string FormatLogEntry(string sector, LogLevel level, string message)
        {
            return $"[{DateTime.UtcNow:dd-MM-yyyy HH:mm:ss.fff}] ({sector}/{level}): {message}";
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

        /// <summary>
        /// Attribute used to specify sector information for logging purposes.
        /// </summary>
        [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = true)]
        public class ConsoleSectorAttribute : Attribute
        {
            public string Class { get; }
            public string Name { get; }

            public ConsoleSectorAttribute(string @class, string name)
            {
                Class = @class;
                Name = name;
            }
        }

        /// <summary>
        /// Gets the calling method's sector information from attributes.
        /// </summary>
        /// <returns>A string representing the sector information.</returns>
        private static string GetCallingMethodSector()
        {
            var stack = new StackTrace();
            foreach (var frame in stack.GetFrames() ?? Array.Empty<StackFrame>())
            {
                var method = frame.GetMethod();
                if (method?.DeclaringType?.FullName.StartsWith("System") == true ||
                    method.Name.Contains("lambda") ||
                    method.Name.Contains("Invoke"))
                    continue;

                if (method.Name == "MoveNext" &&
                    method.DeclaringType?.GetCustomAttributes(false).Any(attr =>
                        attr is AsyncStateMachineAttribute or IteratorStateMachineAttribute) == true)
                    continue;

                var attribute = Attribute.GetCustomAttribute(method, typeof(ConsoleSectorAttribute))
                    as ConsoleSectorAttribute;

                if (attribute != null)
                    return $"{attribute.Class}.{attribute.Name}";

                var classAttribute = Attribute.GetCustomAttribute(method.DeclaringType, typeof(ConsoleSectorAttribute))
                    as ConsoleSectorAttribute;

                if (classAttribute != null)
                    return $"{classAttribute.Class}.{classAttribute.Name}";
            }

            return "Unknown";
        }


        public enum LogLevel
        {
            Info,
            Warning,
            Error
        }

        /// <summary>
        /// Represents log line information for event handlers.
        /// </summary>
        public class LineInfo
        {
            public string Message { get; set; }
            public string Level { get; set; }
            public string Channel { get; set; }
            public DateTime DateTime { get; set; }
        }
    }
}
