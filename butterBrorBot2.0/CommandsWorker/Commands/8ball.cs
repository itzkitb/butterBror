using butterBib;
using butterBror.Utils;
using Discord;
using TwitchLib.Client.Enums;

namespace butterBror
{
    public partial class Commands
    {
        public class eightball
        {
            public static CommandInfo Info = new()
            {
                Name = "EightBall",
                Author = "@ItzKITb",
                AuthorURL = "twitch.tv/itzkitb",
                AuthorImageURL = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                Description = "При помощи этой команды вы можете сделать предсказание, надо вам что-то делать или нет.",
                UseURL = "https://itzkitb.ru/bot/command?name=8ball",
                UserCooldown = 5,
                GlobalCooldown = 1,
                aliases = ["8ball", "eightball", "eb", "8b", "шар"],
                ArgsRequired = "(Нету)",
                ResetCooldownIfItHasNotReachedZero = true,
                CreationDate = DateTime.Parse("08/08/2024"),
                ForAdmins = false,
                ForBotCreator = false,
                ForChannelAdmins = false
            };
            public static CommandReturn Index(CommandData data)
            {
                DebugUtil.SetTaskID(0, data);
                string resultMessage = "";
                Color resultColor = Color.Green;
                ChatColorPresets resultNicknameColor = ChatColorPresets.YellowGreen;
                DebugUtil.SetTaskID(1, data);
                Random rand = new Random();
                int stage1 = rand.Next(1, 5);
                int stage2 = rand.Next(1, 6);
                DebugUtil.SetTaskID(2, data);
                string translationParam = "8ball";
                if (stage1 == 1)
                {
                    DebugUtil.SetTaskID(3, data);
                    resultNicknameColor = ChatColorPresets.DodgerBlue;
                    resultColor = Color.Blue;
                    translationParam += "Positively" + stage2;
                }
                else if (stage1 == 2)
                {
                    DebugUtil.SetTaskID(4, data);
                    translationParam += "Hesitantly" + stage2;
                }
                else if (stage1 == 3)
                {
                    DebugUtil.SetTaskID(5, data);
                    resultNicknameColor = ChatColorPresets.GoldenRod;
                    resultColor = Color.Gold;
                    translationParam += "Neutral" + stage2;
                }
                else if (stage1 == 4)
                {
                    DebugUtil.SetTaskID(6, data);
                    resultNicknameColor = ChatColorPresets.Red;
                    resultColor = Color.Red;
                    translationParam += "Negatively" + stage2;
                }
                DebugUtil.SetTaskID(7, data);
                resultMessage = "🔮 " + TranslationManager.GetTranslation(data.User.Lang, translationParam, data.ChannelID);
                return new()
                {
                    Message = resultMessage,
                    IsSafeExecute = false,
                    Description = "",
                    Author = "",
                    ImageURL = "",
                    ThumbnailUrl = "",
                    Footer = "",
                    IsEmbed = true,
                    Ephemeral = false,
                    Title = "",
                    Color = resultColor,
                    NickNameColor = resultNicknameColor
                };
            }
        }
    }
}