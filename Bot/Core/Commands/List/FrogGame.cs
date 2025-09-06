using bb.Core.Bot;
using bb.Models;
using bb.Utils;
using TwitchLib.Client.Enums;

namespace bb.Core.Commands.List
{
    public class FrogGame : CommandBase
    {
        public override string Name => "Frogs";
        public override string Author => "ItzKITb";
        public override string AuthorsGithub => "https://github.com/itzkitb";
        public override string GithubSource => $"{URLs.githubSource}blob/master/butterBror/Core/Commands/List/FrogGame.cs";
        public override Version Version => new("1.0.3"); // oops
        public override Dictionary<string, string> Description => new() {
            { "ru-RU", "Мини-игра по коллекционированию лягушек." },
            { "en-US", "Minigame about collecting frogs." }
        };
        public override string WikiLink => "https://itzkitb.lol/bot/command?q=frogs";
        public override int CooldownPerUser => 10;
        public override int CooldownPerChannel => 1;
        public override string[] Aliases => ["frog", "frg", "лягушка", "лягушки", "лягуш", "f", "л"];
        public override string HelpArguments => "[(info)/(statistic)/(caught)/(gift [user] [frogs])/(top (gifted/received))]";
        public override DateTime CreationDate => DateTime.Parse("13/10/2024");
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
                if (bb.Bot.DataBase == null || data.ChannelId == null) return null;

                long balance = Convert.ToInt64(bb.Bot.DataBase.Games.GetData("Frogs", data.Platform, DataConversion.ToLong(data.User.ID), "Frogs"));
                long gifted = Convert.ToInt64(bb.Bot.DataBase.Games.GetData("Frogs", data.Platform, DataConversion.ToLong(data.User.ID), "Gifted"));
                long received = Convert.ToInt64(bb.Bot.DataBase.Games.GetData("Frogs", data.Platform, DataConversion.ToLong(data.User.ID), "Received"));

                double frogPriceUSD = 0.12;
                double BTRCurrency = (double)(bb.Bot.Coins == 0 ? 0 : bb.Bot.InBankDollars / bb.Bot.Coins);
                double frogSellPriceBTR = BTRCurrency == 0 ? 9999 : frogPriceUSD / BTRCurrency;
                double frogBuyPriceBTR = frogSellPriceBTR * 3;

                string[] infoAliases = ["info", "i", "information", // en-US
                                        "инфо", "и", "информация"]; // ru-RU
                string[] statisticAliases = ["statistic", "stat", "s",   // en-US
                                             "статистика", "стат", "с"]; // ru-RU
                string[] caughtAliases = ["caught", "c",   // en-US
                                          "поймать", "п"]; // ru-RU
                string[] giftAliases = ["gift", "g",       // en-US
                                        "подарить", "по"]; // ru-RU
                string[] topAliases = ["top", "t",  // en-US
                                       "топ", "т"]; // ru-RU
                string[] currencyAliases = ["currency", "cu",  // en-US
                                            "курс", "ку"];     // ru-RU
                string[] sellAliases = ["sell", "se",     // en-US
                                        "продать", "пр"]; // ru-RU
                string[] buyAliases = ["buy", "b",     // en-US
                                       "купить", "к"]; // ru-RU

                string[] giftedAliases = ["gifted", "gifters", "g", "gift",      // en-US
                                          "подаренные", "даренные", "подарить"]; // ru-RU
                string[] receivedAliases = ["received", "receivers", "receive", "r", // en-US
                                            "полученные", "получить"];               // ru-RU

                if (data.Arguments != null && data.Arguments.Count > 0)
                {
                    if (infoAliases.Contains(data.Arguments[0].ToLower()))
                    {
                        commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "command:frog:info", data.ChannelId, data.Platform));
                    }
                    else if (statisticAliases.Contains(data.Arguments[0].ToLower()))
                    {
                        if (data.Arguments.Count >= 2)
                        {
                            string username = data.Arguments[1].ToLower();
                            string selectedId = UsernameResolver.GetUserID(username, data.Platform, true);

                            if (selectedId == null)
                            {
                                commandReturn.SetMessage(LocalizationService.GetString(
                                    data.User.Language,
                                    "error:user_not_found",
                                    data.ChannelId,
                                    data.Platform,
                                    UsernameResolver.Unmention(username)));
                                commandReturn.SetColor(ChatColorPresets.Red);
                            }
                            else
                            {
                                long selectedBalance = Convert.ToInt64(bb.Bot.DataBase.Games.GetData("Frogs", data.Platform, DataConversion.ToLong(selectedId), "Frogs"));
                                long selectedReceived = Convert.ToInt64(bb.Bot.DataBase.Games.GetData("Frogs", data.Platform, DataConversion.ToLong(selectedId), "Received"));
                                long selectedGifted = Convert.ToInt64(bb.Bot.DataBase.Games.GetData("Frogs", data.Platform, DataConversion.ToLong(selectedId), "Gifted"));

                                commandReturn.SetMessage(LocalizationService.GetString(
                                    data.User.Language,
                                    "command:frog:statistic:user",
                                    data.ChannelId,
                                    data.Platform,
                                    LocalizationService.GetPluralString(data.User.Language, "text:frog", data.ChannelId, data.Platform, selectedBalance, selectedBalance),
                                    LocalizationService.GetPluralString(data.User.Language, "text:frog", data.ChannelId, data.Platform, selectedGifted, selectedGifted),
                                    LocalizationService.GetPluralString(data.User.Language, "text:frog", data.ChannelId, data.Platform, selectedReceived, selectedReceived),
                                    UsernameResolver.Unmention(username)));
                            }
                        }
                        else
                        {
                            commandReturn.SetMessage(LocalizationService.GetString(
                                data.User.Language,
                                "command:frog:statistic",
                                data.ChannelId,
                                data.Platform,
                                LocalizationService.GetPluralString(data.User.Language, "text:frog", data.ChannelId, data.Platform, balance, balance),
                                LocalizationService.GetPluralString(data.User.Language, "text:frog", data.ChannelId, data.Platform, gifted, gifted),
                                LocalizationService.GetPluralString(data.User.Language, "text:frog", data.ChannelId, data.Platform, received, received)));
                        }
                    }
                    else if (caughtAliases.Contains(data.Arguments[0].ToLower()))
                    {
                        if (CooldownManager.CheckCooldown(3600, 0, "FrogsReseter", data.User.ID, data.ChannelId, data.Platform, false, true))
                        {
                            Random rand = new Random();
                            long frogCaughtType = rand.Next(0, 4);
                            long frogsCaughted = rand.Next(1, 11);

                            if (frogCaughtType == 0)
                                frogsCaughted = rand.Next(1, 11);
                            else if (frogCaughtType <= 2)
                                frogsCaughted = rand.Next(10, 101);
                            else if (frogCaughtType == 3)
                                frogsCaughted = rand.Next(100, 1001);

                            string text = GetFrogRange(frogsCaughted);

                            if (text == "error:range")
                            {
                                commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "command:frog:error:range", data.ChannelId, data.Platform));
                                commandReturn.SetColor(ChatColorPresets.Red);
                            }
                            else
                            {
                                long currentBalance = balance + frogsCaughted;
                                commandReturn.SetMessage(LocalizationService.GetString(
                                    data.User.Language,
                                    "command:frog:caught",
                                    data.ChannelId,
                                    data.Platform,
                                    LocalizationService.GetString(data.User.Language, $"command:frog:won:{text}", data.ChannelId, data.Platform),
                                    balance,
                                    currentBalance,
                                    frogsCaughted));

                                bb.Bot.DataBase.Games.SetData("Frogs", data.Platform, DataConversion.ToLong(data.User.ID), "Frogs", currentBalance); // Pizdec
                            }
                        }
                        else
                        {
                            commandReturn.SetMessage(LocalizationService.GetString(
                                data.User.Language,
                                "command:frog:error:caught",
                                data.ChannelId,
                                data.Platform,
                                TextSanitizer.FormatTimeSpan(CooldownManager.GetCooldownTime(data.User.ID, "FrogsReseter", 3600, data.Platform), data.User.Language)));
                            commandReturn.SetColor(ChatColorPresets.Red);
                        }
                    }
                    else if (giftAliases.Contains(data.Arguments[0].ToLower()))
                    {
                        if (data.Arguments.Count > 2)
                        {
                            string username = data.Arguments[1].ToLower();
                            long frogs = DataConversion.ToLong(data.Arguments[2]);
                            string receiverId = UsernameResolver.GetUserID(username, data.Platform, true);

                            if (receiverId == null)
                            {
                                commandReturn.SetMessage(LocalizationService.GetString(
                                    data.User.Language,
                                    "error:user_not_found",
                                    data.ChannelId,
                                    data.Platform,
                                    UsernameResolver.Unmention(username)));
                                commandReturn.SetColor(ChatColorPresets.Red);
                            }
                            else if (frogs == 0)
                            {
                                commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "command:frog:gift:error:zero", data.ChannelId, data.Platform));
                                commandReturn.SetColor(ChatColorPresets.Red);
                            }
                            else if (frogs < 0)
                            {
                                commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "command:frog:gift:error:lowerThanZero", data.ChannelId, data.Platform));
                                commandReturn.SetColor(ChatColorPresets.Red);
                            }
                            else if (balance >= frogs)
                            {
                                commandReturn.SetMessage(LocalizationService.GetString(
                                    data.User.Language,
                                    "command:frog:gift",
                                    data.ChannelId,
                                    data.Platform,
                                    frogs.ToString(),
                                    UsernameResolver.Unmention(username)));

                                long receiverBalance = Convert.ToInt64(bb.Bot.DataBase.Games.GetData("Frogs", data.Platform, DataConversion.ToLong(receiverId), "Frogs"));
                                long receiverReceived = Convert.ToInt64(bb.Bot.DataBase.Games.GetData("Frogs", data.Platform, DataConversion.ToLong(receiverId), "Received"));


                                bb.Bot.DataBase.Games.SetData("Frogs", data.Platform, DataConversion.ToLong(receiverId), "Frogs", receiverBalance + frogs);
                                bb.Bot.DataBase.Games.SetData("Frogs", data.Platform, DataConversion.ToLong(receiverId), "Received", receiverReceived + frogs);
                                bb.Bot.DataBase.Games.SetData("Frogs", data.Platform, DataConversion.ToLong(data.User.ID), "Frogs", balance - frogs);
                                bb.Bot.DataBase.Games.SetData("Frogs", data.Platform, DataConversion.ToLong(data.User.ID), "Gifted", gifted + frogs);
                            }
                            else
                            {
                                commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "frog:gift:error:noFrogs", data.ChannelId, data.Platform));
                                commandReturn.SetColor(ChatColorPresets.Red);
                            }
                        }
                        else
                        {
                            commandReturn.SetMessage(LocalizationService.GetString(
                                data.User.Language,
                                "error:not_enough_arguments",
                                data.ChannelId,
                                data.Platform,
                                $"{bb.Bot.DefaultExecutor}frog gift [user] [frogs]"));
                            commandReturn.SetColor(ChatColorPresets.Red);
                        }
                    }
                    else if (topAliases.Contains(data.Arguments[0].ToLower()))
                    {
                        string topType = "Frogs";
                        if (data.Arguments.Count >= 2)
                        {
                            if (giftedAliases.Contains(data.Arguments[1].ToLower()))
                                topType = "Gifted";
                            else if (receivedAliases.Contains(data.Arguments[1].ToLower()))
                                topType = "Received";
                        }

                        var leaderboard = bb.Bot.DataBase.Games.GetLeaderboard("Frogs", data.Platform, topType);

                        var sortedList = leaderboard
                            .OrderByDescending(kvp => kvp.Value)
                            .Take(5)
                            .ToList();

                        var fullSortedList = leaderboard
                            .OrderByDescending(kvp => kvp.Value)
                            .ToList();

                        string userPosition = LocalizationService.GetString(data.User.Language, "text:empty", data.ChannelId, data.Platform);
                        for (int i = 0; i < fullSortedList.Count; i++)
                        {
                            if (fullSortedList[i].UserId == DataConversion.ToLong(data.User.ID))
                            {
                                userPosition = $"{i + 1}.";
                                break;
                            }
                        }

                        string[] topLines = new string[5];
                        for (int i = 0; i < 5; i++)
                        {
                            if (i < sortedList.Count)
                            {
                                var user = sortedList[i];
                                string username = UsernameResolver.GetUsername(user.UserId.ToString(), data.Platform, true);
                                topLines[i] = $" {i + 1}. {UsernameResolver.Unmention(username)} ({user.Value});";
                            }
                            else
                            {
                                topLines[i] = $" {i + 1}. {LocalizationService.GetString(data.User.Language, "text:empty", data.ChannelId, data.Platform)};";
                            }
                        }

                        string topTypeTranslation = LocalizationService.GetString(
                            data.User.Language,
                            $"command:frog:top:type:{topType.ToLower()}",
                            data.ChannelId,
                            data.Platform
                        ) ?? topType;

                        commandReturn.SetMessage(LocalizationService.GetString(
                            data.User.Language,
                            "command:frog:top",
                            data.ChannelId,
                            data.Platform,
                            topTypeTranslation,
                            topLines[0],
                            topLines[1],
                            topLines[2],
                            topLines[3],
                            topLines[4],
                            userPosition
                        ));
                    }
                    else if (currencyAliases.Contains(data.Arguments[0].ToLower()))
                    {
                        if (data.Arguments.Count >= 2)
                        {
                            long amount = DataConversion.ToLong(data.Arguments[1]);
                            commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "command:frog:currency:amount", data.ChannelId, data.Platform, LocalizationService.GetPluralString(data.User.Language, "text:frog", data.ChannelId, data.Platform, amount, amount), Math.Round(frogSellPriceBTR * amount, 2), Math.Round(frogBuyPriceBTR * amount, 2)));
                        }
                        else
                        {
                            commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "command:frog:currency", data.ChannelId, data.Platform, Math.Round(frogSellPriceBTR, 6), Math.Round(frogBuyPriceBTR, 6)));
                        }
                    }
                    else if (sellAliases.Contains(data.Arguments[0].ToLower()))
                    {
                        if (data.Arguments.Count >= 2)
                        {
                            long amount = DataConversion.ToLong(data.Arguments[1]);
                            double price = amount * frogSellPriceBTR;

                            if (amount < 0)
                            {
                                commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:frog:sell:cant_sell_lower_than_zero", data.ChannelId, data.Platform));
                            }
                            else if (amount > balance)
                            {
                                commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:frog:sell:cant_sell_more_than_balance", data.ChannelId, data.Platform));
                            }
                            else
                            {
                                long plusBalance = (long)price;
                                long plusSubbalance = (long)((price - (double)plusBalance) * 100);

                                Core.Bot.Console.Write($"P:{price};PB:{plusBalance};PS:{plusSubbalance}", "debug");

                                CurrencyManager.Add(data.User.ID, plusBalance, plusSubbalance, data.Platform);
                                bb.Bot.DataBase.Games.SetData("Frogs", data.Platform, DataConversion.ToLong(data.User.ID), "Frogs", balance - amount);
                                commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "command:frog:sell:success", data.ChannelId, data.Platform, LocalizationService.GetPluralString(data.User.Language, "text:frog2", data.ChannelId, data.Platform, amount, amount), Math.Round(frogSellPriceBTR * amount, 2)));
                            }
                        }
                        else
                        {
                            commandReturn.SetMessage(LocalizationService.GetPluralString(data.User.Language, "error:incorrect_parameters", data.ChannelId, data.Platform, data.Arguments.Count)); // Fix AB7
                            commandReturn.SetColor(ChatColorPresets.Red);
                        }
                    }
                    else if (buyAliases.Contains(data.Arguments[0].ToLower()))
                    {
                        if (data.Arguments.Count >= 2)
                        {
                            long amount = DataConversion.ToLong(data.Arguments[1]);
                            double price = amount * frogBuyPriceBTR;
                            double userBalance = CurrencyManager.GetBalance(data.User.ID, data.Platform) + CurrencyManager.GetSubbalance(data.User.ID, data.Platform) / 100;

                            if (amount < 0)
                            {
                                commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:frog:buy:cant_buy_lower_than_zero", data.ChannelId, data.Platform));
                            }
                            else if (price > userBalance)
                            {
                                commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:frog:buy:cant_buy_more_than_balance", data.ChannelId, data.Platform, Math.Round(price, 2), Math.Round(userBalance, 2)));
                            }
                            else
                            {
                                long minusBalance = (long)price;
                                long minusSubbalance = (long)((price - (double)minusBalance) * 100);

                                Core.Bot.Console.Write($"P:{price};MB:{-minusBalance};MS:{-minusSubbalance}", "debug");

                                CurrencyManager.Add(data.User.ID, -minusBalance, -minusSubbalance, data.Platform);
                                bb.Bot.DataBase.Games.SetData("Frogs", data.Platform, DataConversion.ToLong(data.User.ID), "Frogs", balance + amount);
                                commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "command:frog:buy:success", data.ChannelId, data.Platform, LocalizationService.GetPluralString(data.User.Language, "text:frog2", data.ChannelId, data.Platform, amount, amount), Math.Round(price, 2)));
                            }
                        }
                        else
                        {
                            commandReturn.SetMessage(LocalizationService.GetPluralString(data.User.Language, "error:incorrect_parameters", data.ChannelId, data.Platform, data.Arguments.Count)); // Fix AB7

                        }
                    }
                    else
                    {
                        commandReturn.SetMessage(LocalizationService.GetPluralString(data.User.Language, "error:incorrect_parameters", data.ChannelId, data.Platform, data.Arguments.Count)); // Fix AB7
                        commandReturn.SetColor(ChatColorPresets.Red);
                    }
                }
                else
                {
                    commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:not_enough_arguments", data.ChannelId, data.Platform, $"{bb.Bot.DefaultExecutor}frog {HelpArguments}"));
                    commandReturn.SetColor(ChatColorPresets.Red);
                }
            }
            catch (Exception e)
            {
                commandReturn.SetError(e);
            }

            return commandReturn;
        }

        string GetFrogRange(long number)
        {
            var ranges = new Dictionary<string, Tuple<long, long>>
            {
                {"1", Tuple.Create(1L, 1L)},
                {"2", Tuple.Create(2L, 2L)},
                {"3", Tuple.Create(3L, 3L)},
                {"4-6", Tuple.Create(4L, 6L)},
                {"7-10", Tuple.Create(7L, 10L)},
                {"26-50", Tuple.Create(10L, 50L)},
                {"51-75", Tuple.Create(51L, 75L)},
                {"76-100", Tuple.Create(76L, 100L)},
                {"101-200", Tuple.Create(101L, 200L)},
                {"201-300", Tuple.Create(201L, 300L)},
                {"301-400", Tuple.Create(301L, 400L)},
                {"401-500", Tuple.Create(401L, 500L)},
                {"501-600", Tuple.Create(501L, 600L)},
                {"601-700", Tuple.Create(601L, 700L)},
                {"701-800", Tuple.Create(701L, 800L)},
                {"801-900", Tuple.Create(801L, 900L)},
                {"901-1000", Tuple.Create(901L, 1000L)},
            };

            foreach (var range in ranges)
            {
                if (number >= range.Value.Item1 && number <= range.Value.Item2)
                {
                    return range.Key;
                }
            }

            return "error:range";
        }
    }
}
