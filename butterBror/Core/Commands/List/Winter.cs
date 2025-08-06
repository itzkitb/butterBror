using butterBror.Models;
using butterBror.Utils;
using butterBror.Core.Bot;

namespace butterBror.Core.Commands.List
{
    public class Winter : CommandBase
    {
        public override string Name => "Winter";
        public override string Author => "ItzKITb";
        public override string AuthorsGithub => "https://github.com/itzkitb";
        public override string GithubSource => $"{URLs.githubSource}blob/master/butterBror/Core/Commands/List/Winter.cs";
        public override Version Version => new("1.0.0");
        public override Dictionary<string, string> Description => new() {
            { "ru-RU", "Узнать, сколько времени осталось до начала/конца зимы." },
            { "en-US", "Find out how much time is left until the beginning/end of winter." }
        };
        public override string WikiLink => "https://itzkitb.ru/bot/command?name=winter";
        public override int CooldownPerUser => 120;
        public override int CooldownPerChannel => 10;
        public override string[] Aliases => ["winter", "w", "зима"];
        public override string HelpArguments => "(name)";
        public override DateTime CreationDate => DateTime.Parse("07/04/2024");
        public override bool OnlyBotModerator => false;
        public override bool OnlyBotDeveloper => false;
        public override bool OnlyChannelModerator => false;
        public override PlatformsEnum[] Platforms => [PlatformsEnum.Twitch, PlatformsEnum.Telegram, PlatformsEnum.Discord];
        public override bool IsAsync => false;

        public override CommandReturn Execute(CommandData data)
        {
            CommandReturn commandReturn = new CommandReturn();

            try
            {
                commandReturn.SetMessage(Text.TimeTo(
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
