using butterBror.Utils;
using Discord;
using TwitchLib.Client.Enums;
using butterBror.Utils.Tools;
using butterBror.Utils.Types;

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
                AuthorLink = "twitch.tv/itzkitb",
                AuthorAvatar = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                Description = new() { 
                    { "ru", "Грязь, аллергия и лужи - мое любимое" }, 
                    { "en", "Dirt, allergies and puddles are my favorite" } 
                },
                WikiLink = "https://itzkitb.lol/bot/command?q=spring",
                CooldownPerUser = 120,
                CooldownPerChannel = 10,
                Aliases = ["spring", "sp", "весна"],
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
                Engine.Statistics.FunctionsUsed.Add();
                CommandReturn commandReturn = new CommandReturn();

                try
                {
                    DateTime startDate = new(2000, 3, 1);
                    DateTime endDate = new(2000, 6, 1);
                    commandReturn.SetMessage(Text.TimeTo(startDate, endDate, "spring", 0, data.User.Language, data.ArgumentsString, data.ChannelID, data.Platform));
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
