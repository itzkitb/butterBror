using TwitchLib.Client.Enums;
using Discord;
using butterBib;

namespace butterBror
{
    public partial class Commands
    {
        public class Say
        {
            public static CommandInfo Info = new()
            {
                Name = "Say",
                Author = "@ItzKITb",
                AuthorURL = "twitch.tv/itzkitb",
                AuthorImageURL = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                Description = "При помощи этой администратор бота может писать сообщения от лица бота.",
                UseURL = "https://itzkitb.ru/bot/command?name=say",
                UserCooldown = 5,
                GlobalCooldown = 1,
                aliases = ["say", "tell", "сказать", "type", "написать"],
                ArgsRequired = "[Текст]",
                ResetCooldownIfItHasNotReachedZero = true,
                CreationDate = DateTime.Parse("09/07/2024"),
                ForAdmins = false,
                ForBotCreator = true,
                ForChannelAdmins = false,
                AllowedPlatforms = [Platforms.Twitch, Platforms.Telegram]
            };
            public static CommandReturn Index(CommandData data)
            {
                try
                {
                    string resultMessage = data.ArgsAsString;
                    Color resultColor = Color.Green;
                    ChatColorPresets resultNicknameColor = ChatColorPresets.YellowGreen;

                    return new()
                    {
                        Message = resultMessage,
                        IsSafeExecute = true,
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