using bb.Core.Configuration;
using bb.Models.Command;
using bb.Models.Platform;
using bb.Models.Users;
using bb.Utils;

namespace bb.Core.Commands.List.Fun
{
    public class Percent : CommandBase
    {
        public override string Name => "Percent";
        public override string Author => "https://github.com/itzkitb";
        public override string Source => "Fun/Percent.cs";
        public override Dictionary<Language, string> Description => new() {
            { Language.RuRu, "Отправляет раномный процент с запятой." },
            { Language.EnUs, "Sends a random percentage with a comma." }
        };
        public override int UserCooldown => 10;
        public override int Cooldown => 1;
        public override string[] Aliases => ["%", "percent", "процент", "perc", "проц"];
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

                float percent = (float)new Random().Next(10000) / 100;
                commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "command:percent", data.ChannelId, data.Platform, percent));
            }
            catch (Exception e)
            {
                commandReturn.SetError(e);
            }

            return commandReturn;
        }
    }
}