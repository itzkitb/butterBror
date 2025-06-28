using butterBror.Utils;
using butterBror;
using Discord;
using TwitchLib.Client.Enums;
using butterBror.Utils.Tools;

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
                AuthorLink = "twitch.tv/itzkitb",
                AuthorAvatar = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                Description = new() {
                    { "ru", "В Якутске средняя температура зимой: −38,6°C" },
                    { "en", "In Yakutsk, the average temperature in winter is -38.6°C" }
                },
                WikiLink = "https://itzkitb.ru/bot/command?name=winter",
                CooldownPerUser = 120,
                CooldownPerChannel = 10,
                Aliases = ["winter", "w", "зима"],
                Arguments = "(name)",
                CooldownReset = false,
                CreationDate = DateTime.Parse("07/04/2024"),
                IsForBotModerator = false,
                IsForBotDeveloper = false,
                IsForChannelModerator = false,
                Platforms = [Platforms.Twitch, Platforms.Telegram, Platforms.Discord]
            };
            public CommandReturn Index(CommandData data)
            {
                Core.Statistics.FunctionsUsed.Add();
                CommandReturn commandReturn = new CommandReturn();

                try
                {
                    DateTime startDate = new(2000, 12, 1);
                    DateTime endDate = new(2000, 3, 1);
                    commandReturn.SetMessage(Text.TimeTo(startDate, endDate, "Winter", 1, data.user.language, data.arguments_string, data.channel_id, data.platform));
                }
                catch (Exception e)
                {
                    commandReturn.SetError(e);
                }

                return commandReturn;
            }
        }
    }
}
