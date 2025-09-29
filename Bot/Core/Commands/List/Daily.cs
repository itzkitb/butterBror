using bb.Utils;
using bb.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using bb.Models.Command;
using bb.Models.Platform;

namespace bb.Core.Commands.List
{
    public class Daily : CommandBase
    {
        public override string Name => "Daily";
        public override string Author => "ItzKITb";
        public override string AuthorsGithub => "https://github.com/itzkitb";
        public override string GithubSource => $"{URLs.githubSource}blob/master/butterBror/Core/Commands/List/Daily.cs";
        public override Version Version => new("1.0.0");
        public override Dictionary<string, string> Description => new()
        {
            { "ru-RU", "Получить ежедневную награду." },
            { "en-US", "Get daily reward." }
        };
        public override string WikiLink => "https://itzkitb.ru/bot/command?name=daily";
        public override int CooldownPerUser => 0;
        public override int CooldownPerChannel => 0;
        public override string[] Aliases => ["daily", "d", "день"];
        public override DateTime CreationDate => DateTime.Parse("2025-09-18T00:00:00.0000000Z");
        public override string HelpArguments => string.Empty;
        public override bool OnlyBotModerator => false;
        public override bool OnlyBotDeveloper => false;
        public override bool OnlyChannelModerator => false;
        public override PlatformsEnum[] Platforms => [PlatformsEnum.Twitch, PlatformsEnum.Discord];
        public override bool IsAsync => false;

        public override CommandReturn Execute(CommandData data)
        {
            CommandReturn commandReturn = new CommandReturn();
            try
            {
                if (data.ChannelId == null || bb.Bot.UsersBuffer == null)
                {
                    commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:unknown", string.Empty, data.Platform));
                    return commandReturn;
                }

                DateTime currentTime = DateTime.UtcNow;
                string? lastRewardStr = bb.Bot.UsersBuffer.GetParameter(data.Platform, DataConversion.ToLong(data.User.Id), "LastDailyReward").ToString();
                DateTime lastTime = DateTime.MinValue;
                if (!string.IsNullOrEmpty(lastRewardStr))
                {
                    try { lastTime = DateTime.Parse(lastRewardStr, null, DateTimeStyles.AdjustToUniversal); }
                    catch { }
                }

                TimeSpan timeSinceLast = currentTime - lastTime;
                double hourPriceUSD = 0.69;
                double BTRCurrency = bb.Bot.Coins == 0 ? 0 : (double)(bb.Bot.InBankDollars / bb.Bot.Coins);
                double hourPriceBTR = BTRCurrency == 0 ? 0 : hourPriceUSD / BTRCurrency;
                double dailyPriceBTR = hourPriceBTR * 24;
                double periodSeconds = 86400;

                if (timeSinceLast.TotalSeconds >= periodSeconds)
                {
                    long plusBalance = (long)dailyPriceBTR;
                    long plusSubbalance = (long)((dailyPriceBTR - plusBalance) * 100);
                    CurrencyManager.Add(data.User.Id, plusBalance, plusSubbalance, data.Platform);
                    bb.Bot.UsersBuffer.SetParameter(data.Platform, DataConversion.ToLong(data.User.Id), "LastDailyReward", currentTime.ToString("o"));
                    string message = LocalizationService.GetString(data.User.Language, "command:daily:get", data.ChannelId, data.Platform, plusBalance, plusSubbalance);
                    commandReturn.SetMessage(message);
                }
                else
                {
                    double remainingSeconds = periodSeconds - timeSinceLast.TotalSeconds;
                    TimeSpan remainingTime = TimeSpan.FromSeconds(remainingSeconds);
                    string remainingText = TextSanitizer.FormatTimeSpan(remainingTime, data.User.Language);
                    string message = LocalizationService.GetString(data.User.Language, "command:daily:cooldown", data.ChannelId, data.Platform, remainingText);
                    commandReturn.SetMessage(message);
                }
            }
            catch (Exception e) { commandReturn.SetError(e); }
            return commandReturn;
        }
    }
}