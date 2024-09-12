using static butterBror.BotWorker.FileMng;
using static butterBror.BotWorker;
using TwitchLib.Client.Events;
using butterBib;
using Discord;
using TwitchLib.Client.Enums;

namespace butterBror
{
    public partial class Commands
    {
        public class FirstLine
        {
            public static CommandInfo Info = new()
            {
                Name = "FirstLine",
                Author = "@ItzKITb",
                AuthorURL = "twitch.tv/itzkitb",
                AuthorImageURL = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                Description = "Эта команда позволяет узнать время и содержание первого сообщения пользователя в канале (учтите, что butterBror получает это из своей базы данных, поэтому первое сообщение может оказаться недавним).",
                UseURL = "https://itzkitb.ru/bot_command/fl",
                UserCooldown = 10,
                GlobalCooldown = 1,
                aliases = ["fl", "firstline", "прс", "первоесообщение"],
                ArgsRequired = "(Имя пользователя)",
                ResetCooldownIfItHasNotReachedZero = true,
                CreationDate = DateTime.Parse("07/04/2024"),
                ForAdmins = false,
                ForBotCreator = false,
                ForChannelAdmins = false
            };
            public static CommandReturn Index(CommandData data)
            {
                string resultMessage = "";
                Color resultColor = Color.Green;
                string resultMessageTitle = TranslationManager.GetTranslation(data.User.Lang, "dsFLTitle", data.ChannelID);
                ChatColorPresets resultNicknameColor = ChatColorPresets.YellowGreen;
                try
                {
                    if (data.args.Count != 0)
                    {
                        var name = Tools.NicknameFilter(data.args.ElementAt(0).ToLower());
                        var userID = Tools.GetUserID(name);

                        if (userID == "err")
                        {
                            resultMessage = TranslationManager.GetTranslation(data.User.Lang, "noneExistUser", data.ChannelID)
                                .Replace("%user%", Tools.DontPingUsername(name));
                            resultMessageTitle = TranslationManager.GetTranslation(data.User.Lang, "Err", data.ChannelID);
                            resultColor = Color.Red;
                            resultNicknameColor = ChatColorPresets.Red;
                        }
                        else
                        {
                            var message = MessagesWorker.GetMessage(data.ChannelID, userID, -1);
                            var bages = "";
                            if (message != null)
                            {
                                if (message.isMe)
                                {
                                    bages += TranslationManager.GetTranslation(data.User.Lang, "isMe", data.ChannelID);
                                }
                                if (message.isVip)
                                {
                                    bages += TranslationManager.GetTranslation(data.User.Lang, "isVip", data.ChannelID);
                                }
                                if (message.isTurbo)
                                {
                                    bages += TranslationManager.GetTranslation(data.User.Lang, "isTurbo", data.ChannelID);
                                }
                                if (message.isModerator)
                                {
                                    bages += TranslationManager.GetTranslation(data.User.Lang, "isModerator", data.ChannelID);
                                }
                                if (message.isPartner)
                                {
                                    bages += TranslationManager.GetTranslation(data.User.Lang, "isPartner", data.ChannelID);
                                }
                                if (message.isStaff)
                                {
                                    bages += TranslationManager.GetTranslation(data.User.Lang, "isStaff", data.ChannelID);
                                }
                                if (message.isSubscriber)
                                {
                                    bages += TranslationManager.GetTranslation(data.User.Lang, "isSubscriber", data.ChannelID);
                                }
                                var Date = message.messageDate;
                                if (name != Bot.client.TwitchUsername.ToLower())
                                {
                                    if (name == data.User.Name)
                                    {
                                        resultMessage = TranslationManager.GetTranslation(data.User.Lang, "myFirstLine", data.ChannelID)
                                            .Replace("&timeAgo&", Tools.FormatTimeSpan(Tools.GetTimeTo(message.messageDate, DateTime.UtcNow, false), data.User.Lang))
                                            .Replace("%message%", message.messageText).Replace("%bages%", bages);
                                    }
                                    else
                                    {
                                        resultMessage = TranslationManager.GetTranslation(data.User.Lang, "firstLine", data.ChannelID)
                                            .Replace("&timeAgo&", Tools.FormatTimeSpan(Tools.GetTimeTo(message.messageDate, DateTime.UtcNow, false), data.User.Lang))
                                            .Replace("%message%", message.messageText)
                                            .Replace("%bages%", bages)
                                            .Replace("%user%", Tools.DontPingUsername(Tools.GetUsername(userID, data.User.Name)));
                                    }
                                }
                                else
                                {
                                    resultMessage = TranslationManager.GetTranslation(data.User.Lang, "firstLineWait", data.ChannelID);
                                }
                            }
                        }
                    }
                    else
                    {
                        var userID = data.UserUUID;
                        var message = MessagesWorker.GetMessage(data.ChannelID, userID, -1);
                        var bages = "";
                        if (message != null)
                        {
                            if (message.isMe)
                            {
                                bages += TranslationManager.GetTranslation(data.User.Lang, "isMe", data.ChannelID);
                            }
                            if (message.isVip)
                            {
                                bages += TranslationManager.GetTranslation(data.User.Lang, "isVip", data.ChannelID);
                            }
                            if (message.isTurbo)
                            {
                                bages += TranslationManager.GetTranslation(data.User.Lang, "isTurbo", data.ChannelID);
                            }
                            if (message.isModerator)
                            {
                                bages += TranslationManager.GetTranslation(data.User.Lang, "isModerator", data.ChannelID);
                            }
                            if (message.isPartner)
                            {
                                bages += TranslationManager.GetTranslation(data.User.Lang, "isPartner", data.ChannelID);
                            }
                            if (message.isStaff)
                            {
                                bages += TranslationManager.GetTranslation(data.User.Lang, "isStaff", data.ChannelID);
                            }
                            if (message.isSubscriber)
                            {
                                bages += TranslationManager.GetTranslation(data.User.Lang, "isSubscriber", data.ChannelID);
                            }
                            resultMessage = TranslationManager.GetTranslation(data.User.Lang, "myFirstLine", data.ChannelID)
                                .Replace("&timeAgo&", Tools.FormatTimeSpan(Tools.GetTimeTo(message.messageDate, DateTime.UtcNow, false), data.User.Lang))
                                .Replace("%message%", message.messageText).Replace("%bages%", bages);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Tools.ErrorOccured(ex.Message, "cmd4A");
                    resultMessage = TranslationManager.GetTranslation(data.User.Lang, "error", data.ChannelID);
                    resultMessageTitle = TranslationManager.GetTranslation(data.User.Lang, "Err", data.ChannelID);
                    resultColor = Color.Red;
                    resultNicknameColor = ChatColorPresets.Red;
                }

                return new()
                {
                    Message = resultMessage,
                    IsSafeExecute = false,
                    Description = "",
                    Author = "",
                    ImageURL = "",
                    ThumbnailUrl = "",
                    Footer = "",
                    IsEmbed = true,
                    Ephemeral = false,
                    Title = resultMessageTitle,
                    Color = resultColor,
                    NickNameColor = resultNicknameColor
                };
            }
        }
    }
}
