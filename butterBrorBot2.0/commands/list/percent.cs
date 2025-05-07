using butterBror;
using Discord;
using TwitchLib.Client.Enums;

namespace butterBror
{
    public partial class Commands
    {
        public class Percent
        {
            public static CommandInfo Info = new()
            {
                name = "Percent",
                author = "@ItzKITb",
                author_link = "twitch.tv/itzkitb",
                author_avatar = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                description = new() { 
                    { "ru", "MrDestructoid Вероятность уничтожения змели в ближайшие 5 минут: 99.9%" }, 
                    { "en", "MrDestructoid Chance of destroying the snake in the next 5 minutes: 99.9%" } 
                },
                wiki_link = "https://itzkitb.lol/bot/command?q=percent",
                cooldown_per_user = 5,
                cooldown_global = 1,
                aliases = ["%", "percent", "процент", "perc", "проц"],
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
                    float percent = (float)rand.Next(10000) / 100;
                    resultMessage = $"🤔 {percent}%";
                    return new()
                    {
                        message = resultMessage,
                        safe_execute = true,
                        description = "",
                        author = "",
                        image_link = "",
                        thumbnail_link = "",
                        footer = "",
                        is_embed = false,
                        is_ephemeral = false,
                        title = "",
                        embed_color = Color.Green,
                        nickname_color = TwitchLib.Client.Enums.ChatColorPresets.YellowGreen
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