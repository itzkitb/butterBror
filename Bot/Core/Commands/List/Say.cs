using bb.Core.Configuration;
using bb.Models.Command;
using bb.Models.Platform;
using bb.Models.Users;

namespace bb.Core.Commands.List
{
    public class Say : CommandBase
    {
        public override string Name => "Say";
        public override string Author => "ItzKITb";
        public override string AuthorsGithub => "https://github.com/itzkitb";
        public override string GithubSource => $"{URLs.githubSource}blob/master/butterBror/Core/Commands/List/Say.cs";
        public override Version Version => new Version("1.0.0");
        public override Dictionary<Language, string> Description => new() {
            { Language.RuRu, "Отправляет сообщение от лица бота в текущий чат." },
            { Language.EnUs, "Sends a message on behalf of the bot to the current chat." }
        };
        public override string WikiLink => "https://itzkitb.lol/bot/command?q=say";
        public override int CooldownPerUser => 1;
        public override int CooldownPerChannel => 1;
        public override string[] Aliases => ["say", "tell", "сказать", "type", "написать"];
        public override string HelpArguments => "[text]";
        public override DateTime CreationDate => DateTime.Parse("2024-07-09T00:00:00.0000000Z");
        public override bool OnlyBotDeveloper => true;
        public override bool OnlyBotModerator => true;
        public override bool OnlyChannelModerator => false;
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