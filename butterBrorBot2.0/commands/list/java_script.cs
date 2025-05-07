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
                name = "Java",
                author = "@ItzKITb",
                author_link = "twitch.tv/itzkitb",
                author_avatar = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                description = new() { 
                    { "ru", "{}+[]=?" },
                    { "en", "{}+[]=?" } 
                },
                wiki_link = "https://itzkitb.lol/bot/command?q=jaba",
                cooldown_per_user = 10,
                cooldown_global = 5,
                aliases = ["js", "javascript", "джава", "jaba", "supinic", "java"],
                arguments = "[code]",
                cooldown_reset = false,
                creation_date = DateTime.Parse("07/04/2024"),
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

                    if (NoBanwords.Check(data.arguments_string, data.channel_id, data.platform))
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
                                resultMessage = TranslationManager.GetTranslation(data.user.language, "command:js", data.channel_id, data.platform)
                                    .Replace("%result%", result.ToString());
                            }
                            else
                            {
                                resultColor = Color.Red;
                                resultNicknameColor = ChatColorPresets.OrangeRed;
                                resultMessage = TranslationManager.GetTranslation(data.user.language, "error:js", data.channel_id, data.platform).Replace("%err%", "Not allowed");
                            }
                        }
                        catch (Exception ex)
                        {
                            resultColor = Color.Red;
                            resultNicknameColor = ChatColorPresets.Firebrick;
                            resultMessage = "/me " + TranslationManager.GetTranslation(data.user.language, "error:js", data.channel_id, data.platform)
                                .Replace("%err%", ex.Message);
                            LogWorker.Log(ex.Message, LogWorker.LogTypes.Err, "command\\Java\\Index");
                        }
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
