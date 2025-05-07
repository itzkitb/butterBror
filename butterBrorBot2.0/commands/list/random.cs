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
                name = "Random",
                author = "@ItzKITb",
                author_link = "twitch.tv/itzkitb",
                author_avatar = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                description = new() {
                    { "ru", "Перемешать текст или вывести рандомное число" },
                    { "en", "Shuffle text or output a random number" }
                },
                wiki_link = "https://itzkitb.lol/bot/command?q=random",
                cooldown_per_user = 5,
                cooldown_global = 1,
                aliases = ["random", "rnd", "рандом", "ранд"],
                arguments = "(123-456/text)",
                cooldown_reset = true,
                creation_date = DateTime.Parse("08/08/2024"),
                is_for_bot_moderator = false,
                is_for_bot_developer = false,
                is_for_channel_moderator = false,
                platforms = [Platforms.Twitch, Platforms.Telegram, Platforms.Discord]
            };
            public CommandReturn Index(CommandData data)
            {
                Engine.Statistics.functions_used.Add();
                try
                {
                    string resultMessage = "";
                    Color resultColor = Color.Green;
                    ChatColorPresets resultNicknameColor = ChatColorPresets.YellowGreen;
                    bool IsError = false;
                    try
                    {
                        if (data.arguments.Count > 0)
                        {
                            if (data.arguments_string.Contains('-'))
                            {
                                string[] numbers = data.arguments_string.Split('-');
                                if (numbers.Length == 2 && int.TryParse(numbers[0], out int min) && int.TryParse(numbers[1], out int max))
                                    resultMessage = $"{TranslationManager.GetTranslation(data.user.language, "command:random", data.channel_id, data.platform)}{new Random().Next(min, max + 1)}";
                                else
                                    resultMessage = $"{TranslationManager.GetTranslation(data.user.language, "command:random", data.channel_id, data.platform)}{string.Join(" ", [.. data.arguments_string.Split(' ').OrderBy(x => new Random().Next())])}";
                            }
                            else
                                resultMessage = $"{TranslationManager.GetTranslation(data.user.language, "command:random", data.channel_id, data.platform)}{string.Join(" ", [.. data.arguments_string.Split(' ').OrderBy(x => new Random().Next())])}";
                        }
                        else
                            resultMessage = TranslationManager.GetTranslation(data.user.language, "command:random", data.channel_id, data.platform) + "DinoDance";
                    }
                    catch (Exception e)
                    {
                        Utils.Console.WriteError(e, $"Command\\Bot\\Random");
                        IsError = true;
                    }
                    return new()
                    {
                        message = resultMessage,
                        safe_execute = false,
                        description = "",
                        author = "",
                        image_link = "",
                        thumbnail_link = "",
                        footer = "",
                        is_embed = false,
                        is_ephemeral = false,
                        title = "",
                        embed_color = resultColor,
                        nickname_color = resultNicknameColor
                    };
                }
                catch (Exception e)
                {
                    return new()
                    {
                        message = "",
                        safe_execute = false,
                        description = "",
                        author = "",
                        image_link = "",
                        thumbnail_link = "",
                        footer = "",
                        is_embed = true,
                        is_ephemeral = false,
                        title = "",
                        embed_color = Color.Green,
                        nickname_color = ChatColorPresets.YellowGreen,
                        is_error = true,
                        exception = e
                    };
                }
            }
        }
    }
}
