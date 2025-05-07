using Discord;
using TwitchLib.Client.Enums;
using butterBror.Utils;
using butterBror;

namespace butterBror
{
    public partial class Commands
    {
        public class UploadToImgur
        {
            public static CommandInfo Info = new()
            {
                name = "UploadToImgur",
                author = "@ItzKITb",
                author_link = "twitch.tv/itzkitb",
                author_avatar = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                description = new() { 
                    { "ru", "Загрузить картинку на imgur.com" }, 
                    { "en", "Upload image to imgur.com" } 
                },
                wiki_link = "https://itzkitb.lol/bot/command?q=imguruploader",
                cooldown_per_user = 30,
                cooldown_global = 1,
                aliases = ["ui", "imgur", "upload", "uploadimage", "загрузитькартинку", "зк", "imguruploadimage"],
                arguments = "[image url]",
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
                    Color resultColor = Color.Red;
                    ChatColorPresets resultNicknameColor = ChatColorPresets.YellowGreen;

                    string? url = "";
                    if (data.platform == Platforms.Twitch)
                    {
                        url = TextUtil.CleanAscii(data.arguments_string);
                    }
                    else if (data.platform == Platforms.Discord)
                    {
                        url = data.discord_arguments["url"];
                    }
                    string resultMessage = "";
                    if (url != "")
                    {
                        int stage = 0;
                        try
                        {
                            stage++;
                            var imageBytesTask = Utils.API.Imgur.DownloadAsync(url);
                            imageBytesTask.Wait();
                            byte[] imageBytes = imageBytesTask.Result;
                            stage++;
                            var responseTask = Utils.API.Imgur.UploadAsync(imageBytes, "Бот butterBror и его разработчик ItzKITb никак не связаны с данным изображением и не поддерживают его содержимое.", $"Картинка от @{data.user.username}", Maintenance.token_imgur, "https://api.imgur.com/3/upload");
                            responseTask.Wait();
                            string response = responseTask.Result;
                            stage++;
                            string link = Utils.API.Imgur.GetLinkFromResponse(response);
                            resultMessage = TranslationManager.GetTranslation(data.user.language, "command:imgur:uploaded", data.channel_id, data.platform).Replace("%link%", link);
                        }
                        catch (Exception ex)
                        {
                            string errorTranslation = "";
                            switch (stage)
                            {
                                case 1:
                                    errorTranslation = "error:image_bytes_receive";
                                    break;
                                case 2:
                                    errorTranslation = "error:image_imgur_upload";
                                    break;
                                case 3:
                                    errorTranslation = "error:imgur_wrong_result";
                                    break;
                                default:
                                    errorTranslation = "error:unhandled";
                                    break;
                            }
                            resultMessage = TranslationManager.GetTranslation(data.user.language, errorTranslation, data.channel_id, data.platform);
                            Utils.Console.WriteError(ex, errorTranslation);
                        }
                    }
                    else
                    {
                        resultMessage = TranslationManager.GetTranslation(data.user.language, "error:not_enough_arguments", data.channel_id, data.platform).Replace("%command_example%", "imguruploadimage [url]");
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
