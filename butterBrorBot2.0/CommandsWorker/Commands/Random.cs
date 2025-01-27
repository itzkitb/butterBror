using butterBror.Utils;
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
                UseURL = "https://itzkitb.ru/bot/command?name=random",
                UserCooldown = 5,
                GlobalCooldown = 1,
                aliases = ["random", "rnd", "рандом", "ранд"],
                ArgsRequired = "(Диапазон чисел [число-число]/Слова разделенные пробелом)",
                ResetCooldownIfItHasNotReachedZero = true,
                CreationDate = DateTime.Parse("08/08/2024"),
                ForAdmins = false,
                ForBotCreator = false,
                ForChannelAdmins = false,
                AllowedPlatforms = [Platforms.Twitch, Platforms.Telegram, Platforms.Discord]
            };
            public static CommandReturn Index(CommandData data)
            {
                try
                {
                    string resultMessage = "";
                    Color resultColor = Color.Green;
                    ChatColorPresets resultNicknameColor = ChatColorPresets.YellowGreen;
                    var Task = new TasksDebugUtil();
                    bool IsError = false;
                    try
                    {
                        Task.SetTask(1);
                        if (data.args.Count > 0)
                        {
                            Task.SetTask(5);
                            if (data.ArgsAsString.Contains('-'))
                            {
                                Task.SetTask(6);
                                string[] numbers = data.ArgsAsString.Split('-');
                                Task.SetTask(7);
                                if (numbers.Length == 2 && int.TryParse(numbers[0], out int min) && int.TryParse(numbers[1], out int max))
                                {
                                    Task.SetTask(8);
                                    Random rand = new Random();
                                    int randomNum = rand.Next(min, max + 1);
                                    Task.SetTask(9);
                                    resultMessage = $"{TranslationManager.GetTranslation(data.User.Lang, "randomNum", data.ChannelID)}{randomNum}";
                                }
                                else
                                {
                                    Task.SetTask(10);
                                    string[] words = data.ArgsAsString.Split(' ');
                                    Random rand = new Random();
                                    string[] shuffledWords = words.OrderBy(x => rand.Next()).ToArray();
                                    Task.SetTask(11);
                                    resultMessage = $"{TranslationManager.GetTranslation(data.User.Lang, "randomTxt", data.ChannelID)}{string.Join(" ", shuffledWords)}";
                                }
                            }
                            else
                            {
                                Task.SetTask(3);
                                string[] words = data.ArgsAsString.Split(' ');
                                Random rand = new Random();
                                string[] shuffledWords = words.OrderBy(x => rand.Next()).ToArray();
                                Task.SetTask(4);
                                resultMessage = $"{TranslationManager.GetTranslation(data.User.Lang, "randomTxt", data.ChannelID)}{string.Join(" ", shuffledWords)}";
                            }
                        }
                        else
                        {
                            Task.SetTask(2);
                            resultMessage = TranslationManager.GetTranslation(data.User.Lang, "randomTxt", data.ChannelID) + "DinoDance";
                        }
                    }
                    catch (Exception e)
                    {
                        ConsoleUtil.ErrorOccured(e, $"Command\\Bot\\Random#{Task.GetTask()}");
                        IsError = true;
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
                catch (Exception e)
                {
                    return new()
                    {
                        Message = "",
                        IsSafeExecute = false,
                        Description = "",
                        Author = "",
                        ImageURL = "",
                        ThumbnailUrl = "",
                        Footer = "",
                        IsEmbed = true,
                        Ephemeral = false,
                        Title = "",
                        Color = Color.Green,
                        NickNameColor = ChatColorPresets.YellowGreen,
                        IsError = true,
                        Error = e
                    };
                }
            }
        }
    }
}
