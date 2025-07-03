using butterBror.Utils;
using Discord;
using Discord.Rest;
using TwitchLib.Client.Enums;
using butterBror.Utils.Tools;
using butterBror.Utils.Types;

namespace butterBror
{
    public partial class Commands
    {
        public class Eightball
        {
            public static CommandInfo Info = new()
            {
                Name = "EightBall",
                Author = "@ItzKITb",
                AuthorLink = "twitch.tv/itzkitb",
                AuthorAvatar = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                Description = new()
                {
                    { "ru", "Будущее пугает..." },
                    { "en", "The future is scary..." }
                },
                WikiLink = "https://itzkitb.lol/bot/command?q=8ball",
                CooldownPerUser = 5,
                CooldownPerChannel = 1,
                Aliases = ["8ball", "eightball", "eb", "8b", "шар"],
                Arguments = string.Empty,
                CooldownReset = true,
                CreationDate = DateTime.Parse("08/08/2024"),
                IsForBotModerator = false,
                IsForBotDeveloper = false,
                IsForChannelModerator = false,
                Platforms = [Platforms.Twitch, Platforms.Telegram, Platforms.Discord]
            };
            public CommandReturn Index(CommandData data)
            {
                Core.Statistics.FunctionsUsed.Add();
                CommandReturn commandReturn = new CommandReturn();

                try
                {
                    int stage1 = new Random().Next(1, 5);
                    int stage2 = new Random().Next(1, 6);
                    string translationParam = "command:8ball:";
                    if (stage1 == 1)
                    {
                        commandReturn.SetColor(ChatColorPresets.DodgerBlue);
                        translationParam += "positively:" + stage2;
                    }
                    else if (stage1 == 2)
                    {
                        translationParam += "hesitantly:" + stage2;
                    }
                    else if (stage1 == 3)
                    {
                        commandReturn.SetColor(ChatColorPresets.GoldenRod);
                        translationParam += "neutral:" + stage2;
                    }
                    else if (stage1 == 4)
                    {
                        commandReturn.SetColor(ChatColorPresets.Red);
                        translationParam += "negatively:" + stage2;
                    }
                    commandReturn.SetMessage("🔮 " + TranslationManager.GetTranslation(data.User.Language, translationParam, data.ChannelID, data.Platform));
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