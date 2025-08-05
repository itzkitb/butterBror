using butterBror.Models;
using butterBror.Utils;
using butterBror.Core.Bot;

namespace butterBror.Core.Commands.List
{
    public class Coinflip : CommandBase
    {
        public override string Name => "CoinFlip";
        public override string Author => "ItzKITb";
        public override string AuthorsGithub => "https://github.com/itzkitb";
        public override string GithubSource => $"{URLs.githubSource}blob/master/butterBror/Core/Commands/List/Coinflip.cs";
        public override Version Version => new("1.0.0");
        public override Dictionary<string, string> Description => new()
        {
            { "ru-RU", "Подкинь монетку!" },
            { "en-US", "Flip a coin!" }
        };
        public override string WikiLink => "https://itzkitb.lol/bot/command?q=coinflip";
        public override int CooldownPerUser => 5;
        public override int CooldownPerChannel => 1;
        public override string[] Aliases => ["coin", "coinflip", "орелилирешка", "оир", "монетка", "headsortails", "hot", "орел", "решка", "heads", "tails"];
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
                int coin = new Random().Next(1, 3);
                if (coin == 1)
                {
                    commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "symbol:coin", data.ChannelId, data.Platform) + LocalizationService.GetString(data.User.Language, "command:coinflip:heads", data.ChannelId, data.Platform));
                }
                else
                {
                    commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "symbol:coin", data.ChannelId, data.Platform) + LocalizationService.GetString(data.User.Language, "command:coinflip:tails", data.ChannelId, data.Platform));
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
