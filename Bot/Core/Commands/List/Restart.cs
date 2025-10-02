using bb.Utils;
using bb.Core.Configuration;
using bb.Models.Command;
using bb.Models.Platform;

namespace bb.Core.Commands.List
{
    public class Restart : CommandBase
    {
        public override string Name => "Restart";
        public override string Author => "ItzKITb";
        public override string AuthorsGithub => "https://github.com/itzkitb";
        public override string GithubSource => $"{URLs.githubSource}blob/master/butterBror/Core/Commands/List/Restart.cs";
        public override Version Version => new("1.0.1");
        public override Dictionary<string, string> Description => new() {
            { "ru-RU", "Этот маленький манёвр будет стоить нам 51 год." },
            { "en-US", "This Little Maneuver's Gonna Cost Us 51 Years." }
        };
        public override string WikiLink => "https://itzkitb.lol/bot/command?q=restart";
        public override int CooldownPerUser => 1;
        public override int CooldownPerChannel => 1;
        public override string[] Aliases => ["shutdown", "выключить"];
        public override string HelpArguments => string.Empty;
        public override DateTime CreationDate => DateTime.Parse("2024-07-04T00:00:00.0000000Z");
        public override bool OnlyBotModerator => true;
        public override bool OnlyBotDeveloper => true;
        public override bool OnlyChannelModerator => false;
        public override PlatformsEnum[] Platforms => [PlatformsEnum.Twitch, PlatformsEnum.Telegram, PlatformsEnum.Discord];
        public override bool IsAsync => false;

        public override CommandReturn Execute(CommandData data)
        {
            CommandReturn commandReturn = new CommandReturn();

            try
            {
                bool isForse = data.Arguments != null && data.Arguments.Contains("--force");
                commandReturn.SetMessage(isForse ? "❄ Turning off..." : "❄ Restarting...");
                _ = bb.Program.BotInstance.Shutdown(isForse);
            }
            catch (Exception e)
            {
                commandReturn.SetError(e);
            }

            return commandReturn;
        }
    }
}
