using butterBror.Utils;
using System.Drawing;
using TwitchLib.Client.Enums;
using System.Collections.Generic;
using butterBror.Utils.Tools;
using butterBror.Utils.Types;

namespace butterBror
{
    public partial class Commands
    {
        public class Roulette
        {
            public static CommandInfo Info = new()
            {
                Name = "Roulette",
                Author = "@ItzKITb",
                AuthorLink = "twitch.tv/itzkitb",
                AuthorAvatar = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                Description = new() { 
                    { "ru", "Сыграйте в рулетку! Подробности: https://bit.ly/bb_roulette " }, 
                    { "en", "Play Roulette! Details: https://bit.ly/bb_roulette " } 
                },
                WikiLink = "https://itzkitb.lol/bot/command?q=roulette",
                CooldownPerUser = 5,
                CooldownPerChannel = 1,
                Aliases = ["roulette", "r", "рулетка", "р"],
                Arguments = "[select: \"🟩\", \"🟥\", \"⬛\"] [bid]",
                CooldownReset = true,
                CreationDate = DateTime.Parse("29/01/2025"),
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
                    string[] selections = ["🟥", "🟩", "⬛"];
                    Dictionary<string, int[]> selectionsNumbers = new()
                    {
                        { "🟥", new int[]{ 32, 19, 21, 25, 34, 27, 36, 30, 23, 5, 16, 1, 14, 9, 18, 7, 12, 3 } },
                        { "⬛", new int[]{ 15, 4, 2, 17, 6, 13, 11, 8, 10, 24, 33, 20, 31, 22, 29, 28, 35, 26 } },
                        { "🟩", new int[]{ 0, 36 } }
                    };
                    Dictionary<string, double> multipliers = new()
                    {
                        { "🟥", 1.5 },
                        { "⬛", 1.5 },
                        { "🟩", 2 },
                    };

                    if (data.Arguments.Count > 1)
                    {
                        if (selections.Contains(data.Arguments[0]))
                        {
                            int bid = Format.ToInt(data.Arguments[1]);
                            commandReturn.SetColor(ChatColorPresets.Red);

                            if (bid == 0)
                                commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:roulette_wrong_bid", data.ChannelID, data.Platform));
                            else if (bid < 0)
                                commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:roulette_steal", data.ChannelID, data.Platform));
                            else if (Utils.Tools.Balance.GetBalance(data.UserID, data.Platform) < bid)
                                commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:roulette_not_enough_coins", data.ChannelID, data.Platform)
                                            .Replace("%balance%", Utils.Tools.Balance.GetBalance(data.UserID, data.Platform).ToString() + " " + Core.Bot.CoinSymbol));
                            else
                            {
                                int moves = new Random().Next(38, 380);
                                string result_symbol = "";
                                int result = moves % 38;

                                foreach (var item in selectionsNumbers)
                                {
                                    if (item.Value.Contains(result))
                                        result_symbol = item.Key;
                                }

                                if (result_symbol.Equals(data.Arguments[0]))
                                {
                                    commandReturn.SetColor(ChatColorPresets.YellowGreen);

                                    int win = (int)(bid * multipliers[result_symbol]);
                                    Utils.Tools.Balance.Add(data.UserID, win, 0, data.Platform);
                                    commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:roulette:result:win", data.ChannelID, data.Platform)
                                        .Replace("%result%", result_symbol)
                                        .Replace("%result_number%", result.ToString())
                                        .Replace("%win%", win.ToString() + " " + Core.Bot.CoinSymbol)
                                        .Replace("%multipier%", multipliers[result_symbol].ToString()));
                                }
                                else
                                {
                                    Utils.Tools.Balance.Add(data.UserID, -bid, 0, data.Platform);
                                    commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:roulette:result:lose", data.ChannelID, data.Platform)
                                        .Replace("%result%", result_symbol)
                                        .Replace("%result_number%", result.ToString())
                                        .Replace("%lose%", bid.ToString() + " " + Core.Bot.CoinSymbol));
                                }
                            }
                        }
                        else
                            commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:roulette_wrong_select", data.ChannelID, data.Platform));
                    }
                    else
                        commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:not_enough_arguments", data.ChannelID, data.Platform)
                            .Replace("%command_example%", $"#roulette [🟩/🟥/⬛] [{TranslationManager.GetTranslation(data.User.Language, "text:bid", data.ChannelID, data.Platform)}]"));
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