using butterBib;
using butterBror.Utils;
using System.Drawing;
using TwitchLib.Client.Enums;
using System.Collections.Generic;

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
                AuthorURL = "twitch.tv/itzkitb",
                AuthorImageURL = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                Description = "Сыграйте в рулетку! Подробности: https://bit.ly/bb_roulette",
                UseURL = "https://itzkitb.ru/bot/command?name=roulette",
                UserCooldown = 5,
                GlobalCooldown = 1,
                aliases = ["roulette", "r", "рулетка", "р"],
                ArgsRequired = "[Выбор: \"🟩\", \"🟥\", \"⬛\"] [Ставка]",
                ResetCooldownIfItHasNotReachedZero = true,
                CreationDate = DateTime.Parse("29/01/2025"),
                ForAdmins = false,
                ForBotCreator = false,
                ForChannelAdmins = false,
                AllowedPlatforms = [Platforms.Twitch, Platforms.Telegram, Platforms.Discord]
            };
            public static CommandReturn Index(CommandData data)
            {
                try
                {
                    string resultMessage = "";
                    Color resultColor = Color.Green;
                    ChatColorPresets resultNicknameColor = ChatColorPresets.YellowGreen;

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

                    if (data.args.Count > 1)
                    {

                        if (selections.Contains(data.args[0]))
                        {
                            int bid = FormatUtil.ToInt(data.args[1]);
                            resultColor = Color.Red;
                            resultNicknameColor = ChatColorPresets.Red;

                            if (bid == 0)
                                resultMessage = TranslationManager.GetTranslation(data.User.Lang, "roulette:wrong:bid", data.ChannelID);
                            else if (bid < 0)
                                resultMessage = TranslationManager.GetTranslation(data.User.Lang, "roulette:wrong:steal", data.ChannelID);
                            else if (BalanceUtil.GetButters(data.UserUUID) < bid)
                                resultMessage = TranslationManager.GetTranslation(data.User.Lang, "roulette:wrong:not_enough_butters", data.ChannelID)
                                            .Replace("%balance%", BalanceUtil.GetButters(data.UserUUID).ToString() + " " + Bot.CoinSymbol);
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

                                if (result_symbol.Equals(data.args[0]))
                                {
                                    resultColor = Color.Green;
                                    resultNicknameColor = ChatColorPresets.YellowGreen;

                                    int win = (int)(bid * multipliers[result_symbol]);
                                    BalanceUtil.Add(data.UserUUID, win, 0);
                                    resultMessage = TranslationManager.GetTranslation(data.User.Lang, "roulette:result:win", data.ChannelID)
                                        .Replace("%result%", result_symbol)
                                        .Replace("%result_number%", result.ToString())
                                        .Replace("%win%", win.ToString() + " " + Bot.CoinSymbol)
                                        .Replace("%multipier%", multipliers[result_symbol].ToString());
                                }
                                else
                                {
                                    BalanceUtil.Add(data.UserUUID, -bid, 0);
                                    resultMessage = TranslationManager.GetTranslation(data.User.Lang, "roulette:result:lose", data.ChannelID)
                                        .Replace("%result%", result_symbol)
                                        .Replace("%result_number%", result.ToString())
                                        .Replace("%lose%", bid.ToString() + " " + Bot.CoinSymbol);
                                }
                            }
                        }
                        else
                            resultMessage = TranslationManager.GetTranslation(data.User.Lang, "roulette:wrong:select", data.ChannelID);
                    }
                    else
                        resultMessage = TranslationManager.GetTranslation(data.User.Lang, "lowArgs", data.ChannelID).Replace("%commandWorks%", $"#roulette [🟩/🟥/⬛] [{TranslationManager.GetTranslation(data.User.Lang, "word:bid", data.ChannelID)}]");

                    return new()
                    {
                        Message = resultMessage,
                        IsSafeExecute = false,
                        Description = "",
                        Author = "",
                        ImageURL = "",
                        ThumbnailUrl = "",
                        Footer = "",
                        IsEmbed = false,
                        Ephemeral = false,
                        Title = "",
                        Color = resultColor,
                        NickNameColor = resultNicknameColor
                    };
                }
                catch (Exception e)
                {
                    return new()
                    {
                        Message = "",
                        IsSafeExecute = false,
                        Description = "",
                        Author = "",
                        ImageURL = "",
                        ThumbnailUrl = "",
                        Footer = "",
                        IsEmbed = true,
                        Ephemeral = false,
                        Title = "",
                        Color = Color.Green,
                        NickNameColor = ChatColorPresets.YellowGreen,
                        IsError = true,
                        Error = e
                    };
                }
            }
        }
    }
}