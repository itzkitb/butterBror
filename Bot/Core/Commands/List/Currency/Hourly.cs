using bb.Core.Configuration;
using bb.Models.Command;
using bb.Models.Platform;
using bb.Models.Users;
using bb.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using static bb.Core.Bot.Logger;

namespace bb.Core.Commands.List.Currency
{
    public class Hourly : CommandBase
    {
        public override string Name => "Hourly";
        public override string Author => "https://github.com/itzkitb";
        public override string Source => "Currency/Hourly.cs";
        public override Dictionary<Language, string> Description => new()
        {
            { Language.RuRu, "Получить ежечасную награду." },
            { Language.EnUs, "Get hourly reward." }
        };
        public override int UserCooldown => 10;
        public override int Cooldown => 1;
        public override string[] Aliases => ["hourly", "h", "час"];
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
                string? lastRewardStr = Program.BotInstance.UsersBuffer.GetParameter(data.Platform, DataConversion.ToLong(data.User.Id), "LastHourlyReward").ToString();
                DateTime lastTime = DateTime.MinValue;
                if (!string.IsNullOrEmpty(lastRewardStr))
                {
                    try { lastTime = DateTime.Parse(lastRewardStr, null, DateTimeStyles.AdjustToUniversal); }
                    catch { }
                }
                
                TimeSpan timeSinceLast = currentTime - lastTime;
                decimal hourPriceUSD = 0.69M;
                decimal BTRCurrency = Program.BotInstance.Coins == 0 ? 0 : Program.BotInstance.InBankDollars / Program.BotInstance.Coins;
                decimal hourPriceBTR = BTRCurrency == 0 ? 0 : hourPriceUSD / BTRCurrency;
                double periodSeconds = 3600;

                if (timeSinceLast.TotalSeconds >= periodSeconds)
                {
                    Program.BotInstance.Currency.Add(data.User.Id, hourPriceBTR, data.Platform);
                    Program.BotInstance.UsersBuffer.SetParameter(data.Platform, DataConversion.ToLong(data.User.Id), "LastHourlyReward", currentTime.ToString("o"));
                    string message = LocalizationService.GetString(data.User.Language, "command:hourly:get", data.ChannelId, data.Platform, Math.Round(hourPriceBTR, 3));
                    commandReturn.SetMessage(message);
                }
                else
                {
                    double remainingSeconds = periodSeconds - timeSinceLast.TotalSeconds;
                    decimal percent = Math.Round((decimal)timeSinceLast.TotalSeconds / (decimal)periodSeconds * 100, 5);
                    TimeSpan remainingTime = TimeSpan.FromSeconds(remainingSeconds);
                    string remainingText = TextSanitizer.FormatTimeSpan(remainingTime, data.User.Language);
                    string message = LocalizationService.GetString(data.User.Language, "command:hourly:cooldown", data.ChannelId, data.Platform, remainingText, percent);
                    commandReturn.SetMessage(message);
                }
            }
            catch (Exception e) { commandReturn.SetError(e); }
            return commandReturn;
        }
    }
}