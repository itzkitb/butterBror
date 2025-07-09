using butterBror.Utils;
using butterBror.Utils.DataManagers;
using Discord;
using TwitchLib.Client.Enums;
using Microsoft.CodeAnalysis;
using butterBror.Utils.Tools;
using static butterBror.Utils.Bot.Console;
using butterBror.Utils.Types;

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
                Engine.Statistics.FunctionsUsed.Add();
                CommandReturn commandReturn = new CommandReturn();

                try
                {
                    commandReturn.SetColor(ChatColorPresets.HotPink);
                    if (data.Arguments.Count >= 1)
                    {
                        var username = Text.UsernameFilter(Text.CleanAsciiWithoutSpaces(data.Arguments[0]));
                        var isSelectedUserIsNotIgnored = true;
                        var userID = Names.GetUserID(username.ToLower(), Platforms.Twitch);
                        try
                        {
                            if (userID != null)
                                isSelectedUserIsNotIgnored = !UsersData.Get<bool>(userID, "isIgnored", data.Platform);
                        }
                        catch (Exception) { }
                        if (username.ToLower() == Engine.Bot.BotName.ToLower())
                        {
                            commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:tuck:bot", data.ChannelID, data.Platform));
                            commandReturn.SetColor(ChatColorPresets.CadetBlue);
                        }
                        else if (isSelectedUserIsNotIgnored)
                        {
                            if (data.Arguments.Count >= 2)
                            {
                                List<string> list = data.Arguments;
                                list.RemoveAt(0);
                                commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:tuck:text", data.ChannelID, data.Platform).Replace("%user%", Names.DontPing(username)).Replace("%text%", string.Join(" ", list)));
                            }
                            else
                            {
                                commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:tuck", data.ChannelID, data.Platform).Replace("%user%", Names.DontPing(username)));
                            }
                        }
                        else
                        {
                            Write($"User @{data.User.Name} tried to put a user to sleep who is in the ignore list", "info");
                            commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:user_ignored", data.ChannelID, data.Platform));
                        }
                    }
                    else
                    {
                        commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:tuck:none", data.ChannelID, data.Platform));
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
