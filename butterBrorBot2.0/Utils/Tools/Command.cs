using butterBror.Utils.DataManagers;
using DankDB;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Telegram.Bot.Types;
using TwitchLib.Client.Events;
using static butterBror.Utils.Things.Console;

namespace butterBror.Utils.Tools
{
    public class Command
    {
        [ConsoleSector("butterBror.Utils.Tools.Command", "GetArgument#1")]
        public static string GetArgument(List<string> args, int index)
        {
            Core.Statistics.FunctionsUsed.Add();
            if (args.Count > index)
                return args[index];
            return null;
        }

        [ConsoleSector("butterBror.Utils.Tools.Command", "GetArgument#2")]
        public static string GetArgument(List<string> args, string arg_name)
        {
            Core.Statistics.FunctionsUsed.Add();
            foreach (string arg in args)
            {
                if (arg.StartsWith(arg_name + ":")) return arg.Replace(arg_name + ":", "");
            }
            return null;
        }

        [ConsoleSector("butterBror.Utils.Tools.Command", "ExecutedCommand")]
        public static void ExecutedCommand(CommandData data)
        {
            Core.Statistics.FunctionsUsed.Add();
            try
            {
                Write($"Executed command {data.name} (User: {data.user.username}, full message: \"{data.name} {data.arguments_string}\", arguments: \"{data.arguments_string}\", command: \"{data.name}\")", "info");
                Core.CompletedCommands++;
            }
            catch (Exception ex)
            {
                Write(ex);
            }
        }

        [ConsoleSector("butterBror.Utils.Tools.Command", "CheckCooldown")]
        public static bool CheckCooldown(
            int userSecondsCooldown,
            int globalCooldown,
            string cooldownParamName,
            string userID,
            string roomID,
            Platforms platform,
            bool resetUseTimeIfCommandIsNotReseted = true,
            bool ignoreUserVIP = false,
            bool ignoreGlobalCooldown = false
        )
        {
            Core.Statistics.FunctionsUsed.Add();

            try
            {
                // VIP or dev/mod bypass
                bool isVipOrStaff = UsersData.Get<bool>(userID, "isBotModerator", platform)
                                    || UsersData.Get<bool>(userID, "isBotDev", platform);

                if (isVipOrStaff && !ignoreUserVIP)
                {
                    return true;
                }

                string userKey = $"LU_{cooldownParamName}";
                string channelPath = Path.Combine(Core.Bot.Pathes.Channels, Platform.strings[(int)platform], roomID);
                string cddFile = Path.Combine(channelPath, "CDD.json");

                DateTime now = DateTime.UtcNow;

                // First user use
                if (!UsersData.Contains(userID, userKey, platform))
                {
                    UsersData.Save(userID, userKey, now, platform);
                    return true;
                }

                // User cooldown check
                DateTime lastUserUse = UsersData.Get<DateTime>(userID, userKey, platform);
                double userElapsedSec = (now - lastUserUse).TotalSeconds;
                if (userElapsedSec < userSecondsCooldown)
                {
                    if (resetUseTimeIfCommandIsNotReseted)
                        UsersData.Save(userID, userKey, now, platform);

                    var name = Names.GetUsername(userID, platform);
                    Write($"User {name} tried to use the command, but it's on cooldown!", "info", LogLevel.Warning);
                    return false;
                }

                // Reset user timer
                UsersData.Save(userID, userKey, now, platform);

                // Global cooldown bypass
                if (ignoreGlobalCooldown)
                {
                    return true;
                }

                // Ensure channel cooldowns file exists
                if (!FileUtil.FileExists(cddFile))
                {
                    Directory.CreateDirectory(channelPath);
                    SafeManager.Save(cddFile, userKey, DateTime.MinValue);
                }

                // Global cooldown check
                DateTime lastGlobalUse = Manager.Get<DateTime>(cddFile, userKey);
                double globalElapsedSec = (now - lastGlobalUse).TotalSeconds;

                if (lastGlobalUse == default || globalElapsedSec >= globalCooldown)
                {
                    SafeManager.Save(cddFile, userKey, now);
                    return true;
                }
                else
                {
                    var name = Names.GetUsername(userID, platform);
                    Write($"User {name} tried to use the command, but it is on global cooldown!", "info", LogLevel.Warning);
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.Write(ex);
                return false;
            }
        }


        [ConsoleSector("butterBror.Utils.Tools.Command", "GetCooldownTime")]
        public static TimeSpan GetCooldownTime(
            string userID,
            string cooldownParamName,
            int userSecondsCooldown,
            Platforms platform
        )
        {
            Core.Statistics.FunctionsUsed.Add();
            try
            {
                return TimeSpan.FromSeconds(userSecondsCooldown) - (DateTime.UtcNow - UsersData.Get<DateTime>(userID, $"LU_{cooldownParamName}", platform));
            }
            catch (Exception ex)
            {
                Write(ex);
                return new TimeSpan(0);
            }
        }

        public static readonly ConcurrentDictionary<string, (SemaphoreSlim Semaphore, DateTime LastUsed)> messages_semaphores = new(StringComparer.Ordinal);

        [ConsoleSector("butterBror.Utils.Tools.Command", "ProcessMessageAsync")]
        public static async Task ProcessMessageAsync(
            string UserID,
            string RoomId,
            string Username,
            string Message,
            OnMessageReceivedArgs Twitch,
            string Room,
            Platforms Platform,
            Message Telegram,
            string ServerChannel = ""
        )
        {
            Core.Statistics.FunctionsUsed.Add();
            Core.Statistics.MessagesReaded.Add();
            var now = DateTime.UtcNow;
            var semaphore = messages_semaphores.GetOrAdd(UserID, id => (new SemaphoreSlim(1, 1), now));
            try
            {
                await semaphore.Semaphore.WaitAsync().ConfigureAwait(false);
                messages_semaphores.TryUpdate(UserID, (semaphore.Semaphore, now), semaphore);
                // Skip banned or ignored users
                if (UsersData.Get<bool>(UserID, "isBanned", Platform) ||
                    UsersData.Get<bool>(UserID, "isIgnored", Platform))
                    return;

                // Prepare paths and counters
                string platform_key = butterBror.Platform.strings[(int)Platform];
                string channel_base = Path.Combine(Core.Bot.Pathes.Channels, platform_key, RoomId);
                string count_dir = Path.Combine(channel_base, "MS");
                string user_count_file = Path.Combine(count_dir, UserID + ".txt");
                int messages_count = 0;
                DateTime now_utc = DateTime.UtcNow;

                string nick2id = Path.Combine(Core.Bot.Pathes.Nick2ID, platform_key, Username + ".txt");
                string id2nick = Path.Combine(Core.Bot.Pathes.ID2Nick, platform_key, UserID + ".txt");

                // Ensure directories exist
                FileUtil.CreateDirectory(channel_base);
                FileUtil.CreateDirectory(Path.Combine(channel_base, "MSGS"));
                FileUtil.CreateDirectory(count_dir);

                // Count and increment
                if (FileUtil.FileExists(user_count_file))
                    messages_count = Format.ToInt(FileUtil.GetFileContent(user_count_file)) + 1;
                Core.Bot.MessagesProccessed++;

                bool isNewUser = !FileUtil.FileExists(
                    Path.Combine(Core.Bot.Pathes.Users, platform_key, UserID + ".json")
                );

                // Build message prefix
                var prefix = new StringBuilder();
                if (isNewUser)
                {
                    UsersData.Register(UserID, Message, Platform);
                    prefix.Append(Platform == Platforms.Discord
                        ? $"{Room} | {ServerChannel} · {Username}: "
                        : $"{Room} · {Username}: ");
                }
                else
                {
                    // Handle AFK return
                    if ((Platform == Platforms.Twitch || Platform == Platforms.Telegram) &&
                        UsersData.Get<bool>(UserID, "isAfk", Platform))
                    {
                        if (Platform == Platforms.Twitch)
                            Chat.ReturnFromAFK(UserID, RoomId, Room, Username, Twitch.ChatMessage.Id, null, Platform);
                        else
                            Chat.ReturnFromAFK(UserID, RoomId, Room, Username, "", Telegram, Platform);
                    }

                    // Award coins
                    int add_coins = Message.Length / 6 + 1;
                    Balance.Add(UserID, 0, add_coins, Platform);
                    int floatBal = Balance.GetSubbalance(UserID, Platform);
                    int bal = Balance.GetBalance(UserID, Platform);

                    prefix.Append(Platform == Platforms.Discord
                        ? $"{Room} | {ServerChannel} · {Username} ({messages_count}/{bal}.{floatBal} {Core.Bot.CoinSymbol}): "
                        : $"{Room} | {Username} ({messages_count}/{bal}.{floatBal} {Core.Bot.CoinSymbol}): ");
                }

                // Currency init for new users
                if (!UsersData.Get<bool>(UserID, "isReadedCurrency", Platform))
                {
                    UsersData.Save(UserID, "isReadedCurrency", true, Platform);
                    Core.Coins += (float)(UsersData.Get<int>(UserID, "balance", Platform)
                                   + UsersData.Get<int>(UserID, "floatBalance", Platform) / 100.0);
                    Core.Users++;
                    prefix.Append("(Added to currency) ");
                }

                // Append actual message
                prefix.Append(Message);
                string outputMessage = prefix.ToString();

                // Additional processing
                new CAFUS().Maintrance(UserID, Username, Platform);

                // Mentions handling
                foreach (Match m in Regex.Matches(Message, @"@(\w+)"))
                {
                    var mentioned = m.Groups[1].Value.TrimEnd(',');
                    var mentionedId = Names.GetUserID(mentioned, Platform);
                    if (!string.Equals(mentioned, Username, StringComparison.OrdinalIgnoreCase)
                        && mentionedId != null)
                    {
                        Balance.Add(mentionedId, 0, Core.Bot.CurrencyMentioned, Platform);
                        Balance.Add(UserID, 0, Core.Bot.CurrencyMentioner, Platform);
                        prefix.Append($" ({mentioned} +{Core.Bot.CurrencyMentioned}) " +
                                      $"({Username} +{Core.Bot.CurrencyMentioner})");
                    }
                }

                // Save user state
                UsersData.Save(UserID, "lastSeenMessage", Message, Platform);
                UsersData.Save(UserID, "lastSeen", now_utc, Platform);
                try
                {
                    UsersData.Save(
                        UserID,
                        "totalMessages",
                        UsersData.Get<int>(UserID, "totalMessages", Platform) + 1,
                        Platform
                    );
                }
                catch (Exception ex)
                {
                    Console.Write(ex);
                }

                // Persist message history
                var msg = new MessagesWorker.Message
                {
                    messageDate = now_utc,
                    messageText = Message,
                    isMe = Platform == Platforms.Twitch && Twitch.ChatMessage.IsMe,
                    isModerator = Platform == Platforms.Twitch && Twitch.ChatMessage.IsModerator,
                    isPartner = Platform == Platforms.Twitch && Twitch.ChatMessage.IsPartner,
                    isStaff = Platform == Platforms.Twitch && Twitch.ChatMessage.IsStaff,
                    isSubscriber = Platform == Platforms.Twitch && Twitch.ChatMessage.IsSubscriber,
                    isTurbo = Platform == Platforms.Twitch && Twitch.ChatMessage.IsTurbo,
                    isVip = Platform == Platforms.Twitch && Twitch.ChatMessage.IsVip
                };
                MessagesWorker.SaveMessage(RoomId, UserID, msg, Platform);

                // Nickname mappings
                if (!FileUtil.FileExists(nick2id) || !FileUtil.FileExists(id2nick))
                {
                    FileUtil.SaveFileContent(nick2id, UserID);
                    FileUtil.SaveFileContent(id2nick, Username);
                }

                UsersData.Save(UserID, "lastSeenChannel", Room, Platform);
                FileUtil.SaveFileContent(user_count_file, messages_count.ToString());

                // Final console output
                var logTag = Platform switch
                {
                    Platforms.Twitch => "tw_chat",
                    Platforms.Discord => "ds_chat",
                    Platforms.Telegram => "tg_chat",
                    _ => "chat"
                };
                Write(outputMessage, logTag);
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

        [ConsoleSector("butterBror.Utils.Tools.Command", "ExecuteCode")]
        public static string ExecuteCode(string userCode)
        {
            Core.Statistics.FunctionsUsed.Add();

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

        public class CompilationException : Exception
        {
            public CompilationException(string message) : base(message) { }
        }
    }
}
