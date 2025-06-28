using butterBror.Utils;
using butterBror;
using Discord;
using TwitchLib.Client.Enums;
using butterBror.Utils.Tools;

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
                    if (Utils.Tools.Balance.GetBalance(data.user_id, data.platform) > 4)
                    {
                        if (win == 1)
                        {
                            // WIN
                            translationParam += "win:" + page2;
                            Utils.Tools.Balance.Add(data.user_id, 1, 0, data.platform);
                        }
                        else
                        {
                            // GAME OVER
                            translationParam += "over:" + page2;
                            if (page2 == 4)
                            {
                                Utils.Tools.Balance.Add(data.user_id, -1, 0, data.platform);
                            }
                            else
                            {
                                Utils.Tools.Balance.Add(data.user_id, -5, 0, data.platform);
                            }
                            commandReturn.SetColor(ChatColorPresets.Red);
                        }
                        commandReturn.SetMessage("🔫 " + TranslationManager.GetTranslation(data.user.language, translationParam, data.channel_id, data.platform));
                    }
                    else
                    {
                        commandReturn.SetMessage(TranslationManager.GetTranslation(data.user.language, "error:roulette_not_enough_coins", data.channel_id, data.platform)
                            .Replace("%balance%", Utils.Tools.Balance.GetBalance(data.user_id, data.platform).ToString()));
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
