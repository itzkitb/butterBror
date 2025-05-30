using butterBror.Utils;
using butterBror;
using Discord;
using TwitchLib.Client.Enums;

namespace butterBror
{
    public partial class Commands
    {
        public class RandomCMD
        {
            public static CommandInfo Info = new()
            {
                Name = "Random",
                Author = "@ItzKITb",
                AuthorLink = "twitch.tv/itzkitb",
                AuthorAvatar = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                Description = new() {
                    { "ru", "Перемешать текст или вывести рандомное число" },
                    { "en", "Shuffle text or output a random number" }
                },
                WikiLink = "https://itzkitb.lol/bot/command?q=random",
                CooldownPerUser = 5,
                CooldownPerChannel = 1,
                Aliases = ["random", "rnd", "рандом", "ранд"],
                Arguments = "(123-456/text)",
                CooldownReset = true,
                CreationDate = DateTime.Parse("08/08/2024"),
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
                    if (data.arguments.Count > 0)
                    {
                        if (data.arguments_string.Contains('-'))
                        {
                            string[] numbers = data.arguments_string.Split('-');
                            if (numbers.Length == 2 && int.TryParse(numbers[0], out int min) && int.TryParse(numbers[1], out int max))
                                commandReturn.SetMessage($"{TranslationManager.GetTranslation(data.user.language, "command:random", data.channel_id, data.platform)}{new Random().Next(min, max + 1)}");
                            else
                                commandReturn.SetMessage($"{TranslationManager.GetTranslation(data.user.language, "command:random", data.channel_id, data.platform)}{string.Join(" ", [.. data.arguments_string.Split(' ').OrderBy(x => new Random().Next())])}");
                        }
                        else
                            commandReturn.SetMessage($"{TranslationManager.GetTranslation(data.user.language, "command:random", data.channel_id, data.platform)}{string.Join(" ", [.. data.arguments_string.Split(' ').OrderBy(x => new Random().Next())])}");
                    }
                    else
                        commandReturn.SetMessage(TranslationManager.GetTranslation(data.user.language, "command:random", data.channel_id, data.platform) + "DinoDance");
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
