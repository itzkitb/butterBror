using butterBror.Utils;
using butterBror;
using Discord;
using System.Data;
using TwitchLib.Client.Enums;
using butterBror.Utils.Tools;

namespace butterBror
{
    public partial class Commands
    {
        public class Calculator
        {
            public static CommandInfo Info = new()
            {
                Name = "Calculator",
                Author = "@ItzKITb",
                AuthorLink = "twitch.tv/itzkitb",
                AuthorAvatar = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                Description = new(){ 
                    { "ru", "Считает циферки" },
                    { "en", "Counts numbers" }
                },
                WikiLink = "https://itzkitb.lol/bot/command?q=math",
                CooldownPerUser = 15,
                CooldownPerChannel = 5,
                Aliases = ["calc", "calculate", "кальк", "math", "матем", "математика", "калькулятор"],
                Arguments = "[2+2=5]",
                CooldownReset = true,
                CreationDate = DateTime.Parse("07/04/2024"),
                IsForBotDeveloper = false,
                IsForBotModerator = false,
                IsForChannelModerator = false,
                Platforms = [Platforms.Twitch, Platforms.Telegram, Platforms.Discord]
            };
            public CommandReturn Index(CommandData data)
            {
                Core.Statistics.FunctionsUsed.Add();
                CommandReturn commandReturn = new CommandReturn();

                try
                {
                    string input = data.arguments_string;
                    Dictionary<string, string> replacements = new() {
                        { ",", "." },
                        { ":", "/" },
                        { "÷", "/" },
                        { "∙", "*" },
                        { "×", "*" }
                    };
                    foreach (var replacement in replacements)
                    {
                        input.Replace(replacement.Key, replacement.Value);
                    }

                    try
                    {
                        double mathResult = Convert.ToDouble(new DataTable().Compute(input, null));

                        if (double.IsInfinity(mathResult))
                        {
                            throw new DivideByZeroException();
                        }

                        commandReturn.SetMessage(Text.ArgumentReplacement(TranslationManager.GetTranslation(data.user.language, "command:calculator:result", data.channel_id, data.platform), "result", mathResult.ToString()));
                    }
                    catch (DivideByZeroException)
                    {
                        commandReturn.SetMessage(TranslationManager.GetTranslation(data.user.language, "error:divide_by_zero", data.channel_id, data.platform));
                        commandReturn.SetColor(ChatColorPresets.Red);
                    }
                    catch (Exception)
                    {
                        commandReturn.SetMessage(TranslationManager.GetTranslation(data.user.language, "error:invalid_mathematical_expression", data.channel_id, data.platform));
                        commandReturn.SetColor(ChatColorPresets.Red);
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
