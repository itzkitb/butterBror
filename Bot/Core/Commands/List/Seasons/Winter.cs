using bb.Core.Configuration;
using bb.Models.Command;
using bb.Models.Platform;
using bb.Models.Users;
using bb.Utils;

namespace bb.Core.Commands.List.Seasons
{
    public class Winter : CommandBase
    {
        public override string Name => "Winter";
        public override string Author => "https://github.com/itzkitb";
        public override string Source => "Seasons/Winter.cs";
        public override Dictionary<Language, string> Description => new() {
            { Language.RuRu, "Узнать, сколько времени осталось до начала/конца зимы." },
            { Language.EnUs, "Find out how much time is left until the beginning/end of winter." }
        };
        public override int UserCooldown => 10;
        public override int Cooldown => 1;
        public override string[] Aliases => ["winter", "w", "зима"];
        public override string Help => "[username]";
        public override DateTime CreationDate => DateTime.Parse("2024-07-04T00:00:00.0000000Z");
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

                commandReturn.SetMessage(TextSanitizer.TimeTo(
                    new(2000, 12, 1),
                    new(2000, 3, 1),
                    "winter",
                    data.User.Language,
                    data.ArgumentsString,
                    data.ChannelId,
                    data.Platform));
            }
            catch (Exception e)
            {
                commandReturn.SetError(e);
            }

            return commandReturn;
        }
    }
}
