using bb.Core.Configuration;
using bb.Models.Command;
using bb.Models.Platform;
using bb.Models.Users;

namespace bb.Core.Commands.List.Location
{
    public class SetLocation : CommandBase
    {
        public override string Name => "SetLocation";
        public override string Author => "https://github.com/itzkitb";
        public override string Source => "Location/SetLocation.cs";
        public override Dictionary<Language, string> Description => new() {
            { Language.RuRu, "Установить местоположение, для получения информации о погоде." },
            { Language.EnUs, "Set your location to get weather information." }
        };
        public override int UserCooldown => 15;
        public override int Cooldown => 5;
        public override string[] Aliases => ["loc", "location", "city", "setlocation", "setloc", "setcity", "улокацию", "угород", "установитьлокацию", "установитьгород"];
        public override string Help => "<city>";
        public override DateTime CreationDate => DateTime.Parse("2025-04-29T00:00:00.0000000Z");
        public override Roles RoleRequired => Roles.Public;
        public override Platform[] Platforms => [Platform.Twitch, Platform.Telegram, Platform.Discord];
        public override bool IsAsync => true;

        public override async Task<CommandReturn> ExecuteAsync(CommandData data)
        {
            try
            {
                var exdata = data;
                if (exdata.Arguments is not null && exdata.Arguments.Count >= 1)
                {
                    exdata.Arguments.Insert(0, "set");
                }
                else
                {
                    exdata.Arguments = new List<string>();
                    exdata.Arguments.Insert(0, "get");
                }
                var command = new Utility.Weather();
                return await command.ExecuteAsync(exdata);
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