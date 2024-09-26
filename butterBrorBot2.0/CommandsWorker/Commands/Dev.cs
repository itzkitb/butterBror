using butterBib;
using butterBror.Utils;
using Discord;
using Microsoft.CSharp;
using System.CodeDom.Compiler;
using TwitchLib.Client.Enums;

namespace butterBror
{
    public partial class Commands
    {
        public class Dev
        {
            public static CommandInfo Info = new()
            {
                Name = "Dev",
                Author = "@ItzKITb",
                AuthorURL = "twitch.tv/itzkitb",
                AuthorImageURL = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                Description = "Эта команда выполняет C# код и выводит его результат.",
                UseURL = "NONE",
                UserCooldown = 0,
                GlobalCooldown = 0,
                aliases = ["run", "code", "c#", "dev"],
                ArgsRequired = "(C# код)",
                ResetCooldownIfItHasNotReachedZero = true,
                CreationDate = DateTime.Parse("09/26/2024"),
                ForAdmins = false,
                ForBotCreator = true,
                ForChannelAdmins = false
            };
            public static CommandReturn Index(CommandData data)
            {
                string resultMessage = "";
                Color resultColor = Color.Green;
                ChatColorPresets resultNicknameColor = ChatColorPresets.YellowGreen;

                try
                {
                    DateTime StartTime = DateTime.Now;
                    var provider = new CSharpCodeProvider();
                    var parameters = new CompilerParameters();
                    parameters.GenerateInMemory = true;
                    parameters.GenerateExecutable = false;
                    var results = provider.CompileAssemblyFromSource(parameters, data.ArgsAsString);

                    if (results.Errors.Count > 0)
                    {
                        DateTime EndTime = DateTime.Now;
                        resultMessage = TranslationManager.GetTranslation(data.User.Lang, "c#_code:error", data.ChannelID)
                            .Replace("%time%", TextUtil.FormatTimeSpan(EndTime - StartTime, data.User.Lang))
                            .Replace("%result%", results.Errors[0].ErrorText);
                    }
                    else
                    {
                        var assembly = results.CompiledAssembly;
                        var programType = assembly.GetType("Program");
                        var mainMethod = programType.GetMethod("Main");
                        var result = mainMethod.Invoke(null, null);
                        DateTime EndTime = DateTime.Now;
                        // resultMessage = TranslationManager.GetTranslation(data.User.Lang, "c#_code:error", data.ChannelID)
                        //    .Replace("%time%", TextUtil.FormatTimeSpan(EndTime - StartTime, data.User.Lang))
                        //    .Replace("%result%", result);
                    }
                }
                catch (Exception ex)
                {

                }


                return new ()
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
