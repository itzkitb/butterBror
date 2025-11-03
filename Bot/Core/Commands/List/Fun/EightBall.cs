using bb.Core.Configuration;
using bb.Models.Command;
using bb.Models.Platform;
using bb.Models.Users;
using bb.Utils;
using TwitchLib.Client.Enums;

namespace bb.Core.Commands.List.Fun
{
    public class EightBall : CommandBase
    {
        public override string Name => "EightBall";
        public override string Author => "https://github.com/itzkitb";
        public override string Source => "Fun/EightBall.cs";
        public override Dictionary<Language, string> Description => new()
        {
            { Language.RuRu, "Этот шар умеет отвечать на вопросы." },
            { Language.EnUs, "This ball can answer questions." }
        };
        public override int UserCooldown => 5;
        public override int Cooldown => 1;
        public override string[] Aliases => ["8ball", "eightball", "eb", "8b", "шар"];
        public override string Help => string.Empty;
        public override DateTime CreationDate => DateTime.Parse("2024-08-08T00:00:00.0000000Z");
        public override Roles RoleRequired => Roles.Public;
        public override Platform[] Platforms => [Platform.Twitch, Platform.Telegram, Platform.Discord];
        public override bool IsAsync => false;

        public override CommandReturn Execute(CommandData data)
        {
            CommandReturn commandReturn = new CommandReturn();

            try
            {
                if (data.ChannelId == null)
                {
                    commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:unknown", string.Empty, data.Platform));
                    return commandReturn;
                }

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
                commandReturn.SetMessage("🔮 " + LocalizationService.GetString(data.User.Language, translationParam, data.ChannelId, data.Platform));
            }
            catch (Exception e)
            {
                commandReturn.SetError(e);
            }

            return commandReturn;
        }
    }
}