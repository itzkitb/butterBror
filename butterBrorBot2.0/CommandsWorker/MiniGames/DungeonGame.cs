using static butterBror.BotWorker;
using butterBib;
using Discord;
using TwitchLib.Client.Enums;
using butterBror.Utils;

namespace butterBror
{
    public partial class Commands
    {
        public class DangeonGame
        {
            public static CommandInfo Info = new()
            {
                Name = "DangeonGame",
                Author = "@ItzKITb",
                AuthorURL = "twitch.tv/itzkitb",
                AuthorImageURL = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                Description = "При помощи этой команды вы можете пойти в ♂️ Dungeon ♂️ и сразится против ♂️ Slaves ♂️!",
                UseURL = "https://itzkitb.ru/bot_command/dungeongame",
                UserCooldown = 10,
                GlobalCooldown = 1,
                aliases = ["dungeon", "dngn", "dng", "пещера", "подземелье"],
                ArgsRequired = "(statistic/top/go/sell/shop/buy)",
                ResetCooldownIfItHasNotReachedZero = true,
                CreationDate = DateTime.Parse("09/12/2024"),
                ForAdmins = false,
                ForBotCreator = false,
                ForChannelAdmins = false
            };
            public static CommandReturn Index(CommandData data)
            {
                string resultMessage = "";
                Color resultColor = Color.Green;
                ChatColorPresets resultNicknameColor = ChatColorPresets.YellowGreen;
                
                Random rand = new Random();
                int stage1 = rand.Next(1, 4);
                int stage2 = rand.Next(1, 5);
                string translationParam = "8ball";
                if (stage1 == 1)
                {
                    resultNicknameColor = ChatColorPresets.DodgerBlue;
                    resultColor = Color.Blue;
                    translationParam += "Positively" + stage2;
                }
                else if (stage1 == 2) 
                {
                    translationParam += "Hesitantly" + stage2;
                }
                else if (stage1 == 3)
                {
                    resultNicknameColor = ChatColorPresets.GoldenRod;
                    resultColor = Color.Gold;
                    translationParam += "Neutral" + stage2;
                }
                else if (stage1 == 4)
                {
                    resultNicknameColor = ChatColorPresets.Red;
                    resultColor = Color.Red;
                    translationParam += "Negatively" + stage2;
                }
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