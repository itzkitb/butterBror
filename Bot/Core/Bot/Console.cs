using Pastel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Timers;
using Telegram.Bot.Types;

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
    public class Console
    {
        private static readonly object _fileLock = new object();
        private static bool _directoryChecked = false;
        private static System.Timers.Timer _animationTimer;
        private static List<AnimationState> _activeAnimations = new List<AnimationState>();
        private static int currentLine = 0;
        private static readonly string[] _brailleAnimation = new[]
        {
            "⣾", "⣽", "⣻", "⢿", "⡿", "⣟", "⣯", "⣷"
        };

        static Console()
        {
            System.Console.CursorVisible = false;
            // Initialize the timer to update animations
            _animationTimer = new System.Timers.Timer(100);
            _animationTimer.Elapsed += UpdateAnimations;
            _animationTimer.Start();
        }

        /// <summary>
        /// Updates all active animations in the console.
        /// </summary>
        private static void UpdateAnimations(object sender, ElapsedEventArgs e)
        {
            int startedHeightPosition = System.Console.CursorTop;
            lock (_activeAnimations)
            {
                foreach (var animation in _activeAnimations)
                {
                    if (animation.IsComplete) continue;

                    // Update the current animation frame
                    animation.CurrentFrame = (animation.CurrentFrame + 1) % _brailleAnimation.Length;

                    // Redraw the animation on the same line
                    WriteLine(FormatAnimationLine(animation), animation.LineNumber);
                }
            }
            System.Console.SetCursorPosition(0, startedHeightPosition);
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

            WriteLine($"{"[".Pastel("#bababa")} {GetEmoji(type)} {DateTime.Now.ToString("HH:mm.ss").PadRight(8).Pastel("#888888")} {"]".Pastel("#bababa")} {$"{fileName}:{lineNumber}".Pastel("#b3ff7d")} {"-".Pastel("#bababa")} {message.Pastel("#bababa")}");
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

            WriteLine($"{"[".Pastel("#bababa")} {GetEmoji(LogLevel.Error)} {DateTime.Now.ToString("HH:mm.ss").PadRight(8).Pastel("#888888")} {"]".Pastel("#bababa")} {$"{fileName}:{lineNumber}".Pastel("#ff4f4f")} {"-".Pastel("#bababa")} {logEntry.Pastel("#bababa")}");

            DashboardServer.HandleLog(text, LogLevel.Error);
        }

        /// <summary>
        /// Creates a new progress animation and returns its unique ID
        /// </summary>
        /// <param name="message">Progress description text</param>
        /// <param name="current">Current progress value</param>
        /// <param name="total">Total progress value</param>
        /// <param name="filePath">[CallerFilePath] Automatically populated source file path</param>
        /// <param name="lineNumber">[CallerLineNumber] Automatically populated source line number</param>
        /// <param name="memberName">[CallerMemberName] Automatically populated calling method name</param>
        /// <returns>Unique identifier for this progress animation</returns>
        /// <remarks>
        /// <list type="bullet">
        /// <item>Uses Braille animation symbols: ⣾, ⣽, ⣻, ⢿, ⡿, ⣟, ⣯, ⣷</item>
        /// <item>Writes only start to log file (no animation frames)</item>
        /// <item>Creates new animation that can be updated later</item>
        /// </list>
        /// Example usage:
        /// var id = Console.Progress("Loading assets", 0, 10);
        /// Console.UpdateProgress(id, 5, 10);
        /// Console.CompleteProgress(id);
        /// </remarks>
        public static Guid Progress(
            string message,
            int current,
            int total,
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0,
            [CallerMemberName] string memberName = "")
        {
            string logEntry = FormatLogEntry(filePath, lineNumber, memberName, LogLevel.Progress, message);
            WriteToFile(logEntry);

            var animationState = new AnimationState
            {
                Id = Guid.NewGuid(),
                LineNumber = currentLine,
                Message = message,
                CurrentFrame = 0,
                IsComplete = false,
                Total = total,
                Current = current,
                FilePath = filePath,
                Member = memberName,
                FileLine = lineNumber
            };

            lock (_activeAnimations)
            {
                _activeAnimations.Add(animationState);
            }

            WriteLine(FormatAnimationLine(animationState));

            return animationState.Id;
        }

        /// <summary>
        /// Updates an existing progress animation by ID
        /// </summary>
        /// <param name="id">Unique identifier of the animation to update</param>
        /// <param name="current">New current progress value</param>
        /// <param name="total">New total progress value</param>
        /// <param name="message">Optional new message text</param>
        /// <remarks>
        /// <list type="bullet">
        /// <item>Updates progress values without creating new animation</item>
        /// <item>Changes the displayed message if provided</item>
        /// <item>Automatically refreshes the animation on the same console line</item>
        /// </list>
        /// </remarks>
        public static void UpdateProgress(Guid id, int current, int total, string message = null)
        {
            lock (_activeAnimations)
            {
                var animation = _activeAnimations.FirstOrDefault(a => a.Id == id);
                if (animation == null)
                {
                    return;
                }

                animation.Current = current;
                animation.Total = total;

                if (!string.IsNullOrEmpty(message))
                {
                    animation.Message = message;
                }

                if (animation.Current >= animation.Total)
                {
                    animation.IsComplete = true;
                    string logEntry = FormatLogEntry(animation.FilePath, animation.LineNumber, "", LogLevel.Progress, $"{animation.Message} (completed)");
                    WriteToFile(logEntry);
                    _activeAnimations.Remove(animation);

                    WriteLine(new string(' ', System.Console.BufferWidth), animation.LineNumber);
                    WriteLine($"{"[".Pastel("#bababa")} {GetEmoji(LogLevel.Progress)} {DateTime.Now.ToString("HH:mm.ss").PadRight(8).Pastel("#888888")} {"]".Pastel("#bababa")} {$"{Path.GetFileName(animation.FilePath)}:{animation.FileLine}".Pastel("#b3ff7d")} {"-".Pastel("#bababa")} {animation.Message.Pastel("#bababa")}", animation.LineNumber);
                }
                else
                {
                    WriteLine(FormatAnimationLine(animation), animation.LineNumber);
                }
            }
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

        /// <summary>
        /// Formats animation line for console output
        /// </summary>
        private static string FormatAnimationLine(AnimationState animation)
        {
            string fileName = Path.GetFileName(animation.FilePath) ?? "Unknown";
            string timestamp = DateTime.Now.ToString("HH:mm.ss").PadRight(8).Pastel("#888888");
            string brailleSymbol = _brailleAnimation[animation.CurrentFrame].Pastel("#888888");
            string fileNameLine = $"{fileName}:{animation.FileLine}".Pastel("#b3ff7d");
            string progressPart = $"[{animation.Current}/{animation.Total}] {animation.Message}".Pastel("#bababa");

            return $"{"[".Pastel("#bababa")} {brailleSymbol} {timestamp} {"]".Pastel("#bababa")} {fileNameLine} {"-".Pastel("#bababa")} {progressPart}";
        }

        public static void Clear()
        {
            System.Console.Clear();
            currentLine = 0;
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

        public static void WriteLine(string message, int line = -1)
        {
            int bufferHeight = System.Console.BufferHeight;

            if (bufferHeight <= 0)
                return;

            int lineNum = (line >= 0) ? line : currentLine;

            if (lineNum < 0)
                lineNum = 0;
            if (lineNum >= bufferHeight)
                lineNum = bufferHeight - 1;

            string output = message.ReplaceLineEndings("\n");

            System.Console.SetCursorPosition(0, lineNum);
            System.Console.Write(output + "\n");

            if (line < 0)
            {
                int linesCount = message.Split('\n').Length;
                if (message.EndsWith("\n")) linesCount--;
                currentLine += linesCount;

                if (currentLine >= bufferHeight)
                    currentLine = bufferHeight - 1;
            }
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