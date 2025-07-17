using butterBror.Models;
using butterBror.Utils;
using butterBror.Core.Bot;

namespace butterBror.Core.Commands.List
{
    public class RandomCMD : CommandBase
    {
        public override string Name => "Random";
        public override string Author => "ItzKITb";
        public override string AuthorsGithub => "https://github.com/itzkitb";
        public override string GithubSource => $"{URLs.githubSource}blob/master/butterBror/Core/Commands/List/RandomCMD.cs";
        public override Version Version => new("1.0.0");
        public override Dictionary<string, string> Description => new() {
            { "ru", "Перемешать текст или вывести рандомное число." },
            { "en", "Shuffle text or output a random number." }
        };
        public override string WikiLink => "https://itzkitb.lol/bot/command?q=random";
        public override int CooldownPerUser => 5;
        public override int CooldownPerChannel => 1;
        public override string[] Aliases => ["random", "rnd", "рандом", "ранд"];
        public override string HelpArguments => "(123-456/text)";
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
                if (data.Arguments.Count > 0)
                {
                    if (data.ArgumentsString.Contains('-'))
                    {
                        string[] numbers = data.ArgumentsString.Split('-');
                        if (numbers.Length == 2 && int.TryParse(numbers[0], out int min) && int.TryParse(numbers[1], out int max))
                            commandReturn.SetMessage($"{TranslationManager.GetTranslation(data.User.Language, "command:random", data.ChannelID, data.Platform)}{new Random().Next(min, max + 1)}");
                        else
                            commandReturn.SetMessage($"{TranslationManager.GetTranslation(data.User.Language, "command:random", data.ChannelID, data.Platform)}{string.Join(" ", [.. data.ArgumentsString.Split(' ').OrderBy(x => new Random().Next())])}");
                    }
                    else
                        commandReturn.SetMessage($"{TranslationManager.GetTranslation(data.User.Language, "command:random", data.ChannelID, data.Platform)}{string.Join(" ", [.. data.ArgumentsString.Split(' ').OrderBy(x => new Random().Next())])}");
                }
                else
                    commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:random", data.ChannelID, data.Platform) + "DinoDance");
            }
            catch (Exception e)
            {
                commandReturn.SetError(e);
            }

            return commandReturn;
        }
    }
}
