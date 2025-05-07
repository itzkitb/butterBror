using butterBror.Utils;
using butterBror;
using Discord;
using TwitchLib.Client.Enums;

namespace butterBror
{
    public partial class Commands
    {
        public class SetLocation
        {
            public static CommandInfo Info = new()
            {
                name = "Translation",
                author = "@ItzKITb",
                author_link = "twitch.tv/itzkitb",
                author_avatar = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                description = new() {
                    { "ru", "Укажите свое местоположение, чтобы узнать погоду" },
                    { "en", "Set your location to get weather" }
                },
                wiki_link = "https://itzkitb.lol/bot/command?q=setlocation",
                cooldown_per_user = 0,
                cooldown_global = 0,
                aliases = ["loc", "location", "city", "setlocation", "setloc", "setcity", "улокацию", "угород", "установитьлокацию", "установитьгород"],
                arguments = "(city name)",
                cooldown_reset = false,
                creation_date = DateTime.Parse("29/04/2025"),
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
                    var exdata = data;
                    if (exdata.arguments is not null && exdata.arguments.Count >= 1)
                    {
                        exdata.arguments.Insert(0, "set");
                    }
                    else
                    {
                        exdata.arguments = new List<string>();
                        exdata.arguments.Insert(0, "get");
                    }
                    var command = new Weather();
                    return command.Index(exdata);
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