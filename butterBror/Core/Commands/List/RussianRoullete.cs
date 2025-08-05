using butterBror.Models;
using butterBror.Utils;
using butterBror.Core.Bot;
using TwitchLib.Client.Enums;

namespace butterBror.Core.Commands.List
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
        public override int CooldownPerUser => 5;
        public override int CooldownPerChannel => 1;
        public override string[] Aliases => ["rr", "russianroullete", "русскаярулетка", "рр", "br", "buckshotroullete"];
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
                int win = new Random().Next(1, 3);
                int page2 = new Random().Next(1, 5);
                string translationParam = "command:russian_roullete:";
                if (Utils.Balance.GetBalance(data.UserID, data.Platform) > 4)
                {
                    if (win == 1)
                    {
                        // WIN
                        translationParam += "win:" + page2;
                        Utils.Balance.Add(data.UserID, 1, 0, data.Platform);
                    }
                    else
                    {
                        // GAME OVER
                        translationParam += "over:" + page2;
                        if (page2 == 4)
                        {
                            Utils.Balance.Add(data.UserID, -1, 0, data.Platform);
                        }
                        else
                        {
                            Utils.Balance.Add(data.UserID, -5, 0, data.Platform);
                        }
                        commandReturn.SetColor(ChatColorPresets.Red);
                    }
                    commandReturn.SetMessage("🔫 " + LocalizationService.GetString(data.User.Language, translationParam, data.ChannelId, data.Platform));
                }
                else
                {
                    commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:roulette_not_enough_coins", data.ChannelId, data.Platform, Utils.Balance.GetBalance(data.UserID, data.Platform)));
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
