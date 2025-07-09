using butterBror.Utils;
using Discord;
using System.Data;
using TwitchLib.Client.Enums;
using butterBror.Utils.Tools;
using butterBror.Utils.Types;

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
                Engine.Statistics.FunctionsUsed.Add();
                CommandReturn commandReturn = new CommandReturn();

                try
                {
                    string input = data.ArgumentsString;
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

                        commandReturn.SetMessage(Text.ArgumentReplacement(TranslationManager.GetTranslation(data.User.Language, "command:calculator:result", data.ChannelID, data.Platform), "result", mathResult.ToString()));
                    }
                    catch (DivideByZeroException)
                    {
                        commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:divide_by_zero", data.ChannelID, data.Platform));
                        commandReturn.SetColor(ChatColorPresets.Red);
                    }
                    catch (Exception)
                    {
                        commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:invalid_mathematical_expression", data.ChannelID, data.Platform));
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
