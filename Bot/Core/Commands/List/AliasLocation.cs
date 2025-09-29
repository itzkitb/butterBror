using bb.Core.Bot;
using bb.Models;

namespace bb.Core.Commands.List
{
    public class SetLocation : CommandBase
    {
        public override string Name => "SetLocation";
        public override string Author => "ItzKITb";
        public override string AuthorsGithub => "https://github.com/itzkitb";
        public override string GithubSource => $"{URLs.githubSource}blob/master/butterBror/Core/Commands/List/SetLocation.cs";
        public override Version Version => new Version("1.0.0");
        public override Dictionary<string, string> Description => new() {
            { "ru-RU", "Установить местоположение, для получения информации о погоде." },
            { "en-US", "Set your location to get weather information." }
        };
        public override string WikiLink => "https://itzkitb.lol/bot/command?q=setlocation";
        public override int CooldownPerUser => 15;
        public override int CooldownPerChannel => 5;
        public override string[] Aliases => ["loc", "location", "city", "setlocation", "setloc", "setcity", "улокацию", "угород", "установитьлокацию", "установитьгород"];
        public override string HelpArguments => "(city name)";
        public override DateTime CreationDate => DateTime.Parse("2025-04-29T00:00:00.0000000Z");
        public override bool OnlyBotDeveloper => false;
        public override bool OnlyBotModerator => false;
        public override bool OnlyChannelModerator => false;
        public override PlatformsEnum[] Platforms => [PlatformsEnum.Twitch, PlatformsEnum.Telegram, PlatformsEnum.Discord];
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
                var command = new Weather();
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