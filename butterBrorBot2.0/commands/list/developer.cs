using System;
using butterBror.Utils;
using Discord;
using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.Reflection;
using TwitchLib.Client.Enums;
using butterBror;
using butterBror.Utils.Tools;

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
                AuthorLink = "twitch.tv/itzkitb",
                AuthorAvatar = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                Description = new()
                {
                    { "ru", "Эта команда не для тебя PauseChamp" },
                    { "en", "This command is not for you PauseChamp" }
                },
                WikiLink = "https://itzkitb.lol/bot/command?q=dev",
                CooldownPerUser = 0,
                CooldownPerChannel = 0,
                Aliases = ["run", "code", "csharp", "dev"],
                Arguments = "[crap code]",
                CooldownReset = true,
                CreationDate = DateTime.Parse("26/09/2024"),
                IsForBotModerator = false,
                IsForBotDeveloper = true,
                IsForChannelModerator = false,
                Platforms = [Platforms.Twitch, Platforms.Telegram, Platforms.Discord]
            };
            public CommandReturn Index(CommandData data)
            {
                Core.Statistics.FunctionsUsed.Add();
                CommandReturn commandReturn = new CommandReturn();

                try
                {
                    DateTime StartTime = DateTime.Now;

                    try
                    {
                        string result = Command.ExecuteCode(data.arguments_string);
                        DateTime EndTime = DateTime.Now;
                        commandReturn.SetMessage(TranslationManager.GetTranslation(data.user.language, "command:csharp:result", data.channel_id, data.platform)
                            .Replace("%time%", ((int)(EndTime - StartTime).TotalMilliseconds).ToString())
                            .Replace("%result%", result));
                    }
                    catch (Exception ex)
                    {
                        DateTime EndTime = DateTime.Now;
                        commandReturn.SetMessage(TranslationManager.GetTranslation(data.user.language, "command:csharp:error", data.channel_id, data.platform)
                            .Replace("%time%", ((int)(EndTime - StartTime).TotalMilliseconds).ToString())
                            .Replace("%result%", ex.Message));
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
