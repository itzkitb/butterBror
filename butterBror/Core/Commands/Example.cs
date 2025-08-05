using butterBror.Models;

namespace butterBror.Core.Commands
{
    public partial class Commands
    {
        public class Example
        {
            public static CommandInfo Info = new()
            {
                Name = "",
                Author = "",
                AuthorLink = "",
                AuthorAvatar = "",
                Description = new() {
                    { "ru-RU", "" },
                    { "en-US", "" }
                },
                WikiLink = "https://itzkitb.lol/bot/command?q=",
                CooldownPerUser = 0,
                CooldownPerChannel = 0,
                Aliases = ["", "", "", ""],
                Arguments = "",
                CooldownReset = false,
                CreationDate = DateTime.Parse("01/01/2000"),
                IsForBotModerator = false,
                IsForBotDeveloper = false,
                IsForChannelModerator = false,
                Platforms = [PlatformsEnum.Twitch, PlatformsEnum.Telegram, PlatformsEnum.Discord]
            };
            public CommandReturn Index(CommandData data)
            {
                Engine.Statistics.FunctionsUsed.Add();
                CommandReturn commandReturn = new CommandReturn();

                try
                {

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