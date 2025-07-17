using butterBror.Data;
using butterBror.Models;
using butterBror.Core.Bot;

namespace butterBror.Core.Commands.List
{
    public class Restart : CommandBase
    {
        public override string Name => "Restart";
        public override string Author => "ItzKITb";
        public override string AuthorsGithub => "https://github.com/itzkitb";
        public override string GithubSource => $"{URLs.githubSource}blob/master/butterBror/Core/Commands/List/Restart.cs";
        public override Version Version => new("1.0.0");
        public override Dictionary<string, string> Description => new() {
            { "ru", "Этот маленький манёвр будет стоить нам 51 год." },
            { "en", "This Little Maneuver's Gonna Cost Us 51 Years." }
        };
        public override string WikiLink => "https://itzkitb.lol/bot/command?q=restart";
        public override int CooldownPerUser => 1;
        public override int CooldownPerChannel => 1;
        public override string[] Aliases => ["restart", "reload", "перезагрузить", "рестарт"];
        public override string HelpArguments => string.Empty;
        public override DateTime CreationDate => DateTime.Parse("07/04/2024");
        public override bool OnlyBotModerator => true;
        public override bool OnlyBotDeveloper => true;
        public override bool OnlyChannelModerator => false;
        public override PlatformsEnum[] Platforms => [PlatformsEnum.Twitch, PlatformsEnum.Telegram, PlatformsEnum.Discord];
        public override bool IsAsync => false;

        public override CommandReturn Execute(CommandData data)
        {
            Engine.Statistics.FunctionsUsed.Add();
            CommandReturn commandReturn = new CommandReturn();

            try
            {
                if (UsersData.Contains(data.UserID, "isBotModerator", data.Platform) || UsersData.Contains(data.UserID, "isBotDev", data.Platform))
                {
                    commandReturn.SetMessage("❄ Перезагрузка...");
                    Engine.Bot.Restart();
                }
                else
                {
                    commandReturn.SetMessage("PauseChamp");
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
