using butterBror.Models;
using butterBror.Core.Bot;

namespace butterBror.Core.Commands.List
{
    public class Percent : CommandBase
    {
        public override string Name => "Percent";
        public override string Author => "ItzKITb";
        public override string AuthorsGithub => "https://github.com/itzkitb";
        public override string GithubSource => $"{URLs.githubSource}blob/master/butterBror/Core/Commands/List/Percent.cs";
        public override Version Version => new("1.0.0");
        public override Dictionary<string, string> Description => new() {
            { "ru-RU", "Отправляет раномный процент с запятой." },
            { "en-US", "Sends a random percentage with a comma." }
        };
        public override string WikiLink => "https://itzkitb.lol/bot/command?q=percent";
        public override int CooldownPerUser => 5;
        public override int CooldownPerChannel => 1;
        public override string[] Aliases => ["%", "percent", "процент", "perc", "проц"];
        public override string HelpArguments => string.Empty;
        public override DateTime CreationDate => DateTime.Parse("08/08/2024");
        public override bool OnlyBotModerator => false;
        public override bool OnlyBotDeveloper => false;
        public override bool OnlyChannelModerator => false;
        public override PlatformsEnum[] Platforms => [PlatformsEnum.Twitch, PlatformsEnum.Telegram, PlatformsEnum.Discord];
        public override bool IsAsync => false;

        public override CommandReturn Execute(CommandData data)
        {
            Engine.Statistics.FunctionsUsed.Add();
            CommandReturn commandReturn = new CommandReturn();

            try
            {
                float percent = (float)new Random().Next(10000) / 100;
                commandReturn.SetMessage($"🤔 {percent}%");
            }
            catch (Exception e)
            {
                commandReturn.SetError(e);
            }

            return commandReturn;
        }
    }
}