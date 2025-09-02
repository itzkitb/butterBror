using butterBror.Core.Bot.SQLColumnNames;
using butterBror.Models;
using butterBror.Utils;
using Microsoft.CodeAnalysis;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using TwitchLib.Client.Enums;
using static butterBror.Core.Bot.Console;

namespace butterBror.Core.Commands
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
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _userLocks =
            new ConcurrentDictionary<string, SemaphoreSlim>();

        public static readonly ConcurrentBag<ICommand> commandInstances =
            new ConcurrentBag<ICommand>();

        private static bool _commandsInitialized;
        private static readonly object _initLock = new object();

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
        private static void InitializeCommands()
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
        /// <param name="isATest">Indicates if execution is for testing purposes (skips cooldown tracking and some validation)</param>
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
        public static async Task Run(CommandData data, bool isATest = false)
        {
            if (!butterBror.Bot.Initialized) return;

            await Task.Run(async () =>
            {
                InitializeCommands();
                var start = Stopwatch.StartNew();

                try
                {
                    // User data initialization
                    data.User.IsBanned = butterBror.Bot.DataBase.Roles.IsBanned(data.Platform, DataConversion.ToLong(data.User.ID));
                    data.User.Ignored = butterBror.Bot.DataBase.Roles.IsIgnored(data.Platform, DataConversion.ToLong(data.User.ID));

                    if ((bool)data.User.IsBanned || (bool)data.User.Ignored ||
                        (data.Platform is PlatformsEnum.Twitch && data.TwitchArguments.Command.ChatMessage.IsMe))
                        return;

                    string language = (string)butterBror.Bot.UsersBuffer.GetParameter(data.Platform, DataConversion.ToLong(data.User.ID), Users.Language);
                    data.User.Language = language;
                    data.User.IsBotModerator = butterBror.Bot.DataBase.Roles.IsModerator(data.Platform, DataConversion.ToLong(data.User.ID));
                    data.User.IsBotDeveloper = butterBror.Bot.DataBase.Roles.IsDeveloper(data.Platform, DataConversion.ToLong(data.User.ID));

                    string commandName = data.Name.Replace("ё", "е");
                    bool commandFounded = false;
                    CommandReturn? result = null;

                    await Parallel.ForEachAsync(commandInstances, async (cmd, ct) =>
                    {
                        if (!cmd.Aliases.Contains(commandName, StringComparer.OrdinalIgnoreCase))
                            return;

                        commandFounded = true;

                        // Get user-specific lock
                        var userLock = _userLocks.GetOrAdd(data.User.ID,
                            _ => new SemaphoreSlim(1, 1));

                        await userLock.WaitAsync(ct);
                        try
                        {
                            bool isOnlyBotDeveloper = cmd.OnlyBotDeveloper && !(bool)data.User.IsBotDeveloper;
                            bool isOnlyBotModerator = cmd.OnlyBotModerator && !((bool)data.User.IsBotModerator || (bool)data.User.IsBotDeveloper);
                            bool isOnlyChannelModerator = data.Platform == PlatformsEnum.Twitch && cmd.OnlyChannelModerator && !((bool)data.User.IsModerator || (bool)data.User.IsBotModerator || (bool)data.User.IsBotDeveloper);
                            bool cooldown = !CooldownManager.CheckCooldown(cmd.CooldownPerUser, cmd.CooldownPerChannel, cmd.Name, data.User.ID, data.ChannelId, data.Platform, true);

                            // Permission and cooldown checks
                            if (isOnlyBotDeveloper || isOnlyBotModerator || isOnlyChannelModerator || cooldown)
                            {
                                if (!cooldown)
                                {
                                    PlatformMessageSender.SendReply(data.Platform, data.Channel, data.ChannelId,
                                        LocalizationService.GetString(data.User.Language, "error:not_enough_rights", data.ChannelId, data.Platform),
                                        data.User.Language, data.User.Name, data.User.ID, data.Server,
                                        data.ServerID, data.MessageID, data.TelegramMessage,
                                        true, ChatColorPresets.Red);
                                }

                                Write($"Command failed: isOnlyBotDeveloper: {isOnlyBotDeveloper}; isOnlyBotModerator: {isOnlyBotModerator}; isOnlyChannelModerator: {isOnlyChannelModerator}; cooldown: {cooldown};", "info", LogLevel.Warning);
                                return;
                            }

                            if (!isATest) MessageProcessor.ExecutedCommand(data);

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

                                if (!isATest)
                                {
                                    PlatformMessageSender.SendReply(data.Platform, data.Channel, data.ChannelId,
                                        result.Message, data.User.Language,
                                        data.User.Name, data.User.ID, data.Server,
                                        data.ServerID, data.MessageID, data.TelegramMessage,
                                        result.IsSafe, result.BotNameColor);
                                }
                            }
                            else
                            {
                                Write("Result is null", "info", LogLevel.Warning);
                            }
                        }
                        finally
                        {
                            userLock.Release();
                        }
                    });

                    if (!commandFounded)
                    {
                        Write($"@{data.Name} tried unknown command: {commandName}", "info", LogLevel.Warning);
                    }
                }
                catch (Exception ex)
                {
                    Write(ex);
                    if (!isATest)
                    {
                        PlatformMessageSender.SendReply(data.Platform, data.Channel, data.ChannelId,
                            LocalizationService.GetString("en-US", "error:unknown", data.ChannelId, data.Platform),
                            data.User.Language, data.User.Name, data.User.ID, data.Server,
                            data.ServerID, data.MessageID, data.TelegramMessage,
                            true, ChatColorPresets.Red);
                    }
                }
                finally
                {
                    start.Stop();
                    Write($"Command completed in {start.ElapsedMilliseconds}ms", "info");
                }
            });
        }
    }
}