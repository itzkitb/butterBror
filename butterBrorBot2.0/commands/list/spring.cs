using butterBror.Utils;
using butterBror;
using Discord;
using TwitchLib.Client.Enums;

namespace butterBror
{
    public partial class Commands
    {
        public class Spring
        {
            public static CommandInfo Info = new()
            {
                name = "Spring",
                author = "@ItzKITb",
                author_link = "twitch.tv/itzkitb",
                author_avatar = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                description = new() { 
                    { "ru", "Грязь, аллергия и лужи - мое любимое" }, 
                    { "en", "Dirt, allergies and puddles are my favorite" } 
                },
                wiki_link = "https://itzkitb.lol/bot/command?q=spring",
                cooldown_per_user = 120,
                cooldown_global = 10,
                aliases = ["spring", "sp", "весна"],
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
                    DateTime startDate = new(2000, 3, 1);
                    DateTime endDate = new(2000, 6, 1);
                    string result = TextUtil.TimeTo(startDate, endDate, "spring", 0, data.user.language, data.arguments_string, data.channel_id, data.platform);
                    return new()
                    {
                        message = result,
                        safe_execute = true,
                        description = "",
                        author = "",
                        image_link = "",
                        thumbnail_link = "",
                        footer = "",
                        is_embed = true,
                        is_ephemeral = false,
                        title = TranslationManager.GetTranslation(data.user.language, "discord:spring:title", data.channel_id, data.platform),
                        embed_color = Color.Orange,
                        nickname_color = TwitchLib.Client.Enums.ChatColorPresets.Coral
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
