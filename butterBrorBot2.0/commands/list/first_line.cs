using butterBror.Utils;
using butterBror.Utils.DataManagers;
using Discord;
using TwitchLib.Client.Enums;

namespace butterBror
{
    public partial class Commands
    {
        public class FirstLine
        {
            public static CommandInfo Info = new()
            {
                name = "FirstLine",
                author = "@ItzKITb",
                author_link = "twitch.tv/itzkitb",
                author_avatar = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                description = new() {
                    { "ru", "Первое сообщение в текущем чате" },
                    { "en", "First message in the current chat" }
                },
                wiki_link = "https://itzkitb.lol/bot/command?q=fl",
                cooldown_per_user = 10,
                cooldown_global = 1,
                aliases = ["fl", "firstline", "прс", "первоесообщение"],
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
                    string return_message = string.Empty;
                    Color return_message_color = Color.Green;
                    string return_message_title = TranslationManager.GetTranslation(data.user.language, "discord:firstline:title", data.channel_id, data.platform);
                    ChatColorPresets return_message_nickname_color = ChatColorPresets.YellowGreen;

                    if (data.arguments != null && data.arguments.Count != 0)
                    {
                        var name = TextUtil.UsernameFilter(data.arguments.ElementAt(0).ToLower());
                        var userID = Names.GetUserID(name, data.platform);

                        if (userID is null)
                        {
                            return_message = TranslationManager.GetTranslation(data.user.language, "error:user_not_found", data.channel_id, data.platform)
                                .Replace("%user%", Names.DontPing(name));
                            return_message_title = TranslationManager.GetTranslation(data.user.language, "discord:error:title", data.channel_id, data.platform);
                            return_message_color = Color.Red;
                            return_message_nickname_color = ChatColorPresets.Red;
                        }
                        else
                        {
                            var message = MessagesWorker.GetMessage(data.channel_id, userID, data.platform, true, -1);
                            var message_badges = string.Empty;
                            if (message != null)
                            {
                                var badges = new (bool flag, string symbol)[]
                                {
                                    (message.isMe, "symbol:splash_me"),
                                    (message.isVip, "symbol:vip"),
                                    (message.isTurbo, "symbol:turbo"),
                                    (message.isModerator, "symbol:moderator"),
                                    (message.isPartner, "symbol:partner"),
                                    (message.isStaff, "symbol:staff"),
                                    (message.isSubscriber, "symbol:subscriber")
                                };

                                foreach (var (flag, symbol) in badges)
                                {
                                    if (flag) message_badges += TranslationManager.GetTranslation(data.user.language, symbol, data.channel_id, data.platform);
                                }

                                if (!name.Equals(Maintenance.twitch_client.TwitchUsername, StringComparison.CurrentCultureIgnoreCase))
                                {
                                    if (name == data.user.username)
                                    {
                                        return_message = TextUtil.ArgumentsReplacement(
                                            TranslationManager.GetTranslation(data.user.language, "command:first_message", data.channel_id, data.platform), new() {
                                            { "ago", TextUtil.FormatTimeSpan(Utils.Format.GetTimeTo(message.messageDate, DateTime.UtcNow, false), data.user.language) },
                                            { "message", message.messageText },
                                            { "bages", message_badges } });
                                    }
                                    else
                                    {
                                        return_message = TextUtil.ArgumentsReplacement(
                                            TranslationManager.GetTranslation(data.user.language, "command:first_message:user", data.channel_id, data.platform), new() {
                                            { "ago", TextUtil.FormatTimeSpan(Utils.Format.GetTimeTo(message.messageDate, DateTime.UtcNow, false), data.user.language) },
                                            { "message", message.messageText },
                                            { "bages", message_badges },
                                            { "user", Names.DontPing(Names.GetUsername(userID, data.platform)) } });
                                    }
                                }
                                else
                                {
                                    return_message = TranslationManager.GetTranslation(data.user.language, "command:first_message:bot", data.channel_id, data.platform);
                                }
                            }
                        }
                    }
                    else
                    {
                        var user_id = data.user_id;
                        var message = MessagesWorker.GetMessage(data.channel_id, user_id, data.platform, true, -1);
                        var message_badges = "";
                        if (message != null)
                        {
                            var badges = new (bool flag, string symbol)[]
                                {
                                    (message.isMe, "symbol:splash_me"),
                                    (message.isVip, "symbol:vip"),
                                    (message.isTurbo, "symbol:turbo"),
                                    (message.isModerator, "symbol:moderator"),
                                    (message.isPartner, "symbol:partner"),
                                    (message.isStaff, "symbol:staff"),
                                    (message.isSubscriber, "symbol:subscriber")
                                };

                            foreach (var (flag, symbol) in badges)
                            {
                                if (flag) message_badges += TranslationManager.GetTranslation(data.user.language, symbol, data.channel_id, data.platform);
                            }
                            return_message = TranslationManager.GetTranslation(data.user.language, "command:first_message", data.channel_id, data.platform)
                                .Replace("%ago%", TextUtil.FormatTimeSpan(Utils.Format.GetTimeTo(message.messageDate, DateTime.UtcNow, false), data.user.language))
                                .Replace("%message%", message.messageText).Replace("%bages%", message_badges);
                        }
                    }

                    return new()
                    {
                        message = return_message,
                        safe_execute = false,
                        description = "",
                        author = "",
                        image_link = "",
                        thumbnail_link = "",
                        footer = "",
                        is_embed = true,
                        is_ephemeral = false,
                        title = return_message_title,
                        embed_color = return_message_color,
                        nickname_color = return_message_nickname_color
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
