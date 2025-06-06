using Jint;
using Discord;
using TwitchLib.Client.Enums;
using butterBror.Utils;
using butterBror.Utils.DataManagers;
using butterBror;

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
                AuthorLink = "twitch.tv/itzkitb",
                AuthorAvatar = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                Description = new() { 
                    { "ru", "{}+[]=?" },
                    { "en", "{}+[]=?" } 
                },
                WikiLink = "https://itzkitb.lol/bot/command?q=jaba",
                CooldownPerUser = 10,
                CooldownPerChannel = 5,
                Aliases = ["js", "javascript", "джаваскрипт", "жс", "jabascript", "supinic"], // Fix AB0
                Arguments = "[code]",
                CooldownReset = false,
                CreationDate = DateTime.Parse("07/04/2024"),
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
                    if (new NoBanwords().Check(data.arguments_string, data.channel_id, data.platform))
                    {
                        try
                        {
                            var engine = new Jint.Engine(cfg => cfg
            .LimitRecursion(100)
            .LimitMemory(40 * 1024 * 1024)
            .Strict()
            .LocalTimeZone(TimeZoneInfo.Utc));
                            var isSafe = true;
                            engine.SetValue("navigator", new Action(() => isSafe = false));
                            engine.SetValue("WebSocket", new Action(() => isSafe = false));
                            engine.SetValue("XMLHttpRequest", new Action(() => isSafe = false));
                            engine.SetValue("fetch", new Action(() => isSafe = false));
                            string jsCode = data.arguments_string;
                            var result = engine.Evaluate(jsCode);

                            if (isSafe)
                            {
                                commandReturn.SetMessage(TranslationManager.GetTranslation(data.user.language, "command:js", data.channel_id, data.platform)
                                    .Replace("%result%", result.ToString()));
                            }
                            else
                            {
                                commandReturn.SetColor(ChatColorPresets.OrangeRed);
                                commandReturn.SetMessage(TranslationManager.GetTranslation(data.user.language, "error:js", data.channel_id, data.platform).Replace("%err%", "Not allowed"));
                            }
                        }
                        catch (Exception ex)
                        {
                            commandReturn.SetColor(ChatColorPresets.Firebrick);
                            commandReturn.SetMessage("/me " + TranslationManager.GetTranslation(data.user.language, "error:js", data.channel_id, data.platform)
                                .Replace("%err%", ex.Message));
                            LogWorker.Log(ex.Message, LogWorker.LogTypes.Err, "command\\Java\\Index");
                        }
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
