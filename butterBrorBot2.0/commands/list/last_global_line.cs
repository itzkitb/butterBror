using butterBror.Utils;
using butterBror.Utils.DataManagers;
using butterBror;
using Discord;
using TwitchLib.Client.Enums;

namespace butterBror
{
    public partial class Commands
    {
        public class LastGlobalLine
        {
            public static CommandInfo Info = new()
            {
                name = "LastGlobalLine",
                author = "@ItzKITb",
                author_link = "twitch.tv/itzkitb",
                author_avatar = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                description = new() { 
                    { "ru", "Последнее сообщение выбранного пользователя" }, 
                    { "en", "The last message of the selected user" } 
                },
                wiki_link = "https://itzkitb.lol/bot/command?q=lgl",
                cooldown_per_user = 10,
                cooldown_global = 1,
                aliases = ["lgl", "lastgloballine", "пгс", "последнееглобальноесообщение"],
                arguments = "[name]",
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
                    string resultMessage = "";
                    Color resultColor = Color.Green;
                    ChatColorPresets resultNicknameColor = ChatColorPresets.YellowGreen;
                    string resultMessageTitle = TranslationManager.GetTranslation(data.user.language, "discord:lastgloballine:title", data.channel_id, data.platform);
                    if (data.arguments.Count != 0)
                    {
                        var name = TextUtil.UsernameFilter(data.arguments.ElementAt(0).ToLower());
                        var userID = Names.GetUserID(name, Platforms.Twitch);
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
                            var lastLine = UsersData.Get<string>(userID, "lastSeenMessage", data.platform);
                            var lastLineDate = UsersData.Get<DateTime>(userID, "lastSeen", data.platform);
                            DateTime now = DateTime.UtcNow;
                            if (name == Maintenance.twitch_client.TwitchUsername.ToLower())
                            {
                                resultMessage = TranslationManager.GetTranslation(data.user.language, "command:last_global_line:bot", data.channel_id, data.platform);
                            }
                            else if (name == data.user.username.ToLower())
                            {
                                resultMessage = TranslationManager.GetTranslation(data.user.language, "text:you_right_there", data.channel_id, data.platform);
                            }
                            else
                            {
                                resultMessage = TranslationManager.GetTranslation(data.user.language, "command:last_global_line", data.channel_id, data.platform)
                                    .Replace("%user%", Names.DontPing(Names.GetUsername(userID, data.platform)))
                                    .Replace("&timeAgo&", TextUtil.FormatTimeSpan(Utils.Format.GetTimeTo(lastLineDate, now, false), data.user.language))
                                    .Replace("%message%", lastLine);
                            }
                        }
                    }
                    else
                    {
                        resultMessage = TranslationManager.GetTranslation(data.user.language, "text:you_right_there", data.channel_id, data.platform);
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
