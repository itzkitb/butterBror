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
                Engine.Statistics.functions_used.Add();
                CommandReturn commandReturn = new CommandReturn();

                try
                {
                    DateTime now = DateTime.UtcNow;

                    if (data.arguments.Count != 0)
                    {
                        var name = TextUtil.UsernameFilter(data.arguments.ElementAt(0).ToLower());
                        var userID = Names.GetUserID(name, data.platform);
                        if (userID == null)
                        {
                            commandReturn.SetMessage(TranslationManager.GetTranslation(data.user.language, "error:user_not_found", data.channel_id, data.platform)
                                .Replace("%user%", Names.DontPing(name)));
                            commandReturn.SetColor(ChatColorPresets.Red);
                        }
                        else
                        {
                            var firstLine = UsersData.Get<string>(userID, "firstMessage", data.platform);
                            var firstLineDate = UsersData.Get<DateTime>(userID, "firstSeen", data.platform);

                            if (name == Maintenance.twitch_client.TwitchUsername.ToLower())
                            {
                                commandReturn.SetMessage(TranslationManager.GetTranslation(data.user.language, "command:first_global_line:bot", data.channel_id, data.platform));
                            }
                            else if (name == data.user.username)
                            {
                                commandReturn.SetMessage(TranslationManager.GetTranslation(data.user.language, "command:first_global_line", data.channel_id, data.platform)
                                    .Replace("%ago%", TextUtil.FormatTimeSpan(Utils.Format.GetTimeTo(firstLineDate, now, false), data.user.language))
                                    .Replace("%message%", firstLine));
                            }
                            else
                            {
                                commandReturn.SetMessage(TranslationManager.GetTranslation(data.user.language, "command:first_global_line:user", data.channel_id, data.platform)
                                    .Replace("%user%", Names.DontPing(Names.GetUsername(userID, data.platform)))
                                    .Replace("%ago%", TextUtil.FormatTimeSpan(Utils.Format.GetTimeTo(firstLineDate, now, false), data.user.language))
                                    .Replace("%message%", firstLine));
                            }
                        }
                    }
                    else
                    {
                        var firstLine = UsersData.Get<string>(data.user_id, "firstMessage", data.platform);
                        var firstLineDate = UsersData.Get<DateTime>(data.user_id, "firstSeen", data.platform);

                        commandReturn.SetMessage(TranslationManager.GetTranslation(data.user.language, "command:first_global_line", data.channel_id, data.platform)
                            .Replace("%ago%", TextUtil.FormatTimeSpan(Utils.Format.GetTimeTo(firstLineDate, now, false), data.user.language))
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
