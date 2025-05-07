using butterBror.Utils;
using butterBror;
using Discord;
using TwitchLib.Client.Enums;

namespace butterBror
{
    public partial class Commands
    {
        public class Name
        {
            public static CommandInfo Info = new()
            {
                name = "Name",
                author = "@ItzKITb",
                author_link = "twitch.tv/itzkitb",
                author_avatar = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                description = new()
                {
                    { "ru", "Получить имя из ID" },
                    { "en", "Get name from ID" }
                },
                wiki_link = "https://itzkitb.lol/bot/command?q=name",
                cooldown_per_user = 5,
                cooldown_global = 1,
                aliases = ["name", "nick", "nickname", "никнейм", "ник", "имя"],
                arguments = "[user ID]",
                cooldown_reset = true,
                creation_date = DateTime.Parse("25/10/2024"),
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
                    if (data.arguments.Count > 0)
                    {
                        string name = Names.GetUsername(data.arguments[0], Platforms.Twitch);
                        if (name == data.user_id)
                        {
                            resultMessage = TranslationManager.GetTranslation(data.user.language, "command:id", data.channel_id, data.platform).Replace("%id%", data.user_id);
                        }
                        else if (name == null)
                        {
                            resultMessage = TranslationManager.GetTranslation(data.user.language, "error:user_not_found", data.channel_id, data.platform).Replace("%user%", data.arguments[0]);
                            resultNicknameColor = ChatColorPresets.Red;
                            resultColor = Color.Red;
                        }
                        else
                        {
                            resultMessage = TranslationManager.GetTranslation(data.user.language, "command:id:user", data.channel_id, data.platform).Replace("%id%", name).Replace("%user%", data.arguments[0]);
                        }
                    }
                    else
                    {
                        resultMessage = TranslationManager.GetTranslation(data.user.language, "command:id", data.channel_id, data.platform).Replace("%id%", data.user_id);
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
