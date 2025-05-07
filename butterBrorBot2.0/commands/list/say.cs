using TwitchLib.Client.Enums;
using Discord;
using butterBror;

namespace butterBror
{
    public partial class Commands
    {
        public class Say
        {
            public static CommandInfo Info = new()
            {
                name = "Say",
                author = "@ItzKITb",
                author_link = "twitch.tv/itzkitb",
                author_avatar = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                description = new() { 
                    { "ru", "Обернись" }, 
                    { "en", "Обернись" } 
                },
                wiki_link = "https://itzkitb.lol/bot/command?q=say",
                cooldown_per_user = 5,
                cooldown_global = 1,
                aliases = ["say", "tell", "сказать", "type", "написать"],
                arguments = "[text]",
                cooldown_reset = true,
                creation_date = DateTime.Parse("09/07/2024"),
                is_for_bot_moderator = false,
                is_for_bot_developer = true,
                is_for_channel_moderator = false,
                platforms = [Platforms.Twitch, Platforms.Telegram]
            };
            public CommandReturn Index(CommandData data)
            {
                Engine.Statistics.functions_used.Add();
                try
                {
                    string resultMessage = data.arguments_string;
                    Color resultColor = Color.Green;
                    ChatColorPresets resultNicknameColor = ChatColorPresets.YellowGreen;

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