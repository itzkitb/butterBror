using bb.Utils;
using bb.Core.Configuration;
using TwitchLib.Client.Enums;
using bb.Models.Command;
using bb.Models.Platform;

namespace bb.Core.Commands.List
{
    public class RussianRoullete : CommandBase
    {
        public override string Name => "RussianRoullete";
        public override string Author => "ItzKITb";
        public override string AuthorsGithub => "https://github.com/itzkitb";
        public override string GithubSource => $"{URLs.githubSource}blob/master/butterBror/Core/Commands/List/RussianRoullete.cs";
        public override Version Version => new("1.0.0");
        public override Dictionary<string, string> Description => new() {
            { "ru-RU", "Mike Klubnika <3" },
            { "en-US", "Mike Klubnika <3" }
        };
        public override string WikiLink => "https://itzkitb.lol/bot/command?q=rr";
        public override int CooldownPerUser => 10;
        public override int CooldownPerChannel => 1;
        public override string[] Aliases => ["rr", "russianroullete", "русскаярулетка", "рр", "br", "buckshotroullete"];
        public override string HelpArguments => string.Empty;
        public override DateTime CreationDate => DateTime.Parse("2024-08-08T00:00:00.0000000Z");
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
                if (data.ChannelId == null)
                {
                    commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:unknown", string.Empty, data.Platform));
                    return commandReturn;
                }

                int win = new Random().Next(1, 3);
                int page2 = new Random().Next(1, 5);
                string translationParam = "command:russian_roullete:";
                if (Utils.CurrencyManager.GetBalance(data.User.Id, data.Platform) > 4)
                {
                    if (win == 1)
                    {
                        // WIN
                        translationParam += "win:" + page2;
                        Utils.CurrencyManager.Add(data.User.Id, 1, 0, data.Platform);
                    }
                    else
                    {
                        // GAME OVER
                        translationParam += "over:" + page2;
                        if (page2 == 4)
                        {
                            Utils.CurrencyManager.Add(data.User.Id, -1, 0, data.Platform);
                        }
                        else
                        {
                            Utils.CurrencyManager.Add(data.User.Id, -5, 0, data.Platform);
                        }
                        commandReturn.SetColor(ChatColorPresets.Red);
                    }
                    commandReturn.SetMessage("🔫 " + LocalizationService.GetString(data.User.Language, translationParam, data.ChannelId, data.Platform));
                }
                else
                {
                    commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:roulette_not_enough_coins", data.ChannelId, data.Platform, Utils.CurrencyManager.GetBalance(data.User.Id, data.Platform)));
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
