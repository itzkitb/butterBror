using butterBror.Utils;
using butterBror.Utils.DataManagers;
using butterBror;
using Discord;
using TwitchLib.Client.Enums;

namespace butterBror
{
    public partial class Commands
    {
        public class LastLine
        {
            public static CommandInfo Info = new()
            {
                Name = "LastLine",
                Author = "@ItzKITb",
                AuthorLink = "twitch.tv/itzkitb",
                AuthorAvatar = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                Description = new() { 
                    { "ru", "Последнее сообщение выбранного пользователя в текущем чате" }, 
                    { "en", "The last message of the selected user in the current chat" } 
                },
                WikiLink = "https://itzkitb.lol/bot/command?q=ll",
                CooldownPerUser = 10,
                CooldownPerChannel = 1,
                Aliases = ["ll", "lastline", "пс", "последнеесообщение"],
                Arguments = "[name]",
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
                    if (data.arguments.Count != 0)
                    {
                        var name = TextUtil.UsernameFilter(data.arguments.ElementAt(0).ToLower());
                        var userID = Names.GetUserID(name, Platforms.Twitch);
                        var message = await MessagesWorker.GetMessage(data.channel_id, userID, data.platform);
                        var bages = "";
                        if (message != null)
                        {
                            if (userID != null)
                            {
                                if (name != Maintenance.twitch_client.TwitchUsername.ToLower())
                                {
                                    if (name == data.user.username.ToLower())
                                    {
                                        commandReturn.SetMessage(TranslationManager.GetTranslation(data.user.language, "text:you_right_there", data.channel_id, data.platform));
                                    }
                                    else
                                    {
                                        var message_badges = "";
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
                                        var Date = message.messageDate;
                                        commandReturn.SetMessage(TranslationManager.GetTranslation(data.user.language, "command:last_message", data.channel_id, data.platform)
                                            .Replace("&timeAgo&", TextUtil.FormatTimeSpan(Utils.Format.GetTimeTo(message.messageDate, DateTime.Now, false), data.user.language))
                                            .Replace("%message%", message.messageText).Replace("%bages%", message_badges)
                                            .Replace("%user%", Names.DontPing(Names.GetUsername(userID, data.platform))));
                                    }
                                }
                                else
                                {
                                    commandReturn.SetMessage(TranslationManager.GetTranslation(data.user.language, "command:last_line:bot", data.channel_id, data.platform));
                                }
                            }
                            else
                            {
                                commandReturn.SetMessage(TranslationManager.GetTranslation(data.user.language, "error:user_not_found", data.channel_id, data.platform)
                                    .Replace("%user%", Names.DontPing(name)));
                                commandReturn.SetColor(ChatColorPresets.Red);
                            }
                        }
                        else
                        {
                            commandReturn.SetMessage(TranslationManager.GetTranslation(data.user.language, "error:user_not_found", data.channel_id, data.platform)
                                .Replace("%user%", Names.DontPing(name))); // Fix AB1
                            commandReturn.SetColor(ChatColorPresets.Red); // Fix AB1
                        }
                    }
                    else
                    {
                        commandReturn.SetMessage(TranslationManager.GetTranslation(data.user.language, "text:you_right_there", data.channel_id, data.platform));
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