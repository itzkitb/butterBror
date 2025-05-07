using butterBror.Utils;
using butterBror.Utils.DataManagers;
using Discord.WebSocket;
using System.Reflection;
using Telegram.Bot.Types;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Events;
using System.Linq.Expressions;
using static butterBror.Utils.TextUtil;
using System.Collections.Concurrent;

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
            typeof(BotCommand),
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
            typeof(SetLocation)
        ];

        public static List<CommandHandler> commandHandlers = new();

        public static void IndexCommands()
        {
            Engine.Statistics.functions_used.Add();
            Utils.Console.WriteLine($"Indexing commands...", "main");
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
                    Utils.Console.WriteLine($"[COMMAND_INDEXER] INDEX ERROR FOR CLASS {classType.Name}: {ex.Message}\n{ex.StackTrace}", "main");
                }
            }
            Utils.Console.WriteLine($"Indexed! ({commandHandlers.Count} commands loaded)", "main");
        }

        private static Func<object> CreateInstanceFactory(Type type)
        {
            Engine.Statistics.functions_used.Add();
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
                Utils.Console.WriteLine($"Failed to create factory for {type.Name}: {ex.Message}", "main");
                throw;
            }
        }

        private static Delegate CreateSyncDelegate(Type type, MethodInfo method)
        {
            Engine.Statistics.functions_used.Add();
            var instanceParam = Expression.Parameter(typeof(object));
            var dataParam = Expression.Parameter(typeof(CommandData));
            var call = Expression.Call(
                Expression.Convert(instanceParam, type),
                method,
                dataParam
            );
            return Expression.Lambda(call, instanceParam, dataParam).Compile();
        }

        private static Delegate CreateAsyncDelegate(Type type, MethodInfo method)
        {
            Engine.Statistics.functions_used.Add();
            var instanceParam = Expression.Parameter(typeof(object));
            var dataParam = Expression.Parameter(typeof(CommandData));
            var call = Expression.Call(
                Expression.Convert(instanceParam, type),
                method,
                dataParam
            );
            return Expression.Lambda(call, instanceParam, dataParam).Compile();
        }

        public static async void Twitch(object sender, OnChatCommandReceivedArgs args)
        {
            Engine.Statistics.functions_used.Add();
            try
            {
                UserData user = new()
                {
                    id = args.Command.ChatMessage.UserId,
                    language = "ru",
                    username = args.Command.ChatMessage.Username,
                    channel_moderator = args.Command.ChatMessage.IsModerator,
                    channel_broadcaster = args.Command.ChatMessage.IsBroadcaster
                };
                Guid CommandExecutionUuid = Guid.NewGuid();
                CommandData data = new()
                {
                    name = args.Command.CommandText.ToLower(),
                    user_id = args.Command.ChatMessage.UserId,
                    arguments = args.Command.ArgumentsAsList,
                    arguments_string = args.Command.ArgumentsAsString,
                    channel = args.Command.ChatMessage.Channel,
                    channel_id = args.Command.ChatMessage.RoomId,
                    message_id = args.Command.ChatMessage.Id,
                    platform = Platforms.Twitch,
                    user = user,
                    twitch_arguments = args,
                    command_instance_id = CommandExecutionUuid.ToString()
                };
                if (args.Command.ChatMessage.ChatReply != null)
                {
                    string[] trimedReplyText = args.Command.ChatMessage.ChatReply.ParentMsgBody.Split(' ');
                    data.arguments.AddRange(trimedReplyText);
                    data.arguments_string = data.arguments_string + args.Command.ChatMessage.ChatReply.ParentMsgBody;
                }
                await Run(data);
            }
            catch (Exception ex)
            {
                Utils.Console.WriteError(ex, $"Command\\TwitchCommand#Command:{args.Command.CommandText}\\FullMessage:{args.Command.ChatMessage}");
            }
        }
        public static async void Discord(SocketSlashCommand dsCmd)
        {
            Engine.Statistics.functions_used.Add();
            try
            {
                UserData user = new()
                {
                    id = dsCmd.User.Id.ToString(),
                    language = "ru",
                    username = dsCmd.User.Username
                };
                Guid RequestUuid = Guid.NewGuid();
                Guid CommandExecutionUuid = Guid.NewGuid();
                string RequestUuidString = RequestUuid.ToString();
                string ArgsAsString = "";
                Dictionary<string, dynamic> argsDS = new();
                List<string> args = new();
                foreach (var info in dsCmd.Data.Options)
                {
                    ArgsAsString += info.Value.ToString();
                    argsDS.Add(info.Name, info.Value);
                    args.Add(info.Value.ToString());
                }

                CommandData data = new()
                {
                    name = dsCmd.CommandName.ToLower(),
                    user_id = dsCmd.User.Id.ToString(),
                    discord_arguments = argsDS,
                    channel = ((SocketGuildChannel)dsCmd.Channel).Guild.Name,
                    channel_id = ((SocketGuildChannel)dsCmd.Channel).Guild.Id.ToString(),
                    platform = Platforms.Discord,
                    discord_command_base = dsCmd,
                    user = user,
                    arguments_string = ArgsAsString,
                    arguments = args,
                    command_instance_id = CommandExecutionUuid.ToString()
                };
                await Run(data);
            }
            catch (Exception ex)
            {
                Utils.Console.WriteError(ex, $"Command\\DiscordCommand#Command:{dsCmd.CommandName}");
            }
        }

        public static async void Telegram(Message message)
        {
            Engine.Statistics.functions_used.Add();
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
                Utils.Console.WriteError(ex, $"Command\\TelegramCommand#Command:{message.Text}\\FullMessage:{message}");
            }
        }

        public static async Task Run(CommandData data)
        {
            Engine.Statistics.functions_used.Add();

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
                    Utils.Console.WriteError(ex, $"(NOTFATAL)Command\\Command\\GetLang#{data.user_id}");
                }

                data.user.language = user_language;
                data.user.banned = UsersData.Get<bool>(data.user_id, "isBanned", data.platform);
                data.user.ignored = UsersData.Get<bool>(data.user_id, "isIgnored", data.platform);
                data.user.bot_moderator = UsersData.Get<bool>(data.user_id, "isBotModerator", data.platform);
                data.user.bot_developer = UsersData.Get<bool>(data.user_id, "isBotDev", data.platform);
                var command = TextUtil.FilterCommand(data.name).Replace("ё", "е");

                bool command_founded = false;
                int index = 0;
                CommandReturn? result = null;


                List<CommandHandler> command_handlers_list;
                lock (_handlersLock)
                {
                    command_handlers_list = commandHandlers;
                }

                foreach (var handler in command_handlers_list)
                {
                    if (handler.info.aliases.Contains(command))
                    {
                        command_founded = true;

                        if (handler.info.is_for_bot_developer && !(bool)data.user.bot_developer ||
                            handler.info.is_for_bot_moderator && !(bool)data.user.bot_moderator ||
                            (data.platform == Platforms.Twitch && handler.info.is_for_channel_moderator && !(bool)data.user.channel_moderator))
                            break;

                        if (!Command.CheckCooldown(handler.info.cooldown_per_user, handler.info.cooldown_global, handler.info.name,
                            data.user.id, data.channel_id, data.platform, handler.info.cooldown_reset))
                            break;

                        if (handler.info.is_on_development)
                        {
                            result = new()
                            {
                                message = TranslationManager.GetTranslation(data.user.language, "text:tech_works", data.user_id, data.platform),
                                safe_execute = true,
                                description = "",
                                author = "",
                                image_link = "",
                                thumbnail_link = "",
                                footer = "",
                                is_embed = true,
                                is_ephemeral = false,
                                title = "",
                                embed_color = global::Discord.Color.Green,
                                nickname_color = ChatColorPresets.YellowGreen,
                                is_error = true
                            };
                        }
                        else
                        {
                            Command.ExecutedCommand(data);

                            try
                            {
                                var currentHandler = handler;
                                var commandName = currentHandler.info?.name ?? "unknown_command";

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
                                    $"Error Location: {ex.Source}\nCommand: {handler.info.name}\nCommand Arguments: {data.arguments_string}\n" +
                                    $"User: {data.user.username} (#{data.user_id})");
                            }
                        }

                        if (result is not null)
                        {
                            if (result.exception is null)
                                System.Console.WriteLine("exeption is null");
                            else if (result.is_error)
                                throw new Exception($"Command failed: " +
                                    $"\nError: {CheckNull(result.exception.Message)}" +
                                    $"\nStack: {CheckNull(result.exception.StackTrace)}" +
                                    $"\nSource: {CheckNull(result.exception.Source)}" +
                                    $"\nCommand: {CheckNull(handler.info.name)}" +
                                    $"\nArgs: {CheckNull(data.arguments_string)}" +
                                    $"\nUser: {CheckNull(data.user.username)} (#{CheckNull(data.user_id)})");

                            switch (data.platform)
                            {
                                case Platforms.Twitch:
                                    SendCommandReply(new TwitchMessageSendData
                                    {
                                        message = result.message,
                                        channel = data.channel,
                                        channel_id = data.channel_id,
                                        message_id = data.twitch_arguments.Command.ChatMessage.Id,
                                        language = data.user.language,
                                        username = data.user.username,
                                        safe_execute = result.safe_execute,
                                        nickname_color = result.nickname_color
                                    });
                                    break;
                                case Platforms.Discord:
                                    SendCommandReply(new DiscordCommandSendData
                                    {
                                        message = result.message,
                                        title = result.title,
                                        description = result.description,
                                        embed_color = (Discord.Color?)result.embed_color,
                                        is_embed = result.is_embed,
                                        is_ephemeral = result.is_ephemeral,
                                        server = data.channel,
                                        server_id = data.channel_id,
                                        language = data.user.language,
                                        safe_execute = result.safe_execute,
                                        socket_command_base = data.discord_command_base,
                                        author = result.author,
                                        image_link = result.image_link,
                                        thumbnail_link = result.thumbnail_link,
                                        footer = result.footer
                                    });
                                    break;
                                case Platforms.Telegram:
                                    SendCommandReply(new TelegramMessageSendData
                                    {
                                        message = result.message,
                                        language = data.user.language,
                                        safe_execute = result.safe_execute,
                                        channel = data.channel,
                                        channel_id = data.channel_id,
                                        message_id = data.message_id,
                                        username = data.name
                                    });
                                    break;
                            }

                            // Списание стоимости
                            if (handler.info.cost != null)
                                Utils.Balance.Add(data.user_id, -(int)handler.info.cost, 0, data.platform);
                        }
                        else
                        {
                            LogWorker.Log($"Empty response from {handler.info.name}", LogWorker.LogTypes.Warn, $"cmd\\{handler.info.name}");
                        }
                    }
                    index++;
                }

                if (!command_founded)
                {
                    LogWorker.Log($"@{data.name} tried to execute unknown command: {command}", LogWorker.LogTypes.Warn, $"command#{data.user_id}");
                }
            }
            catch (Exception ex)
            {
                Utils.Console.WriteError(ex, $"command\\command#UserID:{data.user_id}\\Command:{data.name}");
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
                        server = data.channel,
                        server_id = data.channel_id,
                        language = data.user.language,
                        safe_execute = true,
                        socket_command_base = data.discord_command_base
                    };
                    butterBror.Commands.SendCommandReply(SendData);
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