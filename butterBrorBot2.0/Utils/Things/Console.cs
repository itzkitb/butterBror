using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace butterBror.Utils.Things
{
    /* All channels:
     * error - Channel for receiving errors
     * info - Channel for receiving information and events
     * kernel - Events from the bot core
     * chat - Messages from various chats
     */

    public class Console
    {
        public delegate void ConsoleHandler(LineInfo line);
        public static event ConsoleHandler OnChatLine;

        public delegate void ErrorHandler(LineInfo line);
        public static event ErrorHandler ErrorOccured;

        public static void Write(string message, string channel, LogLevel type)
        {
            string sector = GetCallingMethodSector();
            string logEntry = $"[{DateTime.UtcNow.ToString("dd-MM-yyyy HH:mm:ss.fff").PadRight(11)}] ({sector}/{type}): {message}";

            try
            {
                var directory = Path.GetDirectoryName(Core.Bot.Pathes.Logs);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using (var writer = new StreamWriter(Core.Bot.Pathes.Logs, true))
                {
                    writer.WriteLine(logEntry);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to write log to file: \n{ex.Message}\n{ex.StackTrace}");
            }

            OnChatLine(new LineInfo()
            {
                Message = message,
                Channel = channel,
                DateTime = DateTime.Now,
                Level = type == LogLevel.Error ? "ERROR" : type == LogLevel.Warning ? "WARNING" : "INFO"
            });
        }

        public static void Write(string message, string channel)
        {
            string sector = GetCallingMethodSector();
            string logEntry = $"[{DateTime.UtcNow.ToString("dd-MM-yyyy HH:mm:ss.fff").PadRight(11)}] ({sector}/{LogLevel.Info}): {message}";

            try
            {
                var directory = Path.GetDirectoryName(Core.Bot.Pathes.Logs);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using (var writer = new StreamWriter(Core.Bot.Pathes.Logs, true))
                {
                    writer.WriteLine(logEntry);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to write log to file: \n{ex.Message}\n{ex.StackTrace}");
            }

            OnChatLine(new LineInfo()
            {
                Message = message,
                Channel = channel,
                DateTime = DateTime.Now,
                Level = "INFO"
            });
        }

        public static void Write(Exception exception)
        {
            string sector = GetCallingMethodSector();
            string text = $"Error occured:\nMessage: {exception.Message}\nSource: {exception.Source}\nStack: {exception.StackTrace}\nTarget: {exception.TargetSite.Name}";

            string logEntry = $"[{DateTime.UtcNow.ToString("dd-MM-yyyy HH:mm:ss.fff").PadRight(11)}] ({sector}/{LogLevel.Error}): {text}";

            try
            {
                var directory = Path.GetDirectoryName(Core.Bot.Pathes.Logs);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using (var writer = new StreamWriter(Core.Bot.Pathes.Logs, true))
                {
                    writer.WriteLine(logEntry);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to write log to file: \n{ex.Message}\n{ex.StackTrace}");
            }

            ErrorOccured(new LineInfo()
            {
                Message = logEntry,
                Channel = "errors",
                DateTime = DateTime.Now,
                Level = "ERROR"
            });
        }

        public enum LogLevel
        {
            Info,
            Warning,
            Error
        }

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

        private static string GetCallingMethodSector()
        {
            var stack = new StackTrace();
            var frames = stack.GetFrames();

            foreach (var frame in frames)
            {
                var method = frame.GetMethod();

                if (method.DeclaringType?.FullName.StartsWith("System") == true ||
                    method.Name.Contains("lambda") ||
                    method.Name.Contains("Invoke"))
                {
                    continue;
                }

                if (method.Name == "MoveNext" && method.DeclaringType != null)
                {
                    var attributes = method.DeclaringType.GetCustomAttributes(false);
                    if (attributes.Any(attr => attr is AsyncStateMachineAttribute ||
                                              attr is IteratorStateMachineAttribute))
                    {
                        continue;
                    }
                }

                var attribute = Attribute.GetCustomAttribute(method, typeof(ConsoleSectorAttribute))
                                  as ConsoleSectorAttribute;
                if (attribute != null)
                {
                    return $"{attribute.Class}.{attribute.Name}";
                }

                var type = method.DeclaringType;
                if (type != null)
                {
                    var classAttribute = Attribute.GetCustomAttribute(type, typeof(ConsoleSectorAttribute))
                                      as ConsoleSectorAttribute;
                    if (classAttribute != null)
                    {
                        return $"{classAttribute.Class}.{classAttribute.Name}";
                    }
                }
            }

            return "Unknown";
        }

        public class LineInfo
        {
            public string Message { get; set; }
            public string Level { get; set; }
            public string Channel { get; set; }
            public DateTime DateTime { get; set; }
        }
    }
}
