using Discord.WebSocket;
using TwitchLib.Client.Events;
using butterBib;
using System.Drawing;
using System.Reflection;
using TwitchLib.Client.Enums;
using butterBror.Utils;
using butterBror.Utils.DataManagers;

namespace butterBror
{
    public partial class Commands
    {
        public static List<Type> classes = new List<Type>
        {
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
            typeof(GPT),
            typeof(CustomTranslation),
            typeof(eightball),
            typeof(coinflip),
            typeof(percent),
            typeof(RussianRoullete),
            typeof(ID),
            typeof(RandomCMD),
            typeof(Say),
            typeof(Help),
            typeof(Dev)
        }; // test
        public static void TwitchCommand(object sender, OnChatCommandReceivedArgs args)
        {
            try
            {
                UserData user = new()
                {
                    Id = args.Command.ChatMessage.UserId,
                    Lang = "ru",
                    Name = args.Command.ChatMessage.Username,
                    IsChannelAdmin = args.Command.ChatMessage.IsModerator,
                    IsChannelBroadcaster = args.Command.ChatMessage.IsBroadcaster
                };
                Guid RequestUuid = Guid.NewGuid();
                string RequestUuidString = RequestUuid.ToString();
                CommandData data = new()
                {
                    Name = args.Command.CommandText.ToLower(),
                    RequestUUID = RequestUuidString,
                    UserUUID = args.Command.ChatMessage.UserId,
                    args = args.Command.ArgumentsAsList,
                    ArgsAsString = args.Command.ArgumentsAsString,
                    Channel = args.Command.ChatMessage.Channel,
                    ChannelID = args.Command.ChatMessage.RoomId,
                    MessageID = args.Command.ChatMessage.Id,
                    Platform = Platforms.Twitch,
                    User = user,
                    TWargs = args
                };
                if (args.Command.ChatMessage.ChatReply != null)
                {
                    string[] trimedReplyText = args.Command.ChatMessage.ChatReply.ParentMsgBody.Split(' ');
                    data.args.AddRange(trimedReplyText);
                    data.ArgsAsString = data.ArgsAsString + args.Command.ChatMessage.ChatReply.ParentMsgBody;
                }
                Command(data);
            }
            catch (Exception ex) 
            {
                ConsoleUtil.ErrorOccured(ex.Message + " - " + ex.TargetSite, "TwitchCommand()");
            }
        }
        public static void DiscordCommand(SocketSlashCommand dsCmd)
        {
            try
            {
                UserData user = new()
                {
                    Id = dsCmd.User.Id.ToString(),
                    Lang = "ru",
                    Name = dsCmd.User.Username
                };
                Guid RequestUuid = Guid.NewGuid();
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
                    Name = dsCmd.CommandName.ToLower(),
                    RequestUUID = RequestUuidString,
                    UserUUID = dsCmd.User.Id.ToString(),
                    DSargs = argsDS,
                    Channel = ((SocketGuildChannel)dsCmd.Channel).Guild.Name,
                    ChannelID = ((SocketGuildChannel)dsCmd.Channel).Guild.Id.ToString(),
                    Platform = Platforms.Discord,
                    d = dsCmd,
                    User = user,
                    ArgsAsString = ArgsAsString,
                    args = args
                };
                Command(data);
            }
            catch (Exception ex) 
            {
                ConsoleUtil.ErrorOccured(ex.Message, "DiscordCommand()");
            }
        }
        public static void Command(CommandData data)
        {
            try
            {
                var allowed = true;
                if (data.Platform == Platforms.Twitch)
                {
                    allowed = !data.TWargs.Command.ChatMessage.IsMe;
                }
                if (allowed)
                {
                    var lang = "ru";
                    try
                    {
                        string Id = "";
                        if (data.Platform == Platforms.Twitch)
                        {
                            Id = data.UserUUID;
                        }
                        else if (data.Platform == Platforms.Discord)
                        {
                            Id = "ds" + data.UserUUID;
                        }

                        if (UsersData.UserGetData<string>(Id, "language") == default)
                        {
                            UsersData.UserSaveData(Id, "language", "ru");
                        }
                        else
                        {
                            lang = UsersData.UserGetData<string>(Id, "language");
                        }
                    }
                    catch (Exception ex)
                    {
                        ConsoleUtil.ErrorOccured(ex.Message, "cmd0A");
                    }
                    data.User.Lang = lang;
                    data.User.IsBanned = UsersData.UserGetData<bool>(data.UserUUID, "isBanned");
                    data.User.IsIgnored = UsersData.UserGetData<bool>(data.UserUUID, "isIgnored");
                    data.User.IsBotAdmin = UsersData.UserGetData<bool>(data.UserUUID, "isBotModerator");
                    data.User.IsBotCreator = UsersData.UserGetData<bool>(data.UserUUID, "isBotDev");
                    Bot.CommandsActive = classes.Count;
                    string[] bot = ["bot", "bt", "бот", "бт"];
                    string[] help = ["help", "sos", "commands", "помощь", "информация", "info", "инфо"];

                    string[] miningVideocard = ["mining", "mng", "манинг", "майн", "мнг"];
                    string[] location = ["location", "loc", "локация", "лок"];
                    string[] pizzas = ["pizza", "хуица", "пицца"];
                    string[] fishingAliases = ["fishing", "fish", "рыба", "рыбалка"];
                    var command = TextUtil.FilterCommand(data.Name).Replace("ё", "е");

                    DebugUtil.LOG("Начало поиска комманды...");
                    if (!(bool)data.User.IsBanned && !(bool)data.User.IsIgnored)
                    {
                        // Console.Write("4 (");
                        int num = 0;
                        CommandReturn cmdReturn = null; // Инициализация переменной для хранения результата
                        DebugUtil.LOG("Поиск комманды...");
                        foreach (var classType in classes)
                        {
                            num++;
                            // Получение значения статического свойства Info
                            var infoProperty = classType.GetField("Info", BindingFlags.Static | BindingFlags.Public);
                            var info = infoProperty.GetValue(null) as CommandInfo;

                            if (info.aliases.Contains(command))
                            {
                                DebugUtil.LOG("Комманда найдена!");
                                if (info.ForBotCreator && !(bool)data.User.IsBotCreator || info.ForAdmins && !(bool)data.User.IsBotAdmin || info.ForChannelAdmins && !(bool)data.User.IsChannelAdmin)
                                {
                                    break;
                                }
                                DebugUtil.LOG("Проверка кулдауна...");
                                if (CommandUtil.IsNotOnCooldown(info.UserCooldown, info.GlobalCooldown, info.Name, data.User.Id, data.ChannelID, info.ResetCooldownIfItHasNotReachedZero))
                                {
                                    DebugUtil.LOG("Подготовка к выполнению...");
                                    CommandUtil.executedCommand(data);
                                    var indexMethod = classType.GetMethod("Index", BindingFlags.Static | BindingFlags.Public);

                                    if (indexMethod.ReturnType == typeof(CommandReturn))
                                    {
                                        DebugUtil.LOG("Выполнение в синхронном режиме...");
                                        // Синхронный метод с возвращаемым значением типа CommandReturn
                                        cmdReturn = (CommandReturn)indexMethod.Invoke(null, new object[] { data });
                                    }
                                    else
                                    {
                                        DebugUtil.LOG("Выполнение в асинхронном режиме...");
                                        // Синхронный метод с возвращаемым значением Task<T>
                                        var result = indexMethod.Invoke(null, new object[] { data }) as Task<CommandReturn>;
                                        if (result != null)
                                        {
                                            try
                                            {
                                                cmdReturn = result.Result;
                                            }
                                            catch (Exception ex)
                                            {
                                                ConsoleServer.SendConsoleMessage("commands", $"Ошибка при получении результата из Task: {ex.Message}");
                                                LogWorker.Log($"Ошибка при получении результата из Task: {ex.Message}", LogWorker.LogTypes.Err, $"commands/{info.Name}");
                                            }
                                        }
                                        else
                                        {
                                            ConsoleServer.SendConsoleMessage("commands", $"Метод '{info.Name}' вернул неверный тип результата: {result.GetType().Name}");
                                            LogWorker.Log($"Метод '{info.Name}' вернул неверный тип результата: {result.GetType().Name}", LogWorker.LogTypes.Err, $"commands/{info.Name}");
                                        }
                                    }
                                    DebugUtil.LOG("Выполнено!");
                                    if (data != null)
                                    {
                                        DebugUtil.LOG("Отправка результата...");
                                        if (data.Platform == Platforms.Twitch)
                                        {
                                            TwitchMessageSendData SendData = new()
                                            {
                                                Message = cmdReturn.Message,
                                                Channel = data.Channel,
                                                ChannelID = data.ChannelID,
                                                AnswerID = data.TWargs.Command.ChatMessage.Id,
                                                Lang = data.User.Lang,
                                                Name = data.User.Name,
                                                IsSafeExecute = cmdReturn.IsSafeExecute,
                                                NickNameColor = cmdReturn.NickNameColor
                                            };
                                            butterBib.Commands.SendCommandReply(SendData);
                                        }
                                        else if (data.Platform == Platforms.Discord)
                                        {
                                            DiscordCommandSendData SendData = new()
                                            {
                                                Message = cmdReturn.Message,
                                                Title = cmdReturn.Title,
                                                Description = cmdReturn.Description,
                                                Color = cmdReturn.Color,
                                                IsEmbed = cmdReturn.IsEmbed,
                                                Ephemeral = cmdReturn.Ephemeral,
                                                Server = data.Channel,
                                                ServerID = data.ChannelID,
                                                Lang = data.User.Lang,
                                                IsSafeExecute = cmdReturn.IsSafeExecute,
                                                d = data.d,
                                                Author = cmdReturn.Author,
                                                ImageURL = cmdReturn.ImageURL,
                                                ThumbnailUrl = cmdReturn.ThumbnailUrl,
                                                Footer = cmdReturn.Footer
                                            };
                                            butterBib.Commands.SendCommandReply(SendData);
                                        }
                                        if (info.Cost != null)
                                        {
                                            BalanceUtil.SaveBalance(data.UserUUID, -(int)(info.Cost), 0);
                                        }
                                        DebugUtil.LOG("Отправлено!");
                                    }
                                    else
                                    {
                                        ConsoleServer.SendConsoleMessage("commands", "Пустой ответ от комманды");
                                        LogWorker.Log("Пустой ответ от комманды", LogWorker.LogTypes.Warn, $"commands/{info.Name}");
                                    }
                                }
                                break;
                            }
                        }
                    }
                    else
                    {
                        if (bot.Contains(command))
                        {
                            BotCommand.Index(data);
                        }
                        else
                        {
                            LogWorker.Log($"{data.Name} попробовал выполнить команду, но был заигнорен или забанен (#{data.Name} {data.ArgsAsString})", LogWorker.LogTypes.Warn, "CMD");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleUtil.ErrorOccured(ex.Message + " - " + ex.StackTrace, "command_main");
                if (data.Platform == Platforms.Twitch)
                {
                    TwitchMessageSendData SendData = new()
                    {
                        Message = TranslationManager.GetTranslation("ru", "error", data.ChannelID),
                        Channel = data.Channel,
                        ChannelID = data.ChannelID,
                        AnswerID = data.TWargs.Command.ChatMessage.Id,
                        Lang = data.User.Lang,
                        Name = data.User.Name,
                        IsSafeExecute = true,
                        NickNameColor = ChatColorPresets.Red
                    };
                    butterBib.Commands.SendCommandReply(SendData);
                }
                else if (data.Platform == Platforms.Discord)
                {
                    DiscordCommandSendData SendData = new()
                    {
                        Message = "",
                        Description = TranslationManager.GetTranslation("ru", "error", data.ChannelID),
                        IsEmbed = true,
                        Color = (Discord.Color?)Color.Red,
                        Ephemeral = true,
                        Server = data.Channel,
                        ServerID = data.ChannelID,
                        Lang = data.User.Lang,
                        IsSafeExecute = true,
                        d = data.d
                    };
                    butterBib.Commands.SendCommandReply(SendData);
                }
            }
        }
    }
}