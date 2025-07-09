using butterBror.Utils;
using butterBror.Utils.DataManagers;
using Discord;
using TwitchLib.Client.Enums;
using butterBror.Utils.Tools;
using butterBror.Utils.Types;

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
                Engine.Statistics.FunctionsUsed.Add();
                CommandReturn commandReturn = new CommandReturn();

                try
                {
                    if (data.Arguments.Count != 0)
                    {
                        var name = Text.UsernameFilter(data.Arguments.ElementAt(0).ToLower());
                        var userID = Names.GetUserID(name, Platforms.Twitch);
                        var message = MessagesWorker.GetMessage(data.ChannelID, userID, data.Platform);
                        var bages = "";
                        if (message != null)
                        {
                            if (userID != null)
                            {
                                if (name != Engine.Bot.BotName.ToLower())
                                {
                                    if (name == data.User.Name.ToLower())
                                    {
                                        commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "text:you_right_there", data.ChannelID, data.Platform));
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
                                            if (flag) message_badges += TranslationManager.GetTranslation(data.User.Language, symbol, data.ChannelID, data.Platform);
                                        }
                                        var Date = message.messageDate;
                                        commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:last_message", data.ChannelID, data.Platform)
                                            .Replace("&timeAgo&", Text.FormatTimeSpan(Utils.Tools.Format.GetTimeTo(message.messageDate, DateTime.Now, false), data.User.Language))
                                            .Replace("%message%", message.messageText).Replace("%bages%", message_badges)
                                            .Replace("%user%", Names.DontPing(Names.GetUsername(userID, data.Platform))));
                                    }
                                }
                                else
                                {
                                    commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:last_line:bot", data.ChannelID, data.Platform));
                                }
                            }
                            else
                            {
                                commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:user_not_found", data.ChannelID, data.Platform)
                                    .Replace("%user%", Names.DontPing(name)));
                                commandReturn.SetColor(ChatColorPresets.Red);
                            }
                        }
                        else
                        {
                            commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:user_not_found", data.ChannelID, data.Platform)
                                .Replace("%user%", Names.DontPing(name))); // Fix AB1
                            commandReturn.SetColor(ChatColorPresets.Red); // Fix AB1
                        }
                    }
                    else
                    {
                        commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "text:you_right_there", data.ChannelID, data.Platform));
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