using butterBror.Utils;
using butterBib;
using Discord;
using TwitchLib.Client.Enums;

namespace butterBror
{
    public partial class Commands
    {
        public class Name
        {
            public static CommandInfo Info = new()
            {
                Name = "Name",
                Author = "@ItzKITb",
                AuthorURL = "twitch.tv/itzkitb",
                AuthorImageURL = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                Description = "Эта команда выводит ваш twitch ID или ID выбранного пользователя.",
                UseURL = "https://itzkitb.ru/bot/command?name=name",
                UserCooldown = 5,
                GlobalCooldown = 1,
                aliases = ["name", "nick", "nickname", "никнейм", "ник", "имя"],
                ArgsRequired = "[ID пользователя]",
                ResetCooldownIfItHasNotReachedZero = true,
                CreationDate = DateTime.Parse("25/10/2024"),
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
                    if (data.args.Count > 0)
                    {
                        string username = TextUtil.NicknameFilter(data.args[0].ToLower());
                        string ID = NamesUtil.GetUserID(username);
                        if (ID == data.UserUUID)
                        {
                            resultMessage = TranslationManager.GetTranslation(data.User.Lang, "IDYourSelfGet", data.ChannelID).Replace("%id%", data.UserUUID);
                        }
                        else if (ID == null)
                        {
                            resultMessage = TranslationManager.GetTranslation(data.User.Lang, "noneExistUser", data.ChannelID).Replace("%user%", username);
                            resultNicknameColor = ChatColorPresets.Red;
                            resultColor = Color.Red;
                        }
                        else
                        {
                            resultMessage = TranslationManager.GetTranslation(data.User.Lang, "IDUserGet", data.ChannelID).Replace("%id%", ID).Replace("%user%", NamesUtil.DontPingUsername(username));
                        }
                    }
                    else
                    {
                        resultMessage = TranslationManager.GetTranslation(data.User.Lang, "IDYourSelfGet", data.ChannelID).Replace("%id%", data.UserUUID);
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
                        IsEmbed = false,
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
