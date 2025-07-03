using butterBror.Utils.DataManagers;
using butterBror.Utils.Tools;
using butterBror.Utils.Types;
using Discord.WebSocket;
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
            Core.Statistics.FunctionsUsed.Add();

            try
            {
                DateTime command_start_time = DateTime.Now;

                if (data.Platform is Platforms.Twitch && data.TwitchArguments.Command.ChatMessage.IsMe)
                    return;

                string user_language = "ru";

                try
                {
                    if (UsersData.Get<string>(data.UserID, "language", data.Platform) == null)
                        UsersData.Save(data.UserID, "language", "ru", data.Platform);
                    else
                        user_language = UsersData.Get<string>(data.UserID, "language", data.Platform);
                }
                catch (Exception ex)
                {
                    Write(ex);
                }

                data.User.Language = user_language;
                data.User.IsBanned = UsersData.Get<bool>(data.UserID, "isBanned", data.Platform);
                data.User.Ignored = UsersData.Get<bool>(data.UserID, "isIgnored", data.Platform);
                data.User.IsBotModerator = UsersData.Get<bool>(data.UserID, "isBotModerator", data.Platform);
                data.User.IsBotDeveloper = UsersData.Get<bool>(data.UserID, "isBotDev", data.Platform);
                var command = Text.FilterCommand(data.Name).Replace("ё", "е");

                bool command_founded = false;
                int index = 0;
                CommandReturn? result = null;

                if ((bool)data.User.IsBanned || (bool)data.User.Ignored) return; // Fix AB9

                List<CommandHandler> command_handlers_list;
                lock (_handlersLock)
                {
                    command_handlers_list = commandHandlers;
                }

                foreach (var handler in command_handlers_list)
                {
                    if (handler.info.Aliases.Contains(command))
                    {
                        command_founded = true;

                        if (handler.info.IsForBotDeveloper && !(bool)data.User.IsBotDeveloper ||
                            handler.info.IsForBotModerator && !(bool)data.User.IsBotModerator ||
                            (data.Platform == Platforms.Twitch && handler.info.IsForChannelModerator && !(bool)data.User.IsModerator))
                            break;

                        if (!Command.CheckCooldown(handler.info.CooldownPerUser, handler.info.CooldownPerChannel, handler.info.Name,
                            data.User.ID, data.ChannelID, data.Platform, handler.info.CooldownReset))
                            break;

                        if (handler.info.is_on_development)
                        {
                            CommandReturn commandReturn = new CommandReturn();
                            commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "text:tech_works", data.UserID, data.Platform));
                            result = commandReturn;
                        }
                        else
                        {
                            if (!isATest)
                            {
                                Command.ExecutedCommand(data);
                            }

                            try
                            {
                                var currentHandler = handler;
                                var commandName = currentHandler.info?.Name ?? "unknown_command";

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
                                throw new Exception($"Failed to execute command: \nError: {ex.Message}\nStack: {ex.StackTrace}\n" +
                                    $"Error Location: {ex.Source}\nCommand: {handler.info.Name}\nCommand Arguments: {data.ArgumentsString}\n" +
                                    $"User: {data.User.Username} (#{data.UserID})");
                            }
                        }

                        if (result is not null)
                        {
                            if (result.Exception is not null && result.IsError)
                                throw new Exception($"Command failed: " +
                                    $"\nError: {CheckNull(result.Exception.Message)}" +
                                    $"\nStack: {CheckNull(result.Exception.StackTrace)}" +
                                    $"\nSource: {CheckNull(result.Exception.Source)}" +
                                    $"\nCommand: {CheckNull(handler.info.Name)}" +
                                    $"\nArgs: {CheckNull(data.ArgumentsString)}" +
                                    $"\nUser: {CheckNull(data.User.Username)} (#{CheckNull(data.UserID)})");

                            if (!isATest)
                            {
                                switch (data.Platform)
                                {
                                    case Platforms.Twitch:
                                        SendCommandReply(new TwitchMessageSendData
                                        {
                                            Message = result.Message,
                                            Channel = data.Channel,
                                            ChannelID = data.ChannelID,
                                            MessageID = data.TwitchArguments.Command.ChatMessage.Id,
                                            Language = data.User.Language,
                                            Username = data.User.Username,
                                            SafeExecute = result.IsSafe,
                                            UsernameColor = result.BotNameColor
                                        });
                                        break;
                                    case Platforms.Discord:
                                        SendCommandReply(new DiscordCommandSendData
                                        {
                                            Message = result.Message,
                                            Title = result.Title,
                                            Description = result.Description,
                                            EmbedColor = (Discord.Color?)result.EmbedColor,
                                            IsEmbed = result.IsEmbed,
                                            IsEphemeral = result.IsEphemeral,
                                            Server = data.Server,
                                            ServerID = data.ServerID,
                                            Language = data.User.Language,
                                            SafeExecute = result.IsSafe,
                                            SocketCommandBase = data.DiscordCommandBase,
                                            Author = result.Author,
                                            ImageLink = result.ImageLink,
                                            ThumbnailLink = result.ThumbnailLink,
                                            Footer = result.Footer,
                                            ChannelID = data.ChannelID,
                                            UserID = data.UserID
                                        });
                                        break;
                                    case Platforms.Telegram:
                                        SendCommandReply(new TelegramMessageSendData
                                        {
                                            Message = result.Message,
                                            Language = data.User.Language,
                                            SafeExecute = result.IsSafe,
                                            Channel = data.Channel,
                                            ChannelID = data.ChannelID,
                                            MessageID = data.MessageID,
                                            Username = data.Name
                                        });
                                        break;
                                }
                            }

                            if (handler.info.cost != null)
                                Utils.Tools.Balance.Add(data.UserID, -(int)handler.info.cost, 0, data.Platform);
                        }
                        else
                        {
                            Write($"Empty response from {handler.info.Name}", "info", LogLevel.Warning);
                        }
                    }
                    index++;
                }

                if (!command_founded)
                {
                    Write($"@{data.Name} tried to execute unknown command: {command}", "info", LogLevel.Warning);
                }
            }
            catch (Exception ex)
            {
                Write(ex);
                if (!isATest)
                {
                    if (data.Platform is Platforms.Twitch)
                    {
                        TwitchMessageSendData SendData = new()
                        {
                            Message = TranslationManager.GetTranslation("ru", "error:unknown", data.ChannelID, data.Platform),
                            Channel = data.Channel,
                            ChannelID = data.ChannelID,
                            MessageID = data.TwitchArguments.Command.ChatMessage.Id,
                            Language = data.User.Language,
                            Username = data.User.Username,
                            SafeExecute = true,
                            UsernameColor = ChatColorPresets.Red
                        };
                        butterBror.Commands.SendCommandReply(SendData);
                    }
                    else if (data.Platform is Platforms.Telegram)
                    {
                        TelegramMessageSendData SendData = new()
                        {
                            Message = TranslationManager.GetTranslation("ru", "error:unknown", data.ChannelID, data.Platform),
                            Channel = data.Channel,
                            ChannelID = data.ChannelID,
                            MessageID = data.MessageID,
                            Language = data.User.Language,
                            Username = data.User.Username,
                            SafeExecute = true
                        };
                        butterBror.Commands.SendCommandReply(SendData);
                    }
                    else if (data.Platform is Platforms.Discord)
                    {
                        DiscordCommandSendData SendData = new()
                        {
                            Message = "",
                            Description = TranslationManager.GetTranslation("ru", "error:unknown", data.ChannelID, data.Platform),
                            IsEmbed = true,
                            EmbedColor = (Discord.Color?)System.Drawing.Color.Red,
                            IsEphemeral = true,
                            Server = data.Server,
                            ServerID = data.ServerID,
                            Language = data.User.Language,
                            SafeExecute = true,
                            SocketCommandBase = data.DiscordCommandBase,
                            ChannelID = data.ChannelID,
                            UserID = data.UserID
                        };
                        butterBror.Commands.SendCommandReply(SendData);
                    }
                }
            }
        }
    }
}