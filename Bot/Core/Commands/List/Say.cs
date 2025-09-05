using bb.Core.Bot;
using bb.Models;

namespace bb.Core.Commands.List
{
    public class Say : CommandBase
    {
        public override string Name => "Say";
        public override string Author => "ItzKITb";
        public override string AuthorsGithub => "https://github.com/itzkitb";
        public override string GithubSource => $"{URLs.githubSource}blob/master/butterBror/Core/Commands/List/Say.cs";
        public override Version Version => new Version("1.0.0");
        public override Dictionary<string, string> Description => new() {
            { "ru-RU", "Отправляет сообщение от лица бота в текущий чат." },
            { "en-US", "Sends a message on behalf of the bot to the current chat." }
        };
        public override string WikiLink => "https://itzkitb.lol/bot/command?q=say";
        public override int CooldownPerUser => 5;
        public override int CooldownPerChannel => 1;
        public override string[] Aliases => ["say", "tell", "сказать", "type", "написать"];
        public override string HelpArguments => "[text]";
        public override DateTime CreationDate => DateTime.Parse("09/07/2024");
        public override bool OnlyBotDeveloper => true;
        public override bool OnlyBotModerator => true;
        public override bool OnlyChannelModerator => false;
        public override PlatformsEnum[] Platforms => [PlatformsEnum.Twitch, PlatformsEnum.Telegram];
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