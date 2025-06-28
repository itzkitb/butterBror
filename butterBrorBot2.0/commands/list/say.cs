using TwitchLib.Client.Enums;
using Discord;
using butterBror;

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
                AuthorLink = "twitch.tv/itzkitb",
                AuthorAvatar = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                Description = new() { 
                    { "ru", "Обернись" }, 
                    { "en", "Обернись" } 
                },
                WikiLink = "https://itzkitb.lol/bot/command?q=say",
                CooldownPerUser = 5,
                CooldownPerChannel = 1,
                Aliases = ["say", "tell", "сказать", "type", "написать"],
                Arguments = "[text]",
                CooldownReset = true,
                CreationDate = DateTime.Parse("09/07/2024"),
                IsForBotModerator = false,
                IsForBotDeveloper = true,
                IsForChannelModerator = false,
                Platforms = [Platforms.Twitch, Platforms.Telegram]
            };
            public CommandReturn Index(CommandData data)
            {
                Core.Statistics.FunctionsUsed.Add();
                CommandReturn commandReturn = new CommandReturn();

                try
                {
                    commandReturn.SetMessage(data.arguments_string);
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