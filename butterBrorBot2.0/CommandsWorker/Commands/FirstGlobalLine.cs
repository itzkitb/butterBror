using butterBror.Utils;
using butterBror.Utils.DataManagers;
using butterBib;
using Discord;
using TwitchLib.Client.Enums;

namespace butterBror
{
    public partial class Commands
    {
        public class FirstGlobalLine
        {
            public static CommandInfo Info = new()
            {
                Name = "FirstGlobalLine",
                Author = "@ItzKITb",
                AuthorURL = "twitch.tv/itzkitb",
                AuthorImageURL = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                Description = "Эта команда позволяет узнать время и содержание первого сообщения пользователя на twitch (учтите, что butterBror получает это из своей базы данных, поэтому первое сообщение может оказаться недавним).",
                UseURL = "https://itzkitb.ru/bot_command/fgl",
                UserCooldown = 10,
                GlobalCooldown = 1,
                aliases = ["fgl", "firstgloballine", "пргс", "первоеглобальноесообщение"],
                ArgsRequired = "(Имя пользователя)",
                ResetCooldownIfItHasNotReachedZero = true,
                CreationDate = DateTime.Parse("07/04/2024"),
                ForAdmins = false,
                ForBotCreator = false,
                ForChannelAdmins = false
            };
            public static CommandReturn Index(CommandData data)
            {
                try
                {
                    ChatColorPresets resultNicknameColor = ChatColorPresets.YellowGreen;
                    string resultMessage = "";
                    Color resultColor = Color.Green;
                    string resultMessageTitle = TranslationManager.GetTranslation(data.User.Lang, "dsFGLTitle", data.ChannelID);
                    DateTime now = DateTime.UtcNow;
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
                            var firstLine = UsersData.UserGetData<string>(userID, "firstMessage");
                            var firstLineDate = UsersData.UserGetData<DateTime>(userID, "firstSeen");

                            if (name == Bot.client.TwitchUsername.ToLower())
                            {
                                resultMessage = TranslationManager.GetTranslation(data.User.Lang, "firstGlobalLineWait", data.ChannelID);
                            }
                            else if (name == data.User.Name)
                            {
                                resultMessage = TranslationManager.GetTranslation(data.User.Lang, "myFirstGlobalLine", data.ChannelID)
                                    .Replace("&timeAgo&", TextUtil.FormatTimeSpan(FormatUtil.GetTimeTo(firstLineDate, now, false), data.User.Lang))
                                    .Replace("%message%", firstLine);
                            }
                            else
                            {
                                resultMessage = TranslationManager.GetTranslation(data.User.Lang, "firstGlobalLine", data.ChannelID)
                                    .Replace("%user%", NamesUtil.DontPingUsername(NamesUtil.GetUsername(userID, data.User.Name)))
                                    .Replace("&timeAgo&", TextUtil.FormatTimeSpan(FormatUtil.GetTimeTo(firstLineDate, now, false), data.User.Lang))
                                    .Replace("%message%", firstLine);
                            }
                        }
                    }
                    else
                    {
                        var firstLine = UsersData.UserGetData<string>(data.UserUUID, "firstMessage");
                        var firstLineDate = UsersData.UserGetData<DateTime>(data.UserUUID, "firstSeen");

                        resultMessage = TranslationManager.GetTranslation(data.User.Lang, "myFirstGlobalLine", data.ChannelID)
                            .Replace("&timeAgo&", TextUtil.FormatTimeSpan(FormatUtil.GetTimeTo(firstLineDate, now, false), data.User.Lang))
                            .Replace("%message%", firstLine);
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
