using butterBror.Utils;
using butterBror.Utils.DataManagers;
using butterBib;
using Discord;
using TwitchLib.Client.Enums;

namespace butterBror
{
    public partial class Commands
    {
        public class LastGlobalLine
        {
            public static CommandInfo Info = new()
            {
                Name = "LastGlobalLine",
                Author = "@ItzKITb",
                AuthorURL = "twitch.tv/itzkitb",
                AuthorImageURL = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                Description = "Эта команда позволяет узнать время и содержание самого последнего сообщения пользователя.",
                UseURL = "https://itzkitb.ru/bot/command?name=lgl",
                UserCooldown = 10,
                GlobalCooldown = 1,
                aliases = ["lgl", "lastgloballine", "пгс", "последнееглобальноесообщение"],
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
                    string resultMessageTitle = TranslationManager.GetTranslation(data.User.Lang, "dsLGLTitle", data.ChannelID);
                    if (data.args.Count != 0)
                    {
                        var name = TextUtil.NicknameFilter(data.args.ElementAt(0).ToLower());
                        var userID = NamesUtil.GetUserID(name);
                        if (userID == "err")
                        {
                            resultMessage = TranslationManager.GetTranslation(data.User.Lang, "noneExistUser", data.ChannelID)
                                .Replace("%user%", NamesUtil.DontPingUsername(name));
                            resultMessageTitle = TranslationManager.GetTranslation(data.User.Lang, "Err", data.ChannelID);
                            resultColor = Color.Red;
                            resultNicknameColor = ChatColorPresets.Red;
                        }
                        else
                        {
                            var lastLine = UsersData.UserGetData<string>(userID, "lastSeenMessage");
                            var lastLineDate = UsersData.UserGetData<DateTime>(userID, "lastSeen");
                            DateTime now = DateTime.UtcNow;
                            if (name == Bot.Client.TwitchUsername.ToLower())
                            {
                                resultMessage = TranslationManager.GetTranslation(data.User.Lang, "lastGlobalLineWait", data.ChannelID);
                            }
                            else if (name == data.User.Name.ToLower())
                            {
                                resultMessage = TranslationManager.GetTranslation(data.User.Lang, "youRightThere", data.ChannelID);
                            }
                            else
                            {
                                resultMessage = TranslationManager.GetTranslation(data.User.Lang, "lastGlobalLine", data.ChannelID)
                                    .Replace("%user%", NamesUtil.DontPingUsername(NamesUtil.GetUsername(userID, data.User.Name)))
                                    .Replace("&timeAgo&", TextUtil.FormatTimeSpan(FormatUtil.GetTimeTo(lastLineDate, now, false), data.User.Lang))
                                    .Replace("%message%", lastLine);
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
                        Title = "",
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
