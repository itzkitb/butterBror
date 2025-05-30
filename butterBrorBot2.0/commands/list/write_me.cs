using butterBror.Utils;
using butterBror;
using Discord;
using TwitchLib.Client.Enums;


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
                Engine.Statistics.functions_used.Add();
                CommandReturn commandReturn = new CommandReturn();

                try
                {
                    if (TextUtil.CleanAsciiWithoutSpaces(data.arguments_string) != "")
                    {
                        string[] blockedEntries = ["/", "$", "#", "+", "-", ">", "<", "*", "\\", ";"];
                        string meMessage = TextUtil.CleanAscii(data.arguments_string);
                        while (true)
                        {
                            while (meMessage.StartsWith(' '))
                            {
                                meMessage = (string)meMessage.Skip(1);
                            }

                            if (meMessage.StartsWith('!'))
                            {
                                meMessage = "❗" + meMessage.Skip(1);
                                break;
                            }

                            foreach (string blockedEntry in blockedEntries)
                            {
                                if (meMessage.StartsWith(blockedEntry))
                                {
                                    meMessage = (string)meMessage.Skip(blockedEntry.Length);
                                    break;
                                }
                            }
                        }
                        commandReturn.SetMessage($"/me \u2063 {meMessage}");
                    }
                    else
                    {
                        commandReturn.SetMessage("/me " + TranslationManager.GetTranslation(data.user.language, "text:ad", data.channel_id, data.platform));
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