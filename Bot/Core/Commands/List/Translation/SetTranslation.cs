using bb.Core.Configuration;
using bb.Models.Command;
using bb.Models.Platform;
using bb.Models.Users;

namespace bb.Core.Commands.List.Translation
{
    public class SetTranslation : CommandBase
    {
        public override string Name => "Translation";
        public override string Author => "https://github.com/itzkitb";
        public override string Source => "Translation/SetTranslation.cs";
        public override Dictionary<Language, string> Description => new() {
            { Language.RuRu, "Установить язык бота" },
            { Language.EnUs, "Set bot language" }
        };
        public override int UserCooldown => 10;
        public override int Cooldown => 1;
        public override string[] Aliases => ["translate", "translation", "lang", "language", "перевод", "язык"];
        public override string Help => "<en|ru>";
        public override DateTime CreationDate => DateTime.Parse("2025-04-29T00:00:00.0000000Z");
        public override Roles RoleRequired => Roles.Public;
        public override Platform[] Platforms => [Platform.Twitch, Platform.Telegram, Platform.Discord];
        public override bool IsAsync => false;

        public override CommandReturn Execute(CommandData data)
        {
            try
            {
                var exdata = data;
                if (exdata.Arguments is not null && exdata.Arguments.Count >= 1)
                {
                    exdata.Arguments.Insert(0, "set");
                    exdata.Arguments.Insert(0, "lang");
                }
                else
                {
                    exdata.Arguments = new List<string>();
                    exdata.Arguments.Insert(0, "get");
                    exdata.Arguments.Insert(0, "lang");
                }
                var command = new BotManagement.Bot();
                CommandReturn result = command.Execute(exdata);
                result.SetSafe(true);
                return result;
            }
            catch (Exception e)
            {
                CommandReturn commandReturn = new CommandReturn();
                commandReturn.SetError(e);
                return commandReturn;
            }
        }
    }
}
