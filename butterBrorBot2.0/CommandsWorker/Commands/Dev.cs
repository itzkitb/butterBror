using System;
using butterBror.Utils;
using Discord;
using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.Reflection;
using TwitchLib.Client.Enums;
using butterBib;

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
                UseURL = "https://itzkitb.ru/bot/command?name=dev",
                UserCooldown = 0,
                GlobalCooldown = 0,
                aliases = ["run", "code", "csharp", "dev"],
                ArgsRequired = "(C# код)",
                ResetCooldownIfItHasNotReachedZero = true,
                CreationDate = DateTime.Parse("26/09/2024"),
                ForAdmins = false,
                ForBotCreator = true,
                ForChannelAdmins = false,
                AllowedPlatforms = [Platforms.Twitch, Platforms.Telegram, Platforms.Discord]
            };
            public static CommandReturn Index(CommandData data)
            {
                try
                {
                    string resultMessage = "";
                    Color resultColor = Color.Green;
                    ChatColorPresets resultNicknameColor = ChatColorPresets.YellowGreen;
                    DateTime StartTime = DateTime.Now;

                    try
                    {
                        string result = CommandUtil.ExecuteCode(data.ArgsAsString);
                        DateTime EndTime = DateTime.Now;
                        resultMessage = TranslationManager.GetTranslation(data.User.Lang, "c#_code:result", data.ChannelID)
                            .Replace("%time%", ((int)(EndTime - StartTime).TotalMilliseconds).ToString())
                            .Replace("%result%", result);
                    }
                    catch (Exception ex)
                    {
                        DateTime EndTime = DateTime.Now;
                        resultMessage = TranslationManager.GetTranslation(data.User.Lang, "c#_code:error", data.ChannelID)
                            .Replace("%time%", ((int)(EndTime - StartTime).TotalMilliseconds).ToString())
                            .Replace("%result%", ex.Message);
                        resultNicknameColor = ChatColorPresets.Red;
                        resultColor = Color.Red;
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
