using butterBror.Utils;
using butterBror.Utils.DataManagers;
using butterBib;
using Discord;
using TwitchLib.Client.Enums;

namespace butterBror
{
    public partial class Commands
    {
        public class Restart
        {
            public static CommandInfo Info = new()
            {
                Name = "Restart",
                Author = "@ItzKITb",
                AuthorURL = "twitch.tv/itzkitb",
                AuthorImageURL = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                Description = "С помощью этой команды можно перезагрузить бота.",
                UseURL = "https://itzkitb.ru/bot/command?name=restart",
                UserCooldown = 1,
                GlobalCooldown = 1,
                aliases = ["restart", "reload", "перезагрузить", "рестарт"],
                ArgsRequired = "(Нету)",
                ResetCooldownIfItHasNotReachedZero = false,
                CreationDate = DateTime.Parse("07/04/2024"),
                ForAdmins = true,
                ForBotCreator = true,
                ForChannelAdmins = false,
                AllowedPlatforms = [Platforms.Twitch, Platforms.Telegram, Platforms.Discord]
            };
            public static CommandReturn Index(CommandData data)
            {
                try
                {
                    string resultMessage = "";
                    if (UsersData.UserGetData<bool>(data.UserUUID, "isBotModerator") || UsersData.UserGetData<bool>(data.UserUUID, "isBotDev"))
                    {
                        resultMessage = "❄ Перезагрузка...";
                        Bot.Restart();
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
                        Color = Color.Blue,
                        NickNameColor = TwitchLib.Client.Enums.ChatColorPresets.DodgerBlue
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
