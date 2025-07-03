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
        public class FirstGlobalLine
        {
            public static CommandInfo Info = new()
            {
                Name = "FirstGlobalLine",
                Author = "@ItzKITb",
                AuthorLink = "twitch.tv/itzkitb",
                AuthorAvatar = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                Description = new() {
                    {"ru", "Ваше первое сообщение на платформе" },
                    {"en", "Your first message on the platform" }
                },
                WikiLink = "https://itzkitb.lol/bot/command?q=fgl",
                CooldownPerUser = 10,
                CooldownPerChannel = 1,
                Aliases = ["fgl", "firstgloballine", "пргс", "первоеглобальноесообщение"],
                Arguments = "(name)",
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
                    DateTime now = DateTime.UtcNow;

                    if (data.Arguments.Count != 0)
                    {
                        var name = Text.UsernameFilter(data.Arguments.ElementAt(0).ToLower());
                        var userID = Names.GetUserID(name, data.Platform);
                        if (userID == null)
                        {
                            commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:user_not_found", data.ChannelID, data.Platform)
                                .Replace("%user%", Names.DontPing(name)));
                            commandReturn.SetColor(ChatColorPresets.Red);
                        }
                        else
                        {
                            var firstLine = UsersData.Get<string>(userID, "firstMessage", data.Platform);
                            var firstLineDate = UsersData.Get<DateTime>(userID, "firstSeen", data.Platform);

                            if (name == Core.Bot.BotName.ToLower())
                            {
                                commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:first_global_line:bot", data.ChannelID, data.Platform));
                            }
                            else if (name == data.User.Username)
                            {
                                commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:first_global_line", data.ChannelID, data.Platform)
                                    .Replace("%ago%", Text.FormatTimeSpan(Utils.Tools.Format.GetTimeTo(firstLineDate, now, false), data.User.Language))
                                    .Replace("%message%", firstLine));
                            }
                            else
                            {
                                commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:first_global_line:user", data.ChannelID, data.Platform)
                                    .Replace("%user%", Names.DontPing(Names.GetUsername(userID, data.Platform)))
                                    .Replace("%ago%", Text.FormatTimeSpan(Utils.Tools.Format.GetTimeTo(firstLineDate, now, false), data.User.Language))
                                    .Replace("%message%", firstLine));
                            }
                        }
                    }
                    else
                    {
                        var firstLine = UsersData.Get<string>(data.UserID, "firstMessage", data.Platform);
                        var firstLineDate = UsersData.Get<DateTime>(data.UserID, "firstSeen", data.Platform);

                        commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:first_global_line", data.ChannelID, data.Platform)
                            .Replace("%ago%", Text.FormatTimeSpan(Utils.Tools.Format.GetTimeTo(firstLineDate, now, false), data.User.Language))
                            .Replace("%message%", firstLine));
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
