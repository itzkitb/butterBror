using static butterBror.BotWorker.FileMng;
using static butterBror.BotWorker;
using butterBib;
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
                AuthorURL = "twitch.tv/itzkitb",
                AuthorImageURL = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                Description = "Эта команда перемешивает текст либо-же выводит рандомное число.",
                UseURL = "https://itzkitb.ru/bot_command/tuck",
                UserCooldown = 5,
                GlobalCooldown = 1,
                aliases = ["random", "rnd", "рандом", "ранд"],
                ArgsRequired = "(Диапазон чисел [число-число]/Слова разделенные пробелом)",
                ResetCooldownIfItHasNotReachedZero = true,
                CreationDate = DateTime.Parse("08/08/2024"),
                ForAdmins = false,
                ForBotCreator = false,
                ForChannelAdmins = false
            };
            public static CommandReturn Index(CommandData data)
            {
                string resultMessage = "";
                Color resultColor = Color.Green;
                ChatColorPresets resultNicknameColor = ChatColorPresets.YellowGreen;
                if (data.args.Count > 0)
                {
                    if (data.ArgsAsString.Contains('-'))
                    {
                        string[] numbers = data.ArgsAsString.Split('-');
                        if (numbers.Length == 2 && int.TryParse(numbers[0], out int min) && int.TryParse(numbers[1], out int max))
                        {
                            Random rand = new Random();
                            int randomNum = rand.Next(min, max + 1);
                            resultMessage = $"{TranslationManager.GetTranslation(data.User.Lang, "randomNum", data.ChannelID)}{randomNum}";
                        }
                        else
                        {
                            string[] words = data.ArgsAsString.Split(' ');
                            Random rand = new Random();
                            string[] shuffledWords = words.OrderBy(x => rand.Next()).ToArray();
                            resultMessage = $"{TranslationManager.GetTranslation(data.User.Lang, "randomTxt", data.ChannelID)}{string.Join(" ", shuffledWords)}";
                        }
                    }
                    else
                    {
                        string[] words = data.ArgsAsString.Split(' ');
                        Random rand = new Random();
                        string[] shuffledWords = words.OrderBy(x => rand.Next()).ToArray();
                        resultMessage = $"{TranslationManager.GetTranslation(data.User.Lang, "randomTxt", data.ChannelID)}{string.Join(" ", shuffledWords)}";
                    }
                }
                else
                {
                    resultMessage = TranslationManager.GetTranslation(data.User.Lang, "randomTxt", data.ChannelID) + "DinoDance";
                }
                return new()
                {
                    Message = resultMessage,
                    IsSafeExecute = false,
                    Description = "",
                    Author = "",
                    ImageURL = "",
                    ThumbnailUrl = "",
                    Footer = "",
                    IsEmbed = false,
                    Ephemeral = false,
                    Title = "",
                    Color = resultColor,
                    NickNameColor = resultNicknameColor
                };
            }
        }
    }
}
