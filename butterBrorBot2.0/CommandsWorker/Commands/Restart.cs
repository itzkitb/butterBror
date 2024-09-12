using static butterBror.BotWorker.FileMng;
using static butterBror.BotWorker;
using butterBib;
using Discord;

namespace butterBror
{
    public partial class Commands
    {
        public class Restart
        {
            public static CommandInfo Info = new()
            {
                Name = "Restart",
                Author = "@ItzKITb",
                AuthorURL = "twitch.tv/itzkitb",
                AuthorImageURL = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                Description = "С помощью этой команды можно перезагрузить бота.",
                UseURL = "NONE",
                UserCooldown = 1,
                GlobalCooldown = 1,
                aliases = ["restart", "reload", "перезагрузить", "рестарт"],
                ArgsRequired = "(Нету)",
                ResetCooldownIfItHasNotReachedZero = false,
                CreationDate = DateTime.Parse("07/04/2024"),
                ForAdmins = true,
                ForBotCreator = true,
                ForChannelAdmins = false,
            };
            public static CommandReturn Index(CommandData data)
            {
                string resultMessage = "";
                try
                {
                    if (UsersData.UserGetData<bool>(data.UserUUID, "isBotModerator") || UsersData.UserGetData<bool>(data.UserUUID, "isBotDev"))
                    {
                        resultMessage = "❄ Перезагрузка...";
                        Bot.RestartPlease();
                    }
                }
                catch (Exception ex)
                {
                    Tools.ErrorOccured(ex.Message, "cmd7A");
                    resultMessage = TranslationManager.GetTranslation(data.User.Lang, "error", data.ChannelID);
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
                    Color = Color.Blue,
                    NickNameColor = TwitchLib.Client.Enums.ChatColorPresets.DodgerBlue
                };
            }
        }
    }
}
