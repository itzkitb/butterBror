using butterBror.Core.Bot;
using butterBror.Models;

namespace butterBror.Core.Commands.List
{
    public class Translation : CommandBase
    {
        public override string Name => "Translation";
        public override string Author => "ItzKITb";
        public override string AuthorsGithub => "https://github.com/itzkitb";
        public override string GithubSource => $"{URLs.githubSource}blob/master/butterBror/Core/Commands/List/Translation.cs";
        public override Version Version => new Version("1.0.0");
        public override Dictionary<string, string> Description => new() {
            { "ru-RU", "Установить язык бота" },
            { "en-US", "Set bot language" }
        };
        public override string WikiLink => "https://itzkitb.lol/bot/command?q=translate";
        public override int CooldownPerUser => 0;
        public override int CooldownPerChannel => 0;
        public override string[] Aliases => ["translate", "translation", "lang", "language", "перевод", "язык"];
        public override string HelpArguments => "[en/ru]";
        public override DateTime CreationDate => DateTime.Parse("29/04/2025");
        public override bool OnlyBotDeveloper => false;
        public override bool OnlyBotModerator => false;
        public override bool OnlyChannelModerator => false;
        public override PlatformsEnum[] Platforms => [PlatformsEnum.Twitch, PlatformsEnum.Telegram, PlatformsEnum.Discord];
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
                var command = new Bot();
                return command.Execute(exdata);
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
