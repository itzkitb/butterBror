using butterBror.Utils;
using butterBror;
using Discord;
using TwitchLib.Client.Enums;

namespace butterBror
{
    public partial class Commands
    {
        public class RussianRoullete
        {
            public static CommandInfo Info = new()
            {
                name = "RussianRoullete",
                author = "@ItzKITb",
                author_link = "twitch.tv/itzkitb",
                author_avatar = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                description = new() { 
                    { "ru", "Mike Klubnika <3" },
                    { "en", "Mike Klubnika <3" } 
                },
                wiki_link = "https://itzkitb.lol/bot/command?q=rr",
                cooldown_per_user = 5,
                cooldown_global = 1,
                aliases = ["rr", "russianroullete", "русскаярулетка", "рр", "br", "buckshotroullete"],
                arguments = string.Empty,
                cooldown_reset = true,
                creation_date = DateTime.Parse("08/08/2024"),
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
                    Random rand = new Random();
                    int win = rand.Next(1, 3);
                    int page2 = rand.Next(1, 5);
                    string translationParam = "command:russian_roullete:";
                    if (Utils.Balance.GetBalance(data.user_id, data.platform) > 4)
                    {
                        if (win == 1)
                        {
                            // WIN
                            translationParam += "win:" + page2;
                            Utils.Balance.Add(data.user_id, 1, 0, data.platform);
                        }
                        else
                        {
                            // GAME OVER
                            translationParam += "over:" + page2;
                            if (page2 == 4)
                            {
                                Utils.Balance.Add(data.user_id, -1, 0, data.platform);
                            }
                            else
                            {
                                Utils.Balance.Add(data.user_id, -5, 0, data.platform);
                            }
                            resultNicknameColor = ChatColorPresets.Red;
                            resultColor = Color.Red;
                        }
                        resultMessage = "🔫 " + TranslationManager.GetTranslation(data.user.language, translationParam, data.channel_id, data.platform);
                    }
                    else
                    {
                        resultMessage = TranslationManager.GetTranslation(data.user.language, "error:roulette_not_enough_coins", data.channel_id, data.platform)
                            .Replace("%balance%", Utils.Balance.GetBalance(data.user_id, data.platform).ToString());
                    }
                    return new()
                    {
                        message = resultMessage,
                        safe_execute = true,
                        description = "",
                        author = "",
                        image_link = "",
                        thumbnail_link = "",
                        footer = "",
                        is_embed = true,
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
