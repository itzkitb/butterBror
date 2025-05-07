using System;
using butterBror.Utils;
using Discord;
using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.Reflection;
using TwitchLib.Client.Enums;
using butterBror;

namespace butterBror
{
    public partial class Commands
    {
        public class Dev
        {
            public static CommandInfo Info = new()
            {
                name = "Dev",
                author = "@ItzKITb",
                author_link = "twitch.tv/itzkitb",
                author_avatar = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                description = new()
                {
                    { "ru", "Эта команда не для тебя PauseChamp" },
                    { "en", "This command is not for you PauseChamp" }
                },
                wiki_link = "https://itzkitb.lol/bot/command?q=dev",
                cooldown_per_user = 0,
                cooldown_global = 0,
                aliases = ["run", "code", "csharp", "dev"],
                arguments = "[crap code]",
                cooldown_reset = true,
                creation_date = DateTime.Parse("26/09/2024"),
                is_for_bot_moderator = false,
                is_for_bot_developer = true,
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
                    DateTime StartTime = DateTime.Now;

                    try
                    {
                        string result = Command.ExecuteCode(data.arguments_string);
                        DateTime EndTime = DateTime.Now;
                        resultMessage = TranslationManager.GetTranslation(data.user.language, "command:csharp:result", data.channel_id, data.platform)
                            .Replace("%time%", ((int)(EndTime - StartTime).TotalMilliseconds).ToString())
                            .Replace("%result%", result);
                    }
                    catch (Exception ex)
                    {
                        DateTime EndTime = DateTime.Now;
                        resultMessage = TranslationManager.GetTranslation(data.user.language, "command:csharp:error", data.channel_id, data.platform)
                            .Replace("%time%", ((int)(EndTime - StartTime).TotalMilliseconds).ToString())
                            .Replace("%result%", ex.Message);
                        resultNicknameColor = ChatColorPresets.Red;
                        resultColor = Color.Red;
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
