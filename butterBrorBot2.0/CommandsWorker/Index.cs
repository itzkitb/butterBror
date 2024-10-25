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
        private static void SaveСommandDataToTheRegistry(CommandData data)
        {

        }
        public static Dictionary<string, int> ErrorsInCommands = new();
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
            typeof(Dev),
            typeof(FrogGame)
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
                Guid CommandExecutionUuid = Guid.NewGuid();
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
                    TWargs = args,
                    CommandInstanceUUID = CommandExecutionUuid.ToString()
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
                ConsoleUtil.ErrorOccured(ex, $"Command\\TwitchCommand#Command:{args.Command.CommandText}\\FullMessage:{args.Command.ChatMessage}");
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
                    args = args,
                    CommandInstanceUUID = CommandExecutionUuid.ToString()
                };
                Command(data);
            }
            catch (Exception ex) 
            {
                ConsoleUtil.ErrorOccured(ex, $"Command\\DiscordCommand#Command:{dsCmd.CommandName}");
            }
        }
        public static async void Command(CommandData data)
        {
            try
            {
                bool allowed = true;
                if (data.Platform == Platforms.Twitch)
                {
                    allowed = !data.TWargs.Command.ChatMessage.IsMe;
                }
                if (allowed)
                {
                    string lang = "ru";
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
                        ConsoleUtil.ErrorOccured(ex, $"(NOTFATAL)Command\\Command\\GetLang#{data.UserUUID}");
                    }
                    data.User.Lang = lang;
                    data.User.IsBanned = UsersData.UserGetData<bool>(data.UserUUID, "isBanned");
                    data.User.IsIgnored = UsersData.UserGetData<bool>(data.UserUUID, "isIgnored");
                    data.User.IsBotAdmin = UsersData.UserGetData<bool>(data.UserUUID, "isBotModerator");
                    data.User.IsBotCreator = UsersData.UserGetData<bool>(data.UserUUID, "isBotDev");
                    Bot.CommandsActive = classes.Count;
                    string[] bot = ["bot", "bt", "бот", "бт"];
                    string[] help = ["help", "sos", "commands", "помощь", "информация", "info", "инфо"];
                    var command = TextUtil.FilterCommand(data.Name).Replace("ё", "е");

                    DebugUtil.LOG("Начало поиска комманды...");
                    if (!(bool)data.User.IsBanned && !(bool)data.User.IsIgnored)
                    {
                        bool isCommandFound = false;
                        int num = 0;
                        CommandReturn? cmdReturn = null;
                        DebugUtil.LOG("Поиск комманды...");
                        foreach (var classType in classes)
                        {
                            num++;
                            var infoProperty = classType.GetField("Info", BindingFlags.Static | BindingFlags.Public);
                            var info = infoProperty.GetValue(null) as CommandInfo;

                            if (info.aliases.Contains(command))
                            {
                                isCommandFound = true;
                                DebugUtil.LOG("Комманда найдена!");
                                if (info.ForBotCreator && !(bool)data.User.IsBotCreator || info.ForAdmins && !(bool)data.User.IsBotAdmin || info.ForChannelAdmins && !(bool)data.User.IsChannelAdmin)
                                {
                                    break;
                                }
                                DebugUtil.LOG("Проверка кулдауна...");
                                if (CommandUtil.IsNotOnCooldown(info.UserCooldown, info.GlobalCooldown, info.Name, data.User.Id, data.ChannelID, info.ResetCooldownIfItHasNotReachedZero))
                                {
                                    DebugUtil.LOG("Подготовка к выполнению...");
                                    CommandUtil.ExecutedCommand(data);
                                    var indexMethod = classType.GetMethod("Index", BindingFlags.Static | BindingFlags.Public);

                                    try
                                    {
                                        if (indexMethod.ReturnType == typeof(CommandReturn))
                                        {
                                            DebugUtil.LOG("Выполнение в синхронном режиме...");
                                            cmdReturn = indexMethod.Invoke(null, [data]) as CommandReturn;
                                        }
                                        else if (indexMethod.ReturnType == typeof(Task<CommandReturn>))
                                        {
                                            DebugUtil.LOG("Выполнение в асинхронном режиме...");
                                            var task = indexMethod.Invoke(null, [data]) as Task<CommandReturn>;
                                            cmdReturn = await task; // Используем await для асинхронного выполнения
                                        }
                                        else
                                        {
                                            throw new InvalidOperationException($"Метод '{info.Name}' вернул неверный тип результата: {indexMethod.ReturnType.Name}");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        throw new ApplicationException($"Не удалось выполнить команду: \nОшибка: {ex.Message}\nСтак: {ex.StackTrace}\nМесто ошибки: {ex.Source}\nКоманда: {info.Name}\nАргументы команды: {data.ArgsAsString}\nПользователь, который выполнил команду: {data.User.Name} (#{data.UserUUID})");
                                    }
                                    DebugUtil.LOG("Выполнено!");
                                    if (cmdReturn != null)
                                    {
                                        if (cmdReturn.IsError)
                                        {
                                            throw new ApplicationException($"Не удалось выполнить команду: \nОшибка: {cmdReturn.Error.Message}\nСтак: {cmdReturn.Error.StackTrace}\nМесто ошибки: {cmdReturn.Error.Source}\nКоманда: {info.Name}\nАргументы команды: {data.ArgsAsString}\nПользователь, который выполнил команду: {data.User.Name} (#{data.UserUUID})");
                                        }
                                        else
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
                                    }
                                    else
                                    {
                                        string message = $"Пустой ответ от комманды!\nКоманда: {info.Name},\nАргументы команды: {data.ArgsAsString},\nПользователь, который выполнил команду: {data.User.Name} (#{data.UserUUID})";
                                        ConsoleServer.SendConsoleMessage($"cmd\\{command}", message.Replace("\n", ""));
                                        LogWorker.Log(message, LogWorker.LogTypes.Warn, $"cmd\\{info.Name}");
                                    }
                                }
                                break;
                            }
                        }
                        if (!isCommandFound)
                        {
                            string message = $"Команда не найдена!\nКоманда: {command},\nАргументы команды: {data.ArgsAsString},\nПользователь, который искал команду: {data.User.Name} (#{data.UserUUID})";
                            ConsoleServer.SendConsoleMessage($"cmd\\{command}", message.Replace("\n", ""));
                            LogWorker.Log(message, LogWorker.LogTypes.Info, $"cmd\\{command}");
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
                            LogWorker.Log($"{data.Name} пытался выполнить команду, но он в игноре или бане (#{data.Name} {data.ArgsAsString})", LogWorker.LogTypes.Warn, $"command#{data.UserUUID}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleUtil.ErrorOccured(ex, $"command\\command#UserID:{data.UserUUID}\\Command:{data.Name}");
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