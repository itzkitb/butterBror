using bb.Core.Configuration;
using bb.Models.Command;
using bb.Models.Platform;
using bb.Models.Users;
using bb.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace bb.Core.Commands.List.Currency
{
    public class Yearly : CommandBase
    {
        public override string Name => "Yearly";
        public override string Author => "https://github.com/itzkitb";
        public override string Source => "Currency/Yearly.cs";
        public override Dictionary<Language, string> Description => new()
        {
            { Language.RuRu, "Получить ежегодную награду." },
            { Language.EnUs, "Get yearly reward." }
        };
        public override int UserCooldown => 10;
        public override int Cooldown => 1;
        public override string[] Aliases => ["yearly", "y", "год"];
        public override DateTime CreationDate => DateTime.Parse("2025-09-18T00:00:00.0000000Z");
        public override string Help => string.Empty;
        public override Roles RoleRequired => Roles.Public;
        public override Platform[] Platforms => [Platform.Twitch, Platform.Discord];
        public override bool IsAsync => false;

        public override CommandReturn Execute(CommandData data)
        {
            CommandReturn commandReturn = new CommandReturn();
            try
            {
                if (Program.BotInstance.UsersBuffer == null || data.ChannelId == null)
                {
                    commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:unknown", string.Empty, data.Platform));
                    return commandReturn;
                }

                DateTime currentTime = DateTime.UtcNow;
                string? lastRewardStr = Program.BotInstance.UsersBuffer.GetParameter(data.Platform, DataConversion.ToLong(data.User.Id), "LastYearlyReward").ToString();
                DateTime lastTime = DateTime.MinValue;
                if (!string.IsNullOrEmpty(lastRewardStr))
                {
                    try { lastTime = DateTime.Parse(lastRewardStr, null, DateTimeStyles.AdjustToUniversal); }
                    catch {}
                }

                TimeSpan timeSinceLast = currentTime - lastTime;
                decimal hourPriceUSD = 0.69M;
                decimal BTRCurrency = Program.BotInstance.Coins == 0 ? 0 : Program.BotInstance.InBankDollars / Program.BotInstance.Coins;
                decimal hourPriceBTR = BTRCurrency == 0 ? 0 : hourPriceUSD / BTRCurrency;
                decimal yearlyPriceBTR = hourPriceBTR * (365 * 24);
                double periodSeconds = 31536000;

                if (timeSinceLast.TotalSeconds >= periodSeconds)
                {
                    Program.BotInstance.Currency.Add(data.User.Id, yearlyPriceBTR, data.Platform);
                    Program.BotInstance.UsersBuffer.SetParameter(data.Platform, DataConversion.ToLong(data.User.Id), "LastYearlyReward", currentTime.ToString("o"));
                    string message = LocalizationService.GetString(data.User.Language, "command:yearly:get", data.ChannelId, data.Platform, Math.Round(yearlyPriceBTR, 3));
                    commandReturn.SetMessage(message);
                }
                else
                {
                    double remainingSeconds = periodSeconds - timeSinceLast.TotalSeconds;
                    decimal percent = Math.Round((decimal)timeSinceLast.TotalSeconds / (decimal)periodSeconds * 100, 5);
                    TimeSpan remainingTime = TimeSpan.FromSeconds(remainingSeconds);
                    string remainingText = TextSanitizer.FormatTimeSpan(remainingTime, data.User.Language);
                    string message = LocalizationService.GetString(data.User.Language, "command:yearly:cooldown", data.ChannelId, data.Platform, remainingText, percent);
                    commandReturn.SetMessage(message);
                }
            }
            catch (Exception e) { commandReturn.SetError(e); }
            return commandReturn;
        }
    }
}