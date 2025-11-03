using bb.Core.Configuration;
using bb.Models.Command;
using bb.Models.Platform;
using bb.Models.Users;

namespace bb.Core.Commands.List.Utility
{
    public class Say : CommandBase
    {
        public override string Name => "Say";
        public override string Author => "https://github.com/itzkitb";
        public override string Source => "Utility/Say.cs";
        public override Dictionary<Language, string> Description => new() {
            { Language.RuRu, "Отправляет сообщение от лица бота в текущий чат." },
            { Language.EnUs, "Sends a message on behalf of the bot to the current chat." }
        };
        public override int UserCooldown => 1;
        public override int Cooldown => 1;
        public override string[] Aliases => ["say", "tell", "сказать", "type", "написать"];
        public override string Help => "<text>";
        public override DateTime CreationDate => DateTime.Parse("2024-07-09T00:00:00.0000000Z");
        public override Roles RoleRequired => Roles.BotMod;
        public override Platform[] Platforms => [Platform.Twitch, Platform.Telegram];
        public override bool IsAsync => false;

        public override CommandReturn Execute(CommandData data)
        {
            CommandReturn commandReturn = new CommandReturn();

            try
            {
                commandReturn.SetMessage(data.ArgumentsString);
            }
            catch (Exception e)
            {
                commandReturn.SetError(e);
            }

            return commandReturn;
        }
    }
}