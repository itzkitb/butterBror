using butterBror.Utils;
using Discord;
using TwitchLib.Client.Enums;
using butterBror.Utils.Tools;
using butterBror.Utils.Types;

namespace butterBror
{
    public partial class Commands
    {
        public class Autumn
        {
            public static CommandInfo Info = new()
            {
                Name = "Autumn",
                Author = "@ItzKITb",
                AuthorLink = "twitch.tv/itzkitb",
                AuthorAvatar = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                Description = new() { 
                    { "ru", "С помощью этой команды вы можете узнать, сколько времени осталось до начала или конца осени" }, 
                    { "en", "With this command you can find out how much time is left until the beginning or end of autumn" } 
                },
                WikiLink = "https://itzkitb.lol/bot/command?q=autumn",
                CooldownPerUser = 120,
                CooldownPerChannel = 10,
                Aliases = ["autumn", "a", "осень", "fall"],
                Arguments = string.Empty,
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
                    DateTime startDate = new(2000, 9, 1);
                    DateTime endDate = new(2000, 12, 1);
                    commandReturn.SetMessage(Text.TimeTo(startDate, endDate, "autumn", 0, data.User.Language, data.ArgumentsString, data.ChannelID, data.Platform));
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
