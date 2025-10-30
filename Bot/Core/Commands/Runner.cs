using bb.Core.Bot;
using bb.Core.Configuration;
using bb.Models.Command;
using bb.Models.Platform;
using bb.Models.Users;
using bb.Utils;
using Discord;
using feels.Dank.Cache.LRU;
using Microsoft.CodeAnalysis;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using Telegram.Bot;
using TwitchLib.Client.Enums;
using static bb.Core.Bot.Logger;

namespace bb.Core.Commands
{
    /// <summary>
    /// Manages command execution lifecycle including initialization, permission checks, and result processing.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item>Implements thread-safe command execution using user-specific semaphores</item>
    /// <item>Handles command discovery through assembly scanning</item>
    /// <item>Enforces permission levels and cooldown mechanisms</item>
    /// <item>Processes command results with proper error handling</item>
    /// </list>
    /// Uses a lazy initialization pattern for command discovery to optimize startup performance.
    /// Implements parallel processing for command matching while maintaining user-level execution serialization.
    /// </remarks>
    public class Runner
    {
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _userLocks =
            new ConcurrentDictionary<string, SemaphoreSlim>();

        public readonly ConcurrentBag<ICommand> commandInstances =
            new ConcurrentBag<ICommand>();

        private bool _commandsInitialized;
        private readonly object _initLock = new object();
        private static readonly LruCache<string, bool> _adminCache = new LruCache<string, bool>(
            capacity: 1000,
            defaultTtl: TimeSpan.FromMinutes(1),
            expirationMode: ExpirationMode.Sliding,
            cleanupInterval: TimeSpan.FromMinutes(2)
        );

        /// <summary>
        /// Initializes command instances through assembly scanning.
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        /// <item>Uses double-checked locking pattern for thread safety</item>
        /// <item>Discovers all ICommand implementations in current assembly</item>
        /// <item>Creates instances of concrete command types via reflection</item>
        /// <item>Skips abstract classes and interfaces</item>
        /// <item>Processes command types in parallel for performance</item>
        /// </list>
        /// Initialization occurs only once during first command execution (lazy initialization).
        /// Uses Assembly.GetExecutingAssembly() to find commands in current module.
        /// </remarks>
        private void InitializeCommands()
        {
            if (_commandsInitialized) return;

            lock (_initLock)
            {
                if (_commandsInitialized) return;

                var assembly = Assembly.GetExecutingAssembly();
                var commandTypes = assembly.GetTypes()
                    .Where(t => typeof(ICommand).IsAssignableFrom(t) &&
                                !t.IsAbstract &&
                                !t.IsInterface)
                    .ToList();

                Parallel.ForEach(commandTypes, type =>
                {
                    if (Activator.CreateInstance(type) is ICommand instance)
                    {
                        commandInstances.Add(instance);
                    }
                });

                _commandsInitialized = true;
            }
        }

        /// <summary>
        /// Executes a command with full permission validation and result processing.
        /// </summary>
        /// <param name="data">Command execution context containing:
        /// <list type="table">
        /// <item><term>Platform</term><description>Target platform (Twitch/Discord/Telegram)</description></item>
        /// <item><term>User</term><description>Executing user information</description></item>
        /// <item><term>Channel</term><description>Target channel details</description></item>
        /// <item><term>Name</term><description>Command name/alias</description></item>
        /// <item><term>TwitchArguments</term><description>Twitch-specific message context</description></item>
        /// </list>
        /// </param>
        /// <param name="test">Indicates if execution is for testing purposes (skips cooldown tracking and some validation)</param>
        /// <returns>Asynchronous task representing command execution</returns>
        /// <remarks>
        /// <list type="bullet">
        /// <item>Performs comprehensive permission checks:
        /// <list type="bullet">
        /// <item>Ban/ignore status verification</item>
        /// <item>Bot developer/moderator authorization</item>
        /// <item>Channel moderator verification (Twitch-specific)</item>
        /// <item>Command cooldown enforcement</item>
        /// </list>
        /// </item>
        /// <item>Executes commands with user-level serialization via semaphores</item>
        /// <item>Handles both synchronous and asynchronous command implementations</item>
        /// <item>Processes command results with automatic reply generation</item>
        /// <item>Provides detailed error logging and user-facing error messages</item>
        /// <item>Measures and logs command execution time</item>
        /// </list>
        /// For technical work commands (TechWorks=true), returns predefined maintenance message.
        /// Automatically converts 'ё' to 'е' in command names for linguistic normalization.
        /// Command execution is canceled if user fails permission checks or cooldowns apply.
        /// </remarks>
        /// <exception cref="Exception">Thrown when command execution fails with detailed error context</exception>
        public async Task Execute(CommandData data, bool test = false)
        {
            if (!bb.Program.BotInstance.Initialized) return;

            await Task.Run(async () =>
            {
                InitializeCommands();
                var start = Stopwatch.StartNew();

                bool blockedWordDetected = false;
                double blockedWordExecutionTime = 0;
                string blockedWordSector = "";
                string blockedWordWord = "";

                try
                {
                    // User data initialization
                    data.User.Roles = (Roles)DataConversion.ToInt(bb.Program.BotInstance.UsersBuffer.GetParameter(data.Platform, DataConversion.ToLong(data.User.Id), Users.Role));

                    if (data.User.Roles == Roles.Public)
                    {
                        if (data.Platform == Models.Platform.Platform.Twitch)
                        {
                            if (data.TwitchMessage.ChatMessage.IsBroadcaster || data.TwitchMessage.ChatMessage.IsModerator)
                            {
                                data.User.Roles = Roles.ChatMod;
                            }
                        }
                        else if (data.Platform == Models.Platform.Platform.Telegram)
                        {
                            if (data.TelegramMessage.Chat.Id.ToString() == data.User.Id)
                            {
                                data.User.Roles = Roles.ChatMod;
                            }
                            else if (data.TelegramMessage.Chat.Type == Telegram.Bot.Types.Enums.ChatType.Group ||
                                    data.TelegramMessage.Chat.Type == Telegram.Bot.Types.Enums.ChatType.Supergroup)
                            {
                                var cacheKey = $"telegram:{data.TelegramMessage.Chat.Id}:{data.TelegramMessage.From.Id}";

                                try
                                {
                                    bool isAdmin = await _adminCache.GetOrAddAsync(cacheKey, async (key, ct) =>
                                    {
                                        var chatMember = await Program.BotInstance.Clients.Telegram.GetChatMember(
                                            data.TelegramMessage.Chat.Id, data.TelegramMessage.From.Id);

                                        return chatMember.Status == Telegram.Bot.Types.Enums.ChatMemberStatus.Administrator ||
                                               chatMember.Status == Telegram.Bot.Types.Enums.ChatMemberStatus.Creator;
                                    }, TimeSpan.FromSeconds(5));

                                    if (isAdmin)
                                    {
                                        data.User.Roles = Roles.ChatMod;
                                    }
                                }
                                catch (TimeoutException)
                                {
                                    Logger.Write($"Timeout checking Telegram admin status for user {data.User.Id}");
                                }
                                catch (Exception ex)
                                {
                                    Logger.Write($"Error checking Telegram admin status: {ex.Message}");
                                }
                            }
                        }
                        else if (data.Platform == Models.Platform.Platform.Discord)
                        {
                            var discordUser = data.DiscordCommandBase != null ? data.DiscordCommandBase.User as IGuildUser : data.DiscordMessage.Author as IGuildUser;

                            if (discordUser != null)
                            {
                                var cacheKey = $"discord:{discordUser.Guild.Id}:{discordUser.Id}";

                                try
                                {
                                    bool isAdmin = _adminCache.GetOrAdd(cacheKey, key =>
                                    {
                                        return discordUser.RoleIds.Any(roleId =>
                                        {
                                            var role = discordUser.Guild.GetRole(roleId);
                                            return role?.Name?.Equals("Bot admin", StringComparison.OrdinalIgnoreCase) == true;
                                        });
                                    }, TimeSpan.FromSeconds(2));

                                    if (isAdmin)
                                    {
                                        data.User.Roles = Roles.ChatMod;
                                    }
                                }
                                catch (TimeoutException)
                                {
                                    Logger.Write($"Timeout checking Discord admin status for user {data.User.Id}");
                                }
                                catch (Exception ex)
                                {
                                    Logger.Write($"Error checking Discord admin status: {ex.Message}");
                                }
                            }
                        }
                    }

                    if (data.User.Roles <= Roles.Bot ||
                        (data.Platform is Models.Platform.Platform.Twitch && data.TwitchMessage.ChatMessage.IsMe))
                        return;

                    Language language = (Language)DataConversion.ToInt(bb.Program.BotInstance.UsersBuffer.GetParameter(data.Platform, DataConversion.ToLong(data.User.Id), Users.Language));
                    data.User.Language = language;

                    string commandName = data.Name.Replace("ё", "е");
                    bool commandFounded = false;
                    CommandReturn? result = null;

                    await Parallel.ForEachAsync(commandInstances, async (cmd, ct) =>
                    {
                        if (!cmd.Aliases.Contains(commandName, StringComparer.OrdinalIgnoreCase) || commandFounded)
                            return;

                        commandFounded = true;

                        // Get user-specific lock
                        var userLock = _userLocks.GetOrAdd(data.User.Id,
                            _ => new SemaphoreSlim(1, 1));

                        await userLock.WaitAsync(ct);
                        try
                        {
                            bool isOnlyBotOwner = cmd.OnlyBotDeveloper && !(bool)(data.User.Roles == Roles.BotOwner);
                            bool isOnlyBotMod = cmd.OnlyBotModerator && !(data.User.Roles >= Roles.BotMod);
                            bool isOnlyChannelMod = data.Platform == Models.Platform.Platform.Twitch && cmd.OnlyChannelModerator && !(data.User.Roles >= Roles.ChatMod);
                            bool isCooldown = !bb.Program.BotInstance.Cooldown.CheckCooldown(cmd.CooldownPerUser, cmd.CooldownPerChannel, cmd.Name, data.User.Id, data.ChannelId, data.Platform, true);

                            // Permission and cooldown checks
                            if (isOnlyBotOwner || isOnlyBotMod || isOnlyChannelMod || isCooldown)
                            {
                                if (!isCooldown)
                                {
                                    string message = LocalizationService.GetString(data.User.Language, "error:not_enough_rights", data.ChannelId, data.Platform);
                                    bb.Program.BotInstance.MessageSender.Send(data.Platform, message, data.Channel, data.ChannelId, data.User.Language, data.User.Name,
                                        data.User.Id, data.Server, data.ServerID, data.MessageID, data.TelegramMessage, true, true, false);
                                }

                                Write($"Command failed:\n - OBD check: {BoolToString(isOnlyBotOwner)}\n - OBM check: {BoolToString(isOnlyBotMod)}\n - OCM check: {BoolToString(isOnlyChannelMod)}\n - Cooldown check: {BoolToString(isCooldown)}", LogLevel.Warning);
                                return;
                            }

                            // Execute command asynchronously
                            if (cmd.TechWorks)
                            {
                                CommandReturn commandReturn = new();
                                commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "text:command_tech_works", data.ChannelId, data.Platform));
                                result = commandReturn;
                            }
                            else
                            {
                                if (cmd.IsAsync)
                                {
                                    result = await cmd.ExecuteAsync(data);
                                }
                                else
                                {
                                    result = await Task.Run(() => cmd.Execute(data));
                                }
                            }

                            // Process result
                            if (result is not null)
                            {
                                if (result.Exception is not null && result.IsError)
                                    throw new Exception($"Error in command: {cmd.Name}\n#MESSAGE\n{result.Exception.Message}\n#STACK\n{result.Exception.StackTrace}", result.Exception);

                                if (!test)
                                {
                                    (bool, double, string, string) bwddata = bb.Program.BotInstance.MessageFilter.Check(result.Message, data.ChatID, data.Platform, false);

                                    blockedWordDetected = !bwddata.Item1;
                                    blockedWordExecutionTime = bwddata.Item2;
                                    blockedWordSector = bwddata.Item3;
                                    blockedWordWord = bwddata.Item4;

                                    if (result.IsSafe || bwddata.Item1)
                                    {
                                        bb.Program.BotInstance.MessageSender.Send(data.Platform, result.Message, data.Channel, data.ChannelId, data.User.Language, data.User.Name,
                                        data.User.Id, data.Server, data.ServerID, data.MessageID, data.TelegramMessage, true, true, false);
                                    }
                                    else
                                    {
                                        bb.Program.BotInstance.MessageSender.Send(data.Platform, LocalizationService.GetString(data.User.Language, "error:message_could_not_be_sent", data.ChatID, Models.Platform.Platform.Twitch), data.Channel,
                            data.ChannelId, data.User.Language, data.User.Name, data.User.Id, data.Server, data.ServerID, data.MessageID, data.TelegramMessage, true, true, false);
                                    }
                                    
                                }
                            }
                            else
                            {
                                Write("Result is null", LogLevel.Warning);
                            }
                        }
                        finally
                        {
                            userLock.Release();
                        }
                    });
                }
                catch (Exception ex)
                {
                    Write(ex);
                    if (!test)
                    {
                        bb.Program.BotInstance.MessageSender.Send(data.Platform, LocalizationService.GetString(Language.EnUs, "error:unknown", data.ChannelId, data.Platform), data.Channel,
                            data.ChannelId, data.User.Language, data.User.Name, data.User.Id, data.Server, data.ServerID, data.MessageID, data.TelegramMessage, true, true, false);
                    }
                }
                finally
                {
                    start.Stop();
                    if (!test)
                    {
                        Write($"🚀 Executed command \"{data.Name}\":" +
$"\n- User: {data.Platform.ToString()}/{data.User.Id}" +
$"\n- Role: {data.User.Roles} ({(int)data.User.Roles})" +
$"\n- Arguments: \"{data.ArgumentsString}\"" +
$"\n- Location: \"{data.Channel}/{data.ChannelId}\" (ChatID: {data.ChatID})" +
$"\n- Balance: {data.User.Balance}" +
$"\n- Completed in: {start.ElapsedMilliseconds}ms" +
$"\n- Blocked words detected: {BoolToString(blockedWordDetected)} in {blockedWordExecutionTime}ms" +
$"\n- Blocked word: {blockedWordSector}/\"{blockedWordWord}\"");
                        bb.Program.BotInstance.CompletedCommands++;
                    }
                }
            });
        }

        /// <summary>
        /// Converts a boolean value to a visual checkmark or cross emoji for user interface representation.
        /// </summary>
        /// <param name="value">Boolean value where true returns ✅ and false returns ❎</param>
        /// <returns>Emoji string representing the boolean state (✅ for true, ❎ for false)</returns>
        /// <remarks>
        /// <list type="bullet">
        /// <item>Provides visual feedback for boolean states using standardized emoji symbols</item>
        /// <item>Ensures consistent representation of true/false states across user interfaces</item>
        /// <item>Used for status indicators in command responses and UI elements</item>
        /// </list>
        /// </remarks>
        private static string BoolToString(bool value)
        {
            return value ? "✅" : "❎";
        }
    }
}