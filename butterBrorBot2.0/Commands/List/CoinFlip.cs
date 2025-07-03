using butterBror.Utils;
using Discord;
using TwitchLib.Client.Enums;
using butterBror.Utils.Tools;
using butterBror.Utils.Types;

namespace butterBror
{
    public partial class Commands
    {
        public class Coinflip
        {
            public static CommandInfo Info = new()
            {
                Name = "CoinFlip",
                Author = "@ItzKITb",
                AuthorLink = "twitch.tv/itzkitb",
                AuthorAvatar = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                Description = new()
                {
                    { "ru", "Подкинь монетку!" },
                    { "en", "Flip a coin!" }
                },
                WikiLink = "https://itzkitb.lol/bot/command?q=coinflip",
                CooldownPerUser = 5,
                CooldownPerChannel = 1,
                Aliases = ["coin", "coinflip", "орелилирешка", "оир", "монетка", "headsortails", "hot", "орел", "решка", "heads", "tails"],
                Arguments = string.Empty,
                CooldownReset = true,
                CreationDate = DateTime.Parse("08/08/2024"),
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
                    int coin = new Random().Next(1, 3);
                    if (coin == 1)
                    {
                        commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "symbol:coin", data.ChannelID, data.Platform) + TranslationManager.GetTranslation(data.User.Language, "command:coinflip:heads", data.ChannelID, data.Platform));
                    }
                    else
                    {
                        commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "symbol:coin", data.ChannelID, data.Platform) + TranslationManager.GetTranslation(data.User.Language, "command:coinflip:tails", data.ChannelID, data.Platform));
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