using Discord;
using TwitchLib.Client.Enums;
using butterBror.Utils;
using butterBror.Utils.DataManagers;
using System.Diagnostics;
using butterBror.Utils.Tools;
using butterBror.Utils.Types;

namespace butterBror
{
    public partial class Commands
    {
        public class RAfk
        {
            public static CommandInfo Info = new()
            {
                Name = "Rafk",
                Author = "@ItzKITb",
                AuthorLink = "twitch.tv/itzkitb",
                AuthorAvatar = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                Description = new() {
                    { "ru", "Вернуться в афк, если вы вышли из него" },
                    { "en", "Return to afk if you left it" } 
                },
                WikiLink = "https://itzkitb.lol/bot/command?q=rafk",
                CooldownPerUser = 30,
                CooldownPerChannel = 5,
                Aliases = ["rafk", "рафк", "вафк", "вернутьафк", "resumeafk"],
                Arguments = string.Empty,
                CooldownReset = true,
                CreationDate = DateTime.Parse("07/07/2024"),
                IsForBotModerator = false,
                IsForBotDeveloper = false,
                IsForChannelModerator = false,
                Platforms = [Platforms.Twitch, Platforms.Telegram]
            };
            public CommandReturn Index(CommandData data)
            {
                Core.Statistics.FunctionsUsed.Add();
                CommandReturn commandReturn = new CommandReturn();

                try
                {
                    if (UsersData.Contains(data.UserID, "fromAfkResumeTimes", data.Platform) && UsersData.Contains(data.UserID, "lastFromAfkResume", data.Platform))
                    {
                        var resumeTimes = UsersData.Get<int>(data.UserID, "fromAfkResumeTimes", data.Platform);
                        if (resumeTimes <= 5)
                        {
                            DateTime lastResume = UsersData.Get<DateTime>(data.UserID, "lastFromAfkResume", data.Platform);
                            TimeSpan cache = DateTime.UtcNow - lastResume;
                            if (cache.TotalMinutes <= 5)
                            {
                                UsersData.Save(data.UserID, "isAfk", true, data.Platform);
                                UsersData.Save(data.UserID, "fromAfkResumeTimes", resumeTimes + 1, data.Platform);
                                commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:rafk", data.ChannelID, data.Platform));
                                commandReturn.SetColor(ChatColorPresets.YellowGreen);
                            }
                            else
                            {
                                commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:afk_resume_after_5_minutes", data.ChannelID, data.Platform));
                            }
                        }
                        else
                        {
                            commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:afk_resume", data.ChannelID, data.Platform));
                        }
                    }
                    else
                    {
                        commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:no_afk", data.ChannelID, data.Platform));
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
