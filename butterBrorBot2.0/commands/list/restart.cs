using butterBror.Utils;
using butterBror.Utils.DataManagers;
using Discord;
using TwitchLib.Client.Enums;
using butterBror.Utils.Types;

namespace butterBror
{
    public partial class Commands
    {
        public class Restart
        {
            public static CommandInfo Info = new()
            {
                Name = "Restart",
                Author = "@ItzKITb",
                AuthorLink = "twitch.tv/itzkitb",
                AuthorAvatar = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                Description = new() { 
                    { "ru", "Этот маленький манёвр будет стоить нам 51 год" }, 
                    { "en", "This little maneuver will cost us 51 years" } 
                },
                WikiLink = "https://itzkitb.lol/bot/command?q=restart",
                CooldownPerUser = 1,
                CooldownPerChannel = 1,
                Aliases = ["restart", "reload", "перезагрузить", "рестарт"],
                Arguments = string.Empty,
                CooldownReset = false,
                CreationDate = DateTime.Parse("07/04/2024"),
                IsForBotModerator = true,
                IsForBotDeveloper = true,
                IsForChannelModerator = false,
                Platforms = [Platforms.Twitch, Platforms.Telegram, Platforms.Discord]
            };
            public CommandReturn Index(CommandData data)
            {
                Core.Statistics.FunctionsUsed.Add();
                CommandReturn commandReturn = new CommandReturn();

                try
                {
                    if (UsersData.Contains(data.UserID, "isBotModerator", data.Platform) || UsersData.Contains(data.UserID, "isBotDev", data.Platform))
                    {
                        commandReturn.SetMessage("❄ Перезагрузка...");
                        Core.Bot.Restart();
                    }
                    else
                    {
                        commandReturn.SetMessage("PauseChamp");
                    }
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
