using butterBror.Models;
using butterBror.Utils;
using butterBror.Core.Bot;
using TwitchLib.Client.Enums;

namespace butterBror.Core.Commands.List
{
    public class Balance : CommandBase
    {
        public override string Name => "Balance";
        public override string Author => "ItzKITb";
        public override string AuthorsGithub => "https://github.com/itzkitb";
        public override string GithubSource => $"{URLs.githubSource}blob/master/butterBror/Core/Commands/List/Balance.cs";
        public override Version Version => new("1.0.0");
        public override Dictionary<string, string> Description => new() {
            { "ru-RU", "Узнать баланс в экономике бота." },
            { "en-US", "Find out the balance in the bot's economy." }
        };
        public override string WikiLink => "https://itzkitb.lol/bot/command?q=wallet";
        public override int CooldownPerUser => 10;
        public override int CooldownPerChannel => 1;
        public override string[] Aliases => ["balance", "баланс", "bal", "бал", "кошелек", "wallet"];
        public override string HelpArguments => string.Empty;
        public override DateTime CreationDate => DateTime.Parse("07/04/2024");
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
                if (data.Arguments.Count == 0)
                {
                    commandReturn.SetMessage(LocalizationService.GetString(
                        data.User.Language,
                        "command:balance",
                        data.ChannelId,
                        data.Platform,
                        Utils.Balance.GetBalance(data.UserID, data.Platform) + "." + Utils.Balance.GetSubbalance(data.UserID, data.Platform)));
                }
                else
                {
                    var userID = Names.GetUserID(data.Arguments[0].Replace("@", "").Replace(",", ""), data.Platform);
                    if (userID != null)
                    {
                        commandReturn.SetMessage(LocalizationService.GetString(
                            data.User.Language,
                            "command:balance:user",
                            data.ChannelId,
                            data.Platform,
                            Names.DontPing(Text.UsernameFilter(data.ArgumentsString)),
                            Utils.Balance.GetBalance(userID, data.Platform) + "." + Utils.Balance.GetSubbalance(userID, data.Platform)));
                    }
                    else
                    {
                        commandReturn.SetMessage(LocalizationService.GetString(
                            data.User.Language,
                            "error:user_not_found",
                            data.ChannelId,
                            data.Platform,
                            Names.DontPing(Text.UsernameFilter(data.ArgumentsString))));
                        commandReturn.SetColor(ChatColorPresets.Red);
                    }
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
