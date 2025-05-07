using butterBror.Utils;
using butterBror;
using Discord;
using TwitchLib.Client.Enums;

namespace butterBror
{
    public partial class Commands
    {
        public class Autumn
        {
            public static CommandInfo Info = new()
            {
                name = "Autumn",
                author = "@ItzKITb",
                author_link = "twitch.tv/itzkitb",
                author_avatar = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                description = new() { 
                    { "ru", "С помощью этой команды вы можете узнать, сколько времени осталось до начала или конца осени" }, 
                    { "en", "With this command you can find out how much time is left until the beginning or end of autumn" } 
                },
                wiki_link = "https://itzkitb.lol/bot/command?q=autumn",
                cooldown_per_user = 120,
                cooldown_global = 10,
                aliases = ["autumn", "a", "осень", "fall"],
                arguments = string.Empty,
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
                    string result = "";
                    DateTime startDate = new(2000, 9, 1);
                    DateTime endDate = new(2000, 12, 1);
                    result = TextUtil.TimeTo(startDate, endDate, "autumn", 0, data.user.language, data.arguments_string, data.channel_id, data.platform);
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
                        title = TranslationManager.GetTranslation(data.user.language, "discord:autumn:title", data.channel_id, data.platform),
                        embed_color = Color.Orange,
                        nickname_color = ChatColorPresets.Coral
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
