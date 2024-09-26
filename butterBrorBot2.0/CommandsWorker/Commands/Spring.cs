using butterBib;
using butterBror.Utils;
using Discord;

namespace butterBror
{
    public partial class Commands
    {
        public class Spring
        {
            public static CommandInfo Info = new()
            {
                Name = "Spring",
                Author = "@ItzKITb",
                AuthorURL = "twitch.tv/itzkitb",
                AuthorImageURL = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                Description = "С помощью этой команды вы можете узнать, сколько времени осталось до начала или конца весны.",
                UseURL = "https://itzkitb.ru/bot_command/spring",
                UserCooldown = 120,
                GlobalCooldown = 10,
                aliases = ["spring", "sp", "весна"],
                ArgsRequired = "(Никнейм)",
                ResetCooldownIfItHasNotReachedZero = false,
                CreationDate = DateTime.Parse("07/04/2024"),
                ForAdmins = false,
                ForBotCreator = false,
                ForChannelAdmins = false
            };
            public static CommandReturn Index(CommandData data)
            {
                DateTime startDate = new(2000, 3, 1);
                DateTime endDate = new(2000, 6, 1);
                string result = TextUtil.TimeTo(startDate, endDate, "Spring", 0, data.User.Lang, data.ArgsAsString, data.ChannelID);
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
                    Title = TranslationManager.GetTranslation(data.User.Lang, "dsSpringTitle", data.ChannelID),
                    Color = Color.Orange,
                    NickNameColor = TwitchLib.Client.Enums.ChatColorPresets.Coral
                };
            }
        }
    }
}
