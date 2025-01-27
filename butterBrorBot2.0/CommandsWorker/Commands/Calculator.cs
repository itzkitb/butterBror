using butterBror.Utils;
using butterBib;
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
                Name = "Calculator",
                Author = "@ItzKITb",
                AuthorURL = "twitch.tv/itzkitb",
                AuthorImageURL = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                Description = "Эта команда помогает вам мгновенно справиться с любым математическим примером.",
                UseURL = "https://itzkitb.ru/bot/command?name=math",
                UserCooldown = 15,
                GlobalCooldown = 5,
                aliases = ["calc", "calculate", "кальк", "math", "матем", "математика", "калькулятор"],
                ArgsRequired = "[Математическое выражение]",
                ResetCooldownIfItHasNotReachedZero = true,
                CreationDate = DateTime.Parse("07/04/2024"),
                ForBotCreator = false,
                ForAdmins = false,
                ForChannelAdmins = false,
                AllowedPlatforms = [Platforms.Twitch, Platforms.Telegram, Platforms.Discord]
            };
            public static CommandReturn Index(CommandData data)
            {
                try
                {
                    string input = data.ArgsAsString;
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

                        result = TranslationManager.GetTranslation(data.User.Lang, "mathResult", data.ChannelID).Replace("%result%", mathResult.ToString());
                    }
                    catch (DivideByZeroException)
                    {
                        result = TranslationManager.GetTranslation(data.User.Lang, "divisionByZeroError", data.ChannelID);
                        colorResult = Color.Red;
                        nicknameColor = ChatColorPresets.Red;
                    }
                    catch (EvaluateException)
                    {
                        result = TranslationManager.GetTranslation(data.User.Lang, "wrongMath", data.ChannelID);
                        colorResult = Color.Red;
                        nicknameColor = ChatColorPresets.Red;
                    }
                    catch (Exception)
                    {
                        result = TranslationManager.GetTranslation(data.User.Lang, "wrongMath", data.ChannelID);
                        colorResult = Color.Red;
                        nicknameColor = ChatColorPresets.Red;
                    }

                    return new()
                    {
                        Message = result,
                        IsSafeExecute = false,
                        Description = "",
                        Author = "",
                        ImageURL = "",
                        ThumbnailUrl = "",
                        Footer = "",
                        IsEmbed = true,
                        Ephemeral = false,
                        Title = TranslationManager.GetTranslation(data.User.Lang, "dsCalcTitle", data.ChannelID),
                        Color = colorResult,
                        NickNameColor = nicknameColor
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
