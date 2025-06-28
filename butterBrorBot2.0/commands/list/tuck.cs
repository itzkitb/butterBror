using butterBror.Utils;
using butterBror.Utils.DataManagers;
using butterBror;
using Discord;
using TwitchLib.Client.Enums;
using Microsoft.CodeAnalysis;
using butterBror.Utils.Tools;
using static butterBror.Utils.Things.Console;

namespace butterBror
{
    public partial class Commands
    {
        public class Tuck
        {
            public static CommandInfo Info = new()
            {
                Name = "Tuck",
                Author = "@ItzKITb",
                AuthorLink = "twitch.tv/itzkitb",
                AuthorAvatar = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                Description = new()
                {
                    { "ru", "Спокойной ночи... 👁" },
                    { "en", "Good night... 👁" }
                },
                WikiLink = "https://itzkitb.lol/bot/command?q=tuck",
                CooldownPerUser = 5,
                CooldownPerChannel = 1,
                Aliases = ["tuck", "уложить", "tk", "улож", "тык"],
                Arguments = "(name) (text)",
                CooldownReset = true,
                CreationDate = DateTime.Parse("07/04/2024"),
                IsForBotModerator = false,
                IsForBotDeveloper = false,
                IsForChannelModerator = false,
                Platforms = [Platforms.Twitch, Platforms.Telegram, Platforms.Discord]
            };

            [ConsoleSector("butterBror.Commands.Tuck", "Index")]
            public CommandReturn Index(CommandData data)
            {
                Core.Statistics.FunctionsUsed.Add();
                CommandReturn commandReturn = new CommandReturn();

                try
                {
                    commandReturn.SetColor(ChatColorPresets.HotPink);
                    if (data.arguments.Count >= 1)
                    {
                        var username = Text.UsernameFilter(Text.CleanAsciiWithoutSpaces(data.arguments[0]));
                        var isSelectedUserIsNotIgnored = true;
                        var userID = Names.GetUserID(username.ToLower(), Platforms.Twitch);
                        try
                        {
                            if (userID != null)
                                isSelectedUserIsNotIgnored = !UsersData.Get<bool>(userID, "isIgnored", data.platform);
                        }
                        catch (Exception) { }
                        if (username.ToLower() == Core.Bot.BotName.ToLower())
                        {
                            commandReturn.SetMessage(TranslationManager.GetTranslation(data.user.language, "command:tuck:bot", data.channel_id, data.platform));
                            commandReturn.SetColor(ChatColorPresets.CadetBlue);
                        }
                        else if (isSelectedUserIsNotIgnored)
                        {
                            if (data.arguments.Count >= 2)
                            {
                                List<string> list = data.arguments;
                                list.RemoveAt(0);
                                commandReturn.SetMessage(TranslationManager.GetTranslation(data.user.language, "command:tuck:text", data.channel_id, data.platform).Replace("%user%", Names.DontPing(username)).Replace("%text%", string.Join(" ", list)));
                            }
                            else
                            {
                                commandReturn.SetMessage(TranslationManager.GetTranslation(data.user.language, "command:tuck", data.channel_id, data.platform).Replace("%user%", Names.DontPing(username)));
                            }
                        }
                        else
                        {
                            Write($"User @{data.user.username} tried to put a user to sleep who is in the ignore list", "info");
                            commandReturn.SetMessage(TranslationManager.GetTranslation(data.user.language, "error:user_ignored", data.channel_id, data.platform));
                        }
                    }
                    else
                    {
                        commandReturn.SetMessage(TranslationManager.GetTranslation(data.user.language, "command:tuck:none", data.channel_id, data.platform));
                        commandReturn.SetColor(ChatColorPresets.CadetBlue);
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
