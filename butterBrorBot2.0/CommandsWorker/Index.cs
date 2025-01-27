using Discord.WebSocket;
using TwitchLib.Client.Events;
using butterBib;
using System.Drawing;
using System.Reflection;
using TwitchLib.Client.Enums;
using butterBror.Utils;
using butterBror.Utils.DataManagers;
using System;
using Telegram.Bot.Types;

namespace butterBror
{
    public partial class Commands
    {
        public static void IndexCommands()
        {
            ConsoleUtil.LOG($"Indexing commands...", "main");
            foreach (var classType in classes)
            {
                try
                {
                    var infoProperty = classType.GetField("Info", BindingFlags.Static | BindingFlags.Public);
                    var info = infoProperty.GetValue(null) as CommandInfo;
                    commandsIndex.Add(info);
                }
                catch (Exception ex) 
                {
                    ConsoleUtil.LOG($"[COMMAND_INDEXER] INDEX ERROR FOR CLASS {classType.Name}: {ex.Message}", "main");
                }
            }
            ConsoleUtil.LOG($"Indexed!", "main");
        }
        public static Dictionary<string, int> ErrorsInCommands = new();
        public static List<CommandInfo> commandsIndex = new();
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
            typeof(Eightball),
            typeof(Coinflip),
            typeof(Percent),
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

        public static void TelegramCommand(Message message)
        {
            try
            {
                UserData user = new()
                {
                    Id = "tg" + message.From.Id.ToString(),
                    Lang = "ru",
                    Name = message.From.Username ?? message.From.FirstName,
                    IsChannelAdmin = false,
                    IsChannelBroadcaster = message.From.Id == message.Chat.Id
                };

                Guid RequestUuid = Guid.NewGuid();
                Guid CommandExecutionUuid = Guid.NewGuid();
                string RequestUuidString = RequestUuid.ToString();

                CommandData data = new()
                {
                    Name = message.Text.ToLower().Split(' ')[0],
                    RequestUUID = RequestUuidString,
                    UserUUID = message.From.Id.ToString(),
                    args = message.Text.Split(' ').Skip(1).ToList(),
                    ArgsAsString = string.Join(" ", message.Text.Split(' ').Skip(1)),
                    Channel = message.Chat.Title ?? message.Chat.Username ?? message.Chat.Id.ToString(),
                    ChannelID = "tg" + message.Chat.Id.ToString(),
                    MessageID = message.MessageId.ToString(),
                    Platform = Platforms.Telegram,
                    User = user,
                    CommandInstanceUUID = CommandExecutionUuid.ToString(),
                    TelegramReply = message
                };

                // Если есть ответ на сообщение, добавляем его аргументы
                if (message.ReplyToMessage != null)
                {
                    string[] trimmedReplyText = message.ReplyToMessage.Text.Split(' ');
                    data.args.AddRange(trimmedReplyText);
                    data.ArgsAsString += " " + string.Join(" ", trimmedReplyText);
                }

                Command(data);
            }
            catch (Exception ex)
            {
                ConsoleUtil.ErrorOccured(ex, $"Command\\TelegramCommand#Command:{message.Text}\\FullMessage:{message}");
            }
        }
        public static async void Command(CommandData data)
        {
            try
            {
                bool allowed = true;
                if (data.Platform == Platforms.Twitch)
                    allowed = !data.TWargs.Command.ChatMessage.IsMe;

                if (allowed)
                {
                    string lang = "ru";
                    try
                    {
                        string Id = "";
                        if (data.Platform == Platforms.Twitch)
                            Id = data.UserUUID;
                        else if (data.Platform == Platforms.Discord)
                            Id = "ds" + data.UserUUID;

                        if (UsersData.UserGetData<string>(Id, "language") == default)
                            UsersData.UserSaveData(Id, "language", "ru");
                        else
                            lang = UsersData.UserGetData<string>(Id, "language");
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
                    string[] help = ["help", "sos", "info", "помощь", "информация", "info", "инфо"];
                    var command = TextUtil.FilterCommand(data.Name).Replace("ё", "е");

                    if (!(bool)data.User.IsBanned && !(bool)data.User.IsIgnored)
                    {
                        bool isCommandFound = false;
                        int num = 0;
                        CommandReturn? cmdReturn = null;
                        foreach (var info in commandsIndex)
                        {
                            if (info.aliases.Contains(command))
                            {
                                isCommandFound = true;
                                if (info.ForBotCreator && !(bool)data.User.IsBotCreator || info.ForAdmins && !(bool)data.User.IsBotAdmin || info.ForChannelAdmins && !(bool)data.User.IsChannelAdmin) break;
                                if (CommandUtil.IsNotOnCooldown(info.UserCooldown, info.GlobalCooldown, info.Name, data.User.Id, data.ChannelID, info.ResetCooldownIfItHasNotReachedZero))
                                {
                                    CommandUtil.ExecutedCommand(data);
                                    var indexMethod = classes[num].GetMethod("Index", BindingFlags.Static | BindingFlags.Public);

                                    try
                                    {
                                        if (indexMethod.ReturnType == typeof(CommandReturn)) cmdReturn = indexMethod.Invoke(null, [data]) as CommandReturn;
                                        else if (indexMethod.ReturnType == typeof(Task<CommandReturn>))cmdReturn = await (indexMethod.Invoke(null, [data]) as Task<CommandReturn>);
                                        else throw new InvalidOperationException($"Method '{info.Name}' returned an incorrect result type: {indexMethod.ReturnType.Name}");
                                    }
                                    catch (Exception ex)
                                    {
                                        throw new ApplicationException($"Failed to execute command: \nError: {ex.Message}\nStack: {ex.StackTrace}\nError Location: {ex.Source}\nCommand: {info.Name}\nCommand Arguments: {data.ArgsAsString}\nUser who executed the command: {data.User.Name} (#{data.UserUUID})");
                                    }
                                    if (cmdReturn != null)
                                    {
                                        if (cmdReturn.IsError) throw new ApplicationException($"The command failed: \nError: {cmdReturn.Error.Message}\nStack: {cmdReturn.Error.StackTrace}\nError location: {cmdReturn.Error.Source}\nCommand: {info.Name}\nCommand arguments: {data.ArgsAsString}\nUser who executed the command: {data.User.Name} (#{data.UserUUID})");
                                        else
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
                                                    Color = (Discord.Color?)cmdReturn.Color,
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
                                            else if (data.Platform == Platforms.Telegram)
                                            {
                                                TelegramMessageSendData SendData = new()
                                                {
                                                    Message = cmdReturn.Message,
                                                    Lang = data.User.Lang,
                                                    IsSafeExecute = cmdReturn.IsSafeExecute,
                                                    Channel = data.Channel,
                                                    ChannelID = data.ChannelID,
                                                    Answer = data.TelegramReply,
                                                    Name = data.Name
                                                };
                                                butterBib.Commands.SendCommandReply(SendData);
                                            }

                                            if (info.Cost != null)
                                            {
                                                BalanceUtil.SaveBalance(data.UserUUID, -(int)(info.Cost), 0);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        string message = $"Empty response from command!\nCommand: {info.Name},\nCommand arguments: {data.ArgsAsString},\nUser who executed the command: {data.User.Name} (#{data.UserUUID})";
                                        ConsoleUtil.LOG(message.Replace("\n", ""), "info");
                                        LogWorker.Log(message, LogWorker.LogTypes.Warn, $"cmd\\{info.Name}");
                                    }
                                }
                                break;
                            }
                            num++;
                        }
                        if (!isCommandFound)
                        {
                            string message = $"Command not found!\nCommand: {command},\nCommand arguments: {data.ArgsAsString},\nUser who searched for the command: {data.User.Name} (#{data.UserUUID})";
                            ConsoleUtil.LOG(message.Replace("\n", ""), "info");
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
                else if (data.Platform == Platforms.Telegram)
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
                        Color = (Discord.Color?)System.Drawing.Color.Red,
                        Ephemeral = true,
                        Server = data.Channel,
                        ServerID = data.ChannelID,
                        Lang = data.User.Lang,
                        IsSafeExecute = true,
                        d = data.d
                    };
                    butterBib.Commands.SendCommandReply(SendData);
                }
                else if (data.Platform == Platforms.Telegram)
                {
                    TelegramMessageSendData SendData = new()
                    {
                        Message = TranslationManager.GetTranslation("ru", "error", data.ChannelID),
                        Lang = data.User.Lang,
                        IsSafeExecute = true,
                        Channel = data.Channel,
                        ChannelID = data.ChannelID,
                        Answer = data.TelegramReply,
                        Name = data.Name
                    };
                    butterBib.Commands.SendCommandReply(SendData);
                }
            }
        }
    }
}