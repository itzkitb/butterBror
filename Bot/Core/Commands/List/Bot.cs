using bb.Core.Bot;
using bb.Core.Bot.SQLColumnNames;
using bb.Models;
using bb.Utils;
using DankDB;
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
        public override int CooldownPerChannel => 5;
        public override string[] Aliases => ["bot", "bt", "бот", "бт", "main", "start", "старт", "главная", "info", "инфо", "information", "информация"];
        public override string HelpArguments => "(lang (set [en/ru]), verify, currency (adddollars [int]), ban [username] (reason), pardon [username], rejoinchannel [channel], addchannel [channel], delchannel [channel], joinchannel [channel], leavechannel [channel], modadd [username], demod [username])";
        public override DateTime CreationDate => DateTime.Parse("07/04/2024");
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
                List<string> arguments = (data.Arguments is null ? [] : data.Arguments);

                bool isModerator = (data.User.IsBotModerator == null ? false : (bool)data.User.IsBotModerator);
                bool isChannelModerator = (data.User.IsModerator == null ? false : (bool)data.User.IsModerator);
                bool isDeveloper = (data.User.IsBotDeveloper == null ? false : (bool)data.User.IsBotDeveloper);
                string userId = data.User.ID;
                string? channelId = data.ChannelId;
                string? channel = data.Channel;
                string? messageId = data.MessageID;
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
                                        bb.Bot.UsersBuffer.SetParameter(data.Platform, DataConversion.ToLong(data.User.ID), Users.Language, result);
                                        commandReturn.SetMessage(LocalizationService.GetString(result, "command:bot:language:set", channelId, data.Platform));
                                    }
                                }
                                else
                                {
                                    commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:not_enough_arguments", channelId, data.Platform, $"{bb.Bot.DefaultCommandPrefix}bot lang set en/ru"));
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
                            commandReturn.SetMessage(LocalizationService.GetString(language, "error:not_enough_arguments", channelId, data.Platform, $"{bb.Bot.DefaultCommandPrefix}bot lang (set en/ru)/get"));
                            commandReturn.SetColor(ChatColorPresets.Red);
                        }
                    }
                    else if (inviteAlias.Contains(argumentOne, StringComparer.OrdinalIgnoreCase))
                    {
                        Write($"Request to add a bot from @{data.User.Name}", "info");
                        Dictionary<string, dynamic> userData = new()
                            {
                                { "language", data.User.Language },
                                { "username", data.User.Name },
                                { "user_indentificator", data.User.ID },
                                { "date", DateTime.UtcNow }
                            };
                        Directory.CreateDirectory(bb.Bot.Paths.General + "INVITE/");
                        Manager.Save(bb.Bot.Paths.General + $"INVITE/{data.User.Name}.txt", $"rq{DateTime.UtcNow}", userData);
                        commandReturn.SetMessage(LocalizationService.GetString(language, "command:bot:user_verify", channelId, data.Platform));
                    }
                    else if (prefixAlias.Contains(argumentOne, StringComparer.OrdinalIgnoreCase))
                    {
                        if (arguments.Count == 1)
                        {
                            string currentPrefix = bb.Bot.DataBase.Channels.GetCommandPrefix(data.Platform, data.ChannelId);
                            commandReturn.SetMessage(LocalizationService.GetString(language, "command:bot:prefix:current", channelId, data.Platform, currentPrefix));
                        }
                        else if (arguments.Count > 1 && setAlias.Contains(arguments[1].ToLower()))
                        {
                            if (isDeveloper || isModerator || isChannelModerator)
                            {
                                if (arguments.Count > 2)
                                {
                                    string newPrefix = arguments[2];
                                    bb.Bot.DataBase.Channels.SetCommandPrefix(data.Platform, data.ChannelId, newPrefix);
                                    commandReturn.SetMessage(LocalizationService.GetString(language, "command:bot:prefix:set", channelId, data.Platform, newPrefix));
                                }
                                else
                                {
                                    commandReturn.SetMessage(LocalizationService.GetString(language, "error:not_enough_arguments", channelId, data.Platform, $"{bb.Bot.DefaultCommandPrefix}bot prefix set [prefix]"));
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
                                string prefix = bb.Bot.DataBase.Channels.GetCommandPrefix(data.Platform, channelIdForOtherChannel);
                                commandReturn.SetMessage(LocalizationService.GetString(language, "command:bot:prefix:other", channelId, data.Platform, channelName, prefix));
                            }
                        }
                        else
                        {
                            commandReturn.SetMessage(LocalizationService.GetString(language, "error:not_enough_arguments", channelId, data.Platform, $"{bb.Bot.DefaultCommandPrefix}bot prefix [set | channel]"));
                            commandReturn.SetColor(ChatColorPresets.Red);
                        }
                    }
                    else if (currencyAlias.Contains(argumentOne, StringComparer.OrdinalIgnoreCase))
                    {
                        if (arguments.Count > 2 && addCurrencyAlias.Contains(arguments.ElementAt(1).ToLower()) && isDeveloper)
                        {
                            int converted = Utils.DataConversion.ToInt(arguments.ElementAt(2).ToLower());
                            bb.Bot.InBankDollars += converted;

                            commandReturn.SetMessage(LocalizationService.GetString(language, "command:bot:currency:add", channelId, data.Platform, converted.ToString(), bb.Bot.InBankDollars.ToString()));
                        }
                        else
                        {
                            // 0_0

                            var date = DateTime.UtcNow.AddDays(-1);
                            var currencyData = Manager.Get<Dictionary<string, dynamic>>(bb.Bot.Paths.Currency, $"{date.Day}.{date.Month}.{date.Year}");

                            decimal oldCurrencyAmount = currencyData.ContainsKey("amount") ? currencyData["amount"].GetDecimal() : 0;
                            float oldCurrencyCost = currencyData.ContainsKey("cost") ? currencyData["cost"].GetSingle() : 0;
                            int oldCurrencyUsers = currencyData.ContainsKey("users") ? currencyData["users"].GetInt32() : 0;
                            int oldDollarsInBank = currencyData.ContainsKey("dollars") ? currencyData["dollars"].GetInt32() : 0;

                            decimal oldMiddle = oldCurrencyUsers != 0 ? decimal.Parse((oldCurrencyAmount / oldCurrencyUsers).ToString("0.00")) : 0;

                            float currencyCost = float.Parse((bb.Bot.InBankDollars / bb.Bot.Coins).ToString("0.00"));
                            decimal middleUsersBalance = decimal.Parse((bb.Bot.Coins / bb.Bot.Users).ToString("0.00"));

                            float plusOrMinusCost = currencyCost - oldCurrencyCost;
                            decimal plusOrMinusAmount = bb.Bot.Coins - oldCurrencyAmount;
                            int plusOrMinusUsers = bb.Bot.Users - oldCurrencyUsers;
                            int plusOrMinusDollars = bb.Bot.InBankDollars - oldDollarsInBank;
                            decimal plusOrMinusMiddle = middleUsersBalance - oldMiddle;

                            string buttersCostProgress = oldCurrencyCost > currencyCost ? $"🔽 ({plusOrMinusCost:0.00})" : oldCurrencyCost == currencyCost ? "⏺️ (0)" : $"🔼 (+{plusOrMinusCost:0.00})";
                            string buttersAmountProgress = oldCurrencyAmount > bb.Bot.Coins ? $"🔽 ({plusOrMinusAmount:0.00})" : oldCurrencyAmount == bb.Bot.Coins ? "⏺️ (0)" : $"🔼 (+{plusOrMinusAmount:0.00})";
                            string buttersDollars = oldDollarsInBank > bb.Bot.InBankDollars ? $"🔽 ({plusOrMinusDollars})" : oldDollarsInBank == bb.Bot.InBankDollars ? "⏺️ (0)" : $"🔼 (+{plusOrMinusDollars})";
                            string buttersMiddle = oldMiddle > middleUsersBalance ? $"🔽 ({plusOrMinusMiddle:0.00})" : oldMiddle == middleUsersBalance ? "⏺️ (0)" : $"🔼 (+{plusOrMinusMiddle:0.00})";

                            commandReturn.SetMessage(LocalizationService.GetString(
                                language,
                                "command:bot:currency",
                                channelId,
                                data.Platform,
                                bb.Bot.Coins.ToString() + " " + buttersAmountProgress,
                                bb.Bot.Users.ToString(),
                                (bb.Bot.Coins / bb.Bot.Users).ToString("0.00") + " " + buttersMiddle,
                                (bb.Bot.InBankDollars / bb.Bot.Coins).ToString("0.00") + " " + buttersCostProgress,
                                bb.Bot.InBankDollars + " " + buttersDollars));
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
                                    if (isDeveloper || (!bb.Bot.DataBase.Roles.IsModerator(data.Platform, DataConversion.ToLong(data.User.ID)) && !bb.Bot.DataBase.Roles.IsDeveloper(data.Platform, DataConversion.ToLong(data.User.ID))))
                                    {
                                        PlatformMessageSender.TwitchSend(bb.Bot.Name, $"widelol #{arg2} banned in bot: {reason}", "", "", "en-US", true);
                                        bb.Bot.DataBase.Roles.AddBannedUser(data.Platform, DataConversion.ToLong(bcid), DateTime.UtcNow, data.User.ID, reason);
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
                                commandReturn.SetMessage(LocalizationService.GetString(language, "error:not_enough_arguments", channelId, data.Platform, $"{bb.Bot.DefaultCommandPrefix}bot ban (channel) (reason)"));
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
                                    if (isDeveloper || isModerator && (!bb.Bot.DataBase.Roles.IsModerator(data.Platform, DataConversion.ToLong(data.User.ID)) && !bb.Bot.DataBase.Roles.IsDeveloper(data.Platform, DataConversion.ToLong(data.User.ID))))
                                    {
                                        PlatformMessageSender.TwitchSend(bb.Bot.Name, $"Pag #{arg2} unbanned in bot", "", "", "en-US", true);
                                        bb.Bot.DataBase.Roles.RemoveBannedUser(bb.Bot.DataBase.Roles.GetBannedUser(data.Platform, DataConversion.ToLong(BanChannelID)).ID);
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
                                commandReturn.SetMessage(LocalizationService.GetString(language, "error:not_enough_arguments", channelId, data.Platform, $"{bb.Bot.DefaultCommandPrefix}bot pardon (channel)"));
                        }
                        else if (rejoinAlias.Contains(argumentOne, StringComparer.OrdinalIgnoreCase) && data.Platform == PlatformsEnum.Twitch)
                        {
                            if (arguments.Count > 1)
                            {
                                string user = arguments[1];
                                if (bb.Bot.Clients.Twitch.JoinedChannels.Contains(new JoinedChannel(user)))
                                    bb.Bot.Clients.Twitch.LeaveChannel(user);
                                bb.Bot.Clients.Twitch.JoinChannel(user);
                                PlatformMessageSender.TwitchSend(bb.Bot.Name, $"ppSpin Reconnected to #{user}", "", "", "en-US", true);
                                commandReturn.SetMessage(LocalizationService.GetString(language, "command:bot:rejoin", channelId, data.Platform));
                            }
                            else
                            {
                                commandReturn.SetMessage(LocalizationService.GetString(language, "error:not_enough_arguments", channelId, data.Platform, $"{bb.Bot.DefaultCommandPrefix}bot rejoin (channel)"));
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
                                    List<string> channels = Manager.Get<List<string>>(bb.Bot.Paths.Settings, "twitch_connect_channels"); // Fix AA2
                                    channels.Add(newid);
                                    string[] output = [.. channels];

                                    Manager.Save(bb.Bot.Paths.Settings, "twitch_connect_channels", output); // Fix AA2
                                    bb.Bot.Clients.Twitch.JoinChannel(arguments[1]);
                                    PlatformMessageSender.TwitchSend(bb.Bot.Name, $"Pag Added to #{arguments[1]}", "", "", "en-US", true);
                                    PlatformMessageSender.TwitchReply(channel, channelId, LocalizationService.GetString(language, "command:bot:channel:add", channelId, data.Platform, arguments[1]), messageId, language, true);
                                    PlatformMessageSender.TwitchSend(arguments[1], LocalizationService.GetString(language, "text:added", channelId, data.Platform, bb.Bot.Version), channelId, messageId, language, true);
                                }
                                else
                                    PlatformMessageSender.TwitchReply(channel, channelId, LocalizationService.GetString(language, "error:user_not_found", channelId, data.Platform, arguments[1]), messageId, language, true);
                            }
                            else
                            {
                                commandReturn.SetMessage(LocalizationService.GetString(
                                    language,
                                    "error:not_enough_arguments",
                                    channelId,
                                    data.Platform,
                                    $"{bb.Bot.DefaultCommandPrefix}bot addchannel (channel)"));
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
                                    List<string> channels = Manager.Get<List<string>>(bb.Bot.Paths.Settings, "channels");
                                    channels.Remove(userID);
                                    string[] output = [.. channels];

                                    Manager.Save(bb.Bot.Paths.Settings, "channels", output);
                                    bb.Bot.Clients.Twitch.LeaveChannel(arguments[1]);
                                    PlatformMessageSender.TwitchSend(bb.Bot.Name, $"What Deleted from #{arguments[1]}", "", "", "en-US", true);
                                    PlatformMessageSender.TwitchReply(channel, channelId, LocalizationService.GetString(language, "command:bot:channel:delete", channelId, data.Platform, arguments[1]), messageId, data.User.Language, true);
                                }
                                else
                                    PlatformMessageSender.TwitchReply(channel, channelId, LocalizationService.GetString(language, "error:user_not_found", channelId, data.Platform, arguments[1]), messageId, data.User.Language, true);
                            }
                            else
                            {
                                commandReturn.SetMessage(LocalizationService.GetString(
                                    language,
                                    "error:not_enough_arguments",
                                    channelId,
                                    data.Platform,
                                    $"{bb.Bot.DefaultCommandPrefix}bot delchannel (channel)"));
                                commandReturn.SetColor(ChatColorPresets.Red);
                                commandReturn.SetSafe(false);
                            }
                        }
                        else if (joinChannelAlias.Contains(argumentOne, StringComparer.OrdinalIgnoreCase) && data.Platform == PlatformsEnum.Twitch)
                        {
                            if (arguments.Count > 1)
                            {
                                PlatformMessageSender.TwitchSend(bb.Bot.Name, $"Pag Manually joined to #{arguments[1]}", "", "", "en-US", true);
                                bb.Bot.Clients.Twitch.JoinChannel(arguments[1]);
                                commandReturn.SetMessage(LocalizationService.GetString(language, "command:bot:connect", channelId, data.Platform)); // Fix #AB8
                            }
                            else
                            {
                                commandReturn.SetMessage(LocalizationService.GetString(language, "error:not_enough_arguments", channelId, data.Platform, $"{bb.Bot.DefaultCommandPrefix}bot joinchannel (channel)"));
                                commandReturn.SetColor(ChatColorPresets.Red);
                                commandReturn.SetSafe(false);
                            }
                        }
                        else if (leaveChannelAlias.Contains(argumentOne, StringComparer.OrdinalIgnoreCase) && data.Platform == PlatformsEnum.Twitch)
                        {
                            if (arguments.Count > 1)
                            {
                                PlatformMessageSender.TwitchSend(bb.Bot.Name, $"What Leaved from #{arguments[1]}", "", "", "en-US", true);
                                bb.Bot.Clients.Twitch.LeaveChannel(arguments[1]);
                                commandReturn.SetMessage(LocalizationService.GetString(language, "command:bot:leave", channelId, data.Platform)); // Fix #AB8
                            }
                            else
                            {
                                commandReturn.SetMessage(LocalizationService.GetString(language, "error:not_enough_arguments", channelId, data.Platform, $"{bb.Bot.DefaultCommandPrefix}bot leavechannel (channel)"));
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
                                        PlatformMessageSender.TwitchSend(bb.Bot.Name, $"Pag New moderator @{arguments[1]}", "", "", "en-US", true);
                                        bb.Bot.DataBase.Roles.AddModerator(data.Platform, DataConversion.ToLong(userID), DateTime.UtcNow, data.User.ID);
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
                                    commandReturn.SetMessage(LocalizationService.GetString(language, "error:not_enough_arguments", channelId, data.Platform, $"{bb.Bot.DefaultCommandPrefix}bot addchannel (channel)"));
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
                                        PlatformMessageSender.TwitchSend(bb.Bot.Name, $"What @{arguments[1]} is no longer a moderator", "", "", "en-US", true);
                                        bb.Bot.DataBase.Roles.RemoveModerator(bb.Bot.DataBase.Roles.GetModerator(data.Platform, DataConversion.ToLong(userID)).ID);
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
                                    commandReturn.SetMessage(LocalizationService.GetString(language, "error:not_enough_arguments", channelId, data.Platform, $"{bb.Bot.DefaultCommandPrefix}bot addchannel (channel)"));
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