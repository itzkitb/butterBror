using butterBror.Utils;
using butterBib;
using Discord;
using TwitchLib.Client.Enums;

namespace butterBror
{
    public partial class Commands
    {
        public class Summer
        {
            public static CommandInfo Info = new()
            {
                Name = "Summer",
                Author = "@ItzKITb",
                AuthorURL = "twitch.tv/itzkitb",
                AuthorImageURL = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                Description = "С помощью этой команды вы можете узнать, сколько времени осталось до начала или конца лета.",
                UseURL = "https://itzkitb.ru/bot/command?name=summer",
                UserCooldown = 120,
                GlobalCooldown = 10,
                aliases = ["summer", "su", "лето"],
                ArgsRequired = "(Никнейм)",
                ResetCooldownIfItHasNotReachedZero = false,
                CreationDate = DateTime.Parse("07/04/2024"),
                ForAdmins = false,
                ForBotCreator = false,
                ForChannelAdmins = false
            };
            public static CommandReturn Index(CommandData data)
            {
                try
                {
                    DateTime startDate = new(2000, 6, 1);
                    DateTime endDate = new(2000, 9, 1);
                    string result = TextUtil.TimeTo(startDate, endDate, "Summer", 0, data.User.Lang, data.ArgsAsString, data.ChannelID);
                    return new()
                    {
                        Message = result,
                        IsSafeExecute = true,
                        Description = "",
                        Author = "",
                        ImageURL = "",
                        ThumbnailUrl = "",
                        Footer = "",
                        IsEmbed = true,
                        Ephemeral = false,
                        Title = TranslationManager.GetTranslation(data.User.Lang, "dsSummerTitle", data.ChannelID),
                        Color = Color.Green,
                        NickNameColor = TwitchLib.Client.Enums.ChatColorPresets.YellowGreen
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
