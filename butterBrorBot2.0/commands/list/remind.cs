using butterBror.Utils;
using butterBror;
using Discord;
using TwitchLib.Client.Enums;

namespace butterBror
{
    public partial class Commands
    {
        public class Remind
        {
            public static CommandInfo Info = new()
            {
                name = "Remind",
                author = "@ItzKITb",
                author_link = "twitch.tv/itzkitb",
                author_avatar = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                description = new() {
                    { "ru", "..." }, 
                    { "en", "..." } 
                },
                wiki_link = "https://itzkitb.ru/bot/command?name=remind",
                cooldown_per_user = 5,
                cooldown_global = 1,
                aliases = ["remind", "rmd", "напомнить", "нап"],
                arguments = "[(me [-y -mn -d -h -m])/([name] [text])]",
                cooldown_reset = true,
                creation_date = DateTime.Parse("01/10/2024"),
                is_for_bot_moderator = false,
                is_for_bot_developer = false,
                is_for_channel_moderator = false,
                platforms = [Platforms.Twitch, Platforms.Telegram]
            };
            public CommandReturn Index(CommandData data)
            {
                Engine.Statistics.functions_used.Add();
                string resultMessage = "";
                Color resultColor = Color.Green;
                ChatColorPresets resultNicknameColor = ChatColorPresets.YellowGreen;

                if (data.arguments.Count > 1)
                {
                    if (data.arguments[0].ToLower() == "me")
                    {

                    }
                    else
                    {

                    }
                }
                else
                {
                    resultMessage = TranslationManager.GetTranslation(data.user.language, "randomTxt", data.channel_id, data.platform) + "DinoDance";
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
                    embed_color = resultColor,
                    nickname_color = resultNicknameColor
                };
            }
        }
    }
}
