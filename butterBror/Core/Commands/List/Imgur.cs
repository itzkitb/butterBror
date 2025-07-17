using butterBror.Utils;
using static butterBror.Core.Bot.Console;
using butterBror.Services.External;
using butterBror.Models;

/*
Fuck you, Imgur <3

namespace butterBror.Core.Commands
{
    public partial class Commands
    {
        public class UploadToImgur
        {
            public static CommandInfo Info = new()
            {
                Name = "UploadToImgur",
                Author = "@ItzKITb",
                AuthorLink = "twitch.tv/itzkitb",
                AuthorAvatar = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                Description = new() { 
                    { "ru", "Загрузить картинку на imgur.com" }, 
                    { "en", "Upload image to imgur.com" } 
                },
                WikiLink = "https://itzkitb.lol/bot/command?q=imguruploader",
                CooldownPerUser = 30,
                CooldownPerChannel = 1,
                Aliases = ["ui", "imgur", "upload", "uploadimage", "загрузитькартинку", "зк", "imguruploadimage"],
                Arguments = "[image url]",
                CooldownReset = true,
                CreationDate = DateTime.Parse("07/04/2024"),
                IsForBotModerator = false,
                IsForBotDeveloper = false,
                IsForChannelModerator = false,
                Platforms = [PlatformsEnum.Twitch, PlatformsEnum.Telegram, PlatformsEnum.Discord]
            };

            [ConsoleSector("butterBror.Commands.UploadToImgur", "Index")]
            public CommandReturn Index(CommandData data)
            {
                Engine.Statistics.FunctionsUsed.Add();
                CommandReturn commandReturn = new CommandReturn();

                try
                {
                    string? url = "";
                    if (data.Platform == PlatformsEnum.Twitch)
                    {
                        url = Text.CleanAscii(data.ArgumentsString);
                    }
                    else if (data.Platform == PlatformsEnum.Discord)
                    {
                        url = data.DiscordArguments["url"];
                    }

                    if (url != "")
                    {
                        int stage = 0;
                        try
                        {
                            stage++;
                            var imageBytesTask = ImgurService.DownloadAsync(url);
                            imageBytesTask.Wait();
                            byte[] imageBytes = imageBytesTask.Result;
                            stage++;
                            var responseTask = ImgurService.UploadAsync(imageBytes, "Бот butterBror и его разработчик ItzKITb никак не связаны с данным изображением и не поддерживают его содержимое.", $"Картинка от @{data.User.Name}", Engine.Bot.Tokens.Imgur, "https://api.imgur.com/3/upload");
                            responseTask.Wait();
                            string response = responseTask.Result;
                            stage++;
                            string link = ImgurService.GetLinkFromResponse(response);
                            commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:imgur:uploaded", data.ChannelID, data.Platform).Replace("%link%", link));
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
                            commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, errorTranslation, data.ChannelID, data.Platform));
                            Write(ex);
                        }
                    }
                    else
                    {
                        commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:not_enough_arguments", data.ChannelID, data.Platform).Replace("%command_example%", "imguruploadimage [url]"));
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
*/