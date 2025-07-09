using butterBror.Utils;
using butterBror.Utils.DataManagers;
using butterBror.Utils.Tools;
using butterBror.Utils.Types;
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
                Engine.Statistics.FunctionsUsed.Add();
                CommandReturn commandReturn = new CommandReturn();

                try
                {
                    if (data.Arguments != null && data.Arguments.Count != 0)
                    {
                        var name = Text.UsernameFilter(data.Arguments.ElementAt(0).ToLower());
                        var userID = Names.GetUserID(name, data.Platform);

                        if (userID is null)
                        {
                            commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:user_not_found", data.ChannelID, data.Platform)
                                .Replace("%user%", Names.DontPing(name)));
                            commandReturn.SetColor(ChatColorPresets.Red);
                        }
                        else
                        {
                            var message = MessagesWorker.GetMessage(data.ChannelID, userID, data.Platform, true, -1);
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
                                    if (flag) message_badges += TranslationManager.GetTranslation(data.User.Language, symbol, data.ChannelID, data.Platform);
                                }

                                if (!name.Equals(Engine.Bot.BotName, StringComparison.CurrentCultureIgnoreCase))
                                {
                                    if (name == data.User.Name)
                                    {
                                        commandReturn.SetMessage(Text.ArgumentsReplacement(
                                            TranslationManager.GetTranslation(data.User.Language, "command:first_message:user", data.ChannelID, data.Platform), new() { // Fix AA8
                                            { "ago", Text.FormatTimeSpan(Utils.Tools.Format.GetTimeTo(message.messageDate, DateTime.UtcNow, false), data.User.Language) },
                                            { "message", message.messageText },
                                            { "bages", message_badges } }));
                                    }
                                    else
                                    {
                                        commandReturn.SetMessage(Text.ArgumentsReplacement(
                                            TranslationManager.GetTranslation(data.User.Language, "command:first_message", data.ChannelID, data.Platform), new() { // Fix AA8
                                            { "ago", Text.FormatTimeSpan(Utils.Tools.Format.GetTimeTo(message.messageDate, DateTime.UtcNow, false), data.User.Language) },
                                            { "message", message.messageText },
                                            { "bages", message_badges },
                                            { "user", Names.DontPing(Names.GetUsername(userID, data.Platform)) } }));
                                    }
                                }
                                else
                                {
                                    commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:first_message:bot", data.ChannelID, data.Platform));
                                }
                            }
                        }
                    }
                    else
                    {
                        var user_id = data.UserID;
                        var message = MessagesWorker.GetMessage(data.ChannelID, user_id, data.Platform, true, -1);
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
                                if (flag) message_badges += TranslationManager.GetTranslation(data.User.Language, symbol, data.ChannelID, data.Platform);
                            }
                            commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:first_message", data.ChannelID, data.Platform)
                                .Replace("%ago%", Text.FormatTimeSpan(Utils.Tools.Format.GetTimeTo(message.messageDate, DateTime.UtcNow, false), data.User.Language))
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
