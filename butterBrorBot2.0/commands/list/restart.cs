using butterBror.Utils;
using butterBror.Utils.DataManagers;
using butterBror;
using Discord;
using TwitchLib.Client.Enums;

namespace butterBror
{
    public partial class Commands
    {
        public class Restart
        {
            public static CommandInfo Info = new()
            {
                name = "Restart",
                author = "@ItzKITb",
                author_link = "twitch.tv/itzkitb",
                author_avatar = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                description = new() { 
                    { "ru", "Этот маленький манёвр будет стоить нам 51 год" }, 
                    { "en", "This little maneuver will cost us 51 years" } 
                },
                wiki_link = "https://itzkitb.lol/bot/command?q=restart",
                cooldown_per_user = 1,
                cooldown_global = 1,
                aliases = ["restart", "reload", "перезагрузить", "рестарт"],
                arguments = string.Empty,
                cooldown_reset = false,
                creation_date = DateTime.Parse("07/04/2024"),
                is_for_bot_moderator = true,
                is_for_bot_developer = true,
                is_for_channel_moderator = false,
                platforms = [Platforms.Twitch, Platforms.Telegram, Platforms.Discord]
            };
            public CommandReturn Index(CommandData data)
            {
                Engine.Statistics.functions_used.Add();
                try
                {
                    string resultMessage = "";
                    if (UsersData.Contains(data.user_id, "isBotModerator", data.platform) || UsersData.Contains(data.user_id, "isBotDev", data.platform))
                    {
                        resultMessage = "❄ Перезагрузка...";
                        Maintenance.Restart();
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
