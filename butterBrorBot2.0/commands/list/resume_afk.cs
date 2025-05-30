using Discord;
using TwitchLib.Client.Enums;
using butterBror.Utils;
using butterBror.Utils.DataManagers;
using butterBror;
using System.Diagnostics;

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
                Engine.Statistics.functions_used.Add();
                CommandReturn commandReturn = new CommandReturn();

                try
                {
                    if (UsersData.Contains(data.user_id, "fromAfkResumeTimes", data.platform) && UsersData.Contains(data.user_id, "lastFromAfkResume", data.platform))
                    {
                        var resumeTimes = UsersData.Get<int>(data.user_id, "fromAfkResumeTimes", data.platform);
                        if (resumeTimes <= 5)
                        {
                            DateTime lastResume = UsersData.Get<DateTime>(data.user_id, "lastFromAfkResume", data.platform);
                            TimeSpan cache = DateTime.UtcNow - lastResume;
                            if (cache.TotalMinutes <= 5)
                            {
                                UsersData.Save(data.user_id, "isAfk", true, data.platform);
                                UsersData.Save(data.user_id, "fromAfkResumeTimes", resumeTimes + 1, data.platform);
                                commandReturn.SetMessage(TranslationManager.GetTranslation(data.user.language, "command:rafk", data.channel_id, data.platform));
                                commandReturn.SetColor(ChatColorPresets.YellowGreen);
                            }
                            else
                            {
                                commandReturn.SetMessage(TranslationManager.GetTranslation(data.user.language, "error:afk_resume_after_5_minutes", data.channel_id, data.platform));
                            }
                        }
                        else
                        {
                            commandReturn.SetMessage(TranslationManager.GetTranslation(data.user.language, "error:afk_resume", data.channel_id, data.platform));
                        }
                    }
                    else
                    {
                        commandReturn.SetMessage(TranslationManager.GetTranslation(data.user.language, "error:no_afk", data.channel_id, data.platform));
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
