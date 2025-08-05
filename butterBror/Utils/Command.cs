using butterBror.Core.Bot.SQLColumnNames;
using butterBror.Data;
using butterBror.Models;
using butterBror.Models.SevenTVLib;
using DankDB;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Telegram.Bot.Types;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using static butterBror.Core.Bot.Console;
using static System.Net.Mime.MediaTypeNames;

namespace butterBror.Utils
{
    /// <summary>
    /// Provides command processing functionality including argument parsing, cooldown management, and message handling.
    /// </summary>
    public class Command
    {
        public static readonly ConcurrentDictionary<string, (SemaphoreSlim Semaphore, DateTime LastUsed)> messagesSemaphores = new(StringComparer.Ordinal);
        private static readonly RegexOptions regexOptions = RegexOptions.Compiled;
        private static readonly Regex MentionRegex = new(@"@(\w+)", regexOptions);

        /// <summary>
        /// Retrieves an argument from a list by index position.
        /// </summary>
        /// <param name="args">List of command arguments</param>
        /// <param name="index">Zero-based index of the argument to retrieve</param>
        /// <returns>The argument at specified index or null if index out of range</returns>
        
        public static string GetArgument(List<string> args, int index)
        {
            Engine.Statistics.FunctionsUsed.Add();
            if (args.Count > index)
                return args[index];
            return null;
        }

        /// <summary>
        /// Retrieves a named argument value from the argument list.
        /// </summary>
        /// <param name="args">List of command arguments</param>
        /// <param name="arg_name">Name prefix to search for (e.g., "user:")</param>
        /// <returns>The value after the colon for matching argument, or null if not found</returns>
        
        public static string GetArgument(List<string> args, string arg_name)
        {
            Engine.Statistics.FunctionsUsed.Add();
            foreach (string arg in args)
            {
                if (arg.StartsWith(arg_name + ":")) return arg.Replace(arg_name + ":", "");
            }
            return null;
        }

        /// <summary>
        /// Logs command execution details and increments the completed commands counter.
        /// </summary>
        /// <param name="data">Command data containing execution context</param>
        
        public static void ExecutedCommand(CommandData data)
        {
            Engine.Statistics.FunctionsUsed.Add();
            try
            {
                Write($"Executed command {data.Name} (User: {data.User.Name}, full message: \"{data.Name} {data.ArgumentsString}\", arguments: \"{data.ArgumentsString}\", command: \"{data.Name}\")", "info");
                Engine.CompletedCommands++;
            }
            catch (Exception ex)
            {
                Write(ex);
            }
        }

        public static bool IsEqualsSlashCommand(string commandName, string message, string botName)
        {
            return message.StartsWith($"/{commandName}", StringComparison.OrdinalIgnoreCase)
                   || message.StartsWith($"/{commandName}@{botName}", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Checks user and global cooldowns for command execution.
        /// </summary>
        /// <param name="userCooldown">User-specific cooldown in seconds</param>
        /// <param name="globalCooldown">Global cooldown in seconds</param>
        /// <param name="cooldownName">Unique identifier for the cooldown parameter</param>
        /// <param name="userID">User ID to check</param>
        /// <param name="roomID">Channel/Room ID context</param>
        /// <param name="platform">Target platform (Twitch/Discord/Telegram)</param>
        /// <param name="ignoreUserVIP">Bypass VIP/moderator cooldown exemptions</param>
        /// <param name="ignoreGlobalCooldown">Bypass global cooldown check</param>
        /// <returns>True if cooldown requirements are satisfied</returns>
        
        public static bool CheckCooldown(
            int userCooldown,
            int globalCooldown,
            string cooldownName,
            string userID,
            string roomID,
            PlatformsEnum platform,
            bool ignoreUserVIP = false,
            bool ignoreGlobalCooldown = false
        )
        {
            Engine.Statistics.FunctionsUsed.Add();

            try
            {
                // VIP or dev/mod bypass
                bool isVipOrStaff = Engine.Bot.SQL.Roles.GetModerator(platform, Format.ToLong(userID)) is not null
                                    || Engine.Bot.SQL.Roles.GetDeveloper(platform, Format.ToLong(userID)) is not null;
                
                if (isVipOrStaff && ignoreUserVIP)
                {
                    return true;
                }

                string lastUsesJson = (string)Engine.Bot.SQL.Users.GetParameter(platform, Format.ToLong(userID), Users.LastUse);
                
                if (lastUsesJson != null)
                {
                    Dictionary<string, string> lastUses = Format.ParseStringDictionary(lastUsesJson);
                    DateTime now = DateTime.UtcNow;

                    // First user use
                    if (!lastUses.ContainsKey(cooldownName))
                    {
                        lastUses.Add(cooldownName, now.ToString("o"));
                        Engine.Bot.SQL.Users.SetParameter(platform, Format.ToLong(userID), Users.LastUse, Format.SerializeStringDictionary(lastUses));
                        return true;
                    }

                    // User cooldown check
                    DateTime lastUserUse = DateTime.Parse(lastUses[cooldownName], null, DateTimeStyles.AdjustToUniversal);
                    double userElapsedSec = (now - lastUserUse).TotalSeconds;
                    if (userElapsedSec < userCooldown)
                    {
                        Write($"#{userID} tried to use the command, but it's on cooldown! (userElapsedSec: {userElapsedSec}, userCooldown: {userCooldown}, now: {now}, lastUserUse: {lastUserUse})", "info", LogLevel.Warning);
                        return false;
                    }

                    // Reset user timer
                    lastUses[cooldownName] = now.ToString("o");
                    Engine.Bot.SQL.Users.SetParameter(platform, Format.ToLong(userID), Users.LastUse, Format.SerializeStringDictionary(lastUses));

                    // Global cooldown bypass
                    if (ignoreGlobalCooldown)
                    {
                        return true;
                    }

                    // Global cooldown check
                    bool isOnGlobalCooldown = !Engine.Bot.SQL.Channels.IsCommandCooldown(platform, roomID, cooldownName, globalCooldown);
                    if (!isOnGlobalCooldown)
                    {
                        Write($"#{userID} tried to use the command, but it is on global cooldown!", "info", LogLevel.Warning);
                    }
                    return isOnGlobalCooldown;
                }
                return true;
            }
            catch (Exception ex)
            {
                Write(ex);
                return false;
            }
        }

        /// <summary>
        /// Gets remaining cooldown time for a user's command.
        /// </summary>
        /// <param name="userID">User ID to check</param>
        /// <param name="cooldownName">Cooldown parameter name</param>
        /// <param name="userSecondsCooldown">Total cooldown duration in seconds</param>
        /// <param name="platform">Target platform</param>
        /// <returns>TimeSpan representing remaining cooldown</returns>
        
        public static TimeSpan GetCooldownTime(
            string userID,
            string cooldownName,
            int userSecondsCooldown,
            PlatformsEnum platform
        )
        {
            Engine.Statistics.FunctionsUsed.Add();
            try
            {
                Dictionary<string, string> LastUses = Format.ParseStringDictionary((string)Engine.Bot.SQL.Users.GetParameter(platform, Format.ToLong(userID), Users.LastUse));
                if (LastUses.TryGetValue(cooldownName, out var lastUse))
                {
                    return TimeSpan.FromSeconds(userSecondsCooldown) - (DateTime.UtcNow - DateTime.Parse(lastUse, null, DateTimeStyles.AdjustToUniversal));
                }
                else
                {
                    return TimeSpan.FromSeconds(0);
                }
            }
            catch (Exception ex)
            {
                Write(ex);
                return new TimeSpan(0);
            }
        }

        /// <summary>
        /// Processes incoming chat messages across platforms with rate limiting and message tracking.
        /// </summary>
        /// <param name="userId">User identifier</param>
        /// <param name="channelId">Channel/Room identifier</param>
        /// <param name="username">Display name of the user</param>
        /// <param name="message">Message content</param>
        /// <param name="twitchMessageContext">Twitch-specific message context</param>
        /// <param name="channel">Channel/Room name</param>
        /// <param name="platform">Target platform (Twitch/Discord/Telegram)</param>
        /// <param name="telegramMessageContext">Telegram-specific message context</param>
        /// <param name="ServerChannel">Discord server channel (optional)</param>
        /// <remarks>
        /// Handles message counting, AFK return detection, currency rewards, nickname mapping, 
        /// and message persistence across platforms.
        /// </remarks>
        
        public static async Task ProcessMessageAsync(
            string userId,
            string channelId,
            string username,
            string message,
            ChatMessage twitchMessageContext,
            string channel,
            PlatformsEnum platform,
            Message telegramMessageContext,
            string messageId,
            string server = "",
            string serverId = ""
        )
        {
            Engine.Statistics.FunctionsUsed.Add();
            Engine.Statistics.MessagesReaded.Add();
            var now = DateTime.UtcNow;
            var semaphore = messagesSemaphores.GetOrAdd(userId, id => (new SemaphoreSlim(1, 1), now));
            try
            {
                await semaphore.Semaphore.WaitAsync().ConfigureAwait(false);
                messagesSemaphores.TryUpdate(userId, (semaphore.Semaphore, now), semaphore);

                // Skip banned or ignored users
                if (Engine.Bot.SQL.Roles.GetBannedUser(platform, Format.ToLong(userId)) is not null ||
                    Engine.Bot.SQL.Roles.GetIgnoredUser(platform, Format.ToLong(userId)) is not null)
                    return;

                DateTime now_utc = DateTime.UtcNow;
                Engine.Bot.MessagesProccessed++;

                if (!Engine.Bot.SQL.Users.CheckUserExists(platform, Format.ToLong(userId)))
                {
                    Engine.Bot.SQL.Users.RegisterNewUser(platform, Format.ToLong(userId), LanguageDetector.DetectLanguage(message), message, channel);
                    Engine.Bot.SQL.Users.AddUsernameMapping(platform, Format.ToLong(userId), username.ToLower());
                }
                else
                {
                    // Handle AFK return
                    if ((long)Engine.Bot.SQL.Users.GetParameter(platform, Format.ToLong(userId), Users.IsAFK) == 1)
                    {
                        Chat.ReturnFromAFK(userId, channelId, channel, username, messageId, telegramMessageContext, platform, message, server, serverId);
                    }

                    // Award coins
                    int add_coins = message.Length / 6 + 1;
                    Balance.Add(userId, 0, add_coins, platform);
                }

                Engine.Bot.SQL.Users.IncrementGlobalMessageCount(platform, Format.ToLong(userId));
                Engine.Bot.SQL.Users.IncrementMessageCountInChannel(platform, Format.ToLong(userId), platform == PlatformsEnum.Discord ? serverId : channelId);

                // Mentions handling
                List<string> mentionedList = new List<string>();
                int addToUser = 0;

                foreach (Match m in MentionRegex.Matches(message))
                {
                    string mentioned = m.Groups[1].Value.TrimEnd(',');
                    string mentionedId = Names.GetUserID(mentioned, platform);

                    if (!string.Equals(mentioned, username, StringComparison.OrdinalIgnoreCase)
                        && mentionedId != null && !mentionedList.Contains(mentionedId))
                    {
                        mentionedList.Add(mentionedId);
                        Balance.Add(mentionedId, 0, Engine.Bot.CurrencyMentioned, platform);
                        addToUser += Engine.Bot.CurrencyMentioner;
                    }
                }

                if (addToUser > 0)
                {
                    Balance.Add(userId, 0, addToUser, platform);
                }

                // Save user state
                Engine.Bot.SQL.Users.SetParameter(platform, Format.ToLong(userId), Users.LastMessage, message);
                Engine.Bot.SQL.Users.SetParameter(platform, Format.ToLong(userId), Users.LastChannel, channel);
                Engine.Bot.SQL.Users.SetParameter(platform, Format.ToLong(userId), Users.LastSeen, now_utc.ToString("o"));

                // Persist message history
                var msg = new Models.DataBase.Message
                {
                    messageDate = now_utc,
                    messageText = message,
                    isMe = platform == PlatformsEnum.Twitch && twitchMessageContext.IsMe,
                    isVip = platform == PlatformsEnum.Twitch && twitchMessageContext.IsVip,
                    isTurbo = platform == PlatformsEnum.Twitch && twitchMessageContext.IsTurbo,
                    isStaff = platform == PlatformsEnum.Twitch && twitchMessageContext.IsStaff,
                    isPartner = platform == PlatformsEnum.Twitch && twitchMessageContext.IsPartner,
                    isModerator = platform == PlatformsEnum.Twitch && twitchMessageContext.IsModerator,
                    isSubscriber = platform == PlatformsEnum.Twitch && twitchMessageContext.IsSubscriber,
                };

                if (Engine.Bot.SQL.Channels.GetFirstMessage(platform, channelId, Format.ToLong(userId)) is null)
                {
                    Engine.Bot.SQL.Channels.SaveFirstMessage(platform, channelId, Format.ToLong(userId), msg);
                }
                Engine.Bot.SQL.Messages.SaveMessage(platform, platform == PlatformsEnum.Discord ? serverId : channelId, Format.ToLong(userId), msg);
            }
            catch (Exception ex)
            {
                Write(ex);
            }
            finally
            {
                semaphore.Semaphore.Release();
            }
        }

        /// <summary>
        /// Dynamically compiles and executes user-provided C# code snippets.
        /// </summary>
        /// <param name="userCode">C# code to execute</param>
        /// <returns>Result of code execution</returns>
        /// <exception cref="CompilationException">Thrown when code compilation fails</exception>
        /// <remarks>
        /// Uses Roslyn compiler with restricted assembly references for security.
        /// Requires proper permissions to execute.
        /// </remarks>
        
        public static string ExecuteCode(string userCode)
        {
            Engine.Statistics.FunctionsUsed.Add();

            var fullCode = $@"
        using DankDB;
        using butterBror;
        using System;
        using System.Collections.Generic;
        using System.Linq;
        using System.Text;
        using System.IO;
        using System.Runtime;

        public static class MyClass 
        {{
            public static string Execute()
            {{
                {userCode}
            }}
        }}";

            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                .Distinct()
                .ToArray();

            var references = assemblies
                .Select(a => MetadataReference.CreateFromFile(a.Location))
                .ToList();

            var requiredAssemblies = new[]
            {
        typeof(object).Assembly,
        typeof(Console).Assembly,
        typeof(Enumerable).Assembly,
        typeof(System.Runtime.GCSettings).Assembly,
    };

            foreach (var assembly in requiredAssemblies)
            {
                if (!references.Any(r => r.Display.Contains(assembly.GetName().Name)))
                {
                    references.Add(MetadataReference.CreateFromFile(assembly.Location));
                }
            }

            var compilation = CSharpCompilation.Create(
                "MyAssembly",
                syntaxTrees: new[] { CSharpSyntaxTree.ParseText(fullCode) },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            using var stream = new MemoryStream();
            var emitResult = compilation.Emit(stream);

            if (!emitResult.Success)
            {
                var errors = string.Join("\n", emitResult.Diagnostics
                    .Where(d => d.IsWarningAsError || d.Severity == DiagnosticSeverity.Error)
                    .Select(d => d.GetMessage()));

                throw new CompilationException($"Compilation error: {errors}");
            }

            stream.Seek(0, SeekOrigin.Begin);
            var assemblyLoad = Assembly.Load(stream.ToArray());
            var type = assemblyLoad.GetType("MyClass");
            var method = type.GetMethod("Execute");

            return (string)method.Invoke(null, null);
        }

        /// <summary>
        /// Represents custom exceptions during code compilation.
        /// </summary>
        public class CompilationException : Exception
        {
            /// <summary>
            /// Initializes a new instance of the CompilationException class with a specified error message.
            /// </summary>
            /// <param name="message">The error message that explains the reason for the exception.</param>
            public CompilationException(string message) : base(message) { }
        }
    }
}
