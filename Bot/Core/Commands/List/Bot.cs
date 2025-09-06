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

                bool is_moderator = (bool)data.User.IsBotModerator;
                string user_id = data.User.ID;
                string? channel_id = data.ChannelId;
                string? channel = data.Channel;
                string? message_id = data.MessageID;
                bool is_developer = (data.User.IsBotDeveloper == null ? false : (bool)data.User.IsBotDeveloper);
                string language = data.User.Language;

                string[] language_alias = ["lang", "l", "language", "язык", "я"];
                string[] set_alias = ["set", "s", "сет", "установить", "у"];

                string[] language_english_alias = ["en-us", "en", "e", "англ", "английский"];
                string[] language_russian_alias = ["ru-ru", "ru", "r", "рус", "русский"];

                string[] add_currency_alias = ["adddollars", "addd", "ad", "добавитьдоллары", "дд"];
                string[] currency_alias = ["currency", "c", "курс"];
                string[] invite_alias = ["invite", "пригласить", "i", "п"];
                string[] update_translation_alias = ["updatetranslation", "uptr", "ut", "обновитьперевод", "оп"];

                string[] ban_alias = ["ban", "бан", "block", "kill", "заблокировать", "чел"];
                string[] pardon_alias = ["pardon", "unblock", "unban", "разблокировать", "разбанить", "анбан"];
                string[] rejoin_alias = ["rejoin", "rej", "переподключить", "reenter", "ree"];
                string[] add_channel_alias = ["addchannel", "newchannel", "addc", "newc", "дканал", "нканал"];
                string[] delete_channel_alias = ["delchannel", "deletechannel", "удалитьканал", "уканал"];
                string[] join_channel_alias = ["joinchannel", "joinc", "вканал"];
                string[] leave_channel_alias = ["leavechannel", "leavec", "пканал"];

                string[] moderator_add_alias = ["modadd", "дм", "добавитьмодератора"];
                string[] moderator_delete_alias = ["demod", "ум", "удалитьмодератора"];

                if (arguments.Count > 0)
                {
                    var argument_one = arguments[0].ToLower();
                    if (language_alias.Contains(argument_one))
                    {
                        if (arguments.Count > 1)
                        {
                            string argument2 = arguments.ElementAt(1).ToLower();
                            if (set_alias.Contains(argument2))
                            {
                                if (arguments.Count > 2)
                                {
                                    string arg3 = arguments.ElementAt(2).ToLower();
                                    string result = string.Empty;
                                    if (language_english_alias.Contains(arg3))
                                        result = "en-US";
                                    else if (language_russian_alias.Contains(arg3))
                                        result = "ru-RU";

                                    if (result.Equals(string.Empty))
                                    {
                                        commandReturn.SetMessage(LocalizationService.GetPluralString(language, "error:incorrect_parameters", channel_id, data.Platform, arguments.Count));
                                        commandReturn.SetColor(ChatColorPresets.Red);
                                    }
                                    else
                                    {
                                        bb.Bot.UsersBuffer.SetParameter(data.Platform, DataConversion.ToLong(data.User.ID), Users.Language, result);
                                        commandReturn.SetMessage(LocalizationService.GetString(result, "command:bot:language:set", channel_id, data.Platform));
                                    }
                                }
                                else
                                {
                                    commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:not_enough_arguments", channel_id, data.Platform, $"{bb.Bot.DefaultExecutor}bot lang set en/ru"));
                                    commandReturn.SetColor(ChatColorPresets.Red);
                                }
                            }
                            else if (argument2.Contains("get"))
                                commandReturn.SetMessage(LocalizationService.GetString(language, "info:language", channel_id, data.Platform));
                            else
                            {
                                commandReturn.SetMessage(LocalizationService.GetPluralString(language, "error:incorrect_parameters", channel_id, data.Platform, arguments.Count));
                                commandReturn.SetColor(ChatColorPresets.Red);
                            }
                        }
                        else
                        {
                            commandReturn.SetMessage(LocalizationService.GetString(language, "error:not_enough_arguments", channel_id, data.Platform, $"{bb.Bot.DefaultExecutor}bot lang (set en/ru)/get"));
                            commandReturn.SetColor(ChatColorPresets.Red);
                        }
                    }
                    else if (invite_alias.Contains(argument_one))
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
                        commandReturn.SetMessage(LocalizationService.GetString(language, "command:bot:user_verify", channel_id, data.Platform));
                    }
                    else if (currency_alias.Contains(argument_one))
                    {
                        if (arguments.Count > 2 && add_currency_alias.Contains(arguments.ElementAt(1).ToLower()) && is_developer)
                        {
                            int converted = Utils.DataConversion.ToInt(arguments.ElementAt(2).ToLower());
                            bb.Bot.InBankDollars += converted;

                            commandReturn.SetMessage(LocalizationService.GetString(language, "command:bot:currency:add", channel_id, data.Platform, converted.ToString(), bb.Bot.InBankDollars.ToString()));
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
                                channel_id,
                                data.Platform,
                                bb.Bot.Coins.ToString() + " " + buttersAmountProgress,
                                bb.Bot.Users.ToString(),
                                (bb.Bot.Coins / bb.Bot.Users).ToString("0.00") + " " + buttersMiddle,
                                (bb.Bot.InBankDollars / bb.Bot.Coins).ToString("0.00") + " " + buttersCostProgress,
                                bb.Bot.InBankDollars + " " + buttersDollars));
                            commandReturn.SetSafe(false);
                        }
                    }
                    else if (is_moderator || is_developer)
                    {
                        if (ban_alias.Contains(argument_one))
                        {
                            if (arguments.Count > 1)
                            {
                                string arg2 = arguments[1].ToLower().Replace("@", "").Replace(",", "");
                                string bcid = UsernameResolver.GetUserID(arg2, data.Platform);
                                string reason = data.ArgumentsString.Replace(arguments.ElementAt(0), "").Replace(arguments.ElementAt(1), "").Replace("  ", "");
                                if (bcid.Equals(null))
                                {
                                    commandReturn.SetMessage(LocalizationService.GetString(language, "error:user_not_found", channel_id, data.Platform, arg2));
                                    commandReturn.SetColor(ChatColorPresets.Red);
                                    commandReturn.SetSafe(false);
                                }
                                else
                                {
                                    if (is_developer || (!bb.Bot.DataBase.Roles.IsModerator(data.Platform, DataConversion.ToLong(data.User.ID)) && !bb.Bot.DataBase.Roles.IsDeveloper(data.Platform, DataConversion.ToLong(data.User.ID))))
                                    {
                                        PlatformMessageSender.TwitchSend(bb.Bot.BotName, $"widelol #{arg2} banned in bot: {reason}", "", "", "en-US", true);
                                        bb.Bot.DataBase.Roles.AddBannedUser(data.Platform, DataConversion.ToLong(bcid), DateTime.UtcNow, data.User.ID, reason);
                                        commandReturn.SetMessage(LocalizationService.GetString(language, "command:bot:user_ban", channel_id, data.Platform, arg2, reason));
                                    }
                                    else
                                    {
                                        commandReturn.SetMessage(LocalizationService.GetString(language, "error:not_enough_rights", channel_id, data.Platform));
                                        commandReturn.SetColor(ChatColorPresets.Red);
                                    }
                                }
                            }
                            else
                            {
                                commandReturn.SetMessage(LocalizationService.GetString(language, "error:not_enough_arguments", channel_id, data.Platform, $"{bb.Bot.DefaultExecutor}bot ban (channel) (reason)"));
                                commandReturn.SetColor(ChatColorPresets.Red);
                            }
                        }
                        else if (pardon_alias.Equals("pardon"))
                        {
                            if (arguments.Count > 1)
                            {
                                var arg2 = arguments.ElementAt(1).ToLower().Replace("@", "").Replace(",", "");
                                var BanChannelID = UsernameResolver.GetUserID(arg2, data.Platform);
                                if (BanChannelID.Equals(null))
                                {
                                    commandReturn.SetMessage(LocalizationService.GetString(language, "error:user_not_found", channel_id, data.Platform, arg2));
                                    commandReturn.SetColor(ChatColorPresets.Red);
                                    commandReturn.SetSafe(false);
                                }
                                else
                                {
                                    if (is_developer || is_moderator && (!bb.Bot.DataBase.Roles.IsModerator(data.Platform, DataConversion.ToLong(data.User.ID)) && !bb.Bot.DataBase.Roles.IsDeveloper(data.Platform, DataConversion.ToLong(data.User.ID))))
                                    {
                                        PlatformMessageSender.TwitchSend(bb.Bot.BotName, $"Pag #{arg2} unbanned in bot", "", "", "en-US", true);
                                        bb.Bot.DataBase.Roles.RemoveBannedUser(bb.Bot.DataBase.Roles.GetBannedUser(data.Platform, DataConversion.ToLong(BanChannelID)).ID);
                                        commandReturn.SetMessage(LocalizationService.GetString(language, "command:bot:user_unban", channel_id, data.Platform, arg2));
                                    }
                                    else
                                    {
                                        commandReturn.SetMessage(LocalizationService.GetString(language, "error:not_enough_rights", channel_id, data.Platform));
                                        commandReturn.SetColor(ChatColorPresets.Red);
                                    }
                                }
                            }
                            else
                                commandReturn.SetMessage(LocalizationService.GetString(language, "error:not_enough_arguments", channel_id, data.Platform, $"{bb.Bot.DefaultExecutor}bot pardon (channel)"));
                        }
                        else if (rejoin_alias.Contains(argument_one) && data.Platform.Equals(PlatformsEnum.Twitch))
                        {
                            if (arguments.Count > 1)
                            {
                                string user = arguments[1];
                                if (bb.Bot.Clients.Twitch.JoinedChannels.Contains(new JoinedChannel(user)))
                                    bb.Bot.Clients.Twitch.LeaveChannel(user);
                                bb.Bot.Clients.Twitch.JoinChannel(user);
                                PlatformMessageSender.TwitchSend(bb.Bot.BotName, $"ppSpin Reconnected to #{user}", "", "", "en-US", true);
                                commandReturn.SetMessage(LocalizationService.GetString(language, "command:bot:rejoin", channel_id, data.Platform));
                            }
                            else
                            {
                                commandReturn.SetMessage(LocalizationService.GetString(language, "error:not_enough_arguments", channel_id, data.Platform, $"{bb.Bot.DefaultExecutor}bot rejoin (channel)"));
                                commandReturn.SetColor(ChatColorPresets.Red);
                                commandReturn.SetSafe(false);
                            }
                        }
                        else if (add_channel_alias.Contains(argument_one) && data.Platform is PlatformsEnum.Twitch)
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
                                    PlatformMessageSender.TwitchSend(bb.Bot.BotName, $"Pag Added to #{arguments[1]}", "", "", "en-US", true);
                                    PlatformMessageSender.TwitchReply(channel, channel_id, LocalizationService.GetString(language, "command:bot:channel:add", channel_id, data.Platform, arguments[1]), message_id, language, true);
                                    PlatformMessageSender.TwitchSend(arguments[1], LocalizationService.GetString(language, "text:added", channel_id, data.Platform, bb.Bot.Version), channel_id, message_id, language, true);
                                }
                                else
                                    PlatformMessageSender.TwitchReply(channel, channel_id, LocalizationService.GetString(language, "error:user_not_found", channel_id, data.Platform, arguments[1]), message_id, language, true);
                            }
                            else
                            {
                                commandReturn.SetMessage(LocalizationService.GetString(
                                    language,
                                    "error:not_enough_arguments",
                                    channel_id,
                                    data.Platform,
                                    $"{bb.Bot.DefaultExecutor}bot addchannel (channel)"));
                                commandReturn.SetColor(ChatColorPresets.Red);
                                commandReturn.SetSafe(false);
                            }
                        }
                        else if (delete_channel_alias.Contains(argument_one) && data.Platform == PlatformsEnum.Twitch)
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
                                    PlatformMessageSender.TwitchSend(bb.Bot.BotName, $"What Deleted from #{arguments[1]}", "", "", "en-US", true);
                                    PlatformMessageSender.TwitchReply(channel, channel_id, LocalizationService.GetString(language, "command:bot:channel:delete", channel_id, data.Platform, arguments[1]), message_id, data.User.Language, true);
                                }
                                else
                                    PlatformMessageSender.TwitchReply(channel, channel_id, LocalizationService.GetString(language, "error:user_not_found", channel_id, data.Platform, arguments[1]), message_id, data.User.Language, true);
                            }
                            else
                            {
                                commandReturn.SetMessage(LocalizationService.GetString(
                                    language,
                                    "error:not_enough_arguments",
                                    channel_id,
                                    data.Platform,
                                    $"{bb.Bot.DefaultExecutor}bot delchannel (channel)"));
                                commandReturn.SetColor(ChatColorPresets.Red);
                                commandReturn.SetSafe(false);
                            }
                        }
                        else if (join_channel_alias.Contains(argument_one) && data.Platform == PlatformsEnum.Twitch)
                        {
                            if (arguments.Count > 1)
                            {
                                PlatformMessageSender.TwitchSend(bb.Bot.BotName, $"Pag Manually joined to #{arguments[1]}", "", "", "en-US", true);
                                bb.Bot.Clients.Twitch.JoinChannel(arguments[1]);
                                commandReturn.SetMessage(LocalizationService.GetString(language, "command:bot:connect", channel_id, data.Platform)); // Fix #AB8
                            }
                            else
                            {
                                commandReturn.SetMessage(LocalizationService.GetString(language, "error:not_enough_arguments", channel_id, data.Platform, $"{bb.Bot.DefaultExecutor}bot joinchannel (channel)"));
                                commandReturn.SetColor(ChatColorPresets.Red);
                                commandReturn.SetSafe(false);
                            }
                        }
                        else if (leave_channel_alias.Contains(argument_one) && data.Platform == PlatformsEnum.Twitch)
                        {
                            if (arguments.Count > 1)
                            {
                                PlatformMessageSender.TwitchSend(bb.Bot.BotName, $"What Leaved from #{arguments[1]}", "", "", "en-US", true);
                                bb.Bot.Clients.Twitch.LeaveChannel(arguments[1]);
                                commandReturn.SetMessage(LocalizationService.GetString(language, "command:bot:leave", channel_id, data.Platform)); // Fix #AB8
                            }
                            else
                            {
                                commandReturn.SetMessage(LocalizationService.GetString(language, "error:not_enough_arguments", channel_id, data.Platform, $"{bb.Bot.DefaultExecutor}bot leavechannel (channel)"));
                                commandReturn.SetColor(ChatColorPresets.Red);
                                commandReturn.SetSafe(false);
                            }
                        }
                        else if (is_developer)
                        {
                            if (moderator_add_alias.Contains(argument_one))
                            {
                                if (arguments.Count > 1)
                                {
                                    var userID = UsernameResolver.GetUserID(arguments[1], data.Platform);
                                    if (userID != null)
                                    {
                                        PlatformMessageSender.TwitchSend(bb.Bot.BotName, $"Pag New moderator @{arguments[1]}", "", "", "en-US", true);
                                        bb.Bot.DataBase.Roles.AddModerator(data.Platform, DataConversion.ToLong(userID), DateTime.UtcNow, data.User.ID);
                                        commandReturn.SetMessage(LocalizationService.GetString(language, "command:bot:moderator:add", channel_id, data.Platform, arguments[1]));
                                    }
                                    else
                                    {
                                        commandReturn.SetMessage(LocalizationService.GetString(language, "error:user_not_found", channel_id, data.Platform, arguments[1]));
                                        commandReturn.SetColor(ChatColorPresets.Red);
                                    }
                                }
                                else
                                {
                                    commandReturn.SetMessage(LocalizationService.GetString(language, "error:not_enough_arguments", channel_id, data.Platform, $"{bb.Bot.DefaultExecutor}bot addchannel (channel)"));
                                    commandReturn.SetColor(ChatColorPresets.Red);
                                    commandReturn.SetSafe(false);
                                }
                            }
                            else if (moderator_delete_alias.Contains(argument_one))
                            {
                                if (arguments.Count > 1)
                                {
                                    var userID = UsernameResolver.GetUserID(arguments[1], data.Platform);
                                    if (userID != null)
                                    {
                                        PlatformMessageSender.TwitchSend(bb.Bot.BotName, $"What @{arguments[1]} is no longer a moderator", "", "", "en-US", true);
                                        bb.Bot.DataBase.Roles.RemoveModerator(bb.Bot.DataBase.Roles.GetModerator(data.Platform, DataConversion.ToLong(userID)).ID);
                                        commandReturn.SetMessage(LocalizationService.GetString(language, "command:bot:moderator:delete", channel_id, data.Platform, arguments[1]));
                                    }
                                    else
                                    {
                                        commandReturn.SetMessage(LocalizationService.GetString(language, "error:user_not_found", channel_id, data.Platform, arguments[1]));
                                        commandReturn.SetColor(ChatColorPresets.Red);
                                    }
                                }
                                else
                                {
                                    commandReturn.SetMessage(LocalizationService.GetString(language, "error:not_enough_arguments", channel_id, data.Platform, $"{bb.Bot.DefaultExecutor}bot addchannel (channel)"));
                                    commandReturn.SetColor(ChatColorPresets.Red);
                                    commandReturn.SetSafe(false);
                                }
                            }
                            else if (update_translation_alias.Contains(argument_one))
                            {
                                LocalizationService.UpdateTranslation("ru-RU", channel_id, data.Platform);
                                LocalizationService.UpdateTranslation("en-US", channel_id, data.Platform);
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
                    commandReturn.SetMessage(LocalizationService.GetString(language, "text:bot_info", channel_id, data.Platform));
            }
            catch (Exception e)
            {
                commandReturn.SetError(e);
            }

            return commandReturn;
        }
    }
}