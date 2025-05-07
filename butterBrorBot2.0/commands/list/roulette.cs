using butterBror;
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
                name = "Roulette",
                author = "@ItzKITb",
                author_link = "twitch.tv/itzkitb",
                author_avatar = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                description = new() { 
                    { "ru", "Сыграйте в рулетку! Подробности: https://bit.ly/bb_roulette " }, 
                    { "en", "Play Roulette! Details: https://bit.ly/bb_roulette " } 
                },
                wiki_link = "https://itzkitb.lol/bot/command?q=roulette",
                cooldown_per_user = 5,
                cooldown_global = 1,
                aliases = ["roulette", "r", "рулетка", "р"],
                arguments = "[select: \"🟩\", \"🟥\", \"⬛\"] [bid]",
                cooldown_reset = true,
                creation_date = DateTime.Parse("29/01/2025"),
                is_for_bot_moderator = false,
                is_for_bot_developer = false,
                is_for_channel_moderator = false,
                platforms = [Platforms.Twitch, Platforms.Telegram, Platforms.Discord]
            };
            public CommandReturn Index(CommandData data)
            {
                Engine.Statistics.functions_used.Add();
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

                    if (data.arguments.Count > 1)
                    {
                        if (selections.Contains(data.arguments[0]))
                        {
                            int bid = Format.ToInt(data.arguments[1]);
                            resultColor = Color.Red;
                            resultNicknameColor = ChatColorPresets.Red;

                            if (bid == 0)
                                resultMessage = TranslationManager.GetTranslation(data.user.language, "error:roulette_wrong_bid", data.channel_id, data.platform);
                            else if (bid < 0)
                                resultMessage = TranslationManager.GetTranslation(data.user.language, "error:roulette_steal", data.channel_id, data.platform);
                            else if (Utils.Balance.GetBalance(data.user_id, data.platform) < bid)
                                resultMessage = TranslationManager.GetTranslation(data.user.language, "error:roulette_not_enough_coins", data.channel_id, data.platform)
                                            .Replace("%balance%", Utils.Balance.GetBalance(data.user_id, data.platform).ToString() + " " + Maintenance.coin_symbol);
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

                                if (result_symbol.Equals(data.arguments[0]))
                                {
                                    resultColor = Color.Green;
                                    resultNicknameColor = ChatColorPresets.YellowGreen;

                                    int win = (int)(bid * multipliers[result_symbol]);
                                    Utils.Balance.Add(data.user_id, win, 0, data.platform);
                                    resultMessage = TranslationManager.GetTranslation(data.user.language, "command:roulette:result:win", data.channel_id, data.platform)
                                        .Replace("%result%", result_symbol)
                                        .Replace("%result_number%", result.ToString())
                                        .Replace("%win%", win.ToString() + " " + Maintenance.coin_symbol)
                                        .Replace("%multipier%", multipliers[result_symbol].ToString());
                                }
                                else
                                {
                                    Utils.Balance.Add(data.user_id, -bid, 0, data.platform);
                                    resultMessage = TranslationManager.GetTranslation(data.user.language, "command:roulette:result:lose", data.channel_id, data.platform)
                                        .Replace("%result%", result_symbol)
                                        .Replace("%result_number%", result.ToString())
                                        .Replace("%lose%", bid.ToString() + " " + Maintenance.coin_symbol);
                                }
                            }
                        }
                        else
                            resultMessage = TranslationManager.GetTranslation(data.user.language, "error:roulette_wrong_select", data.channel_id, data.platform);
                    }
                    else
                        resultMessage = TranslationManager.GetTranslation(data.user.language, "error:not_enough_arguments", data.channel_id, data.platform)
                            .Replace("%command_example%", $"#roulette [🟩/🟥/⬛] [{TranslationManager.GetTranslation(data.user.language, "text:bid", data.channel_id, data.platform)}]");

                    return new()
                    {
                        message = resultMessage,
                        safe_execute = false,
                        description = "",
                        author = "",
                        image_link = "",
                        thumbnail_link = "",
                        footer = "",
                        is_embed = false,
                        is_ephemeral = false,
                        title = "",
                        embed_color = resultColor,
                        nickname_color = resultNicknameColor
                    };
                }
                catch (Exception e)
                {
                    return new()
                    {
                        message = "",
                        safe_execute = false,
                        description = "",
                        author = "",
                        image_link = "",
                        thumbnail_link = "",
                        footer = "",
                        is_embed = true,
                        is_ephemeral = false,
                        title = "",
                        embed_color = Color.Green,
                        nickname_color = ChatColorPresets.YellowGreen,
                        is_error = true,
                        exception = e
                    };
                }
            }
        }
    }
}