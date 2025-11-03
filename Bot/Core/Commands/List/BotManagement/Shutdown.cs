using bb.Utils;
using bb.Core.Configuration;
using bb.Models.Command;
using bb.Models.Platform;
using bb.Models.Users;

namespace bb.Core.Commands.List.BotManagement
{
    public class Shutdown : CommandBase
    {
        public override string Name => "Shutdown";
        public override string Author => "https://github.com/itzkitb";
        public override string Source => "BotManagement/Shutdown.cs";
        public override Dictionary<Language, string> Description => new() {
            { Language.RuRu, "Этот маленький манёвр будет стоить нам 51 год." },
            { Language.EnUs, "This Little Maneuver's Gonna Cost Us 51 Years." }
        };
        public override int UserCooldown => 1;
        public override int Cooldown => 1;
        public override string[] Aliases => ["shutdown", "off", "выкл", "выключить"];
        public override string Help => string.Empty;
        public override DateTime CreationDate => DateTime.Parse("2025-10-21T00:00:00.0000000Z");
        public override Roles RoleRequired => Roles.BotMod;
        public override Platform[] Platforms => [Platform.Twitch, Platform.Telegram, Platform.Discord];
        public override bool IsAsync => false;

        public override CommandReturn Execute(CommandData data)
        {
            CommandReturn commandReturn = new CommandReturn();

            try
            {
                commandReturn.SetMessage("❄ | Shutting down in 3 seconds...");
                _ = Task.Run(async () => {
                    await Task.Delay(3000);
                    await Program.BotInstance.Shutdown(force: true);
                });
            }
            catch (Exception e)
            {
                commandReturn.SetError(e);
            }

            return commandReturn;
        }
    }
}
