using butterBror.Core.Bot.SQLColumnNames;
using butterBror.Data;
using butterBror.Models;
using butterBror.Utils;
using Microsoft.CodeAnalysis;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TwitchLib.Client.Enums;
using static butterBror.Core.Bot.Console;

namespace butterBror.Core.Commands
{
    public class Runner
    {
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _userLocks =
            new ConcurrentDictionary<string, SemaphoreSlim>();

        public static readonly ConcurrentBag<ICommand> commandInstances =
            new ConcurrentBag<ICommand>();

        private static bool _commandsInitialized;
        private static readonly object _initLock = new object();

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

        
        public static async Task Run(CommandData data, bool isATest = false)
        {
            await Task.Run(async () =>
            {
                InitializeCommands();
                Engine.Statistics.FunctionsUsed.Add();
                var start = Stopwatch.StartNew();

                try
                {
                    // User data initialization
                    data.User.IsBanned = Engine.Bot.SQL.Roles.GetBannedUser(data.Platform, Format.ToLong(data.User.ID)) is not null;
                    data.User.Ignored = Engine.Bot.SQL.Roles.GetIgnoredUser(data.Platform, Format.ToLong(data.User.ID)) is not null;

                    if ((bool)data.User.IsBanned || (bool)data.User.Ignored ||
                        (data.Platform is PlatformsEnum.Twitch && data.TwitchArguments.Command.ChatMessage.IsMe))
                        return;

                    string language = (string)Engine.Bot.SQL.Users.GetParameter(data.Platform, Format.ToLong(data.User.ID), Users.Language);
                    data.User.Language = language;
                    data.User.IsBotModerator = Engine.Bot.SQL.Roles.GetModerator(data.Platform, Format.ToLong(data.User.ID)) is not null;
                    data.User.IsBotDeveloper = Engine.Bot.SQL.Roles.GetDeveloper(data.Platform, Format.ToLong(data.User.ID)) is not null;

                    string command = Text.FilterCommand(data.Name).Replace("ё", "е");
                    bool commandFounded = false;
                    CommandReturn? result = null;

                    await Parallel.ForEachAsync(commandInstances, async (cmd, ct) =>
                    {
                        if (!cmd.Aliases.Contains(command, StringComparer.OrdinalIgnoreCase))
                            return;

                        commandFounded = true;

                        // Get user-specific lock
                        var userLock = _userLocks.GetOrAdd(data.UserID,
                            _ => new SemaphoreSlim(1, 1));

                        await userLock.WaitAsync(ct);
                        try
                        {
                            bool isOnlyBotDeveloper = cmd.OnlyBotDeveloper && !(bool)data.User.IsBotDeveloper;
                            bool isOnlyBotModerator = cmd.OnlyBotModerator && !((bool)data.User.IsBotModerator || (bool)data.User.IsBotDeveloper);
                            bool isOnlyChannelModerator = data.Platform == PlatformsEnum.Twitch && cmd.OnlyChannelModerator && !((bool)data.User.IsModerator || (bool)data.User.IsBotModerator || (bool)data.User.IsBotDeveloper);
                            bool cooldown = !Command.CheckCooldown(cmd.CooldownPerUser, cmd.CooldownPerChannel, cmd.Name, data.User.ID, data.ChannelId, data.Platform, true);

                            // Permission and cooldown checks
                            if (isOnlyBotDeveloper || isOnlyBotModerator || isOnlyChannelModerator || cooldown)
                            {
                                Write($"Command failed: isOnlyBotDeveloper: {isOnlyBotDeveloper}; isOnlyBotModerator: {isOnlyBotModerator}; isOnlyChannelModerator: {isOnlyChannelModerator}; cooldown: {cooldown};", "info", LogLevel.Warning);
                                return;
                            }

                            if (!isATest) Command.ExecutedCommand(data);

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
                                    Chat.SendReply(data.Platform, data.Channel, data.ChannelId,
                                        result.Message, data.User.Language,
                                        data.User.Name, data.UserID, data.Server,
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
                        Write($"@{data.Name} tried unknown command: {command}", "info", LogLevel.Warning);
                    }
                }
                catch (Exception ex)
                {
                    Write(ex);
                    if (!isATest)
                    {
                        Chat.SendReply(data.Platform, data.Channel, data.ChannelId,
                            LocalizationService.GetString("en-US", "error:unknown", data.ChannelId, data.Platform),
                            data.User.Language, data.User.Name, data.UserID, data.Server,
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