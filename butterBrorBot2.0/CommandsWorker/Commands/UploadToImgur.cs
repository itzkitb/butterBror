using Discord;
using butterBib;
using TwitchLib.Client.Enums;
using butterBror.Utils;

namespace butterBror
{
    public partial class Commands
    {
        public class UploadToImgur
        {
            public static CommandInfo Info = new()
            {
                Name = "UploadToImgur",
                Author = "@ItzKITb",
                AuthorURL = "twitch.tv/itzkitb",
                AuthorImageURL = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                Description = "С помощью этой команды вы можете быстро загрузить свою картинку на сервис Imgur.com",
                UseURL = "NONE",
                UserCooldown = 30,
                GlobalCooldown = 1,
                aliases = ["ui", "imgur", "upload", "uploadimage", "загрузитькартинку", "зк", "imguruploadimage"],
                ArgsRequired = "[URL картинки, которую вы хотите загрузить]",
                ResetCooldownIfItHasNotReachedZero = true,
                CreationDate = DateTime.Parse("07/04/2024"),
                ForAdmins = false,
                ForBotCreator = false,
                ForChannelAdmins = false
            };

            public static CommandReturn Index(CommandData data)
            {
                Color resultColor = Color.Red;
                ChatColorPresets resultNicknameColor = ChatColorPresets.YellowGreen;

                string? url = "";
                if (data.Platform == Platforms.Twitch)
                {
                    url = TextUtil.FilterText(data.ArgsAsString);
                }
                else if (data.Platform == Platforms.Discord)
                {
                    url = data.DSargs["url"];
                }
                string resultMessage = "";
                if (url != "")
                {
                    int stage = 0;
                    try
                    {
                        stage++;
                        var imageBytesTask = Utils.APIUtil.Imgur.DownloadImageAsync(url);
                        imageBytesTask.Wait();
                        byte[] imageBytes = imageBytesTask.Result;
                        stage++;
                        var responseTask = Utils.APIUtil.Imgur.UploadImageToImgurAsync(imageBytes, "Бот butterBror и его разработчик ItzKITb никак не связаны с данным изображением и не поддерживают его содержимое.", $"Картинка от @{data.User.Name}", Bot.imgurAPIkey, "https://api.imgur.com/3/upload");
                        responseTask.Wait();
                        string response = responseTask.Result;
                        stage++;
                        string link = Utils.APIUtil.Imgur.GetImgurLinkFromResponse(response);
                        resultMessage = TranslationManager.GetTranslation(data.User.Lang, "imgurUploaded", data.ChannelID).Replace("%link%", link);
                    }
                    catch (Exception ex)
                    {
                        string errorTranslation = "";
                        switch (stage)
                        {
                            case 1:
                                errorTranslation = "imgurUploaderDownloadImageError";
                                break;
                            case 2:
                                errorTranslation = "imgurUploaderUploadImageError";
                                break;
                            case 3:
                                errorTranslation = "imgurUploaderGetLinkError";
                                break;
                            default:
                                errorTranslation = "unhandledError";
                                break;
                        }
                        resultMessage = TranslationManager.GetTranslation(data.User.Lang, errorTranslation, data.ChannelID);
                        ConsoleUtil.ErrorOccured(ex.Message, errorTranslation);
                    }
                }
                else
                {
                    resultMessage = TranslationManager.GetTranslation(data.User.Lang, "lowArgs", data.ChannelID).Replace("%commandWorks%", "imguruploadimage [url]");
                }
                return new()
                {
                    Message = resultMessage,
                    IsSafeExecute = true,
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
        }
    }
}
