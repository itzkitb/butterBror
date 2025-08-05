using butterBror.Core.Bot;
using butterBror.Core.Bot.SQLColumnNames;
using butterBror.Data;
using butterBror.Models;
using butterBror.Utils;
using DankDB;
using Microsoft.CodeAnalysis;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Models;
using static butterBror.Core.Bot.Console;

namespace butterBror.Core.Commands.List
{
    public class Bot : CommandBase
    {
        public override string Name => "Bot";
        public override string Author => "ItzKITb";
        public override string AuthorsGithub => "https://github.com/itzkitb";
        public override string GithubSource => $"{URLs.githubSource}blob/master/butterBror/Core/Commands/List/Bot.cs";
        public override Version Version => new("1.0.0");
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
            Engine.Statistics.FunctionsUsed.Add();
            CommandReturn commandReturn = new CommandReturn();

            try
            {
                List<string> arguments = (data.Arguments is null ? [] : data.Arguments);

                bool is_moderator = (bool)data.User.IsBotModerator;
                string user_id = data.UserID;
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
                                        Engine.Bot.SQL.Users.SetParameter(data.Platform, Format.ToLong(data.User.ID), Users.Language, result);
                                        commandReturn.SetMessage(LocalizationService.GetString(result, "command:bot:language:set", channel_id, data.Platform));
                                    }
                                }
                                else
                                {
                                    commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:not_enough_arguments", channel_id, data.Platform, $"{Engine.Bot.Executor}bot lang set en/ru"));
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
                            commandReturn.SetMessage(LocalizationService.GetString(language, "error:not_enough_arguments", channel_id, data.Platform, $"{Engine.Bot.Executor}bot lang (set en/ru)/get"));
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
                                { "user_indentificator", data.UserID },
                                { "date", DateTime.UtcNow }
                            };
                        Directory.CreateDirectory(Engine.Bot.Pathes.General + "INVITE/");
                        Manager.Save(Engine.Bot.Pathes.General + $"INVITE/{data.User.Name}.txt", $"rq{DateTime.UtcNow}", userData);
                        commandReturn.SetMessage(LocalizationService.GetString(language, "command:bot:user_verify", channel_id, data.Platform));
                    }
                    else if (currency_alias.Contains(argument_one))
                    {
                        if (arguments.Count > 2 && add_currency_alias.Contains(arguments.ElementAt(1).ToLower()) && is_developer)
                        {
                            int converted = Utils.Format.ToInt(arguments.ElementAt(2).ToLower());
                            Engine.BankDollars += converted;

                            commandReturn.SetMessage(LocalizationService.GetString(language, "command:bot:currency:add", channel_id, data.Platform, converted.ToString(), Engine.BankDollars.ToString()));
                        }
                        else
                        {
                            // 0_0

                            var date = DateTime.UtcNow.AddDays(-1);

                            float oldCurrencyAmount = Manager.Get<float>(Engine.Bot.Pathes.Currency, $"[{date.Day}.{date.Month}.{date.Year}] amount");
                            float oldCurrencyCost = Manager.Get<float>(Engine.Bot.Pathes.Currency, $"[{date.Day}.{date.Month}.{date.Year}] cost");
                            int oldCurrencyUsers = Manager.Get<int>(Engine.Bot.Pathes.Currency, $"[{date.Day}.{date.Month}.{date.Year}] users");
                            int oldDollarsInBank = Manager.Get<int>(Engine.Bot.Pathes.Currency, $"[{date.Day}.{date.Month}.{date.Year}] dollars");
                            float oldMiddle = oldCurrencyUsers != 0 ? float.Parse((oldCurrencyAmount / oldCurrencyUsers).ToString("0.00")) : 0;

                            float currencyCost = float.Parse((Engine.BankDollars / Engine.Coins).ToString("0.00"));
                            float middleUsersBalance = float.Parse((Engine.Coins / Engine.Users).ToString("0.00"));

                            float plusOrMinusCost = currencyCost - oldCurrencyCost;
                            float plusOrMinusAmount = Engine.Coins - oldCurrencyAmount;
                            int plusOrMinusUsers = Engine.Users - oldCurrencyUsers;
                            int plusOrMinusDollars = Engine.BankDollars - oldDollarsInBank;
                            float plusOrMinusMiddle = middleUsersBalance - oldMiddle;

                            string buttersCostProgress = oldCurrencyCost > currencyCost ? $"🔽 ({plusOrMinusCost:0.00})" : oldCurrencyCost == currencyCost ? "⏺️ (0)" : $"🔼 (+{plusOrMinusCost:0.00})";
                            string buttersAmountProgress = oldCurrencyAmount > Engine.Coins ? $"🔽 ({plusOrMinusAmount:0.00})" : oldCurrencyAmount == Engine.Coins ? "⏺️ (0)" : $"🔼 (+{plusOrMinusAmount:0.00})";
                            string buttersDollars = oldDollarsInBank > Engine.BankDollars ? $"🔽 ({plusOrMinusDollars})" : oldDollarsInBank == Engine.BankDollars ? "⏺️ (0)" : $"🔼 (+{plusOrMinusDollars})";
                            string buttersMiddle = oldMiddle > middleUsersBalance ? $"🔽 ({plusOrMinusMiddle:0.00})" : oldMiddle == middleUsersBalance ? "⏺️ (0)" : $"🔼 (+{plusOrMinusMiddle:0.00})";

                            commandReturn.SetMessage(LocalizationService.GetString(
                                language,
                                "command:bot:currency",
                                channel_id,
                                data.Platform,
                                Engine.Coins.ToString() + " " + buttersAmountProgress,
                                Engine.Users.ToString(),
                                (Engine.Coins / Engine.Users).ToString("0.00") + " " + buttersMiddle,
                                (Engine.BankDollars / Engine.Coins).ToString("0.00") + " " + buttersCostProgress,
                                Engine.BankDollars + " " + buttersDollars));
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
                                string bcid = Names.GetUserID(arg2, data.Platform);
                                string reason = data.ArgumentsString.Replace(arguments.ElementAt(0), "").Replace(arguments.ElementAt(1), "").Replace("  ", "");
                                if (bcid.Equals(null))
                                {
                                    commandReturn.SetMessage(LocalizationService.GetString(language, "error:user_not_found", channel_id, data.Platform, arg2));
                                    commandReturn.SetColor(ChatColorPresets.Red);
                                    commandReturn.SetSafe(false);
                                }
                                else
                                {
                                    if (is_developer || (!(Engine.Bot.SQL.Roles.GetModerator(data.Platform, Format.ToLong(data.User.ID)) is not null) && !(Engine.Bot.SQL.Roles.GetDeveloper(data.Platform, Format.ToLong(data.User.ID)) is not null)))
                                    {
                                        Engine.Bot.SQL.Roles.AddBannedUser(data.Platform, Format.ToLong(bcid), DateTime.UtcNow, data.User.ID, reason);
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
                                commandReturn.SetMessage(LocalizationService.GetString(language, "error:not_enough_arguments", channel_id, data.Platform, $"{Engine.Bot.Executor}bot ban (channel) (reason)"));
                                commandReturn.SetColor(ChatColorPresets.Red);
                            }
                        }
                        else if (pardon_alias.Equals("pardon"))
                        {
                            if (arguments.Count > 1)
                            {
                                var arg2 = arguments.ElementAt(1).ToLower().Replace("@", "").Replace(",", "");
                                var BanChannelID = Names.GetUserID(arg2, data.Platform);
                                if (BanChannelID.Equals(null))
                                {
                                    commandReturn.SetMessage(LocalizationService.GetString(language, "error:user_not_found", channel_id, data.Platform, arg2));
                                    commandReturn.SetColor(ChatColorPresets.Red);
                                    commandReturn.SetSafe(false);
                                }
                                else
                                {
                                    if (is_developer || is_moderator && (!(Engine.Bot.SQL.Roles.GetModerator(data.Platform, Format.ToLong(data.User.ID)) is not null) && !(Engine.Bot.SQL.Roles.GetDeveloper(data.Platform, Format.ToLong(data.User.ID)) is not null)))
                                    {
                                        Engine.Bot.SQL.Roles.RemoveBannedUser(Engine.Bot.SQL.Roles.GetBannedUser(data.Platform, Format.ToLong(BanChannelID)).ID);
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
                                commandReturn.SetMessage(LocalizationService.GetString(language, "error:not_enough_arguments", channel_id, data.Platform, $"{Engine.Bot.Executor}bot pardon (channel)"));
                        }
                        else if (rejoin_alias.Contains(argument_one) && data.Platform.Equals(PlatformsEnum.Twitch))
                        {
                            if (arguments.Count > 1)
                            {
                                string user = arguments[1];
                                if (Engine.Bot.Clients.Twitch.JoinedChannels.Contains(new JoinedChannel(user)))
                                    Engine.Bot.Clients.Twitch.LeaveChannel(user);
                                Engine.Bot.Clients.Twitch.JoinChannel(user);
                                commandReturn.SetMessage(LocalizationService.GetString(language, "command:bot:rejoin", channel_id, data.Platform));
                            }
                            else
                            {
                                commandReturn.SetMessage(LocalizationService.GetString(language, "error:not_enough_arguments", channel_id, data.Platform, $"{Engine.Bot.Executor}bot rejoin (channel)"));
                                commandReturn.SetColor(ChatColorPresets.Red);
                                commandReturn.SetSafe(false);
                            }
                        }
                        else if (add_channel_alias.Contains(argument_one) && data.Platform is PlatformsEnum.Twitch)
                        {
                            if (arguments.Count > 1)
                            {
                                string newid = Names.GetUserID(arguments[1], data.Platform);
                                if (newid is not null) // Fix AA2
                                {
                                    List<string> channels = Manager.Get<List<string>>(Engine.Bot.Pathes.Settings, "twitch_connect_channels"); // Fix AA2
                                    channels.Add(newid);
                                    string[] output = [.. channels];

                                    Manager.Save(Engine.Bot.Pathes.Settings, "twitch_connect_channels", output); // Fix AA2
                                    Engine.Bot.Clients.Twitch.JoinChannel(arguments[1]);
                                    Chat.TwitchReply(channel, channel_id, LocalizationService.GetString(language, "command:bot:channel:add", channel_id, data.Platform, arguments[1]), message_id, language, true);
                                    Chat.TwitchSend(arguments[1], LocalizationService.GetString(language, "text:added", channel_id, data.Platform, Engine.Version), channel_id, message_id, language, true);
                                }
                                else
                                    Chat.TwitchReply(channel, channel_id, LocalizationService.GetString(language, "error:user_not_found", channel_id, data.Platform, arguments[1]), message_id, language, true);
                            }
                            else
                            {
                                commandReturn.SetMessage(LocalizationService.GetString(
                                    language,
                                    "error:not_enough_arguments",
                                    channel_id,
                                    data.Platform,
                                    $"{Engine.Bot.Executor}bot addchannel (channel)"));
                                commandReturn.SetColor(ChatColorPresets.Red);
                                commandReturn.SetSafe(false);
                            }
                        }
                        else if (delete_channel_alias.Contains(argument_one) && data.Platform == PlatformsEnum.Twitch)
                        {
                            if (arguments.Count > 1)
                            {
                                var userID = Names.GetUserID(arguments[1], data.Platform);
                                if (!userID.Equals(null))
                                {
                                    List<string> channels = Manager.Get<List<string>>(Engine.Bot.Pathes.Settings, "channels");
                                    channels.Remove(userID);
                                    string[] output = [.. channels];

                                    Manager.Save(Engine.Bot.Pathes.Settings, "channels", output);
                                    Engine.Bot.Clients.Twitch.LeaveChannel(arguments[1]);
                                    Chat.TwitchReply(channel, channel_id, LocalizationService.GetString(language, "command:bot:channel:delete", channel_id, data.Platform, arguments[1]), message_id, data.User.Language, true);
                                }
                                else
                                    Chat.TwitchReply(channel, channel_id, LocalizationService.GetString(language, "error:user_not_found", channel_id, data.Platform, arguments[1]), message_id, data.User.Language, true);
                            }
                            else
                            {
                                commandReturn.SetMessage(LocalizationService.GetString(
                                    language,
                                    "error:not_enough_arguments",
                                    channel_id,
                                    data.Platform,
                                    $"{Engine.Bot.Executor}bot delchannel (channel)"));
                                commandReturn.SetColor(ChatColorPresets.Red);
                                commandReturn.SetSafe(false);
                            }
                        }
                        else if (join_channel_alias.Contains(argument_one) && data.Platform == PlatformsEnum.Twitch)
                        {
                            if (arguments.Count > 1)
                            {
                                Engine.Bot.Clients.Twitch.JoinChannel(arguments[1]);
                                commandReturn.SetMessage(LocalizationService.GetString(language, "command:bot:connect", channel_id, data.Platform)); // Fix #AB8
                            }
                            else
                            {
                                commandReturn.SetMessage(LocalizationService.GetString(language, "error:not_enough_arguments", channel_id, data.Platform, $"{Engine.Bot.Executor}bot joinchannel (channel)"));
                                commandReturn.SetColor(ChatColorPresets.Red);
                                commandReturn.SetSafe(false);
                            }
                        }
                        else if (leave_channel_alias.Contains(argument_one) && data.Platform == PlatformsEnum.Twitch)
                        {
                            if (arguments.Count > 1)
                            {
                                Engine.Bot.Clients.Twitch.LeaveChannel(arguments[1]);
                                commandReturn.SetMessage(LocalizationService.GetString(language, "command:bot:leave", channel_id, data.Platform)); // Fix #AB8
                            }
                            else
                            {
                                commandReturn.SetMessage(LocalizationService.GetString(language, "error:not_enough_arguments", channel_id, data.Platform, $"{Engine.Bot.Executor}bot leavechannel (channel)"));
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
                                    var userID = Names.GetUserID(arguments[1], data.Platform);
                                    if (userID != null)
                                    {
                                        Engine.Bot.SQL.Roles.AddModerator(data.Platform, Format.ToLong(userID), DateTime.UtcNow, data.User.ID);
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
                                    commandReturn.SetMessage(LocalizationService.GetString(language, "error:not_enough_arguments", channel_id, data.Platform, $"{Engine.Bot.Executor}bot addchannel (channel)"));
                                    commandReturn.SetColor(ChatColorPresets.Red);
                                    commandReturn.SetSafe(false);
                                }
                            }
                            else if (moderator_delete_alias.Contains(argument_one))
                            {
                                if (arguments.Count > 1)
                                {
                                    var userID = Names.GetUserID(arguments[1], data.Platform);
                                    if (userID != null)
                                    {
                                        Engine.Bot.SQL.Roles.RemoveModerator(Engine.Bot.SQL.Roles.GetModerator(data.Platform, Format.ToLong(userID)).ID);
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
                                    commandReturn.SetMessage(LocalizationService.GetString(language, "error:not_enough_arguments", channel_id, data.Platform, $"{Engine.Bot.Executor}bot addchannel (channel)"));
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