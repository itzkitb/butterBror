using butterBror.Utils.DataManagers;
using butterBror.Utils.Tools;
using butterBror.Utils.Types;
using Discord.WebSocket;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using Telegram.Bot.Types;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Events;
using static butterBror.Utils.Bot.Console;
using static butterBror.Utils.Tools.Text;

namespace butterBror
{
    /// <summary>
    /// Central command processing engine handling execution flow across platforms with cooldown and permission management.
    /// </summary>
    public partial class Commands
    {
        private static readonly object _handlersLock = new object();
        public static List<Type> commands =
        [
            typeof(Afk),
            typeof(Pinger),
            typeof(Autumn),
            typeof(Balance),
            typeof(Calculator),
            typeof(Winter),
            typeof(Spring),
            typeof(Summer),
            typeof(Vhs),
            typeof(Weather),
            typeof(Tuck),
            typeof(UploadToImgur),
            typeof(Me),
            typeof(RAfk),
            typeof(Status),
            typeof(Restart),
            typeof(Java),
            typeof(FirstGlobalLine),
            typeof(LastGlobalLine),
            typeof(LastLine),
            typeof(FirstLine),
            typeof(Emotes),
            typeof(Bot),
            typeof(AI_CHATBOT),
            typeof(CustomTranslation),
            typeof(Eightball),
            typeof(Coinflip),
            typeof(Percent),
            typeof(RussianRoullete),
            typeof(ID),
            typeof(RandomCMD),
            typeof(Say),
            typeof(Help),
            typeof(Dev),
            typeof(FrogGame),
            typeof(Roulette),
            typeof(Name),
            typeof(Translation),
            typeof(SetLocation),
            typeof(Cookie),
            typeof(Currency)
        ];

        public static List<CommandHandler> commandHandlers = new();

        /// <summary>
        /// Main command execution pipeline with platform-specific routing and error handling.
        /// </summary>
        /// <param name="data">Command execution context including user, channel, and platform info.</param>
        /// <param name="isATest">Indicates whether this is a test execution (skips logging).</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// - Loads user language preferences
        /// - Verifies command permissions (mod/broadcaster)
        /// - Applies command cost if defined
        /// - Routes result to appropriate platform handler
        /// - Handles development mode restrictions
        /// </remarks>
        [ConsoleSector("butterBror.Commands", "Run")]
        public static async Task Run(CommandData data, bool isATest = false)
        {
            Engine.Statistics.FunctionsUsed.Add();
            Stopwatch start = Stopwatch.StartNew();

            try
            {
                string userLanguage = "en";

                if (UsersData.Get<string>(data.UserID, "language", data.Platform) == null)
                    UsersData.Save(data.UserID, "language", "en", data.Platform);
                else
                    userLanguage = UsersData.Get<string>(data.UserID, "language", data.Platform);

                data.User.Language = userLanguage;
                data.User.IsBanned = UsersData.Get<bool>(data.UserID, "isBanned", data.Platform);
                data.User.Ignored = UsersData.Get<bool>(data.UserID, "isIgnored", data.Platform);
                data.User.IsBotModerator = UsersData.Get<bool>(data.UserID, "isBotModerator", data.Platform);
                data.User.IsBotDeveloper = UsersData.Get<bool>(data.UserID, "isBotDev", data.Platform);

                if ((bool)data.User.IsBanned || (bool)data.User.Ignored ||
                    (data.Platform is Platforms.Twitch && data.TwitchArguments.Command.ChatMessage.IsMe)) return; // Fix AB9

                string command = FilterCommand(data.Name).Replace("ё", "е");
                bool commandFounded = false;
                CommandReturn? result = null;

                List<CommandHandler> commandHandlersList;
                lock (_handlersLock)
                {
                    commandHandlersList = commandHandlers;
                }

                foreach (var handler in commandHandlersList)
                {
                    if (handler.Info.Aliases.Contains(command, StringComparer.OrdinalIgnoreCase))
                    {
                        if (handler.Info.Cost is not null)
                        {
                            int UserBalance = Utils.Tools.Balance.GetBalance(data.UserID, data.Platform);
                            if (UserBalance >= handler.Info.Cost)
                                Utils.Tools.Balance.Add(data.UserID, -(int)handler.Info.Cost, 0, data.Platform);
                            else
                            {
                                string message = TranslationManager.GetTranslation(data.User.Language, "error:command_not_enough_coins", data.ChannelID,
                                    data.Platform, new() { { "balance", UserBalance.ToString() }, { "need", handler.Info.Cost.ToString() } });
                                Utils.Tools.Chat.SendReply(data.Platform, data.Channel, data.ChannelID, message, data.User.Language,
                                    data.User.Name, data.UserID, data.Server, data.ServerID, data.MessageID, data.TelegramMessage, true);
                            }
                        }

                        commandFounded = true;

                        if (handler.Info.IsForBotDeveloper && !(bool)data.User.IsBotDeveloper ||
                            handler.Info.IsForBotModerator && !(bool)data.User.IsBotModerator ||
                            (data.Platform == Platforms.Twitch && handler.Info.IsForChannelModerator && !(bool)data.User.IsModerator) ||
                            !Command.CheckCooldown(handler.Info.CooldownPerUser, handler.Info.CooldownPerChannel, handler.Info.Name,
                            data.User.ID, data.ChannelID, data.Platform, handler.Info.CooldownReset))
                            break;

                        if (handler.Info.isOnDevelopment)
                        {
                            CommandReturn commandReturn = new CommandReturn();
                            commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "text:tech_works", data.UserID, data.Platform));
                            result = commandReturn;
                        }
                        else
                        {
                            if (!isATest) Command.ExecutedCommand(data);

                            try
                            {
                                var currentHandler = handler;
                                var commandName = currentHandler.Info?.Name ?? "unknown_command";

                                if (currentHandler.sync_executor != null)
                                {
                                    result = currentHandler.sync_executor(data);
                                }
                                else if (currentHandler.async_executor != null)
                                {
                                    result = await currentHandler.async_executor(data);
                                }
                            }
                            catch (Exception ex)
                            {
                                throw new Exception($"--How did we get here?--\n#Message:\n{ex.Message}\n#Stack:\n{ex.StackTrace}\n" +
                                    $"#ErrorSource:\n{ex.Source}\n#Command:\n{handler.Info.Name}\n#CommandArguments:\n{data.ArgumentsString}\n" +
                                    $"#User:\n{data.User.Name} (#{data.UserID})\n--End--");
                            }
                        }

                        if (result is not null)
                        {
                            if (result.Exception is not null && result.IsError)
                                throw new Exception($"--It's not your fault--\n#Message:\n{result.Exception.Message}\n#Stack:\n{result.Exception.StackTrace}\n" +
                                    $"#ErrorSource:\n{result.Exception.Source}\n#Command:\n{handler.Info.Name}\n#CommandArguments:\n{data.ArgumentsString}\n" +
                                    $"#User:\n{data.User.Name} (#{data.UserID})\n--End--");

                            if (!isATest)
                            {
                                Utils.Tools.Chat.SendReply(data.Platform, data.Channel, data.ChannelID, result.Message, data.User.Language,
                                    data.User.Name, data.UserID, data.Server, data.ServerID, data.MessageID, data.TelegramMessage, result.IsSafe, result.BotNameColor);
                            }
                        }
                        else
                        {
                            Write($"Empty response from {handler.Info.Name}", "info", LogLevel.Error);
                        }
                    }
                }

                if (!commandFounded)
                {
                    Write($"@{data.Name} tried to run unknown command: {command}", "info", LogLevel.Warning);
                }
            }
            catch (Exception ex)
            {
                Write(ex);
                if (!isATest)
                {
                    Utils.Tools.Chat.SendReply(data.Platform, data.Channel, data.ChannelID, TranslationManager.GetTranslation("ru", "error:unknown", data.ChannelID, data.Platform), data.User.Language,
                        data.User.Name, data.UserID, data.Server, data.ServerID, data.MessageID, data.TelegramMessage, true, ChatColorPresets.Red);
                }
            }
            finally
            {
                start.Stop();
                Write($"Command completed in {start.ElapsedMilliseconds}ms", "info");
            }
        }
    }
}