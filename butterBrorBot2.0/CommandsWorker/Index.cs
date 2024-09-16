using Discord.WebSocket;
using TwitchLib.Client.Events;
using static butterBror.BotWorker.FileMng;
using static butterBror.BotWorker;
using butterBib;
using System.Drawing;
using System.Reflection;
using Discord.Rest;
using TwitchLib.Client.Enums;

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
            typeof(say),
            typeof(Help)
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
                Tools.ErrorOccured(ex.Message + " - " + ex.TargetSite, "twCMD");
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
                Tools.ErrorOccured(ex.Message, "dsCMD");
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
                        Tools.ErrorOccured(ex.Message, "cmd0A");
                    }
                    data.User.Lang = lang;
                    data.User.IsBanned = UsersData.UserGetData<bool>(data.UserUUID, "isBanned");
                    data.User.IsIgnored = UsersData.UserGetData<bool>(data.UserUUID, "isIgnored");
                    data.User.IsBotAdmin = UsersData.UserGetData<bool>(data.UserUUID, "isBotModerator");
                    data.User.IsBotCreator = UsersData.UserGetData<bool>(data.UserUUID, "isBotDev");
                    // Console.Write("1");
                    // Console.Write("2");
                    Bot.CommandsActive = classes.Count;
                    string[] bot = ["bot", "bt", "бот", "бт"];
                    string[] help = ["help", "sos", "commands", "помощь", "информация", "info", "инфо"];

                    string[] miningVideocard = ["mining", "mng", "манинг", "майн", "мнг"];
                    string[] location = ["location", "loc", "локация", "лок"];
                    string[] pizzas = ["pizza", "хуица", "пицца"];
                    string[] fishingAliases = ["fishing", "fish", "рыба", "рыбалка"];
                    var command = Tools.FilterCommand(data.Name).Replace("ё", "е");
                    // Console.Write("3");

                    if (!(bool)data.User.IsBanned && !(bool)data.User.IsIgnored)
                    {
                        // Console.Write("4 (");
                        int num = 0;
                        CommandReturn cmdReturn = null; // Инициализация переменной для хранения результата

                        foreach (var classType in classes)
                        {
                            num++;
                            // Получение значения статического свойства Info
                            var infoProperty = classType.GetField("Info", BindingFlags.Static | BindingFlags.Public);
                            var info = infoProperty.GetValue(null) as CommandInfo;

                            if (info.aliases.Contains(command))
                            {
                                if (info.ForBotCreator && !(bool)data.User.IsBotCreator)
                                {
                                    break;
                                }
                                if (info.ForAdmins && !(bool)data.User.IsBotAdmin)
                                {
                                    
                                    break;
                                }
                                if (info.ForChannelAdmins && !(bool)data.User.IsChannelAdmin)
                                {
                                    break;
                                }

                                if (Tools.IsNotOnCooldown(info.UserCooldown, info.GlobalCooldown, info.Name, data.User.Id, data.ChannelID, info.ResetCooldownIfItHasNotReachedZero))
                                {
                                    Tools.executedCommand(data);

                                    var indexMethod = classType.GetMethod("Index", BindingFlags.Static | BindingFlags.Public);
                                    cmdReturn = (CommandReturn)indexMethod.Invoke(null, [data]);

                                    if (data != null)
                                    {
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
                                    }
                                    else
                                    {
                                        ConsoleServer.SendConsoleMessage("commands", "Пустой ответ от комманды");
                                        LogWorker.LogWarning("Пустой ответ от комманды", $"commands/{info.Name}");
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
                            LogWorker.LogInfo($"{data.Name} попробовал выполнить команду, но был заигнорен или забанен (#{data.Name} {data.ArgsAsString})", "CMD");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Tools.ErrorOccured(ex.Message + " - " + ex.StackTrace, "CMD");
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