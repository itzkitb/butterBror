using TwitchLib.Client.Models;
using Discord;
using System.Xml.Linq;
using TwitchLib.Client.Enums;
using butterBror.Utils.DataManagers;
using butterBror.Utils;
using butterBib;

namespace butterBror
{
    public partial class Commands
    {
        public class BotCommand
        {
            public static CommandInfo Info = new()
            {
                Name = "Bot",
                Author = "@ItzKITb",
                AuthorURL = "twitch.tv/itzkitb",
                AuthorImageURL = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                Description = "",
                UseURL = "https://itzkitb.ru/bot/command?name=bot",
                UserCooldown = 10,
                GlobalCooldown = 5,
                aliases = ["bot", "bt", "бот", "бт"],
                ArgsRequired = "(lang (set [en/ru]), verify, currency (adddollars [int]), ban [username] (reason), pardon [username], rejoinchannel [channel], addchannel [channel], delchannel [channel], joinchannel [channel], leavechannel [channel], modadd [username], demod [username])",
                ResetCooldownIfItHasNotReachedZero = false,
                CreationDate = DateTime.Parse("07/04/2024"),
                ForAdmins = false,
                ForBotCreator = false,
                ForChannelAdmins = false,
                AllowedPlatforms = [Platforms.Twitch, Platforms.Telegram, Platforms.Discord]
            };
            public static CommandReturn Index(CommandData data)
            {
                try
                {
                    string message = "";
                    string title = "";
                    Color color = Color.Green;
                    bool safe = true;
                    bool mod = (bool)data.User.IsBotAdmin;
                    List<string> args = (data.args == null ? [] : data.args);
                    int argsc = (args == null ? 0 : args.Count);
                    string uid = data.UserUUID;
                    string? rid = data.ChannelID;
                    string? chnl = data.Channel;
                    string? mid = data.MessageID;
                    bool dev = (data.User.IsBotCreator == null ? false : (bool)data.User.IsBotCreator);
                    string lang = data.User.Lang;

                    string[] langAlias = ["lang", "l", "language", "язык", "я"];
                    string[] setAlias = ["set", "s", "сет", "установить", "у"];
                    string[] langsEnAlias = ["en", "e", "англ", "английский"];
                    string[] langsRuAlias = ["ru", "r", "рус", "русский"];
                    string[] addDollarsAlias = ["adddollars", "addd", "ad", "добавитьдоллары", "дд"];
                    string[] currencyAlias = ["currency", "c", "курс"];
                    string[] inviteAlias = ["invite", "пригласить", "i", "п"];
                    string[] updateTranslaion = ["updatetranslation", "uptr", "ut", "обновитьперевод", "оп"];

                    string[] banAlias = ["ban", "бан", "block", "kill", "заблокировать", "чел"];
                    string[] pardonAlias = ["pardon", "unblock", "unban", "разблокировать", "разбанить", "анбан"];
                    string[] rejoinAlias = ["rejoin", "rej", "переподключить", "reenter", "ree"];
                    string[] addchnlAlias = ["addchannel", "newchannel", "addc", "newc", "дканал", "нканал"];
                    string[] delchnlAlias = ["delchannel", "deletechannel", "удалитьканал", "уканал"];
                    string[] joinchnlAlias = ["joinchannel", "joinc", "вканал"];
                    string[] leavechnlAlias = ["leavechannel", "leavec", "пканал"];

                    string[] modaddAlias = ["modadd", "дм", "добавитьмодератора"];
                    string[] demodAlias = ["demod", "ум", "удалитьмодератора"];

                    var user = NamesUtil.GetUsernameFromText(data.ArgsAsString);

                    switch (args.Count)
                    {
                        case > 0:
                            {
                                var arg1 = args.ElementAt(0).ToLower();
                                if (langAlias.Contains(arg1))
                                {
                                    if (argsc > 1)
                                    {
                                        string argument2 = args.ElementAt(1).ToLower();
                                        if (setAlias.Contains(argument2))
                                        {
                                            if (argsc > 2)
                                            {
                                                string arg3 = args.ElementAt(2).ToLower();
                                                string result = string.Empty;
                                                if (langsEnAlias.Contains(arg3))
                                                    result = "en";
                                                else if (langsRuAlias.Contains(arg3))
                                                    result = "ru";

                                                if (result.Equals(string.Empty))
                                                {
                                                    message = TranslationManager.GetTranslation(lang, "wrongArgs", rid);
                                                    color = Color.Red;
                                                }
                                                else
                                                {
                                                    UsersData.UserSaveData(uid, "language", result);
                                                    message = TranslationManager.GetTranslation("ru", "changedLang", rid);
                                                }
                                            }
                                            else
                                            {
                                                message = TranslationManager.GetTranslation(data.User.Lang, "lowArgs", rid)
                                                    .Replace("%commandWorks%", "#bot lang set en/ru");
                                                color = Color.Red;
                                            }
                                        }
                                        else if (argument2.Contains("get"))
                                            message = TranslationManager.GetTranslation(lang, "lang", rid);
                                        else
                                        {
                                            message = TranslationManager.GetTranslation(lang, "wrongArgs", rid);
                                            color = Color.Red;
                                        }
                                    }
                                    else
                                    {
                                        message = TranslationManager.GetTranslation(lang, "lowArgs", rid)
                                            .Replace("%commandWorks%", "#bot lang (set en/ru)/get");
                                        color = Color.Red;
                                    }
                                }
                                else if (inviteAlias.Contains(arg1))
                                {
                                    ConsoleUtil.LOG($"Request to add a bot from @{data.User.Name}", "info");
                                    Dictionary<string, dynamic> userData = new()
                                    {
                                        { "userLang", data.User.Lang },
                                        { "username", data.User.Name },
                                        { "userID", data.UserUUID },
                                        { "requestVerifyDate", DateTime.UtcNow }
                                    };
                                    DataManager.SaveData(Bot.ProgramPath + $"INVITE/{data.User.Name}.txt", $"rq{DateTime.UtcNow}", userData);
                                    message = TranslationManager.GetTranslation(lang, "botVerified", rid);
                                }
                                else if (currencyAlias.Contains(arg1))
                                {
                                    if (argsc > 2)
                                    {
                                        string arg2 = args.ElementAt(1).ToLower();
                                        if (addDollarsAlias.Contains(arg2) && dev)
                                        {
                                            var converted = FormatUtil.ToNumber(args.ElementAt(2).ToLower());
                                            BotEngine.buttersDollars += converted;
                                            message = TranslationManager.GetTranslation(lang, "currencyDollarsAdded", rid)
                                                .Replace("%dollarsAdded%", converted.ToString())
                                                .Replace("%dollarsNOW%", BotEngine.buttersDollars.ToString());
                                        }
                                    }
                                    else
                                    {
                                        // 0_0

                                        var date = DateTime.UtcNow.AddDays(-1);

                                        float oldCurrencyAmount = DataManager.GetData<float>(Bot.CurrencyPath, $"{date.Day}.{date.Month}.{date.Year}totalAmount");
                                        float oldCurrencyCost = DataManager.GetData<float>(Bot.CurrencyPath, $"{date.Day}.{date.Month}.{date.Year}butter'sCost");
                                        int oldCurrencyUsers = DataManager.GetData<int>(Bot.CurrencyPath, $"{date.Day}.{date.Month}.{date.Year}totalUsers");
                                        int oldDollarsInBank = DataManager.GetData<int>(Bot.CurrencyPath, $"{date.Day}.{date.Month}.{date.Year}totalDollarsInTheBank");
                                        float oldMiddle = oldCurrencyUsers != 0 ? float.Parse((oldCurrencyAmount / oldCurrencyUsers).ToString("0.00")) : 0;

                                        float currencyCost = float.Parse((BotEngine.buttersDollars / BotEngine.buttersAmount).ToString("0.00"));
                                        float middleUsersBalance = float.Parse((BotEngine.buttersAmount / BotEngine.users).ToString("0.00"));

                                        float plusOrMinusCost = currencyCost - oldCurrencyCost;
                                        float plusOrMinusAmount = BotEngine.buttersAmount - oldCurrencyAmount;
                                        int plusOrMinusUsers = BotEngine.users - oldCurrencyUsers;
                                        int plusOrMinusDollars = BotEngine.buttersDollars - oldDollarsInBank;
                                        float plusOrMinusMiddle = middleUsersBalance - oldMiddle;

                                        string buttersCostProgress = oldCurrencyCost > currencyCost ? $"🔽 ({plusOrMinusCost.ToString("0.00")})" : oldCurrencyCost == currencyCost ? "⏺️ (0)" : $"🔼 (+{plusOrMinusCost.ToString("0.00")})";
                                        string buttersAmountProgress = oldCurrencyAmount > BotEngine.buttersAmount ? $"🔽 ({plusOrMinusAmount.ToString("0.00")})" : oldCurrencyAmount == BotEngine.buttersAmount ? "⏺️ (0)" : $"🔼 (+{plusOrMinusAmount.ToString("0.00")})";
                                        string buttersUsers = oldCurrencyUsers > BotEngine.users ? $"🔽 ({plusOrMinusUsers})" : oldCurrencyUsers == BotEngine.users ? "⏺️ (0)" : $"🔼 (+{plusOrMinusUsers})";
                                        string buttersDollars = oldDollarsInBank > BotEngine.buttersDollars ? $"🔽 ({plusOrMinusDollars})" : oldDollarsInBank == BotEngine.buttersDollars ? "⏺️ (0)" : $"🔼 (+{plusOrMinusDollars})";
                                        string buttersMiddle = oldMiddle > middleUsersBalance ? $"🔽 ({plusOrMinusMiddle.ToString("0.00")})" : oldMiddle == middleUsersBalance ? "⏺️ (0)" : $"🔼 (+{plusOrMinusMiddle.ToString("0.00")})";

                                        message = TranslationManager.GetTranslation(lang, "currencyInfo", rid)
                                            .Replace("%totalAmount%", BotEngine.buttersAmount.ToString() + " " + buttersAmountProgress)
                                            .Replace("%totalUsers%", BotEngine.users.ToString() + " " + buttersUsers)
                                            .Replace("%midleBalance%", (BotEngine.buttersAmount / BotEngine.users).ToString("0.00") + " " + buttersMiddle)
                                            .Replace("%buterCost%", (BotEngine.buttersDollars / BotEngine.buttersAmount).ToString("0.00") + "$ " + buttersCostProgress)
                                            .Replace("%buterDollars%", BotEngine.buttersDollars + "$ " + buttersDollars);
                                        safe = false;
                                    }
                                }
                                else if (mod || dev)
                                {
                                    if (banAlias.Contains(arg1))
                                    {
                                        if (args.Count > 1)
                                        {
                                            string arg2 = args.ElementAt(1).ToLower().Replace("@", "").Replace(",", "");
                                            string bcid = NamesUtil.GetUserID(arg2);
                                            string reason = data.ArgsAsString.Replace(args.ElementAt(0), "").Replace(args.ElementAt(1), "").Replace("  ", "");
                                            if (bcid.Equals("err"))
                                            {
                                                message = TranslationManager.GetTranslation(lang, "noneExistUser", rid)
                                                    .Replace("%user%", arg2);
                                                color = Color.Red;
                                                safe = false;
                                            }
                                            else
                                            {
                                                if (dev)
                                                {
                                                    UsersData.UserSaveData(bcid, "isBanned", true);
                                                    UsersData.UserSaveData(bcid, "banReason", reason);
                                                    message = TranslationManager.GetTranslation(lang, "userBanned", rid)
                                                        .Replace("%user%", arg2)
                                                        .Replace("%reason%", reason);
                                                }
                                                else
                                                {
                                                    if (!UsersData.UserGetData<bool>(bcid, "isBotModerator") && !UsersData.UserGetData<bool>(bcid, "isBotDev"))
                                                    {
                                                        UsersData.UserSaveData(bcid, "isBanned", true);
                                                        UsersData.UserSaveData(bcid, "banReason", reason);
                                                        message = TranslationManager.GetTranslation(lang, "userBanned", rid)
                                                            .Replace("%user%", arg2)
                                                            .Replace("%reason%", reason);
                                                    }
                                                    else
                                                    {
                                                        message = TranslationManager.GetTranslation(lang, "noAccess", rid);
                                                        color = Color.Red;
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            message = TranslationManager.GetTranslation(lang, "lowArgs", rid)
                                                .Replace("%commandWorks%", "#bot ban (channel) (reason)");
                                            color = Color.Red;
                                        }
                                    }
                                    else if (pardonAlias.Equals("pardon"))
                                    {
                                        if (argsc > 1)
                                        {
                                            var arg2 = args.ElementAt(1).ToLower().Replace("@", "").Replace(",", "");
                                            var BanChannelID = NamesUtil.GetUserID(arg2);
                                            if (BanChannelID.Equals("err"))
                                            {
                                                message = TranslationManager.GetTranslation(lang, "noneExistUser", rid)
                                                    .Replace("%user%", arg2);
                                                color = Color.Red;
                                                safe = false;
                                            }
                                            else
                                            {
                                                if (dev || mod && (!UsersData.UserGetData<bool>(BanChannelID, "isBotModerator") && !UsersData.UserGetData<bool>(BanChannelID, "isBotDev")))
                                                {
                                                    UsersData.UserSaveData(BanChannelID, "isBanned", false);
                                                    message = TranslationManager.GetTranslation(lang, "userPardon", rid)
                                                        .Replace("%user%", arg2);
                                                }
                                                else
                                                {
                                                    message = TranslationManager.GetTranslation(lang, "noAccess", rid);
                                                    color = Color.Red;
                                                }
                                            }
                                        }
                                        else
                                            message = TranslationManager.GetTranslation(lang, "lowArgs", rid)
                                                .Replace("%commandWorks%", "#bot pardon (channel)");
                                    }
                                    else if (rejoinAlias.Contains(arg1) && data.Platform.Equals(Platforms.Twitch) && TextUtil.FilterTextWithoutSpaces(user) != "")
                                    {
                                        JoinedChannel userRecon = new(user);
                                        if (Bot.Client.JoinedChannels.ToList().Contains(userRecon))
                                            Bot.Client.LeaveChannel(user);
                                        Bot.Client.JoinChannel(user);
                                        message = TranslationManager.GetTranslation(lang, "rejoinedChannel", rid);
                                    }
                                    else if (addchnlAlias.Contains(arg1) && data.Platform.Equals(Platforms.Twitch) && TextUtil.FilterTextWithoutSpaces(user) != "")
                                    {
                                        string newid = NamesUtil.GetUserID(user);
                                        if (newid != "err")
                                        {
                                            List<string> channels = DataManager.GetData<List<string>>(Bot.SettingsPath, "channels");
                                            channels.Add(newid);
                                            string[] output = [.. channels];

                                            DataManager.SaveData(Bot.SettingsPath, "channels", output);
                                            Bot.Client.JoinChannel(user);
                                            ChatUtil.TWSendMsgReply(chnl, rid, TranslationManager.GetTranslation(lang, "addedChannel", rid).Replace("%user%", user), mid, lang, true);
                                            ChatUtil.SendMessage(user, TranslationManager.GetTranslation(lang, "welcomeChannel", rid).Replace("%version%", BotEngine.botVersion), rid, mid, lang, true);
                                        }
                                        else
                                            ChatUtil.TWSendMsgReply(chnl, rid, TranslationManager.GetTranslation(lang, "noneExistUser", rid).Replace("%user%", user), mid, lang, true);
                                    }
                                    else if (delchnlAlias.Contains(arg1) && data.Platform == Platforms.Twitch && TextUtil.FilterTextWithoutSpaces(user) != "")
                                    {
                                        var userID = NamesUtil.GetUserID(user);
                                        if (userID != "err")
                                        {
                                            List<string> channels = DataManager.GetData<List<string>>(Bot.SettingsPath, "channels");
                                            channels.Remove(userID);
                                            string[] output = channels.ToArray();

                                            DataManager.SaveData(Bot.SettingsPath, "channels", output);
                                            Bot.Client.LeaveChannel(user);
                                            ChatUtil.TWSendMsgReply(chnl, rid, TranslationManager.GetTranslation(lang, "delChannel", rid).Replace("%user%", user), mid, data.User.Lang, true);
                                        }
                                        else
                                            ChatUtil.TWSendMsgReply(chnl, rid, TranslationManager.GetTranslation(lang, "noneExistUser", rid).Replace("%user%", user), mid, data.User.Lang, true);
                                    }
                                    else if (joinchnlAlias.Contains(arg1) && data.Platform == Platforms.Twitch && TextUtil.FilterTextWithoutSpaces(user) != "")
                                    {
                                        Bot.Client.JoinChannel(user);
                                        ChatUtil.TWSendMsgReply(chnl, rid, TranslationManager.GetTranslation(lang, "joinedChannel", rid), mid, data.User.Lang, true);
                                    }
                                    else if (leavechnlAlias.Contains(arg1) && data.Platform == Platforms.Twitch && TextUtil.FilterTextWithoutSpaces(user) != "")
                                    {
                                        Bot.Client.LeaveChannel(user);
                                        ChatUtil.TWSendMsgReply(chnl, rid, TranslationManager.GetTranslation(lang, "leavedChannel", rid), mid, data.User.Lang, true);
                                    }
                                    else if (dev)
                                    {
                                        if (modaddAlias.Contains(arg1))
                                        {
                                            var userID = NamesUtil.GetUserID(user);
                                            if (userID != "err")
                                            {
                                                UsersData.UserSaveData(userID, "isBotModerator", true);
                                                message = TranslationManager.GetTranslation(lang, "modAdded", rid)
                                                    .Replace("%user%", user);
                                            }
                                            else
                                            {
                                                message = TranslationManager.GetTranslation(lang, "noneExistUser", rid)
                                                    .Replace("%user%", user);
                                                color = Color.Red;
                                            }
                                        }
                                        else if (demodAlias.Contains(arg1))
                                        {
                                            var userID = NamesUtil.GetUserID(user);
                                            if (userID != "err")
                                            {
                                                UsersData.UserSaveData(userID, "isBotModerator", false);
                                                message = TranslationManager.GetTranslation(lang, "modDel", rid)
                                                    .Replace("%user%", user);
                                            }
                                            else
                                            {
                                                message = TranslationManager.GetTranslation(lang, "noneExistUser", rid)
                                                    .Replace("%user%", user);
                                                color = Color.Red;
                                            }
                                        }
                                        else if (updateTranslaion.Contains(arg1))
                                        {
                                            TranslationManager.UpdateTranslation("ru", rid);
                                            TranslationManager.UpdateTranslation("en", rid);
                                            message = "MrDestructoid 👍 Го-тово!";
                                        }
                                    }
                                }

                                break;
                            }
                        default:
                            message = TranslationManager.GetTranslation(lang, "botInfo", rid);
                            break;
                    }

                    ChatColorPresets resultNicknameColor = ChatColorPresets.YellowGreen;
                    if (color == Color.Red)
                        resultNicknameColor = ChatColorPresets.Red;

                    return new()
                    {
                        Message = message,
                        IsSafeExecute = safe,
                        Description = "",
                        Author = "",
                        ImageURL = "",
                        ThumbnailUrl = "",
                        Footer = "",
                        IsEmbed = true,
                        Ephemeral = false,
                        Title = title,
                        Color = color,
                        NickNameColor = resultNicknameColor
                    };
                }
                catch (Exception e)
                {
                    return new()
                    {
                        Message = "",
                        IsSafeExecute = false,
                        Description = "",
                        Author = "",
                        ImageURL = "",
                        ThumbnailUrl = "",
                        Footer = "",
                        IsEmbed = true,
                        Ephemeral = false,
                        Title = "",
                        Color = Color.Green,
                        NickNameColor = ChatColorPresets.YellowGreen,
                        IsError = true,
                        Error = e
                    };
                }
            }
        }
    }
}
