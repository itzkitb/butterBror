using TwitchLib.Client.Models;
using Discord;
using TwitchLib.Client.Enums;
using butterBror.Utils.DataManagers;
using butterBror.Utils;
using DankDB;
using butterBror.Utils.Tools;
using static butterBror.Utils.Bot.Console;
using butterBror.Utils.Types;

namespace butterBror
{
    public partial class Commands
    {
        public class Bot
        {
            public static CommandInfo Info = new()
            {
                Name = "Bot",
                Author = "@ItzKITb",
                AuthorLink = "twitch.tv/itzkitb",
                AuthorAvatar = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                Description = new(){ 
                    { "ru", "Главная команда, которой можно управлять ботом." },
                    { "en", "The main command that can control the bot." } 
                },
                WikiLink = "https://itzkitb.lol/bot/command?q=bot",
                CooldownPerUser = 10,
                CooldownPerChannel = 5,
                Aliases = ["bot", "bt", "бот", "бт", "main", "start", "старт", "главная", "info", "инфо", "information", "информация"],
                Arguments = "(lang (set [en/ru]), verify, currency (adddollars [int]), ban [username] (reason), pardon [username], rejoinchannel [channel], addchannel [channel], delchannel [channel], joinchannel [channel], leavechannel [channel], modadd [username], demod [username])",
                CooldownReset = false,
                CreationDate = DateTime.Parse("07/04/2024"),
                IsForBotModerator = false,
                IsForBotDeveloper = false,
                IsForChannelModerator = false,
                Platforms = [Platforms.Twitch, Platforms.Telegram, Platforms.Discord]
            };

            [ConsoleSector("butterBror.Commands.Bot", "Index")]
            public CommandReturn Index(CommandData data)
            {
                Core.Statistics.FunctionsUsed.Add();
                CommandReturn commandReturn = new CommandReturn();

                try
                {
                    List<string> arguments = (data.Arguments is null ? [] : data.Arguments);

                    bool    is_moderator   = (bool)data.User.IsBotModerator;
                    string  user_id        = data.UserID;
                    string? channel_id     = data.ChannelID;
                    string? channel        = data.Channel;
                    string? message_id     = data.MessageID;
                    bool    is_developer   = (data.User.IsBotDeveloper == null ? false : (bool)data.User.IsBotDeveloper);
                    string  language       = data.User.Language;

                    string[] language_alias           = ["lang", "l", "language", "язык", "я"];
                    string[] set_alias                = ["set", "s", "сет", "установить", "у"];
                    string[] language_english_alias   = ["en", "e", "англ", "английский"];
                    string[] language_russian_alias   = ["ru", "r", "рус", "русский"];
                    string[] add_currency_alias       = ["adddollars", "addd", "ad", "добавитьдоллары", "дд"];
                    string[] currency_alias           = ["currency", "c", "курс"];
                    string[] invite_alias             = ["invite", "пригласить", "i", "п"];
                    string[] update_translation_alias = ["updatetranslation", "uptr", "ut", "обновитьперевод", "оп"];

                    string[] ban_alias                = ["ban", "бан", "block", "kill", "заблокировать", "чел"];
                    string[] pardon_alias             = ["pardon", "unblock", "unban", "разблокировать", "разбанить", "анбан"];
                    string[] rejoin_alias             = ["rejoin", "rej", "переподключить", "reenter", "ree"];
                    string[] add_channel_alias        = ["addchannel", "newchannel", "addc", "newc", "дканал", "нканал"];
                    string[] delete_channel_alias     = ["delchannel", "deletechannel", "удалитьканал", "уканал"];
                    string[] join_channel_alias       = ["joinchannel", "joinc", "вканал"];
                    string[] leave_channel_alias      = ["leavechannel", "leavec", "пканал"];

                    string[] moderator_add_alias      = ["modadd", "дм", "добавитьмодератора"];
                    string[] moderator_delete_alias   = ["demod", "ум", "удалитьмодератора"];

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
                                            result = "en";
                                        else if (language_russian_alias.Contains(arg3))
                                            result = "ru";

                                        if (result.Equals(string.Empty))
                                        {
                                            commandReturn.SetMessage(TranslationManager.GetTranslation(language, "error:incorrect_parameters", channel_id, data.Platform));
                                            commandReturn.SetColor(ChatColorPresets.Red);
                                        }
                                        else
                                        {
                                            UsersData.Save(user_id, "language", result, data.Platform);
                                            commandReturn.SetMessage(TranslationManager.GetTranslation(result, "command:bot:language:set", channel_id, data.Platform));
                                        }
                                    }
                                    else
                                    {
                                        commandReturn.SetMessage(Text.ArgumentReplacement(TranslationManager.GetTranslation(data.User.Language, "error:not_enough_arguments", channel_id, data.Platform), "command_example", "#bot lang set en/ru"));
                                        commandReturn.SetColor(ChatColorPresets.Red);
                                    }
                                }
                                else if (argument2.Contains("get"))
                                    commandReturn.SetMessage(TranslationManager.GetTranslation(language, "info:language", channel_id, data.Platform));
                                else
                                {
                                    commandReturn.SetMessage(TranslationManager.GetTranslation(language, "error:incorrect_parameters", channel_id, data.Platform));
                                    commandReturn.SetColor(ChatColorPresets.Red);
                                }
                            }
                            else
                            {
                                commandReturn.SetMessage(Text.ArgumentReplacement(TranslationManager.GetTranslation(language, "error:not_enough_arguments", channel_id, data.Platform),
                                    "command_example", "#bot lang (set en/ru)/get"));
                                commandReturn.SetColor(ChatColorPresets.Red);
                            }
                        }
                        else if (invite_alias.Contains(argument_one))
                        {
                            Write($"Request to add a bot from @{data.User.Username}", "info");
                            Dictionary<string, dynamic> userData = new()
                            {
                                { "language", data.User.Language },
                                { "username", data.User.Username },
                                { "user_indentificator", data.UserID },
                                { "date", DateTime.UtcNow }
                            };
                            Directory.CreateDirectory(Core.Bot.Pathes.General + "INVITE/");
                            SafeManager.Save(Core.Bot.Pathes.General + $"INVITE/{data.User.Username}.txt", $"rq{DateTime.UtcNow}", userData);
                            commandReturn.SetMessage(TranslationManager.GetTranslation(language, "command:bot:user_verify", channel_id, data.Platform));
                        }
                        else if (currency_alias.Contains(argument_one))
                        {
                            if (arguments.Count > 2 && add_currency_alias.Contains(arguments.ElementAt(1).ToLower()) && is_developer)
                            {
                                int converted = Utils.Tools.Format.ToInt(arguments.ElementAt(2).ToLower());
                                Core.BankDollars += converted;

                                commandReturn.SetMessage(Text.ArgumentsReplacement(TranslationManager.GetTranslation(language, "command:bot:currency:add", channel_id, data.Platform), new() {
                                            { "added", converted.ToString() },
                                            { "now", Core.BankDollars.ToString() }
                                          }));
                            }
                            else
                            {
                                // 0_0

                                var date = DateTime.UtcNow.AddDays(-1);

                                float oldCurrencyAmount = Manager.Get<float>(Core.Bot.Pathes.Currency, $"[{date.Day}.{date.Month}.{date.Year}] amount");
                                float oldCurrencyCost = Manager.Get<float>(Core.Bot.Pathes.Currency, $"[{date.Day}.{date.Month}.{date.Year}] cost");
                                int oldCurrencyUsers = Manager.Get<int>(Core.Bot.Pathes.Currency, $"[{date.Day}.{date.Month}.{date.Year}] users");
                                int oldDollarsInBank = Manager.Get<int>(Core.Bot.Pathes.Currency, $"[{date.Day}.{date.Month}.{date.Year}] dollars");
                                float oldMiddle = oldCurrencyUsers != 0 ? float.Parse((oldCurrencyAmount / oldCurrencyUsers).ToString("0.00")) : 0;

                                float currencyCost = float.Parse((Core.BankDollars / Core.Coins).ToString("0.00"));
                                float middleUsersBalance = float.Parse((Core.Coins / Core.Users).ToString("0.00"));

                                float plusOrMinusCost = currencyCost - oldCurrencyCost;
                                float plusOrMinusAmount = Core.Coins - oldCurrencyAmount;
                                int plusOrMinusUsers = Core.Users - oldCurrencyUsers;
                                int plusOrMinusDollars = Core.BankDollars - oldDollarsInBank;
                                float plusOrMinusMiddle = middleUsersBalance - oldMiddle;

                                string buttersCostProgress = oldCurrencyCost > currencyCost ? $"🔽 ({plusOrMinusCost:0.00})" : oldCurrencyCost == currencyCost ? "⏺️ (0)" : $"🔼 (+{plusOrMinusCost:0.00})";
                                string buttersAmountProgress = oldCurrencyAmount > Core.Coins ? $"🔽 ({plusOrMinusAmount:0.00})" : oldCurrencyAmount == Core.Coins ? "⏺️ (0)" : $"🔼 (+{plusOrMinusAmount:0.00})";
                                string buttersUsers = oldCurrencyUsers > Core.Users ? $"🔽 ({plusOrMinusUsers})" : oldCurrencyUsers == Core.Users ? "⏺️ (0)" : $"🔼 (+{plusOrMinusUsers})";
                                string buttersDollars = oldDollarsInBank > Core.BankDollars ? $"🔽 ({plusOrMinusDollars})" : oldDollarsInBank == Core.BankDollars ? "⏺️ (0)" : $"🔼 (+{plusOrMinusDollars})";
                                string buttersMiddle = oldMiddle > middleUsersBalance ? $"🔽 ({plusOrMinusMiddle:0.00})" : oldMiddle == middleUsersBalance ? "⏺️ (0)" : $"🔼 (+{plusOrMinusMiddle:0.00})";

                                commandReturn.SetMessage(Text.ArgumentsReplacement(TranslationManager.GetTranslation(language, "command:bot:currency", channel_id, data.Platform), new() {
                                    {"total", Core.Coins.ToString() + " " + buttersAmountProgress},
                                    {"users", Core.Users.ToString() + " " + buttersUsers},
                                    {"midle", (Core.Coins / Core.Users).ToString("0.00") + " " + buttersMiddle},
                                    {"cost", (Core.BankDollars / Core.Coins).ToString("0.00") + "$ " + buttersCostProgress},
                                    {"dollars", Core.BankDollars + "$ " + buttersDollars},
                                }));
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
                                        commandReturn.SetMessage(Text.ArgumentReplacement(TranslationManager.GetTranslation(language, "error:user_not_found", channel_id, data.Platform),
                                            "user", arg2));
                                        commandReturn.SetColor(ChatColorPresets.Red);
                                        commandReturn.SetSafe(false);
                                    }
                                    else
                                    {
                                        if (is_developer || (!UsersData.Get<bool>(bcid, "isBotModerator", data.Platform) && !UsersData.Get<bool>(bcid, "isBotDev", data.Platform)))
                                        {
                                            UsersData.Save(bcid, "isBanned", true, data.Platform);
                                            UsersData.Save(bcid, "banReason", reason, data.Platform);
                                            commandReturn.SetMessage(Text.ArgumentsReplacement(TranslationManager.GetTranslation(language, "command:bot:user_ban", channel_id, data.Platform), new() {
                                                { "user", arg2 },
                                                { "reason", reason } }));
                                        }
                                        else
                                        {
                                            commandReturn.SetMessage(TranslationManager.GetTranslation(language, "error:not_enough_rights", channel_id, data.Platform));
                                            commandReturn.SetColor(ChatColorPresets.Red);
                                        }
                                    }
                                }
                                else
                                {
                                    commandReturn.SetMessage(Text.ArgumentReplacement(TranslationManager.GetTranslation(language, "error:not_enough_arguments", channel_id, data.Platform),
                                        "command_example", "#bot ban (channel) (reason)"));
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
                                        commandReturn.SetMessage(Text.ArgumentReplacement(TranslationManager.GetTranslation(language, "error:user_not_found", channel_id, data.Platform),
                                            "user", arg2));
                                        commandReturn.SetColor(ChatColorPresets.Red);
                                        commandReturn.SetSafe(false);
                                    }
                                    else
                                    {
                                        if (is_developer || is_moderator && (!UsersData.Get<bool>(BanChannelID, "isBotModerator", data.Platform) && !UsersData.Get<bool>(BanChannelID, "isBotDev", data.Platform)))
                                        {
                                            UsersData.Save(BanChannelID, "isBanned", false, data.Platform);
                                            commandReturn.SetMessage(Text.ArgumentReplacement(TranslationManager.GetTranslation(language, "command:bot:user_unban", channel_id, data.Platform),
                                                "user", arg2));
                                        }
                                        else
                                        {
                                            commandReturn.SetMessage(TranslationManager.GetTranslation(language, "error:not_enough_rights", channel_id, data.Platform));
                                            commandReturn.SetColor(ChatColorPresets.Red);
                                        }
                                    }
                                }
                                else
                                    commandReturn.SetMessage(Text.ArgumentReplacement(TranslationManager.GetTranslation(language, "error:not_enough_arguments", channel_id, data.Platform),
                                        "command_example", "#bot pardon (channel)"));
                            }
                            else if (rejoin_alias.Contains(argument_one) && data.Platform.Equals(Platforms.Twitch))
                            {
                                if (arguments.Count > 1)
                                {
                                    string user = arguments[1];
                                    if (Core.Bot.Clients.Twitch.JoinedChannels.Contains(new JoinedChannel(user)))
                                        Core.Bot.Clients.Twitch.LeaveChannel(user);
                                    Core.Bot.Clients.Twitch.JoinChannel(user);
                                    commandReturn.SetMessage(TranslationManager.GetTranslation(language, "command:bot:rejoin", channel_id, data.Platform));
                                }
                                else
                                {
                                    commandReturn.SetMessage(Text.ArgumentReplacement(TranslationManager.GetTranslation(language, "error:not_enough_arguments", channel_id, data.Platform),
                                        "command_example", "#bot rejoin (channel)"));
                                    commandReturn.SetColor(ChatColorPresets.Red);
                                    commandReturn.SetSafe(false);
                                }
                            }
                            else if (add_channel_alias.Contains(argument_one) && data.Platform is Platforms.Twitch)
                            {
                                if (arguments.Count > 1)
                                {
                                    string newid = Names.GetUserID(arguments[1], data.Platform);
                                    if (newid is not null) // Fix AA2
                                    {
                                        List<string> channels = Manager.Get<List<string>>(Core.Bot.Pathes.Settings, "twitch_connect_channels"); // Fix AA2
                                        channels.Add(newid);
                                        string[] output = [.. channels];

                                        SafeManager.Save(Core.Bot.Pathes.Settings, "twitch_connect_channels", output); // Fix AA2
                                        Core.Bot.Clients.Twitch.JoinChannel(arguments[1]);
                                        Chat.TwitchReply(channel, channel_id, Text.ArgumentReplacement(TranslationManager.GetTranslation(language, "command:bot:channel:add", channel_id, data.Platform), "user", arguments[1]), message_id, language, true);
                                        Chat.TwitchSend(arguments[1], Text.ArgumentReplacement(TranslationManager.GetTranslation(language, "text:added", channel_id, data.Platform), "version", Core.Version), channel_id, message_id, language, true);
                                    }
                                    else
                                        Chat.TwitchReply(channel, channel_id, Text.ArgumentReplacement(TranslationManager.GetTranslation(language, "error:user_not_found", channel_id, data.Platform), "user", arguments[1]), message_id, language, true);
                                }
                                else
                                {
                                    commandReturn.SetMessage(Text.ArgumentReplacement(TranslationManager.GetTranslation(language, "error:not_enough_arguments", channel_id, data.Platform),
                                        "command_example", "#bot addchannel (channel)"));
                                    commandReturn.SetColor(ChatColorPresets.Red);
                                    commandReturn.SetSafe(false);
                                }
                            }
                            else if (delete_channel_alias.Contains(argument_one) && data.Platform == Platforms.Twitch)
                            {
                                if (arguments.Count > 1)
                                {
                                    var userID = Names.GetUserID(arguments[1], data.Platform);
                                    if (!userID.Equals(null))
                                    {
                                        List<string> channels = Manager.Get<List<string>>(Core.Bot.Pathes.Settings, "channels");
                                        channels.Remove(userID);
                                        string[] output = [.. channels];

                                        SafeManager.Save(Core.Bot.Pathes.Settings, "channels", output);
                                        Core.Bot.Clients.Twitch.LeaveChannel(arguments[1]);
                                        Chat.TwitchReply(channel, channel_id, Text.ArgumentReplacement(TranslationManager.GetTranslation(language, "command:bot:channel:delete", channel_id, data.Platform), "user", arguments[1]), message_id, data.User.Language, true);
                                    }
                                    else
                                        Chat.TwitchReply(channel, channel_id, Text.ArgumentReplacement(TranslationManager.GetTranslation(language, "error:user_not_found", channel_id, data.Platform), "user", arguments[1]), message_id, data.User.Language, true);
                                }
                                else
                                {
                                    commandReturn.SetMessage(Text.ArgumentReplacement(TranslationManager.GetTranslation(language, "error:not_enough_arguments", channel_id, data.Platform),
                                        "command_example", "#bot delchannel (channel)"));
                                    commandReturn.SetColor(ChatColorPresets.Red);
                                    commandReturn.SetSafe(false);
                                }
                            }
                            else if (join_channel_alias.Contains(argument_one) && data.Platform == Platforms.Twitch)
                            {
                                if (arguments.Count > 1)
                                {
                                    Core.Bot.Clients.Twitch.JoinChannel(arguments[1]);
                                    Chat.TwitchReply(channel, channel_id, TranslationManager.GetTranslation(language, "command:bot:connect", channel_id, data.Platform), message_id, data.User.Language, true);
                                }
                                else
                                {
                                    commandReturn.SetMessage(Text.ArgumentReplacement(TranslationManager.GetTranslation(language, "error:not_enough_arguments", channel_id, data.Platform),
                                        "command_example", "#bot joinchannel (channel)"));
                                    commandReturn.SetColor(ChatColorPresets.Red);
                                    commandReturn.SetSafe(false);
                                }
                            }
                            else if (leave_channel_alias.Contains(argument_one) && data.Platform == Platforms.Twitch)
                            {
                                if (arguments.Count > 1)
                                {
                                    Core.Bot.Clients.Twitch.LeaveChannel(arguments[1]);
                                    Chat.TwitchReply(channel, channel_id, TranslationManager.GetTranslation(language, "command:bot:leave", channel_id, data.Platform), message_id, data.User.Language, true);
                                }
                                else
                                {
                                    commandReturn.SetMessage(Text.ArgumentReplacement(TranslationManager.GetTranslation(language, "error:not_enough_arguments", channel_id, data.Platform),
                                        "command_example", "#bot leavechannel (channel)"));
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
                                            UsersData.Save(userID, "isBotModerator", true, data.Platform);
                                            commandReturn.SetMessage(Text.ArgumentReplacement(TranslationManager.GetTranslation(language, "command:bot:moderator:add", channel_id, data.Platform),
                                                "user", arguments[1]));
                                        }
                                        else
                                        {
                                            commandReturn.SetMessage(Text.ArgumentReplacement(TranslationManager.GetTranslation(language, "error:user_not_found", channel_id, data.Platform),
                                                "user", arguments[1]));
                                            commandReturn.SetColor(ChatColorPresets.Red);
                                        }
                                    }
                                    else
                                    {
                                        commandReturn.SetMessage(Text.ArgumentReplacement(TranslationManager.GetTranslation(language, "error:not_enough_arguments", channel_id, data.Platform),
                                            "command_example", "#bot addchannel (channel)"));
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
                                            UsersData.Save(userID, "isBotModerator", false, data.Platform);
                                            commandReturn.SetMessage(Text.ArgumentReplacement(TranslationManager.GetTranslation(language, "command:bot:moderator:delete", channel_id, data.Platform),
                                                "user", arguments[1]));
                                        }
                                        else
                                        {
                                            commandReturn.SetMessage(Text.ArgumentReplacement(TranslationManager.GetTranslation(language, "error:user_not_found", channel_id, data.Platform),
                                                "user", arguments[1]));
                                            commandReturn.SetColor(ChatColorPresets.Red);
                                        }
                                    }
                                    else
                                    {
                                        commandReturn.SetMessage(Text.ArgumentReplacement(TranslationManager.GetTranslation(language, "error:not_enough_arguments", channel_id, data.Platform),
                                            "command_example", "#bot addchannel (channel)"));
                                        commandReturn.SetColor(ChatColorPresets.Red);
                                        commandReturn.SetSafe(false);
                                    }
                                }
                                else if (update_translation_alias.Contains(argument_one))
                                {
                                    TranslationManager.UpdateTranslation("ru", channel_id, data.Platform);
                                    TranslationManager.UpdateTranslation("en", channel_id, data.Platform);
                                    commandReturn.SetMessage("MrDestructoid 👍 DO-NE!");
                                }
                                else
                                {
                                    commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:incorrect_parameters", data.ChannelID, data.Platform)); // Fix AA5
                                    commandReturn.SetColor(ChatColorPresets.Red);
                                }
                            }
                            else
                            {
                                commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:incorrect_parameters", data.ChannelID, data.Platform)); // Fix AA5
                                commandReturn.SetColor(ChatColorPresets.Red);
                            }
                        }
                        else
                        {
                            commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:incorrect_parameters", data.ChannelID, data.Platform)); // Fix AA5
                            commandReturn.SetColor(ChatColorPresets.Red);
                        }
                    }
                    else
                        commandReturn.SetMessage(TranslationManager.GetTranslation(language, "text:bot_info", channel_id, data.Platform));
                }
                catch (Exception e)
                {
                    commandReturn.SetError(e);
                }

                return commandReturn;
            }
        }
    }
}
