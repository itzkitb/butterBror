using bb.Utils;
using bb.Core.Configuration;
using System.Globalization;
using TwitchLib.Client.Enums;
using bb.Models.Command;
using bb.Models.Platform;
using bb.Models.Users;

namespace bb.Core.Commands.List.Currency
{
    public class Pay : CommandBase
    {
        public override string Name => "Pay";
        public override string Author => "https://github.com/itzkitb";
        public override string Source => "Currency/Pay.cs";
        public override Dictionary<Language, string> Description => new() {
            { Language.RuRu, "Перевести деньги другому пользователю" },
            { Language.EnUs, "Transfer money to another user" }
        };
        public override int UserCooldown => 5;
        public override int Cooldown => 1;
        public override string[] Aliases => ["pay", "send", "transfer", "перевод", "отправить", "перевести"];
        public override string Help => "<@username> <amount>";
        public override DateTime CreationDate => DateTime.Parse("2024-11-29T00:00:00.0000000Z");
        public override Roles RoleRequired => Roles.Public;
        public override Platform[] Platforms => [Platform.Twitch, Platform.Telegram];
        public override bool IsAsync => false;

        public override CommandReturn Execute(CommandData data)
        {
            CommandReturn commandReturn = new CommandReturn();

            try
            {
                if (data.Arguments == null || data.Arguments.Count < 2)
                {
                    commandReturn.SetMessage(LocalizationService.GetString(
                        data.User.Language,
                        "error:not_enough_arguments",
                        data.ChannelId,
                        data.Platform,
                        $"{Program.BotInstance.DataBase.Channels.GetCommandPrefix(Platform.Twitch, data.ChatID)}{Aliases[0]} {Help}"));
                    return commandReturn;
                }

                string targetUsername = data.Arguments[0].TrimStart('@');
                string amountString = data.Arguments[1];

                if (targetUsername.Equals(data.User.Name, StringComparison.OrdinalIgnoreCase))
                {
                    commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "command:pay:self_transfer", data.ChannelId, data.Platform));
                    return commandReturn;
                }

                if (!decimal.TryParse(amountString, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal amount) || amount <= 0)
                {
                    commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "command:pay:invalid_amount", data.ChannelId, data.Platform));
                    return commandReturn;
                }

                string targetUserId = UsernameResolver.GetUserID(targetUsername, data.Platform, false);
                if (string.IsNullOrEmpty(targetUserId))
                {
                    commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:user_not_found", data.ChannelId, data.Platform, UsernameResolver.Unmention(targetUsername)));
                    return commandReturn;
                }

                decimal senderBalance = Program.BotInstance.Currency.Get(data.User.Id, data.Platform);

                if (senderBalance < amount)
                {
                    commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "command:pay:insufficient_funds", data.ChannelId, data.Platform, senderBalance.ToString("F2", CultureInfo.InvariantCulture)));
                    return commandReturn;
                }

                Program.BotInstance.Currency.Add(data.User.Id, -amount, data.Platform);
                Program.BotInstance.Currency.Add(targetUserId, amount, data.Platform);

                commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "command:pay:success", data.ChannelId, data.Platform, amount.ToString("F2", CultureInfo.InvariantCulture), targetUsername));
                commandReturn.SetColor(ChatColorPresets.Green);

            }
            catch (Exception e)
            {
                commandReturn.SetError(e);
            }

            return commandReturn;
        }
    }
}