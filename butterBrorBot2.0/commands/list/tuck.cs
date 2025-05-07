using butterBror.Utils;
using butterBror.Utils.DataManagers;
using butterBror;
using Discord;
using TwitchLib.Client.Enums;
using Microsoft.CodeAnalysis;

namespace butterBror
{
    public partial class Commands
    {
        public class Tuck
        {
            public static CommandInfo Info = new()
            {
                name = "Tuck",
                author = "@ItzKITb",
                author_link = "twitch.tv/itzkitb",
                author_avatar = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                description = new()
                {
                    { "ru", "Спокойной ночи... 👁" },
                    { "en", "Good night... 👁" }
                },
                wiki_link = "https://itzkitb.lol/bot/command?q=tuck",
                cooldown_per_user = 5,
                cooldown_global = 1,
                aliases = ["tuck", "уложить", "tk", "улож", "тык"],
                arguments = "(name) (text)",
                cooldown_reset = true,
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
                    ChatColorPresets resultNicknameColor = ChatColorPresets.HotPink;
                    if (data.arguments.Count >= 1)
                    {
                        var username = TextUtil.UsernameFilter(TextUtil.CleanAsciiWithoutSpaces(data.arguments[0]));
                        var isSelectedUserIsNotIgnored = true;
                        var userID = Names.GetUserID(username.ToLower(), Platforms.Twitch);
                        try
                        {
                            if (userID != null)
                                isSelectedUserIsNotIgnored = !UsersData.Get<bool>(userID, "isIgnored", data.platform);
                        }
                        catch (Exception) { }
                        if (username.ToLower() == Maintenance.bot_name.ToLower())
                        {
                            resultMessage = TranslationManager.GetTranslation(data.user.language, "command:tuck:bot", data.channel_id, data.platform);
                            resultColor = Color.Blue;
                        }
                        else if (isSelectedUserIsNotIgnored)
                        {
                            if (data.arguments.Count >= 2)
                            {
                                List<string> list = data.arguments;
                                list.RemoveAt(0);
                                resultMessage = TranslationManager.GetTranslation(data.user.language, "command:tuck:text", data.channel_id, data.platform).Replace("%user%", Names.DontPing(username)).Replace("%text%", string.Join(" ", list));
                            }
                            else
                            {
                                resultMessage = TranslationManager.GetTranslation(data.user.language, "command:tuck", data.channel_id, data.platform).Replace("%user%", Names.DontPing(username));
                            }
                        }
                        else
                        {
                            LogWorker.Log($"User @{data.user.username} tried to put a user to sleep who is in the ignore list", LogWorker.LogTypes.Warn, $"command\\Tuck\\Index#{username}");
                            resultMessage = TranslationManager.GetTranslation(data.user.language, "error:user_ignored", data.channel_id, data.platform);
                        }
                    }
                    else
                    {
                        resultMessage = TranslationManager.GetTranslation(data.user.language, "command:tuck:none", data.channel_id, data.platform);
                        resultColor = Color.LightGrey;
                        resultNicknameColor = ChatColorPresets.DodgerBlue;
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
