using bb.Core.Configuration;
using bb.Models.Command;
using bb.Models.Platform;
using bb.Models.Users;
using bb.Utils;

namespace bb.Core.Commands.List
{
    public class Coinflip : CommandBase
    {
        public override string Name => "CoinFlip";
        public override string Author => "ItzKITb";
        public override string AuthorsGithub => "https://github.com/itzkitb";
        public override string GithubSource => $"{URLs.githubSource}blob/master/butterBror/Core/Commands/List/Coinflip.cs";
        public override Version Version => new("1.0.0");
        public override Dictionary<Language, string> Description => new()
        {
            { Language.RuRu, "Подкинь монетку!" },
            { Language.EnUs, "Flip a coin!" }
        };
        public override string WikiLink => "https://itzkitb.lol/bot/command?q=coinflip";
        public override int CooldownPerUser => 5;
        public override int CooldownPerChannel => 1;
        public override string[] Aliases => ["coin", "coinflip", "орелилирешка", "оир", "монетка", "headsortails", "hot", "орел", "решка", "heads", "tails"];
        public override string HelpArguments => string.Empty;
        public override DateTime CreationDate => DateTime.Parse("2024-08-08T00:00:00.0000000Z");
        public override bool OnlyBotModerator => false;
        public override bool OnlyBotDeveloper => false;
        public override bool OnlyChannelModerator => false;
        public override Platform[] Platforms => [Platform.Twitch, Platform.Telegram, Platform.Discord];
        public override bool IsAsync => false;

        public override CommandReturn Execute(CommandData data)
        {
            CommandReturn commandReturn = new CommandReturn();
            commandReturn.SetSafe(true);

            try
            {
                if (data.ChannelId == null)
                {
                    commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:unknown", string.Empty, data.Platform));
                    return commandReturn;
                }

                int coin = new Random().Next(1, 3);
                if (coin == 1)
                {
                    commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "command:coinflip:heads", data.ChannelId, data.Platform));
                }
                else
                {
                    commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "command:coinflip:tails", data.ChannelId, data.Platform));
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
