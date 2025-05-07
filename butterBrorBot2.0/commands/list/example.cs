using butterBror.Utils;
using butterBror;
using Discord;
using TwitchLib.Client.Enums;

namespace butterBror
{
    public partial class Commands
    {
        public class Example
        {
            public static CommandInfo Info = new()
            {
                name = "",
                author = "",
                author_link = "",
                author_avatar = "",
                description = new() {
                    { "ru", "" },
                    { "en", "" }
                },
                wiki_link = "https://itzkitb.lol/bot/command?q=",
                cooldown_per_user = 0,
                cooldown_global = 0,
                aliases = ["", "", "", ""],
                arguments = "",
                cooldown_reset = false,
                creation_date = DateTime.Parse("01/01/2000"),
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