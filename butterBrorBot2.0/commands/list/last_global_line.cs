using butterBror.Utils;
using butterBror.Utils.DataManagers;
using butterBror;
using Discord;
using TwitchLib.Client.Enums;
using butterBror.Utils.Tools;

namespace butterBror
{
    public partial class Commands
    {
        public class LastGlobalLine
        {
            public static CommandInfo Info = new()
            {
                Name = "LastGlobalLine",
                Author = "@ItzKITb",
                AuthorLink = "twitch.tv/itzkitb",
                AuthorAvatar = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                Description = new() { 
                    { "ru", "Последнее сообщение выбранного пользователя" }, 
                    { "en", "The last message of the selected user" } 
                },
                WikiLink = "https://itzkitb.lol/bot/command?q=lgl",
                CooldownPerUser = 10,
                CooldownPerChannel = 1,
                Aliases = ["lgl", "lastgloballine", "пгс", "последнееглобальноесообщение"],
                Arguments = "[name]",
                CooldownReset = true,
                CreationDate = DateTime.Parse("07/04/2024"),
                IsForBotModerator = false,
                IsForBotDeveloper = false,
                IsForChannelModerator = false,
                Platforms = [Platforms.Twitch, Platforms.Telegram, Platforms.Discord]
            };
            public CommandReturn Index(CommandData data)
            {
                Core.Statistics.FunctionsUsed.Add();
                CommandReturn commandReturn = new CommandReturn();

                try
                {
                    if (data.arguments.Count != 0)
                    {
                        var name = Text.UsernameFilter(data.arguments.ElementAt(0).ToLower());
                        var userID = Names.GetUserID(name, Platforms.Twitch);
                        if (userID == null)
                        {
                            commandReturn.SetMessage(TranslationManager.GetTranslation(data.user.language, "error:user_not_found", data.channel_id, data.platform)
                                .Replace("%user%", Names.DontPing(name)));
                            commandReturn.SetColor(ChatColorPresets.Red);
                        }
                        else
                        {
                            var lastLine = UsersData.Get<string>(userID, "lastSeenMessage", data.platform);
                            var lastLineDate = UsersData.Get<DateTime>(userID, "lastSeen", data.platform);
                            DateTime now = DateTime.UtcNow;
                            if (name == Core.Bot.BotName.ToLower())
                            {
                                commandReturn.SetMessage(TranslationManager.GetTranslation(data.user.language, "command:last_global_line:bot", data.channel_id, data.platform));
                            }
                            else if (name == data.user.username.ToLower())
                            {
                                commandReturn.SetMessage(TranslationManager.GetTranslation(data.user.language, "text:you_right_there", data.channel_id, data.platform));
                            }
                            else
                            {
                                commandReturn.SetMessage(TranslationManager.GetTranslation(data.user.language, "command:last_global_line", data.channel_id, data.platform)
                                    .Replace("%user%", Names.DontPing(Names.GetUsername(userID, data.platform)))
                                    .Replace("&timeAgo&", Text.FormatTimeSpan(Utils.Tools.Format.GetTimeTo(lastLineDate, now, false), data.user.language))
                                    .Replace("%message%", lastLine));
                            }
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
