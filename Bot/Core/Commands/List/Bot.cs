using bb.Core.Configuration;
using bb.Models.Command;
using bb.Models.Platform;
using bb.Utils;
using DankDB;
using SevenTV.Types.Rest;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Models;
using static bb.Core.Bot.Console;

namespace bb.Core.Commands.List
{
    public class Bot : CommandBase
    {
        public override string Name => "Bot";
        public override string Author => "ItzKITb";
        public override string AuthorsGithub => "https://github.com/itzkitb";
        public override string GithubSource => $"{URLs.githubSource}blob/master/butterBror/Core/Commands/List/Bot.cs";
        public override Version Version => new("1.0.1");
        public override Dictionary<string, string> Description => new(){
            { "ru-RU", "Главная команда бота." },
            { "en-US", "The main command of the bot." }
        };
        public override string WikiLink => "https://itzkitb.lol/bot/command?q=bot";
        public override int CooldownPerUser => 10;
        public override int CooldownPerChannel => 1;
        public override string[] Aliases => ["bot", "bt", "бот", "бт", "main", "start", "старт", "главная", "info", "инфо", "information", "информация"];
        public override string HelpArguments => "(lang (set [en/ru]), verify, currency (adddollars [int]), ban [username] (reason), pardon [username], rejoinchannel [channel], addchannel [channel], delchannel [channel], joinchannel [channel], leavechannel [channel], modadd [username], demod [username])";
        public override DateTime CreationDate => DateTime.Parse("2024-07-04T00:00:00.0000000Z");
        public override bool OnlyBotModerator => false;
        public override bool OnlyBotDeveloper => false;
        public override bool OnlyChannelModerator => false;
        public override PlatformsEnum[] Platforms => [PlatformsEnum.Twitch, PlatformsEnum.Telegram, PlatformsEnum.Discord];
        public override bool IsAsync => false;


        public override CommandReturn Execute(CommandData data)
        {
            CommandReturn commandReturn = new CommandReturn();
            commandReturn.SetSafe(true);

            try
            {
                if (data.ChannelId == null || bb.Program.BotInstance.DataBase == null || bb.Program.BotInstance.TwitchName == null || bb.Program.BotInstance.UsersBuffer == null
                    || bb.Program.BotInstance.Clients.Twitch == null)
                {
                    commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:unknown", string.Empty, data.Platform));
                    return commandReturn;
                }

                List<string> arguments = (data.Arguments is null ? [] : data.Arguments);

                bool isModerator = (data.User.IsBotModerator == null ? false : (bool)data.User.IsBotModerator);
                bool isChannelModerator = (data.User.IsModerator == null ? false : (bool)data.User.IsModerator);
                bool isDeveloper = (data.User.IsBotDeveloper == null ? false : (bool)data.User.IsBotDeveloper);
                string userId = data.User.Id;
                string channelId = data.ChannelId;
                string channel = data.Channel ?? string.Empty;
                string messageId = data.MessageID;
                string language = data.User.Language;

                string[] languageAlias = ["lang", "l", "language", "язык", "я"];
                string[] setAlias = ["set", "s", "сет", "установить", "у"];

                string[] languageEnglishAlias = ["en-us", "en", "e", "англ", "английский"];
                string[] languageRussianAlias = ["ru-ru", "ru", "r", "рус", "русский"];

                string[] addCurrencyAlias = ["adddollars", "addd", "ad", "добавитьдоллары", "дд"];
                string[] currencyAlias = ["currency", "c", "курс"];
                string[] inviteAlias = ["invite", "пригласить", "i", "п"];
                string[] updateTranslationAlias = ["updatetranslation", "uptr", "ut", "обновитьперевод", "оп"];

                string[] banAlias = ["ban", "бан", "block", "kill", "заблокировать", "чел"];
                string[] pardonAlias = ["pardon", "unblock", "unban", "разблокировать", "разбанить", "анбан"];
                string[] rejoinAlias = ["rejoin", "rej", "переподключить", "reenter", "ree"];
                string[] addChannelAlias = ["addchannel", "newchannel", "addc", "newc", "дканал", "нканал"];
                string[] deleteChannelAlias = ["delchannel", "deletechannel", "удалитьканал", "уканал"];
                string[] joinChannelAlias = ["joinchannel", "joinc", "вканал"];
                string[] leaveChannelAlias = ["leavechannel", "leavec", "пканал"];
                string[] prefixAlias = ["prefix", "pref", "pre", "преф", "префикс"];

                string[] moderatorAddAlias = ["modadd", "дм", "добавитьмодератора"];
                string[] moderatorDeleteAlias = ["demod", "ум", "удалитьмодератора"];

                if (arguments.Count > 0)
                {
                    var argumentOne = arguments[0].ToLower();
                    if (languageAlias.Contains(argumentOne, StringComparer.OrdinalIgnoreCase))
                    {
                        if (arguments.Count > 1)
                        {
                            string argument2 = arguments.ElementAt(1).ToLower();
                            if (setAlias.Contains(argument2))
                            {
                                if (arguments.Count > 2)
                                {
                                    string arg3 = arguments.ElementAt(2).ToLower();
                                    string result = string.Empty;
                                    if (languageEnglishAlias.Contains(arg3))
                                        result = "en-US";
                                    else if (languageRussianAlias.Contains(arg3))
                                        result = "ru-RU";

                                    if (result.Equals(string.Empty))
                                    {
                                        commandReturn.SetMessage(LocalizationService.GetPluralString(language, "error:incorrect_parameters", channelId, data.Platform, arguments.Count));
                                        commandReturn.SetColor(ChatColorPresets.Red);
                                    }
                                    else
                                    {
                                        bb.Program.BotInstance.UsersBuffer.SetParameter(data.Platform, DataConversion.ToLong(data.User.Id), Users.Language, result);
                                        commandReturn.SetMessage(LocalizationService.GetString(result, "command:bot:language:set", channelId, data.Platform));
                                    }
                                }
                                else
                                {
                                    commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:not_enough_arguments", channelId, data.Platform, $"{bb.Program.BotInstance.DefaultCommandPrefix}bot lang set en/ru"));
                                    commandReturn.SetColor(ChatColorPresets.Red);
                                }
                            }
                            else if (argument2.Contains("get"))
                                commandReturn.SetMessage(LocalizationService.GetString(language, "info:language", channelId, data.Platform));
                            else
                            {
                                commandReturn.SetMessage(LocalizationService.GetPluralString(language, "error:incorrect_parameters", channelId, data.Platform, arguments.Count));
                                commandReturn.SetColor(ChatColorPresets.Red);
                            }
                        }
                        else
                        {
                            commandReturn.SetMessage(LocalizationService.GetString(language, "error:not_enough_arguments", channelId, data.Platform, $"{bb.Program.BotInstance.DefaultCommandPrefix}bot lang (set en/ru)/get"));
                            commandReturn.SetColor(ChatColorPresets.Red);
                        }
                    }
                    else if (inviteAlias.Contains(argumentOne, StringComparer.OrdinalIgnoreCase))
                    {
                        Write($"Request to add a bot from @{data.User.Name}");
                        Dictionary<string, dynamic> userData = new()
                            {
                                { "language", data.User.Language },
                                { "username", data.User.Name },
                                { "user_indentificator", data.User.Id },
                                { "date", DateTime.UtcNow }
                            };
                        Directory.CreateDirectory(bb.Program.BotInstance.Paths.Root + "INVITE/");
                        Manager.Save(bb.Program.BotInstance.Paths.Root + $"INVITE/{data.User.Name}.txt", $"rq{DateTime.UtcNow}", userData);
                        commandReturn.SetMessage(LocalizationService.GetString(language, "command:bot:user_verify", channelId, data.Platform));
                    }
                    else if (prefixAlias.Contains(argumentOne, StringComparer.OrdinalIgnoreCase))
                    {
                        if (arguments.Count == 1)
                        {
                            string currentPrefix = bb.Program.BotInstance.DataBase.Channels.GetCommandPrefix(data.Platform, data.ChannelId);
                            commandReturn.SetMessage(LocalizationService.GetString(language, "command:bot:prefix:current", channelId, data.Platform, currentPrefix));
                        }
                        else if (arguments.Count > 1 && setAlias.Contains(arguments[1].ToLower()))
                        {
                            if (isDeveloper || isModerator || isChannelModerator)
                            {
                                if (arguments.Count > 2)
                                {
                                    string newPrefix = arguments[2];
                                    bb.Program.BotInstance.DataBase.Channels.SetCommandPrefix(data.Platform, data.ChannelId, newPrefix);
                                    commandReturn.SetMessage(LocalizationService.GetString(language, "command:bot:prefix:set", channelId, data.Platform, newPrefix));
                                }
                                else
                                {
                                    commandReturn.SetMessage(LocalizationService.GetString(language, "error:not_enough_arguments", channelId, data.Platform, $"{bb.Program.BotInstance.DefaultCommandPrefix}bot prefix set [prefix]"));
                                    commandReturn.SetColor(ChatColorPresets.Red);
                                }
                            }
                            else
                            {
                                commandReturn.SetMessage(LocalizationService.GetString(language, "error:not_enough_rights", channelId, data.Platform));
                                commandReturn.SetColor(ChatColorPresets.Red);
                            }
                        }
                        else if (arguments.Count > 1)
                        {
                            string channelName = arguments[1];
                            string channelIdForOtherChannel = UsernameResolver.GetUserID(channelName, data.Platform);
                            if (string.IsNullOrEmpty(channelIdForOtherChannel))
                            {
                                commandReturn.SetMessage(LocalizationService.GetString(language, "error:user_not_found", channelId, data.Platform, channelName));
                                commandReturn.SetColor(ChatColorPresets.Red);
                            }
                            else
                            {
                                string prefix = bb.Program.BotInstance.DataBase.Channels.GetCommandPrefix(data.Platform, channelIdForOtherChannel);
                                commandReturn.SetMessage(LocalizationService.GetString(language, "command:bot:prefix:other", channelId, data.Platform, channelName, prefix));
                            }
                        }
                        else
                        {
                            commandReturn.SetMessage(LocalizationService.GetString(language, "error:not_enough_arguments", channelId, data.Platform, $"{bb.Program.BotInstance.DefaultCommandPrefix}bot prefix [set | channel]"));
                            commandReturn.SetColor(ChatColorPresets.Red);
                        }
                    }
                    else if (currencyAlias.Contains(argumentOne, StringComparer.OrdinalIgnoreCase))
                    {
                        if (arguments.Count > 2 && addCurrencyAlias.Contains(arguments.ElementAt(1).ToLower()) && isDeveloper)
                        {
                            int converted = Utils.DataConversion.ToInt(arguments.ElementAt(2).ToLower());
                            bb.Program.BotInstance.InBankDollars += converted;

                            commandReturn.SetMessage(LocalizationService.GetString(language, "command:bot:currency:add", channelId, data.Platform, converted.ToString(), bb.Program.BotInstance.InBankDollars.ToString()));
                        }
                        else
                        {
                            // 0_0

                            var date = DateTime.UtcNow.AddDays(-1);
                            var currencyData = Manager.Get<Dictionary<string, dynamic>>(bb.Program.BotInstance.Paths.Currency, $"{date.Day}.{date.Month}.{date.Year}");

                            decimal oldCurrencyAmount = currencyData.ContainsKey("amount") ? currencyData["amount"].GetDecimal() : 0;
                            float oldCurrencyCost = currencyData.ContainsKey("cost") ? currencyData["cost"].GetSingle() : 0;
                            int oldCurrencyUsers = currencyData.ContainsKey("users") ? currencyData["users"].GetInt32() : 0;
                            int oldDollarsInBank = currencyData.ContainsKey("dollars") ? currencyData["dollars"].GetInt32() : 0;

                            decimal oldMiddle = oldCurrencyUsers != 0 ? decimal.Parse((oldCurrencyAmount / oldCurrencyUsers).ToString("0.00")) : 0;

                            float currencyCost = float.Parse((bb.Program.BotInstance.InBankDollars / bb.Program.BotInstance.Coins).ToString("0.00"));
                            decimal middleUsersBalance = decimal.Parse((bb.Program.BotInstance.Coins / bb.Program.BotInstance.Users).ToString("0.00"));

                            float plusOrMinusCost = currencyCost - oldCurrencyCost;
                            decimal plusOrMinusAmount = bb.Program.BotInstance.Coins - oldCurrencyAmount;
                            int plusOrMinusUsers = bb.Program.BotInstance.Users - oldCurrencyUsers;
                            int plusOrMinusDollars = bb.Program.BotInstance.InBankDollars - oldDollarsInBank;
                            decimal plusOrMinusMiddle = middleUsersBalance - oldMiddle;

                            string buttersCostProgress = oldCurrencyCost > currencyCost ? $"🔽 ({plusOrMinusCost:0.00})" : oldCurrencyCost == currencyCost ? "⏺️ (0)" : $"🔼 (+{plusOrMinusCost:0.00})";
                            string buttersAmountProgress = oldCurrencyAmount > bb.Program.BotInstance.Coins ? $"🔽 ({plusOrMinusAmount:0.00})" : oldCurrencyAmount == bb.Program.BotInstance.Coins ? "⏺️ (0)" : $"🔼 (+{plusOrMinusAmount:0.00})";
                            string buttersDollars = oldDollarsInBank > bb.Program.BotInstance.InBankDollars ? $"🔽 ({plusOrMinusDollars})" : oldDollarsInBank == bb.Program.BotInstance.InBankDollars ? "⏺️ (0)" : $"🔼 (+{plusOrMinusDollars})";
                            string buttersMiddle = oldMiddle > middleUsersBalance ? $"🔽 ({plusOrMinusMiddle:0.00})" : oldMiddle == middleUsersBalance ? "⏺️ (0)" : $"🔼 (+{plusOrMinusMiddle:0.00})";

                            commandReturn.SetMessage(LocalizationService.GetString(
                                language,
                                "command:bot:currency",
                                channelId,
                                data.Platform,
                                bb.Program.BotInstance.Coins.ToString() + " " + buttersAmountProgress,
                                bb.Program.BotInstance.Users.ToString(),
                                (bb.Program.BotInstance.Coins / bb.Program.BotInstance.Users).ToString("0.00") + " " + buttersMiddle,
                                (bb.Program.BotInstance.InBankDollars / bb.Program.BotInstance.Coins).ToString("0.00") + " " + buttersCostProgress,
                                bb.Program.BotInstance.InBankDollars + " " + buttersDollars));
                            commandReturn.SetSafe(false);
                        }
                    }
                    else if (isModerator || isDeveloper)
                    {
                        if (banAlias.Contains(argumentOne, StringComparer.OrdinalIgnoreCase))
                        {
                            if (arguments.Count > 1)
                            {
                                string arg2 = arguments[1].ToLower().Replace("@", "").Replace(",", "");
                                string bcid = UsernameResolver.GetUserID(arg2, data.Platform);
                                string reason = data.ArgumentsString.Replace(arguments.ElementAt(0), "").Replace(arguments.ElementAt(1), "").Replace("  ", "");
                                if (bcid.Equals(null))
                                {
                                    commandReturn.SetMessage(LocalizationService.GetString(language, "error:user_not_found", channelId, data.Platform, arg2));
                                    commandReturn.SetColor(ChatColorPresets.Red);
                                    commandReturn.SetSafe(false);
                                }
                                else
                                {
                                    if (isDeveloper || (!bb.Program.BotInstance.DataBase.Roles.IsModerator(data.Platform, DataConversion.ToLong(data.User.Id)) && !bb.Program.BotInstance.DataBase.Roles.IsDeveloper(data.Platform, DataConversion.ToLong(data.User.Id))))
                                    {
                                        bb.Program.BotInstance.MessageSender.Send(PlatformsEnum.Twitch, $"widelol #{arg2} banned in bot: {reason}", bb.Program.BotInstance.TwitchName, isSafe: true);
                                        bb.Program.BotInstance.DataBase.Roles.AddBannedUser(data.Platform, DataConversion.ToLong(bcid), DateTime.UtcNow, data.User.Id, reason);
                                        commandReturn.SetMessage(LocalizationService.GetString(language, "command:bot:user_ban", channelId, data.Platform, arg2, reason));
                                    }
                                    else
                                    {
                                        commandReturn.SetMessage(LocalizationService.GetString(language, "error:not_enough_rights", channelId, data.Platform));
                                        commandReturn.SetColor(ChatColorPresets.Red);
                                    }
                                }
                            }
                            else
                            {
                                commandReturn.SetMessage(LocalizationService.GetString(language, "error:not_enough_arguments", channelId, data.Platform, $"{bb.Program.BotInstance.DefaultCommandPrefix}bot ban (channel) (reason)"));
                                commandReturn.SetColor(ChatColorPresets.Red);
                            }
                        }
                        else if (pardonAlias.Contains(argumentOne, StringComparer.OrdinalIgnoreCase))
                        {
                            if (arguments.Count > 1)
                            {
                                var arg2 = arguments.ElementAt(1).ToLower().Replace("@", "").Replace(",", "");
                                var BanChannelID = UsernameResolver.GetUserID(arg2, data.Platform);
                                if (BanChannelID.Equals(null))
                                {
                                    commandReturn.SetMessage(LocalizationService.GetString(language, "error:user_not_found", channelId, data.Platform, arg2));
                                    commandReturn.SetColor(ChatColorPresets.Red);
                                    commandReturn.SetSafe(false);
                                }
                                else
                                {
                                    if (isDeveloper || isModerator && (!bb.Program.BotInstance.DataBase.Roles.IsModerator(data.Platform, DataConversion.ToLong(data.User.Id)) && !bb.Program.BotInstance.DataBase.Roles.IsDeveloper(data.Platform, DataConversion.ToLong(data.User.Id))))
                                    {
                                        bb.Program.BotInstance.MessageSender.Send(PlatformsEnum.Twitch, $"Pag #{arg2} unbanned in bot", bb.Program.BotInstance.TwitchName, isSafe: true);
                                        bb.Program.BotInstance.DataBase.Roles.RemoveBannedUser(bb.Program.BotInstance.DataBase.Roles.GetBannedUser(data.Platform, DataConversion.ToLong(BanChannelID)).ID);
                                        commandReturn.SetMessage(LocalizationService.GetString(language, "command:bot:user_unban", channelId, data.Platform, arg2));
                                    }
                                    else
                                    {
                                        commandReturn.SetMessage(LocalizationService.GetString(language, "error:not_enough_rights", channelId, data.Platform));
                                        commandReturn.SetColor(ChatColorPresets.Red);
                                    }
                                }
                            }
                            else
                                commandReturn.SetMessage(LocalizationService.GetString(language, "error:not_enough_arguments", channelId, data.Platform, $"{bb.Program.BotInstance.DefaultCommandPrefix}bot pardon (channel)"));
                        }
                        else if (rejoinAlias.Contains(argumentOne, StringComparer.OrdinalIgnoreCase) && data.Platform == PlatformsEnum.Twitch)
                        {
                            if (arguments.Count > 1)
                            {
                                string user = arguments[1];
                                if (bb.Program.BotInstance.Clients.Twitch.JoinedChannels.Contains(new JoinedChannel(user)))
                                    bb.Program.BotInstance.Clients.Twitch.LeaveChannel(user);
                                bb.Program.BotInstance.Clients.Twitch.JoinChannel(user);
                                bb.Program.BotInstance.MessageSender.Send(PlatformsEnum.Twitch, $"ppSpin Reconnected to #{user}", bb.Program.BotInstance.TwitchName, isSafe: true);
                                commandReturn.SetMessage(LocalizationService.GetString(language, "command:bot:rejoin", channelId, data.Platform));
                            }
                            else
                            {
                                commandReturn.SetMessage(LocalizationService.GetString(language, "error:not_enough_arguments", channelId, data.Platform, $"{bb.Program.BotInstance.DefaultCommandPrefix}bot rejoin (channel)"));
                                commandReturn.SetColor(ChatColorPresets.Red);
                                commandReturn.SetSafe(false);
                            }
                        }
                        else if (addChannelAlias.Contains(argumentOne, StringComparer.OrdinalIgnoreCase) && data.Platform == PlatformsEnum.Twitch)
                        {
                            if (arguments.Count > 1)
                            {
                                string newid = UsernameResolver.GetUserID(arguments[1], data.Platform);
                                if (newid is not null) // Fix AA2
                                {
                                    List<string> channels = bb.Program.BotInstance.Settings.Get<List<string>>("twitch_connect_channels"); // Fix AA2
                                    channels.Add(newid);
                                    string[] output = [.. channels];

                                    bb.Program.BotInstance.Settings.Set("twitch_connect_channels", output);
                                    bb.Program.BotInstance.Clients.Twitch.JoinChannel(arguments[1]);
                                    bb.Program.BotInstance.MessageSender.Send(PlatformsEnum.Twitch, $"Pag Added to #{arguments[1]}", bb.Program.BotInstance.TwitchName, isSafe: true);
                                    bb.Program.BotInstance.MessageSender.Send(PlatformsEnum.Twitch, LocalizationService.GetString(language, "command:bot:channel:add", channelId, data.Platform, arguments[1]),
                                        channel, channelId, language, messageId: messageId, isSafe: true);
                                    bb.Program.BotInstance.MessageSender.Send(PlatformsEnum.Twitch, "text:added", arguments[1], isSafe: true);
                                }
                                else
                                    bb.Program.BotInstance.MessageSender.Send(PlatformsEnum.Twitch, LocalizationService.GetString(language, "error:user_not_found", channelId, data.Platform, arguments[1]),
                                        channel, channelId, language, messageId: messageId, isSafe: true);
                            }
                            else
                            {
                                commandReturn.SetMessage(LocalizationService.GetString(
                                    language,
                                    "error:not_enough_arguments",
                                    channelId,
                                    data.Platform,
                                    $"{bb.Program.BotInstance.DefaultCommandPrefix}bot addchannel (channel)"));
                                commandReturn.SetColor(ChatColorPresets.Red);
                                commandReturn.SetSafe(false);
                            }
                        }
                        else if (deleteChannelAlias.Contains(argumentOne, StringComparer.OrdinalIgnoreCase) && data.Platform == PlatformsEnum.Twitch)
                        {
                            if (arguments.Count > 1)
                            {
                                var userID = UsernameResolver.GetUserID(arguments[1], data.Platform);
                                if (!userID.Equals(null))
                                {
                                    List<string> channels = bb.Program.BotInstance.Settings.Get<List<string>>("twitch_connect_channels");
                                    channels.Remove(userID);
                                    string[] output = [.. channels];

                                    bb.Program.BotInstance.Settings.Set("twitch_connect_channels", output);
                                    bb.Program.BotInstance.Clients.Twitch.LeaveChannel(arguments[1]);
                                    bb.Program.BotInstance.MessageSender.Send(PlatformsEnum.Twitch, $"What Deleted from #{arguments[1]}", bb.Program.BotInstance.TwitchName, isSafe: true);
                                    bb.Program.BotInstance.MessageSender.Send(PlatformsEnum.Twitch, LocalizationService.GetString(language, "command:bot:channel:delete", channelId, data.Platform, arguments[1]),
                                        channel, channelId, language, messageId: messageId, isSafe: true);
                                }
                                else
                                    bb.Program.BotInstance.MessageSender.Send(PlatformsEnum.Twitch, LocalizationService.GetString(language, "error:user_not_found", channelId, data.Platform, arguments[1]), channel, channelId, language, messageId: messageId, isSafe: true);
                            }
                            else
                            {
                                commandReturn.SetMessage(LocalizationService.GetString(
                                    language,
                                    "error:not_enough_arguments",
                                    channelId,
                                    data.Platform,
                                    $"{bb.Program.BotInstance.DefaultCommandPrefix}bot delchannel (channel)"));
                                commandReturn.SetColor(ChatColorPresets.Red);
                                commandReturn.SetSafe(false);
                            }
                        }
                        else if (joinChannelAlias.Contains(argumentOne, StringComparer.OrdinalIgnoreCase) && data.Platform == PlatformsEnum.Twitch)
                        {
                            if (arguments.Count > 1)
                            {
                                bb.Program.BotInstance.MessageSender.Send(PlatformsEnum.Twitch, $"Pag Manually joined to #{arguments[1]}", bb.Program.BotInstance.TwitchName, isSafe: true);
                                bb.Program.BotInstance.Clients.Twitch.JoinChannel(arguments[1]);
                                commandReturn.SetMessage(LocalizationService.GetString(language, "command:bot:connect", channelId, data.Platform)); // Fix #AB8
                            }
                            else
                            {
                                commandReturn.SetMessage(LocalizationService.GetString(language, "error:not_enough_arguments", channelId, data.Platform, $"{bb.Program.BotInstance.DefaultCommandPrefix}bot joinchannel (channel)"));
                                commandReturn.SetColor(ChatColorPresets.Red);
                                commandReturn.SetSafe(false);
                            }
                        }
                        else if (leaveChannelAlias.Contains(argumentOne, StringComparer.OrdinalIgnoreCase) && data.Platform == PlatformsEnum.Twitch)
                        {
                            if (arguments.Count > 1)
                            {
                                bb.Program.BotInstance.MessageSender.Send(PlatformsEnum.Twitch, $"What Leaved from #{arguments[1]}", bb.Program.BotInstance.TwitchName, isSafe: true);
                                bb.Program.BotInstance.Clients.Twitch.LeaveChannel(arguments[1]);
                                commandReturn.SetMessage(LocalizationService.GetString(language, "command:bot:leave", channelId, data.Platform)); // Fix #AB8
                            }
                            else
                            {
                                commandReturn.SetMessage(LocalizationService.GetString(language, "error:not_enough_arguments", channelId, data.Platform, $"{bb.Program.BotInstance.DefaultCommandPrefix}bot leavechannel (channel)"));
                                commandReturn.SetColor(ChatColorPresets.Red);
                                commandReturn.SetSafe(false);
                            }
                        }
                        else if (isDeveloper)
                        {
                            if (moderatorAddAlias.Contains(argumentOne, StringComparer.OrdinalIgnoreCase))
                            {
                                if (arguments.Count > 1)
                                {
                                    var userID = UsernameResolver.GetUserID(arguments[1], data.Platform);
                                    if (userID != null)
                                    {
                                        bb.Program.BotInstance.MessageSender.Send(PlatformsEnum.Twitch, $"Pag New moderator @{arguments[1]}", bb.Program.BotInstance.TwitchName, isSafe: true);
                                        bb.Program.BotInstance.DataBase.Roles.AddModerator(data.Platform, DataConversion.ToLong(userID), DateTime.UtcNow, data.User.Id);
                                        commandReturn.SetMessage(LocalizationService.GetString(language, "command:bot:moderator:add", channelId, data.Platform, arguments[1]));
                                    }
                                    else
                                    {
                                        commandReturn.SetMessage(LocalizationService.GetString(language, "error:user_not_found", channelId, data.Platform, arguments[1]));
                                        commandReturn.SetColor(ChatColorPresets.Red);
                                    }
                                }
                                else
                                {
                                    commandReturn.SetMessage(LocalizationService.GetString(language, "error:not_enough_arguments", channelId, data.Platform, $"{bb.Program.BotInstance.DefaultCommandPrefix}bot addchannel (channel)"));
                                    commandReturn.SetColor(ChatColorPresets.Red);
                                    commandReturn.SetSafe(false);
                                }
                            }
                            else if (moderatorDeleteAlias.Contains(argumentOne, StringComparer.OrdinalIgnoreCase))
                            {
                                if (arguments.Count > 1)
                                {
                                    var userID = UsernameResolver.GetUserID(arguments[1], data.Platform);
                                    if (userID != null)
                                    {
                                        bb.Program.BotInstance.MessageSender.Send(PlatformsEnum.Twitch, $"What @{arguments[1]} is no longer a moderator", bb.Program.BotInstance.TwitchName, isSafe: true);
                                        bb.Program.BotInstance.DataBase.Roles.RemoveModerator(bb.Program.BotInstance.DataBase.Roles.GetModerator(data.Platform, DataConversion.ToLong(userID)).ID);
                                        commandReturn.SetMessage(LocalizationService.GetString(language, "command:bot:moderator:delete", channelId, data.Platform, arguments[1]));
                                    }
                                    else
                                    {
                                        commandReturn.SetMessage(LocalizationService.GetString(language, "error:user_not_found", channelId, data.Platform, arguments[1]));
                                        commandReturn.SetColor(ChatColorPresets.Red);
                                    }
                                }
                                else
                                {
                                    commandReturn.SetMessage(LocalizationService.GetString(language, "error:not_enough_arguments", channelId, data.Platform, $"{bb.Program.BotInstance.DefaultCommandPrefix}bot addchannel (channel)"));
                                    commandReturn.SetColor(ChatColorPresets.Red);
                                    commandReturn.SetSafe(false);
                                }
                            }
                            else if (updateTranslationAlias.Contains(argumentOne, StringComparer.OrdinalIgnoreCase))
                            {
                                LocalizationService.UpdateTranslation("ru-RU", channelId, data.Platform);
                                LocalizationService.UpdateTranslation("en-US", channelId, data.Platform);
                                commandReturn.SetMessage("MrDestructoid 👍 DO-NE!");
                            }
                            else
                            {
                                commandReturn.SetMessage(LocalizationService.GetPluralString(data.User.Language, "error:incorrect_parameters", data.ChannelId, data.Platform, arguments.Count)); // Fix AA5
                                commandReturn.SetColor(ChatColorPresets.Red);
                            }
                        }
                        else
                        {
                            commandReturn.SetMessage(LocalizationService.GetPluralString(data.User.Language, "error:incorrect_parameters", data.ChannelId, data.Platform, arguments.Count)); // Fix AA5
                            commandReturn.SetColor(ChatColorPresets.Red);
                        }
                    }
                    else
                    {
                        commandReturn.SetMessage(LocalizationService.GetPluralString(data.User.Language, "error:incorrect_parameters", data.ChannelId, data.Platform, arguments.Count)); // Fix AA5
                        commandReturn.SetColor(ChatColorPresets.Red);
                    }
                }
                else
                {
                    commandReturn.SetMessage(LocalizationService.GetString(language, "text:bot_info", channelId, data.Platform));
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