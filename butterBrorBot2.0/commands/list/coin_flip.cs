using butterBror.Utils;
using butterBror;
using Discord;
using TwitchLib.Client.Enums;

namespace butterBror
{
    public partial class Commands
    {
        public class Coinflip
        {
            public static CommandInfo Info = new()
            {
                name = "CoinFlip",
                author = "@ItzKITb",
                author_link = "twitch.tv/itzkitb",
                author_avatar = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                description = new()
                {
                    { "ru", "Подкинь монетку!" },
                    { "en", "Flip a coin!" }
                },
                wiki_link = "https://itzkitb.lol/bot/command?q=coinflip",
                cooldown_per_user = 5,
                cooldown_global = 1,
                aliases = ["coin", "coinflip", "орелилирешка", "оир", "монетка", "headsortails", "hot", "орел", "решка", "heads", "tails"],
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
                    Random rand = new Random();
                    int coin = rand.Next(1, 3);
                    if (coin == 1)
                    {
                        resultMessage = TranslationManager.GetTranslation(data.user.language, "symbol:coin", data.channel_id, data.platform) + TranslationManager.GetTranslation(data.user.language, "command:coinflip:heads", data.channel_id, data.platform);
                    }
                    else
                    {
                        resultMessage = TranslationManager.GetTranslation(data.user.language, "symbol:coin", data.channel_id, data.platform) + TranslationManager.GetTranslation(data.user.language, "command:coinflip:tails", data.channel_id, data.platform);
                    }
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
                        embed_color = Color.Green,
                        nickname_color = ChatColorPresets.YellowGreen
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