using butterBror.Utils;
using Discord;
using TwitchLib.Client.Enums;
using butterBror.Utils.Types;

namespace butterBror
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
                    { "ru", "" },
                    { "en", "" }
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
                Platforms = [Platforms.Twitch, Platforms.Telegram, Platforms.Discord]
            };
            public CommandReturn Index(CommandData data)
            {
                Core.Statistics.FunctionsUsed.Add();
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