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
                name = "Rafk",
                author = "@ItzKITb",
                author_link = "twitch.tv/itzkitb",
                author_avatar = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                description = new() {
                    { "ru", "Вернуться в афк, если вы вышли из него" },
                    { "en", "Return to afk if you left it" } 
                },
                wiki_link = "https://itzkitb.lol/bot/command?q=rafk",
                cooldown_per_user = 30,
                cooldown_global = 5,
                aliases = ["rafk", "рафк", "вафк", "вернутьафк", "resumeafk"],
                arguments = string.Empty,
                cooldown_reset = true,
                creation_date = DateTime.Parse("07/07/2024"),
                is_for_bot_moderator = false,
                is_for_bot_developer = false,
                is_for_channel_moderator = false,
                platforms = [Platforms.Twitch, Platforms.Telegram]
            };
            public CommandReturn Index(CommandData data)
            {
                Engine.Statistics.functions_used.Add();
                try
                {
                    string resultMessage = "";
                    string resultTitle = TranslationManager.GetTranslation(data.user.language, "discord:error:title", data.channel_id, data.platform);
                    Color resultColor = Color.Red;
                    ChatColorPresets resultNicknameColor = ChatColorPresets.Red;
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
                                resultMessage = TranslationManager.GetTranslation(data.user.language, "command:rafk", data.channel_id, data.platform);
                                resultTitle = TranslationManager.GetTranslation(data.user.language, "discord:afk:title", data.channel_id, data.platform);
                                resultColor = Color.Green;
                                resultNicknameColor = ChatColorPresets.YellowGreen;
                            }
                            else
                            {
                                resultMessage = TranslationManager.GetTranslation(data.user.language, "error:afk_resume_after_5_minutes", data.channel_id, data.platform);
                            }
                        }
                        else
                        {
                            resultMessage = TranslationManager.GetTranslation(data.user.language, "error:afk_resume", data.channel_id, data.platform);
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"fromAfkResumeTimes = {UsersData.Get<int>(data.user_id, "fromAfkResumeTimes", data.platform)}; lastFromAfkResume = {UsersData.Get<DateTime>(data.user_id, "lastFromAfkResume", data.platform)}");
                        Debug.WriteLine($"fromAfkResumeTimes = {UsersData.Contains(data.user_id, "fromAfkResumeTimes", data.platform)}; lastFromAfkResume = {UsersData.Contains(data.user_id, "lastFromAfkResume", data.platform)}");
                        resultMessage = TranslationManager.GetTranslation(data.user.language, "error:no_afk", data.channel_id, data.platform);
                    }
                    return new()
                    {
                        message = resultMessage,
                        safe_execute = true,
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
