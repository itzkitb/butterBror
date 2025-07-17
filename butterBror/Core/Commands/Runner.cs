using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using TwitchLib.Client.Enums;
using static butterBror.Core.Bot.Console;
using butterBror.Data;
using butterBror.Models;
using butterBror.Utils;
using System.Collections.Concurrent;
using System.Linq;

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

        [ConsoleSector("butterBror.Commands", "Run")]
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
                    string userLanguage = UsersData.Get<string>(data.UserID, "language", data.Platform) ?? "en";
                    if (UsersData.Get<string>(data.UserID, "language", data.Platform) == null)
                    {
                        UsersData.Save(data.UserID, "language", "en", data.Platform);
                    }

                    data.User.Language = userLanguage;
                    data.User.IsBanned = UsersData.Get<bool>(data.UserID, "isBanned", data.Platform);
                    data.User.Ignored = UsersData.Get<bool>(data.UserID, "isIgnored", data.Platform);
                    data.User.IsBotModerator = UsersData.Get<bool>(data.UserID, "isBotModerator", data.Platform);
                    data.User.IsBotDeveloper = UsersData.Get<bool>(data.UserID, "isBotDev", data.Platform);

                    if ((bool)data.User.IsBanned || (bool)data.User.Ignored ||
                        (data.Platform is PlatformsEnum.Twitch && data.TwitchArguments.Command.ChatMessage.IsMe))
                        return;

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
                            // Permission and cooldown checks
                            if (cmd.OnlyBotDeveloper && !(bool)data.User.IsBotDeveloper ||
                                cmd.OnlyBotModerator && !(bool)data.User.IsBotModerator ||
                                (data.Platform == PlatformsEnum.Twitch && cmd.OnlyChannelModerator && !(bool)data.User.IsModerator) ||
                                !Command.CheckCooldown(cmd.CooldownPerUser, cmd.CooldownPerChannel, cmd.Name,
                                    data.User.ID, data.ChannelID, data.Platform, true))
                            {
                                return;
                            }

                            if (!isATest) Command.ExecutedCommand(data);

                            // Execute command asynchronously
                            if (cmd.IsAsync)
                            {
                                result = await cmd.ExecuteAsync(data);
                            }
                            else
                            {
                                result = await Task.Run(() => cmd.Execute(data));
                            }

                            // Process result
                            if (result is not null)
                            {
                                if (result.Exception is not null && result.IsError)
                                    throw new Exception($"Error in command: {cmd.Name}", result.Exception);

                                if (!isATest)
                                {
                                    Chat.SendReply(data.Platform, data.Channel, data.ChannelID,
                                        result.Message, data.User.Language,
                                        data.User.Name, data.UserID, data.Server,
                                        data.ServerID, data.MessageID, data.TelegramMessage,
                                        result.IsSafe, result.BotNameColor);
                                }
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
                        Chat.SendReply(data.Platform, data.Channel, data.ChannelID,
                            TranslationManager.GetTranslation("ru", "error:unknown", data.ChannelID, data.Platform),
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