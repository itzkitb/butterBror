using butterBror.Utils;
using butterBror;
using Discord;
using System.Data;
using TwitchLib.Client.Enums;

namespace butterBror
{
    public partial class Commands
    {
        public class Calculator
        {
            public static CommandInfo Info = new()
            {
                name = "Calculator",
                author = "@ItzKITb",
                author_link = "twitch.tv/itzkitb",
                author_avatar = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                description = new(){ 
                    { "ru", "Считает циферки" },
                    { "en", "Counts numbers" }
                },
                wiki_link = "https://itzkitb.lol/bot/command?q=math",
                cooldown_per_user = 15,
                cooldown_global = 5,
                aliases = ["calc", "calculate", "кальк", "math", "матем", "математика", "калькулятор"],
                arguments = "[2+2=5]",
                cooldown_reset = true,
                creation_date = DateTime.Parse("07/04/2024"),
                is_for_bot_developer = false,
                is_for_bot_moderator = false,
                is_for_channel_moderator = false,
                platforms = [Platforms.Twitch, Platforms.Telegram, Platforms.Discord]
            };
            public CommandReturn Index(CommandData data)
            {
                Engine.Statistics.functions_used.Add();
                try
                {
                    string input = data.arguments_string;
                    Dictionary<string, string> replacements = new();
                    replacements.Add(",", ".");
                    replacements.Add(":", "/");
                    replacements.Add("÷", "/");
                    replacements.Add("∙", "*");
                    replacements.Add("×", "*");
                    foreach (var replacement in replacements)
                    {
                        input.Replace(replacement.Key, replacement.Value);
                    }
                    string result = "";
                    Color colorResult = Color.Green;
                    ChatColorPresets nicknameColor = ChatColorPresets.YellowGreen;

                    try
                    {
                        double mathResult = Convert.ToDouble(new DataTable().Compute(input, null));

                        if (double.IsInfinity(mathResult))
                        {
                            throw new DivideByZeroException();
                        }

                        result = TextUtil.ArgumentReplacement(TranslationManager.GetTranslation(data.user.language, "command:calculator:result", data.channel_id, data.platform), "result", mathResult.ToString());
                    }
                    catch (DivideByZeroException)
                    {
                        result = TranslationManager.GetTranslation(data.user.language, "error:divide_by_zero", data.channel_id, data.platform);
                        colorResult = Color.Red;
                        nicknameColor = ChatColorPresets.Red;
                    }
                    catch (Exception)
                    {
                        result = TranslationManager.GetTranslation(data.user.language, "error:invalid_mathematical_expression", data.channel_id, data.platform);
                        colorResult = Color.Red;
                        nicknameColor = ChatColorPresets.Red;
                    }

                    return new()
                    {
                        message = result,
                        safe_execute = false,
                        description = "",
                        author = "",
                        image_link = "",
                        thumbnail_link = "",
                        footer = "",
                        is_embed = true,
                        is_ephemeral = false,
                        title = TranslationManager.GetTranslation(data.user.language, "discord:calculator:title", data.channel_id, data.platform),
                        embed_color = colorResult,
                        nickname_color = nicknameColor
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
