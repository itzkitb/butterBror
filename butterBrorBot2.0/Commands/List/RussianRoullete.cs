using butterBror.Utils;
using Discord;
using TwitchLib.Client.Enums;
using butterBror.Utils.Tools;
using butterBror.Utils.Types;

namespace butterBror
{
    public partial class Commands
    {
        public class RussianRoullete
        {
            public static CommandInfo Info = new()
            {
                Name = "RussianRoullete",
                Author = "@ItzKITb",
                AuthorLink = "twitch.tv/itzkitb",
                AuthorAvatar = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                Description = new() { 
                    { "ru", "Mike Klubnika <3" },
                    { "en", "Mike Klubnika <3" } 
                },
                WikiLink = "https://itzkitb.lol/bot/command?q=rr",
                CooldownPerUser = 5,
                CooldownPerChannel = 1,
                Aliases = ["rr", "russianroullete", "русскаярулетка", "рр", "br", "buckshotroullete"],
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
                    int win = new Random().Next(1, 3);
                    int page2 = new Random().Next(1, 5);
                    string translationParam = "command:russian_roullete:";
                    if (Utils.Tools.Balance.GetBalance(data.UserID, data.Platform) > 4)
                    {
                        if (win == 1)
                        {
                            // WIN
                            translationParam += "win:" + page2;
                            Utils.Tools.Balance.Add(data.UserID, 1, 0, data.Platform);
                        }
                        else
                        {
                            // GAME OVER
                            translationParam += "over:" + page2;
                            if (page2 == 4)
                            {
                                Utils.Tools.Balance.Add(data.UserID, -1, 0, data.Platform);
                            }
                            else
                            {
                                Utils.Tools.Balance.Add(data.UserID, -5, 0, data.Platform);
                            }
                            commandReturn.SetColor(ChatColorPresets.Red);
                        }
                        commandReturn.SetMessage("🔫 " + TranslationManager.GetTranslation(data.User.Language, translationParam, data.ChannelID, data.Platform));
                    }
                    else
                    {
                        commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:roulette_not_enough_coins", data.ChannelID, data.Platform)
                            .Replace("%balance%", Utils.Tools.Balance.GetBalance(data.UserID, data.Platform).ToString()));
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
