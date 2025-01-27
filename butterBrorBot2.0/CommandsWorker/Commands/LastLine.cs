using butterBror.Utils;
using butterBror.Utils.DataManagers;
using butterBib;
using Discord;
using TwitchLib.Client.Enums;

namespace butterBror
{
    public partial class Commands
    {
        public class LastLine
        {
            public static CommandInfo Info = new()
            {
                Name = "LastLine",
                Author = "@ItzKITb",
                AuthorURL = "twitch.tv/itzkitb",
                AuthorImageURL = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                Description = "Эта команда позволяет узнать время и содержание последнего сообщения пользователя в канале, откуда эта команды была выполнена.",
                UseURL = "https://itzkitb.ru/bot/command?name=ll",
                UserCooldown = 10,
                GlobalCooldown = 1,
                aliases = ["ll", "lastline", "пс", "последнеесообщение"],
                ArgsRequired = "[Имя пользователя]",
                ResetCooldownIfItHasNotReachedZero = true,
                CreationDate = DateTime.Parse("07/04/2024"),
                ForAdmins = false,
                ForBotCreator = false,
                ForChannelAdmins = false,
                AllowedPlatforms = [Platforms.Twitch, Platforms.Telegram, Platforms.Discord]
            };
            public static CommandReturn Index(CommandData data)
            {
                try
                {
                    string resultMessage = "";
                    Color resultColor = Color.Green;
                    ChatColorPresets resultNicknameColor = ChatColorPresets.YellowGreen;
                    string resultMessageTitle = TranslationManager.GetTranslation(data.User.Lang, "dsLLTitle", data.ChannelID);
                    if (data.args.Count != 0)
                    {
                        var name = TextUtil.NicknameFilter(data.args.ElementAt(0).ToLower());
                        var userID = NamesUtil.GetUserID(name);
                        var message = MessagesWorker.GetMessage(data.ChannelID, userID);
                        var bages = "";
                        if (message != null)
                        {
                            if (userID != "err")
                            {
                                if (name != Bot.Client.TwitchUsername.ToLower())
                                {
                                    if (name == data.User.Name.ToLower())
                                    {
                                        resultMessage = TranslationManager.GetTranslation(data.User.Lang, "youRightThere", data.ChannelID);
                                    }
                                    else
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
                                        resultMessage = TranslationManager.GetTranslation(data.User.Lang, "lastMessage", data.ChannelID)
                                            .Replace("&timeAgo&", TextUtil.FormatTimeSpan(FormatUtil.GetTimeTo(message.messageDate, DateTime.Now, false), data.User.Lang))
                                            .Replace("%message%", message.messageText).Replace("%bages%", bages)
                                            .Replace("%user%", NamesUtil.DontPingUsername(NamesUtil.GetUsername(userID, data.User.Name)));
                                    }
                                }
                                else
                                {
                                    resultMessage = TranslationManager.GetTranslation(data.User.Lang, "lastMessageWait", data.ChannelID);
                                }
                            }
                            else
                            {
                                resultMessage = TranslationManager.GetTranslation(data.User.Lang, "noneExistUser", data.ChannelID)
                                    .Replace("%user%", NamesUtil.DontPingUsername(name));
                                resultMessageTitle = TranslationManager.GetTranslation(data.User.Lang, "Err", data.ChannelID);
                                resultColor = Color.Red;
                                resultNicknameColor = ChatColorPresets.Red;
                            }
                        }
                    }
                    else
                    {
                        resultMessage = TranslationManager.GetTranslation(data.User.Lang, "youRightThere", data.ChannelID);
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
                catch (Exception e)
                {
                    return new()
                    {
                        Message = "",
                        IsSafeExecute = false,
                        Description = "",
                        Author = "",
                        ImageURL = "",
                        ThumbnailUrl = "",
                        Footer = "",
                        IsEmbed = true,
                        Ephemeral = false,
                        Title = "",
                        Color = Color.Green,
                        NickNameColor = ChatColorPresets.YellowGreen,
                        IsError = true,
                        Error = e
                    };
                }
            }
        }
    }
}