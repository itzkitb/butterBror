using static butterBror.BotWorker;
using butterBib;
using Discord;
using Discord.Rest;

namespace butterBror
{
    public partial class Commands
    {
        public class Winter
        {
            public static CommandInfo Info = new()
            {
                Name = "Winter",
                Author = "@ItzKITb",
                AuthorURL = "twitch.tv/itzkitb",
                AuthorImageURL = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                Description = "С помощью этой команды вы можете узнать, сколько времени осталось до начала или конца зимы.",
                UseURL = "https://itzkitb.ru/bot_command/winter",
                UserCooldown = 120,
                GlobalCooldown = 10,
                aliases = ["winter", "w", "зима"],
                ArgsRequired = "(Никнейм)",
                ResetCooldownIfItHasNotReachedZero = false,
                CreationDate = DateTime.Parse("07/04/2024"),
                ForAdmins = false,
                ForBotCreator = false,
                ForChannelAdmins = false
            };
            public static CommandReturn Index(CommandData data)
            {
                DateTime startDate = new(2000, 12, 1);
                DateTime endDate = new(2000, 3, 1);
                string result = Tools.TimeTo(startDate, endDate, "Winter", 1, data.User.Lang, data.ArgsAsString, data.ChannelID);
                return new()
                {
                    Message = result,
                    IsSafeExecute = false,
                    Description = "",
                    Author = "",
                    ImageURL = "",
                    ThumbnailUrl = "",
                    Footer = "",
                    IsEmbed = true,
                    Ephemeral = false,
                    Title = TranslationManager.GetTranslation(data.User.Lang, "dsWinterTitle", data.ChannelID),
                    Color = Color.Blue,
                    NickNameColor = TwitchLib.Client.Enums.ChatColorPresets.DodgerBlue
                };
            }
        }
    }
}
