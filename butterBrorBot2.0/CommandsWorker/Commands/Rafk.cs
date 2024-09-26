using Discord;
using butterBib;
using TwitchLib.Client.Enums;
using butterBror.Utils;
using butterBror.Utils.DataManagers;

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
                AuthorURL = "twitch.tv/itzkitb",
                AuthorImageURL = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                Description = "Эта команда помогает вам вернуть афк статус, если вы вышли из него.",
                UseURL = "https://itzkitb.ru/bot_command/rafk",
                UserCooldown = 30,
                GlobalCooldown = 5,
                aliases = ["rafk", "рафк", "вафк", "вернутьафк", "resumeafk"],
                ArgsRequired = "(Нету)",
                ResetCooldownIfItHasNotReachedZero = true,
                CreationDate = DateTime.Parse("07/07/2024"),
                ForAdmins = false,
                ForBotCreator = false,
                ForChannelAdmins = false
            };
            public static CommandReturn Index(CommandData data)
            {
                string resultMessage = "";
                string resultTitle = TranslationManager.GetTranslation(data.User.Lang, "Err", data.ChannelID);
                Color resultColor = Color.Red;
                ChatColorPresets resultNicknameColor = ChatColorPresets.Red;
                if (UsersData.IsContainsKey("fromAfkResumeTimes", data.UserUUID) && UsersData.IsContainsKey("lastFromAfkResume", data.UserUUID))
                {
                    var resumeTimes = UsersData.UserGetData<int>(data.UserUUID, "fromAfkResumeTimes");
                    if (resumeTimes <= 5)
                    {
                        DateTime lastResume = UsersData.UserGetData<DateTime>(data.UserUUID, "lastFromAfkResume");
                        TimeSpan cache = DateTime.UtcNow - lastResume;
                        if (cache.TotalMinutes <= 5)
                        {
                            UsersData.UserSaveData(data.UserUUID, "isAfk", true);
                            UsersData.UserSaveData(data.UserUUID, "fromAfkResumeTimes", resumeTimes + 1);
                            resultMessage = TranslationManager.GetTranslation(data.User.Lang, "rafk", data.ChannelID);
                            resultTitle = TranslationManager.GetTranslation(data.User.Lang, "dsAfkTitle", data.ChannelID);
                            resultColor = Color.Green;
                            resultNicknameColor = ChatColorPresets.YellowGreen;
                        }
                        else
                        {
                            resultMessage = TranslationManager.GetTranslation(data.User.Lang, "rafkCant:moreThan5Minutes", data.ChannelID);
                        }
                    }
                    else
                    {
                        resultMessage = TranslationManager.GetTranslation(data.User.Lang, "rafkCant:moreThan5TimesResume", data.ChannelID);
                    }
                }
                else
                {
                    resultMessage = TranslationManager.GetTranslation(data.User.Lang, "rafkCant:NoTimes", data.ChannelID);
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
