using bb.Core.Bot;
using bb.Models;
using bb.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace bb.Core.Commands.List
{
    public class Yearly : CommandBase
    {
        public override string Name => "Yearly";
        public override string Author => "ItzKITb";
        public override string AuthorsGithub => "https://github.com/itzkitb";
        public override string GithubSource => $"{URLs.githubSource}blob/master/butterBror/Core/Commands/List/Yearly.cs";
        public override Version Version => new("1.0.0");
        public override Dictionary<string, string> Description => new()
        {
            { "ru-RU", "Получить ежегодную награду" },
            { "en-US", "Get yearly reward" }
        };
        public override string WikiLink => "https://itzkitb.ru/bot/command?name=yearly";
        public override int CooldownPerUser => 0;
        public override int CooldownPerChannel => 0;
        public override string[] Aliases => new[] { "yearly", "y", "год" };
        public override DateTime CreationDate => DateTime.Parse("2025-09-18T00:00:00.0000000Z");
        public override string HelpArguments => string.Empty;
        public override bool OnlyBotModerator => false;
        public override bool OnlyBotDeveloper => false;
        public override bool OnlyChannelModerator => false;
        public override PlatformsEnum[] Platforms => new[] { PlatformsEnum.Twitch, PlatformsEnum.Discord };
        public override bool IsAsync => false;

        public override CommandReturn Execute(CommandData data)
        {
            CommandReturn commandReturn = new CommandReturn();
            try
            {
                DateTime currentTime = DateTime.UtcNow;
                string lastRewardStr = bb.Bot.DataBase.Users.GetParameter(data.Platform, DataConversion.ToLong(data.User.ID), "LastYearlyReward").ToString();
                DateTime lastTime = DateTime.MinValue;
                if (!string.IsNullOrEmpty(lastRewardStr))
                {
                    try { lastTime = DateTime.Parse(lastRewardStr, null, DateTimeStyles.AdjustToUniversal); }
                    catch {}
                }

                TimeSpan timeSinceLast = currentTime - lastTime;
                double hourPriceUSD = 1.69;
                double BTRCurrency = bb.Bot.Coins == 0 ? 0 : (double)(bb.Bot.InBankDollars / bb.Bot.Coins);
                double hourPriceBTR = BTRCurrency == 0 ? 0 : hourPriceUSD / BTRCurrency;
                double yearlyPriceBTR = hourPriceBTR * (365 * 24);
                double periodSeconds = 31536000;

                if (timeSinceLast.TotalSeconds >= periodSeconds)
                {
                    long plusBalance = (long)yearlyPriceBTR;
                    long plusSubbalance = (long)((yearlyPriceBTR - plusBalance) * 100);
                    CurrencyManager.Add(data.User.ID, plusBalance, plusSubbalance, data.Platform);
                    bb.Bot.DataBase.Users.SetParameter(data.Platform, DataConversion.ToLong(data.User.ID), "LastYearlyReward", currentTime.ToString("o"));
                    string message = LocalizationService.GetString(data.User.Language, "command:yearly:get", data.ChannelId, data.Platform, plusBalance, plusSubbalance);
                    commandReturn.SetMessage(message);
                }
                else
                {
                    double remainingSeconds = periodSeconds - timeSinceLast.TotalSeconds;
                    TimeSpan remainingTime = TimeSpan.FromSeconds(remainingSeconds);
                    string remainingText = TextSanitizer.FormatTimeSpan(remainingTime, data.User.Language);
                    string message = LocalizationService.GetString(data.User.Language, "command:yearly:cooldown", data.ChannelId, data.Platform, remainingText);
                    commandReturn.SetMessage(message);
                }
            }
            catch (Exception e) { commandReturn.SetError(e); }
            return commandReturn;
        }
    }
}