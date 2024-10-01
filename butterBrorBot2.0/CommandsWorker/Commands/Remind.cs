using butterBib;
using butterBror.Utils;
using Discord;
using TwitchLib.Client.Enums;

namespace butterBror
{
    public partial class Commands
    {
        public class Remind
        {
            public static CommandInfo Info = new()
            {
                Name = "Remind",
                Author = "@ItzKITb",
                AuthorURL = "twitch.tv/itzkitb",
                AuthorImageURL = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                Description = "Эта команда перемешивает текст либо-же выводит рандомное число.",
                UseURL = "NONE",
                UserCooldown = 5,
                GlobalCooldown = 1,
                aliases = ["remind", "rmd", "напомнить", "нап"],
                ArgsRequired = "[(me [-y -mn -d -h -m])/([user] [text])]",
                ResetCooldownIfItHasNotReachedZero = true,
                CreationDate = DateTime.Parse("01/10/2024"),
                ForAdmins = false,
                ForBotCreator = false,
                ForChannelAdmins = false
            };
            public static CommandReturn Index(CommandData data)
            {
                string resultMessage = "";
                Color resultColor = Color.Green;
                ChatColorPresets resultNicknameColor = ChatColorPresets.YellowGreen;

                if (data.args.Count > 1)
                {
                    if (data.args[0].ToLower() == "me")
                    {

                    }
                    else
                    {

                    }
                }
                else
                {
                    resultMessage = TranslationManager.GetTranslation(data.User.Lang, "randomTxt", data.ChannelID) + "DinoDance";
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
                    Color = resultColor,
                    NickNameColor = resultNicknameColor
                };
            }
        }
    }
}
