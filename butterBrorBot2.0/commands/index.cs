using butterBror.Utils.DataManagers;
using butterBror.Utils.Tools;
using Discord.WebSocket;
using System.Linq.Expressions;
using System.Reflection;
using Telegram.Bot.Types;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Events;
using static butterBror.Utils.Things.Console;
using static butterBror.Utils.Tools.Text;

namespace butterBror
{
    public partial class Commands
    {
        private static readonly object _handlersLock = new object();
        private static readonly Dictionary<Type, Func<object>> instance_factories = [];
        private static readonly Dictionary<Type, MethodInfo> method_cache = [];
        private static readonly Dictionary<Type, Delegate> sync_delegates = [];
        private static readonly Dictionary<Type, Delegate> async_delegates = [];
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

        [ConsoleSector("butterBror.Commands", "IndexCommands")]
        public static void IndexCommands()
        {
            Core.Statistics.FunctionsUsed.Add();
            Write($"Indexing commands...", "info");

            foreach (var classType in commands)
            {
                try
                {
                    var infoProperty = classType.GetField("Info", BindingFlags.Static | BindingFlags.Public);
                    var info = infoProperty.GetValue(null) as CommandInfo;

                    instance_factories[classType] = CreateInstanceFactory(classType);
                    var method = classType.GetMethod("Index", BindingFlags.Public | BindingFlags.Instance);
                    method_cache[classType] = method;

                    Delegate syncDelegate = null;
                    Delegate asyncDelegate = null;

                    if (method.ReturnType == typeof(CommandReturn))
                    {
                        syncDelegate = CreateSyncDelegate(classType, method);
                        sync_delegates[classType] = syncDelegate;
                    }
                    else if (method.ReturnType == typeof(Task<CommandReturn>))
                    {
                        asyncDelegate = CreateAsyncDelegate(classType, method);
                        async_delegates[classType] = asyncDelegate;
                    }

                    lock (_handlersLock)
                    {
                        var handler = new CommandHandler
                        {
                            info = info,
                            sync_executor = null,
                            async_executor = null
                        };

                        if (syncDelegate != null)
                        {
                            handler.sync_executor = (data) =>
                            {
                                var instance = instance_factories[classType]();
                                return (CommandReturn)syncDelegate.DynamicInvoke(instance, data);
                            };
                        }
                        else if (asyncDelegate != null)
                        {
                            handler.async_executor = async (data) =>
                            {
                                var instance = instance_factories[classType]();
                                return await (Task<CommandReturn>)asyncDelegate.DynamicInvoke(instance, data);
                            };
                        }

                        commandHandlers.Add(handler);
                    }
                }
                catch (Exception ex)
                {
                    Write($"[COMMAND_INDEXER] INDEX ERROR FOR CLASS {classType.Name}: {ex.Message}\n{ex.StackTrace}", "info", LogLevel.Warning);
                }
            }
            Write($"Indexed! ({commandHandlers.Count} commands loaded)", "info");
        }

        [ConsoleSector("butterBror.Commands", "CreateInstanceFactory")]
        private static Func<object> CreateInstanceFactory(Type type)
        {
            Core.Statistics.FunctionsUsed.Add();
            try
            {
                var constructor = type.GetConstructor(Type.EmptyTypes);
                if (constructor == null)
                    throw new MissingMethodException($"No parameterless constructor found for {type.Name}");

                var newExpr = Expression.New(constructor);
                var lambda = Expression.Lambda<Func<object>>(newExpr);
                return lambda.Compile();
            }
            catch (Exception ex)
            {
                Write($"Failed to create factory for {type.Name}: {ex.Message}", "info");
                throw;
            }
        }

        [ConsoleSector("butterBror.Commands", "CreateSyncDelegate")]
        private static Delegate CreateSyncDelegate(Type type, MethodInfo method)
        {
            Core.Statistics.FunctionsUsed.Add();
            var instanceParam = Expression.Parameter(typeof(object));
            var dataParam = Expression.Parameter(typeof(CommandData));
            var call = Expression.Call(
                Expression.Convert(instanceParam, type),
                method,
                dataParam
            );
            return Expression.Lambda(call, instanceParam, dataParam).Compile();
        }

        [ConsoleSector("butterBror.Commands", "CreateAsyncDelegate")]
        private static Delegate CreateAsyncDelegate(Type type, MethodInfo method)
        {
            Core.Statistics.FunctionsUsed.Add();
            var instanceParam = Expression.Parameter(typeof(object));
            var dataParam = Expression.Parameter(typeof(CommandData));
            var call = Expression.Call(
                Expression.Convert(instanceParam, type),
                method,
                dataParam
            );
            return Expression.Lambda(call, instanceParam, dataParam).Compile();
        }

        [ConsoleSector("butterBror.Commands", "Twitch")]
        public static async void Twitch(object sender, OnChatCommandReceivedArgs command)
        {
            Core.Statistics.FunctionsUsed.Add();
            try
            {
                string CommandName = command.Command.CommandText;
                List<string> CommandArguments = command.Command.ArgumentsAsList;
                string CommandArgumentsAsString = command.Command.ArgumentsAsString;

                if (CommandName == string.Empty && CommandArguments.Count > 0)
                {
                    CommandName = CommandArguments[0];
                    CommandArguments = CommandArguments.Skip(1).ToList();
                    CommandArgumentsAsString = string.Join(" ", CommandArguments);
                }
                else if (CommandName == string.Empty && CommandArguments.Count == 0)
                {
                    return;
                }

                UserData user = new()
                {
                    id = command.Command.ChatMessage.UserId,
                    language = "ru",
                    username = command.Command.ChatMessage.Username,
                    channel_moderator = command.Command.ChatMessage.IsModerator,
                    channel_broadcaster = command.Command.ChatMessage.IsBroadcaster
                };

                CommandData data = new()
                {
                    name = CommandName.ToLower(),
                    user_id = command.Command.ChatMessage.UserId,
                    arguments = CommandArguments,
                    arguments_string = CommandArgumentsAsString,
                    channel = command.Command.ChatMessage.Channel,
                    channel_id = command.Command.ChatMessage.RoomId,
                    message_id = command.Command.ChatMessage.Id,
                    platform = Platforms.Twitch,
                    user = user,
                    twitch_arguments = command,
                    command_instance_id = Guid.NewGuid().ToString()
                };

                if (command.Command.ChatMessage.ChatReply != null)
                {
                    string[] trimedReplyText = command.Command.ChatMessage.ChatReply.ParentMsgBody.Split(' ');
                    data.arguments.AddRange(trimedReplyText);
                    data.arguments_string = data.arguments_string + command.Command.ChatMessage.ChatReply.ParentMsgBody;
                }
                await Run(data);
            }
            catch (Exception ex)
            {
                Write(ex);
            }
        }

        [ConsoleSector("butterBror.Commands", "Discord#1")]
        public static async void Discord(SocketSlashCommand command)
        {
            Core.Statistics.FunctionsUsed.Add();
            try
            {
                UserData user = new()
                {
                    id = command.User.Id.ToString(),
                    language = "ru",
                    username = command.User.Username
                };
                Guid RequestUuid = Guid.NewGuid();
                Guid CommandExecutionUuid = Guid.NewGuid();
                string RequestUuidString = RequestUuid.ToString();
                string ArgsAsString = "";
                Dictionary<string, dynamic> argsDS = new();
                List<string> args = new();
                foreach (var info in command.Data.Options)
                {
                    ArgsAsString += info.Value.ToString();
                    argsDS.Add(info.Name, info.Value);
                    args.Add(info.Value.ToString());
                }

                CommandData data = new()
                {
                    name = command.CommandName.ToLower(),
                    user_id = command.User.Id.ToString(),
                    discord_arguments = argsDS,
                    channel = command.Channel.Name,
                    channel_id = command.Channel.Id.ToString(),
                    server = ((SocketGuildChannel)command.Channel).Guild.Name,
                    server_id = ((SocketGuildChannel)command.Channel).Guild.Id.ToString(),
                    platform = Platforms.Discord,
                    discord_command_base = command,
                    user = user,
                    arguments_string = ArgsAsString,
                    arguments = args,
                    command_instance_id = CommandExecutionUuid.ToString()
                };
                await Run(data);
            }
            catch (Exception ex)
            {
                Write(ex);
            }
        }

        [ConsoleSector("butterBror.Commands", "Discord#2")]
        public static async void Discord(SocketMessage message)
        {
            Core.Statistics.FunctionsUsed.Add();
            try
            {
                string CommandName = message.Content.Split(' ')[0].Remove(0, 1);
                List<string> CommandArguments = message.Content.Split(' ').Skip(1).ToList();
                string CommandArgumentsAsString = string.Join(' ', CommandArguments);

                if (CommandName == string.Empty && CommandArguments.Count > 0)
                {
                    CommandName = CommandArguments[0];
                    CommandArguments = CommandArguments.Skip(1).ToList();
                    CommandArgumentsAsString = string.Join(" ", CommandArguments);
                }
                else if (CommandName == string.Empty && CommandArguments.Count == 0)
                {
                    return;
                }

                UserData user = new()
                {
                    id = message.Author.Id.ToString(),
                    language = "ru",
                    username = message.Author.Username
                };

                CommandData data = new()
                {
                    name = CommandName,
                    user_id = message.Author.Id.ToString(),
                    channel = message.Channel.Name,
                    channel_id = message.Channel.Id.ToString(),
                    server = ((SocketGuildChannel)message.Channel).Guild.Name,
                    server_id = ((SocketGuildChannel)message.Channel).Guild.Id.ToString(),
                    platform = Platforms.Discord,
                    user = user,
                    arguments_string = CommandArgumentsAsString,
                    arguments = CommandArguments,
                    command_instance_id = Guid.NewGuid().ToString()
                };
                await Run(data);
            }
            catch (Exception ex)
            {
                Write(ex);
            }
        }

        [ConsoleSector("butterBror.Commands", "Telegram")]
        public static async void Telegram(Message message)
        {
            Core.Statistics.FunctionsUsed.Add();
            try
            {
                UserData user = new()
                {
                    id = message.From.Id.ToString(),
                    language = "ru",
                    username = message.From.Username ?? message.From.FirstName,
                    channel_moderator = false,
                    channel_broadcaster = message.From.Id == message.Chat.Id
                };

                Guid command_execution_uid = Guid.NewGuid();
                CommandData data = new()
                {
                    name = message.Text.ToLower().Split(' ')[0],
                    user_id = message.From.Id.ToString(),
                    arguments = message.Text.Split(' ').Skip(1).ToList(),
                    arguments_string = string.Join(" ", message.Text.Split(' ').Skip(1)),
                    channel = message.Chat.Title ?? message.Chat.Username ?? message.Chat.Id.ToString(),
                    channel_id = message.Chat.Id.ToString(),
                    platform = Platforms.Telegram,
                    user = user,
                    command_instance_id = command_execution_uid.ToString(),
                    telegram_message = message,
                    message_id = message.Id.ToString()
                };

                if (message.ReplyToMessage != null)
                {
                    string[] trimmedReplyText = message.ReplyToMessage.Text.Split(' ');
                    data.arguments.AddRange(trimmedReplyText);
                    data.arguments_string += " " + string.Join(" ", trimmedReplyText);
                }

                await Run(data);
            }
            catch (Exception ex)
            {
                Write(ex);
            }
        }

        [ConsoleSector("butterBror.Commands", "Run")]
        public static async Task Run(CommandData data, bool isATest = false)
        {
            Core.Statistics.FunctionsUsed.Add();

            try
            {
                DateTime command_start_time = DateTime.Now;

                if (data.platform is Platforms.Twitch && data.twitch_arguments.Command.ChatMessage.IsMe)
                    return;

                string user_language = "ru";

                try
                {
                    if (UsersData.Get<string>(data.user_id, "language", data.platform) == null)
                        UsersData.Save(data.user_id, "language", "ru", data.platform);
                    else
                        user_language = UsersData.Get<string>(data.user_id, "language", data.platform);
                }
                catch (Exception ex)
                {
                    Write(ex);
                }

                data.user.language = user_language;
                data.user.banned = UsersData.Get<bool>(data.user_id, "isBanned", data.platform);
                data.user.ignored = UsersData.Get<bool>(data.user_id, "isIgnored", data.platform);
                data.user.bot_moderator = UsersData.Get<bool>(data.user_id, "isBotModerator", data.platform);
                data.user.bot_developer = UsersData.Get<bool>(data.user_id, "isBotDev", data.platform);
                var command = Text.FilterCommand(data.name).Replace("ё", "е");

                bool command_founded = false;
                int index = 0;
                CommandReturn? result = null;

                if ((bool)data.user.banned || (bool)data.user.ignored) return; // Fix AB9

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

                        if (handler.info.IsForBotDeveloper && !(bool)data.user.bot_developer ||
                            handler.info.IsForBotModerator && !(bool)data.user.bot_moderator ||
                            (data.platform == Platforms.Twitch && handler.info.IsForChannelModerator && !(bool)data.user.channel_moderator))
                            break;

                        if (!Command.CheckCooldown(handler.info.CooldownPerUser, handler.info.CooldownPerChannel, handler.info.Name,
                            data.user.id, data.channel_id, data.platform, handler.info.CooldownReset))
                            break;

                        if (handler.info.is_on_development)
                        {
                            CommandReturn commandReturn = new CommandReturn();
                            commandReturn.SetMessage(TranslationManager.GetTranslation(data.user.language, "text:tech_works", data.user_id, data.platform));
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
                                    $"Error Location: {ex.Source}\nCommand: {handler.info.Name}\nCommand Arguments: {data.arguments_string}\n" +
                                    $"User: {data.user.username} (#{data.user_id})");
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
                                    $"\nArgs: {CheckNull(data.arguments_string)}" +
                                    $"\nUser: {CheckNull(data.user.username)} (#{CheckNull(data.user_id)})");

                            if (!isATest)
                            {
                                switch (data.platform)
                                {
                                    case Platforms.Twitch:
                                        SendCommandReply(new TwitchMessageSendData
                                        {
                                            message = result.Message,
                                            channel = data.channel,
                                            channel_id = data.channel_id,
                                            message_id = data.twitch_arguments.Command.ChatMessage.Id,
                                            language = data.user.language,
                                            username = data.user.username,
                                            safe_execute = result.IsSafe,
                                            nickname_color = result.BotNameColor
                                        });
                                        break;
                                    case Platforms.Discord:
                                        SendCommandReply(new DiscordCommandSendData
                                        {
                                            message = result.Message,
                                            title = result.Title,
                                            description = result.Description,
                                            embed_color = (Discord.Color?)result.EmbedColor,
                                            is_embed = result.IsEmbed,
                                            is_ephemeral = result.IsEphemeral,
                                            server = data.server,
                                            server_id = data.server_id,
                                            language = data.user.language,
                                            safe_execute = result.IsSafe,
                                            socket_command_base = data.discord_command_base,
                                            author = result.Author,
                                            image_link = result.ImageLink,
                                            thumbnail_link = result.ThumbnailLink,
                                            footer = result.Footer,
                                            channel_id = data.channel_id,
                                            user_id = data.user_id
                                        });
                                        break;
                                    case Platforms.Telegram:
                                        SendCommandReply(new TelegramMessageSendData
                                        {
                                            message = result.Message,
                                            language = data.user.language,
                                            safe_execute = result.IsSafe,
                                            channel = data.channel,
                                            channel_id = data.channel_id,
                                            message_id = data.message_id,
                                            username = data.name
                                        });
                                        break;
                                }
                            }

                            if (handler.info.cost != null)
                                Utils.Tools.Balance.Add(data.user_id, -(int)handler.info.cost, 0, data.platform);
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
                    Write($"@{data.name} tried to execute unknown command: {command}", "info", LogLevel.Warning);
                }
            }
            catch (Exception ex)
            {
                Write(ex);
                if (!isATest)
                {
                    if (data.platform is Platforms.Twitch)
                    {
                        TwitchMessageSendData SendData = new()
                        {
                            message = TranslationManager.GetTranslation("ru", "error:unknown", data.channel_id, data.platform),
                            channel = data.channel,
                            channel_id = data.channel_id,
                            message_id = data.twitch_arguments.Command.ChatMessage.Id,
                            language = data.user.language,
                            username = data.user.username,
                            safe_execute = true,
                            nickname_color = ChatColorPresets.Red
                        };
                        butterBror.Commands.SendCommandReply(SendData);
                    }
                    else if (data.platform is Platforms.Telegram)
                    {
                        TelegramMessageSendData SendData = new()
                        {
                            message = TranslationManager.GetTranslation("ru", "error:unknown", data.channel_id, data.platform),
                            channel = data.channel,
                            channel_id = data.channel_id,
                            message_id = data.message_id,
                            language = data.user.language,
                            username = data.user.username,
                            safe_execute = true
                        };
                        butterBror.Commands.SendCommandReply(SendData);
                    }
                    else if (data.platform is Platforms.Discord)
                    {
                        DiscordCommandSendData SendData = new()
                        {
                            message = "",
                            description = TranslationManager.GetTranslation("ru", "error:unknown", data.channel_id, data.platform),
                            is_embed = true,
                            embed_color = (Discord.Color?)System.Drawing.Color.Red,
                            is_ephemeral = true,
                            server = data.server,
                            server_id = data.server_id,
                            language = data.user.language,
                            safe_execute = true,
                            socket_command_base = data.discord_command_base,
                            channel_id = data.channel_id,
                            user_id = data.user_id
                        };
                        butterBror.Commands.SendCommandReply(SendData);
                    }
                }
            }
        }

        public class CommandHandler
        {
            public CommandInfo info { get; set; }
            public Func<CommandData, CommandReturn> sync_executor { get; set; }
            public Func<CommandData, Task<CommandReturn>> async_executor { get; set; }
        }
    }
}