using butterBror.Utils;
using Discord;
using TwitchLib.Client.Enums;
using butterBror.Utils.Tools;
using butterBror.Utils.Types;


namespace butterBror
{
    public partial class Commands
    {
        public class Me
        {
            public static CommandInfo Info = new()
            {
                Name = "Me",
                Author = "@ItzKITb",
                AuthorLink = "twitch.tv/itzkitb",
                AuthorAvatar = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                Description = new()
                {
                    { "ru", "Эта команда... Просто зачем-то существует" },
                    { "en", "This command... Just exists for some reason" }
                },
                WikiLink = "https://itzkitb.ru/bot/command?name=me",
                CooldownPerUser = 15,
                CooldownPerChannel = 5,
                Aliases = ["me", "m", "я"],
                Arguments = "[text]",
                CooldownReset = true,
                CreationDate = DateTime.Parse("07/04/2024"),
                IsForBotModerator = false,
                IsForBotDeveloper = false,
                IsForChannelModerator = false,
                Platforms = [Platforms.Twitch]
            };
            public CommandReturn Index(CommandData data)
            {
                Engine.Statistics.FunctionsUsed.Add();
                CommandReturn commandReturn = new CommandReturn();

                try
                {
                    if (Text.CleanAsciiWithoutSpaces(data.ArgumentsString) != "")
                    {
                        string[] blockedEntries = ["/", "$", "#", "+", "-", ">", "<", "*", "\\", ";"];
                        string meMessage = Text.CleanAscii(data.ArgumentsString);
                        while (true)
                        {
                            while (meMessage.StartsWith(' '))
                            {
                                meMessage = string.Join("", meMessage.Skip(1)); // AB6 fix
                            }

                            if (meMessage.StartsWith('!'))
                            {
                                meMessage = "❗" + string.Join("", meMessage.Skip(1)); // AB6 fix
                                break;
                            }

                            foreach (string blockedEntry in blockedEntries)
                            {
                                if (meMessage.StartsWith(blockedEntry))
                                {
                                    meMessage = string.Join("", meMessage.Skip(blockedEntry.Length)); // AB6 fix
                                    break;
                                }
                            }

                            break; // AB5 fix
                        }
                        commandReturn.SetMessage($"/me \u2063 {meMessage}");
                    }
                    else
                    {
                        commandReturn.SetMessage("/me " + TranslationManager.GetTranslation(data.User.Language, "text:ad", data.ChannelID, data.Platform));
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