using butterBror.Utils;
using butterBror;
using Discord;
using TwitchLib.Client.Enums;

namespace butterBror
{
    public partial class Commands
    {
        public class Winter
        {
            public static CommandInfo Info = new()
            {
                name = "Winter",
                author = "@ItzKITb",
                author_link = "twitch.tv/itzkitb",
                author_avatar = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                description = new() {
                    { "ru", "В Якутске средняя температура зимой: −38,6°C" },
                    { "en", "In Yakutsk, the average temperature in winter is -38.6°C" }
                },
                wiki_link = "https://itzkitb.ru/bot/command?name=winter",
                cooldown_per_user = 120,
                cooldown_global = 10,
                aliases = ["winter", "w", "зима"],
                arguments = "(name)",
                cooldown_reset = false,
                creation_date = DateTime.Parse("07/04/2024"),
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
                    DateTime startDate = new(2000, 12, 1);
                    DateTime endDate = new(2000, 3, 1);
                    string result = TextUtil.TimeTo(startDate, endDate, "Winter", 1, data.user.language, data.arguments_string, data.channel_id, data.platform);
                    return new()
                    {
                        message = result,
                        safe_execute = false,
                        description = "",
                        author = "",
                        image_link = "",
                        thumbnail_link = "",
                        footer = "",
                        is_embed = true,
                        is_ephemeral = false,
                        title = TranslationManager.GetTranslation(data.user.language, "discord:winter:title", data.channel_id, data.platform),
                        embed_color = Color.Blue,
                        nickname_color = TwitchLib.Client.Enums.ChatColorPresets.DodgerBlue
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
