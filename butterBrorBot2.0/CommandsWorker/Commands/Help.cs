using butterBror.Utils;
using butterBib;
using Discord;
using System.Reflection;
using TwitchLib.Client.Enums;

namespace butterBror
{
    public partial class Commands
    {
        public class Help
        {
            public static CommandInfo Info = new()
            {
                Name = "Help",
                Author = "@ItzKITb",
                AuthorURL = "twitch.tv/itzkitb",
                AuthorImageURL = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                Description = "С помощью этой команды вы можете узнать, данные о выбранной команде",
                UseURL = "https://itzkitb.ru/bot/command?name=help",
                UserCooldown = 120,
                GlobalCooldown = 10,
                aliases = ["help", "info", "помощь", "hlp"],
                ArgsRequired = "(название команды)",
                ResetCooldownIfItHasNotReachedZero = false,
                CreationDate = DateTime.Parse("09/12/2024"),
                ForAdmins = false,
                ForBotCreator = false,
                ForChannelAdmins = false
            };
            public static CommandReturn Index(CommandData data)
            {
                try
                {
                    string result = "";
                    if (data.args.Count == 1)
                    {
                        string classToFound = data.args[0];
                        result = TranslationManager.GetTranslation(data.User.Lang, "help:notFound", data.ChannelID);
                        foreach (var classType in classes)
                        {
                            // Получение значения статического свойства Info
                            var infoProperty = classType.GetField("Info", BindingFlags.Static | BindingFlags.Public);
                            var info = infoProperty.GetValue(null) as CommandInfo;

                            if (info.aliases.Contains(classToFound))
                            {
                                string aliasesList = "";
                                int num = 0;
                                int numWithoutComma = 5;
                                if (info.aliases.Length < 5)
                                {
                                    numWithoutComma = info.aliases.Length;
                                }

                                foreach (string alias in info.aliases)
                                {
                                    num++;
                                    if (num < numWithoutComma)
                                    {
                                        aliasesList += $"#{alias}, ";
                                    }
                                    else if (num == numWithoutComma)
                                    {
                                        aliasesList += $"#{alias}";
                                    }
                                }
                                result = TranslationManager.GetTranslation(data.User.Lang, "help:found", data.ChannelID)
                                    .Replace("%commandName%", info.Name)
                                    .Replace("%Variables%", aliasesList)
                                    .Replace("%Args%", info.ArgsRequired)
                                    .Replace("%Link%", info.UseURL)
                                    .Replace("%Description%", info.Description)
                                    .Replace("%Author%", NamesUtil.DontPingUsername(info.Author))
                                    .Replace("%creationDate%", info.CreationDate.ToShortDateString())
                                    .Replace("%uCooldown%", info.UserCooldown.ToString())
                                    .Replace("%gCooldown%", info.GlobalCooldown.ToString());
                                break;
                            }
                        }
                    }
                    else if (data.args.Count > 1)
                    {
                        result = TranslationManager.GetTranslation(data.User.Lang, "aFewArgs", data.ChannelID).Replace("%args%", "(command_name)");
                    }
                    else
                    {
                        result = TranslationManager.GetTranslation(data.User.Lang, "botInfo", data.ChannelID);
                    }


                    return new()
                    {
                        Message = result,
                        IsSafeExecute = true,
                        Description = "",
                        Author = "",
                        ImageURL = "",
                        ThumbnailUrl = "",
                        Footer = "",
                        IsEmbed = true,
                        Ephemeral = false,
                        Title = TranslationManager.GetTranslation(data.User.Lang, "dsWinterTitle", data.ChannelID),
                        Color = Color.Blue,
                        NickNameColor = TwitchLib.Client.Enums.ChatColorPresets.DodgerBlue
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
