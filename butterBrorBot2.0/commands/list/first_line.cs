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
                Name = "FirstLine",
                Author = "@ItzKITb",
                AuthorLink = "twitch.tv/itzkitb",
                AuthorAvatar = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                Description = new() {
                    { "ru", "Первое сообщение в текущем чате" },
                    { "en", "First message in the current chat" }
                },
                WikiLink = "https://itzkitb.lol/bot/command?q=fl",
                CooldownPerUser = 10,
                CooldownPerChannel = 1,
                Aliases = ["fl", "firstline", "прс", "первоесообщение"],
                Arguments = "(name)",
                CooldownReset = true,
                CreationDate = DateTime.Parse("07/04/2024"),
                IsForBotModerator = false,
                IsForBotDeveloper = false,
                IsForChannelModerator = false,
                Platforms = [Platforms.Twitch, Platforms.Telegram, Platforms.Discord]
            };
            public async Task<CommandReturn> Index(CommandData data)
            {
                Engine.Statistics.functions_used.Add();
                CommandReturn commandReturn = new CommandReturn();

                try
                {
                    if (data.arguments != null && data.arguments.Count != 0)
                    {
                        var name = TextUtil.UsernameFilter(data.arguments.ElementAt(0).ToLower());
                        var userID = Names.GetUserID(name, data.platform);

                        if (userID is null)
                        {
                            commandReturn.SetMessage(TranslationManager.GetTranslation(data.user.language, "error:user_not_found", data.channel_id, data.platform)
                                .Replace("%user%", Names.DontPing(name)));
                            commandReturn.SetColor(ChatColorPresets.Red);
                        }
                        else
                        {
                            var message = await MessagesWorker.GetMessage(data.channel_id, userID, data.platform, true, -1);
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
                                        commandReturn.SetMessage(TextUtil.ArgumentsReplacement(
                                            TranslationManager.GetTranslation(data.user.language, "command:first_message", data.channel_id, data.platform), new() {
                                            { "ago", TextUtil.FormatTimeSpan(Utils.Format.GetTimeTo(message.messageDate, DateTime.UtcNow, false), data.user.language) },
                                            { "message", message.messageText },
                                            { "bages", message_badges } }));
                                    }
                                    else
                                    {
                                        commandReturn.SetMessage(TextUtil.ArgumentsReplacement(
                                            TranslationManager.GetTranslation(data.user.language, "command:first_message:user", data.channel_id, data.platform), new() {
                                            { "ago", TextUtil.FormatTimeSpan(Utils.Format.GetTimeTo(message.messageDate, DateTime.UtcNow, false), data.user.language) },
                                            { "message", message.messageText },
                                            { "bages", message_badges },
                                            { "user", Names.DontPing(Names.GetUsername(userID, data.platform)) } }));
                                    }
                                }
                                else
                                {
                                    commandReturn.SetMessage(TranslationManager.GetTranslation(data.user.language, "command:first_message:bot", data.channel_id, data.platform));
                                }
                            }
                        }
                    }
                    else
                    {
                        var user_id = data.user_id;
                        var message = await MessagesWorker.GetMessage(data.channel_id, user_id, data.platform, true, -1);
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
                            commandReturn.SetMessage(TranslationManager.GetTranslation(data.user.language, "command:first_message", data.channel_id, data.platform)
                                .Replace("%ago%", TextUtil.FormatTimeSpan(Utils.Format.GetTimeTo(message.messageDate, DateTime.UtcNow, false), data.user.language))
                                .Replace("%message%", message.messageText).Replace("%bages%", message_badges));
                        }
                    }
                }
                catch (Exception e)
                {
                    commandReturn.SetError(e);
                }

                return commandReturn;
            }
        }
    }
}
