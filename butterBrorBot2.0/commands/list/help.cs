using butterBror.Utils;
using Discord;
using System.Reflection;
using TwitchLib.Client.Enums;
using butterBror.Utils.Tools;
using butterBror.Utils.Types;

namespace butterBror
{
    public partial class Commands
    {
        public class Help
        {
            public static CommandInfo Info = new()
            {
                Name = "Help",
                Author = "@ItzKITb",
                AuthorLink = "twitch.tv/itzkitb",
                AuthorAvatar = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                Description = new() { 
                    { "ru", "Чувак, ты думал здесь что-то будет?" }, 
                    { "en", "Dude, did you think something was going to happen here?" } 
                },
                WikiLink = "https://itzkitb.ru/bot/command?name=help",
                CooldownPerUser = 120,
                CooldownPerChannel = 10,
                Aliases = ["help", "помощь", "hlp"],
                Arguments = "(command name)",
                CooldownReset = false,
                CreationDate = DateTime.Parse("09/12/2024"),
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
                    if (data.Arguments.Count == 1)
                    {
                        string classToFind = data.Arguments[0];
                        commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:help:not_found", data.ChannelID, data.Platform));
                        foreach (var classType in Commands.commands)
                        {
                            var infoProperty = classType.GetField("Info", BindingFlags.Static | BindingFlags.Public);
                            var info = infoProperty.GetValue(null) as CommandInfo;

                            if (info.Aliases.Contains(classToFind))
                            {
                                string aliasesList = "";
                                int num = 0;
                                int numWithoutComma = 5;
                                if (info.Aliases.Length < 5)
                                    numWithoutComma = info.Aliases.Length;

                                foreach (string alias in info.Aliases)
                                {
                                    num++;
                                    if (num < numWithoutComma)
                                        aliasesList += $"{Core.Bot.Executor}{alias}, ";
                                    else if (num == numWithoutComma)
                                        aliasesList += $"{Core.Bot.Executor}{alias}";
                                }
                                commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:help", data.ChannelID, data.Platform)
                                    .Replace("%commandName%", info.Name)
                                    .Replace("%Variables%", aliasesList)
                                    .Replace("%Args%", info.Arguments)
                                    .Replace("%Link%", info.WikiLink)
                                    .Replace("%Description%", info.Description[data.User.Language])
                                    .Replace("%Author%", Names.DontPing(info.Author))
                                    .Replace("%creationDate%", info.CreationDate.ToShortDateString())
                                    .Replace("%uCooldown%", info.CooldownPerUser.ToString())
                                    .Replace("%gCooldown%", info.CooldownPerChannel.ToString()));

                                break;
                            }
                        }
                    }
                    else if (data.Arguments.Count > 1)
                        commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:a_few_arguments", data.ChannelID, data.Platform).Replace("%args%", "(command_name)"));
                    else
                        commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "text:bot_info", data.ChannelID, data.Platform));
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
