using bb.Core.Configuration;
using bb.Models.Command;
using bb.Models.Platform;
using bb.Models.Users;
using bb.Utils;
using TwitchLib.Client.Enums;

namespace bb.Core.Commands.List
{
    public class Balance : CommandBase
    {
        public override string Name => "Balance";
        public override string Author => "https://github.com/itzkitb";
        public override string Source => "Currency/Balance.cs";
        public override Dictionary<Language, string> Description => new() {
            { Language.RuRu, "Узнать баланс в экономике бота." },
            { Language.EnUs, "Find out the balance in the bot's economy." }
        };
        public override int UserCooldown => 5;
        public override int Cooldown => 1;
        public override string[] Aliases => ["balance", "баланс", "bal", "бал", "кошелек", "wallet"];
        public override string Help => "[username]";
        public override DateTime CreationDate => DateTime.Parse("2024-07-04T00:00:00.0000000Z");
        public override Roles RoleRequired => Roles.Public;
        public override Platform[] Platforms => [Platform.Twitch, Platform.Telegram, Platform.Discord];
        public override bool IsAsync => false;

        public override CommandReturn Execute(CommandData data)
        {
            CommandReturn commandReturn = new CommandReturn();

            try
            {
                if (data.ChannelId == null || data.Arguments == null)
                {
                    commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:unknown", string.Empty, data.Platform));
                    return commandReturn;
                }

                if (data.Arguments.Count == 0)
                {
                    commandReturn.SetMessage(LocalizationService.GetString(
                        data.User.Language,
                        "command:balance",
                        data.ChannelId,
                        data.Platform,
                        Math.Round(bb.Program.BotInstance.Currency.Get(data.User.Id, data.Platform), 3)));
                    commandReturn.SetSafe(true);
                }
                else
                {
                    var userID = UsernameResolver.GetUserID(data.Arguments[0].Replace("@", "").Replace(",", ""), data.Platform);
                    if (userID != null)
                    {
                        commandReturn.SetMessage(LocalizationService.GetString(
                            data.User.Language,
                            "command:balance:user",
                            data.ChannelId,
                            data.Platform,
                            UsernameResolver.Unmention(TextSanitizer.UsernameFilter(data.ArgumentsString)),
                            Math.Round(bb.Program.BotInstance.Currency.Get(userID, data.Platform), 3)));
                        commandReturn.SetSafe(true);
                    }
                    else
                    {
                        commandReturn.SetMessage(LocalizationService.GetString(
                            data.User.Language,
                            "error:user_not_found",
                            data.ChannelId,
                            data.Platform,
                            UsernameResolver.Unmention(TextSanitizer.UsernameFilter(data.ArgumentsString))));
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
