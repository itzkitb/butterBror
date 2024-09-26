using Jint;
using butterBib;
using Discord;
using TwitchLib.Client.Enums;
using butterBror.Utils;
using butterBror.Utils.DataManagers;

namespace butterBror
{
    public partial class Commands
    {
        public class Java
        {
            public static CommandInfo Info = new()
            {
                Name = "Java",
                Author = "@ItzKITb",
                AuthorURL = "twitch.tv/itzkitb",
                AuthorImageURL = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                Description = "Эта команда помогает вам выполнить java script код и вывести результат его выполнения.",
                UseURL = "https://itzkitb.ru/bot_command/js",
                UserCooldown = 10,
                GlobalCooldown = 5,
                aliases = ["js", "javascript", "джава", "jaba", "supinic", "java"],
                ArgsRequired = "[Код]",
                ResetCooldownIfItHasNotReachedZero = false,
                CreationDate = DateTime.Parse("07/04/2024"),
                ForAdmins = false,
                ForBotCreator = false,
                ForChannelAdmins = false
            };
            public static CommandReturn Index(CommandData data)
            {
                string resultMessage = "";
                Color resultColor = Color.Green;
                ChatColorPresets resultNicknameColor = ChatColorPresets.YellowGreen;

                if (NoBanwords.fullCheck(data.ArgsAsString, data.ChannelID))
                {
                    try
                    {
                        var engine = new Engine(cfg => cfg
        .LimitRecursion(100)
        .LimitMemory(40 * 1024 * 1024)
        .Strict()
        .LocalTimeZone(TimeZoneInfo.Utc));
                        var isSafe = true;
                        engine.SetValue("navigator", new Action(() => isSafe = false));
                        engine.SetValue("WebSocket", new Action(() => isSafe = false));
                        engine.SetValue("XMLHttpRequest", new Action(() => isSafe = false));
                        engine.SetValue("fetch", new Action(() => isSafe = false));
                        string jsCode = data.ArgsAsString;
                        var result = engine.Evaluate(jsCode);

                        if (isSafe)
                        {
                            resultMessage = TranslationManager.GetTranslation(data.User.Lang, "jsResult", data.ChannelID)
                                .Replace("%result%", result.ToString());
                        }
                        else
                        {
                            resultColor = Color.Red;
                            resultNicknameColor = ChatColorPresets.OrangeRed;
                            resultMessage = TranslationManager.GetTranslation(data.User.Lang, "jsAccess", data.ChannelID);
                        }
                    }
                    catch (Exception ex)
                    {
                        resultColor = Color.Red;
                        resultNicknameColor = ChatColorPresets.Firebrick;
                        resultMessage = "/me " + TranslationManager.GetTranslation(data.User.Lang, "jsError", data.ChannelID)
                            .Replace("%err%", ex.Message);
                        LogWorker.Log(ex.Message, LogWorker.LogTypes.Err, "js");
                    }
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
