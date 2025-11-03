using bb.Core.Configuration;
using bb.Models.Command;
using bb.Models.Platform;
using bb.Models.Users;
using bb.Utils;

namespace bb.Core.Commands.List.Games
{
    public class CoinFlip : CommandBase
    {
        public override string Name => "CoinFlip";
        public override string Author => "https://github.com/itzkitb";
        public override string Source => "Games/Coinflip.cs";
        public override Dictionary<Language, string> Description => new()
        {
            { Language.RuRu, "Подкинь монетку!" },
            { Language.EnUs, "Flip a coin!" }
        };
        public override int UserCooldown => 5;
        public override int Cooldown => 1;
        public override string[] Aliases => ["coin", "coinflip", "орелилирешка", "оир", "монетка", "headsortails", "hot", "орел", "решка", "heads", "tails"];
        public override string Help => string.Empty;
        public override DateTime CreationDate => DateTime.Parse("2024-08-08T00:00:00.0000000Z");
        public override Roles RoleRequired => Roles.Public;
        public override Platform[] Platforms => [Platform.Twitch, Platform.Telegram, Platform.Discord];
        public override bool IsAsync => false;

        public override CommandReturn Execute(CommandData data)
        {
            CommandReturn commandReturn = new CommandReturn();
            commandReturn.SetSafe(true);

            try
            {
                if (data.ChannelId == null)
                {
                    commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:unknown", string.Empty, data.Platform));
                    return commandReturn;
                }

                int coin = new System.Random().Next(1, 3);
                if (coin == 1)
                {
                    commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "command:coinflip:heads", data.ChannelId, data.Platform));
                }
                else
                {
                    commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "command:coinflip:tails", data.ChannelId, data.Platform));
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
