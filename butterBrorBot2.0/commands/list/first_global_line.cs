using butterBror.Utils;
using butterBror.Utils.DataManagers;
using butterBror;
using Discord;
using TwitchLib.Client.Enums;

namespace butterBror
{
    public partial class Commands
    {
        public class FirstGlobalLine
        {
            public static CommandInfo Info = new()
            {
                name = "FirstGlobalLine",
                author = "@ItzKITb",
                author_link = "twitch.tv/itzkitb",
                author_avatar = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                description = new() {
                    {"ru", "Ваше первое сообщение на платформе" },
                    {"en", "Your first message on the platform" }
                },
                wiki_link = "https://itzkitb.lol/bot/command?q=fgl",
                cooldown_per_user = 10,
                cooldown_global = 1,
                aliases = ["fgl", "firstgloballine", "пргс", "первоеглобальноесообщение"],
                arguments = "(name)",
                cooldown_reset = true,
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
                    ChatColorPresets resultNicknameColor = ChatColorPresets.YellowGreen;
                    string resultMessage = "";
                    Color resultColor = Color.Green;
                    string resultMessageTitle = TranslationManager.GetTranslation(data.user.language, "discord:firstgloballine:title", data.channel_id, data.platform);
                    DateTime now = DateTime.UtcNow;
                    if (data.arguments.Count != 0)
                    {
                        var name = TextUtil.UsernameFilter(data.arguments.ElementAt(0).ToLower());
                        var userID = Names.GetUserID(name, data.platform);
                        if (userID == null)
                        {
                            resultMessage = TranslationManager.GetTranslation(data.user.language, "error:user_not_found", data.channel_id, data.platform)
                                .Replace("%user%", Names.DontPing(name));
                            resultMessageTitle = TranslationManager.GetTranslation(data.user.language, "discord:error:title", data.channel_id, data.platform);
                            resultColor = Color.Red;
                            resultNicknameColor = ChatColorPresets.Red;
                        }
                        else
                        {
                            var firstLine = UsersData.Get<string>(userID, "firstMessage", data.platform);
                            var firstLineDate = UsersData.Get<DateTime>(userID, "firstSeen", data.platform);

                            if (name == Maintenance.twitch_client.TwitchUsername.ToLower())
                            {
                                resultMessage = TranslationManager.GetTranslation(data.user.language, "command:first_global_line:bot", data.channel_id, data.platform);
                            }
                            else if (name == data.user.username)
                            {
                                resultMessage = TranslationManager.GetTranslation(data.user.language, "command:first_global_line", data.channel_id, data.platform)
                                    .Replace("%ago%", TextUtil.FormatTimeSpan(Utils.Format.GetTimeTo(firstLineDate, now, false), data.user.language))
                                    .Replace("%message%", firstLine);
                            }
                            else
                            {
                                resultMessage = TranslationManager.GetTranslation(data.user.language, "command:first_global_line:user", data.channel_id, data.platform)
                                    .Replace("%user%", Names.DontPing(Names.GetUsername(userID, data.platform)))
                                    .Replace("%ago%", TextUtil.FormatTimeSpan(Utils.Format.GetTimeTo(firstLineDate, now, false), data.user.language))
                                    .Replace("%message%", firstLine);
                            }
                        }
                    }
                    else
                    {
                        var firstLine = UsersData.Get<string>(data.user_id, "firstMessage", data.platform);
                        var firstLineDate = UsersData.Get<DateTime>(data.user_id, "firstSeen", data.platform);

                        resultMessage = TranslationManager.GetTranslation(data.user.language, "command:first_global_line", data.channel_id, data.platform)
                            .Replace("%ago%", TextUtil.FormatTimeSpan(Utils.Format.GetTimeTo(firstLineDate, now, false), data.user.language))
                            .Replace("%message%", firstLine);
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
                        is_embed = true,
                        is_ephemeral = false,
                        title = resultMessageTitle,
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
