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
                ForChannelAdmins = false
            };
            public static CommandReturn Index(CommandData data)
            {
                try
                {
                    string resultMessage = "";
                    string resultMessageTitle = "";
                    Color resultColor = Color.Green;
                    bool resultIdSafe = true;
                    var is_mod = UsersData.UserGetData<bool>(data.UserUUID, "isBotModerator");
                    var args = data.args;
                    var argsc = args.Count;
                    var UserId = data.UserUUID;
                    var RoomId = data.ChannelID;
                    var Channel = data.Channel;
                    var MsgId = data.MessageID;
                    var is_dev = UsersData.UserGetData<bool>(UserId, "isBotDev");

                    string[] langAlias = ["lang", "l", "language", "язык", "я"];
                    string[] setAlias = ["set", "s", "сет", "установить", "у"];
                    string[] langsEnAlias = ["en", "e", "англ", "английский"];
                    string[] langsRuAlias = ["ru", "r", "рус", "русский"];
                    string[] addDollarsAlias = ["adddollars", "addd", "ad", "добавитьдоллары", "дд"];
                    string[] currencyAlias = ["currency", "c", "курс"];
                    string[] updateTranslaion = ["updatetranslation", "uptr", "ut", "обновитьперевод", "оп"];

                    var user = args.ElementAt(1).ToLower().Replace("@", "").Replace(",", "");

                    if (args.Count > 0)
                    {
                        var argument1 = args.ElementAt(0).ToLower();
                        if (langAlias.Contains(argument1))
                        {
                            if (argsc > 1)
                            {
                                var argument2 = args.ElementAt(1).ToLower();
                                if (setAlias.Contains(argument2))
                                {
                                    if (argsc > 2)
                                    {
                                        var argument3 = args.ElementAt(2).ToLower();
                                        if (langsEnAlias.Contains(argument3))
                                        {
                                            UsersData.UserSaveData(UserId, "language", "en");
                                            resultMessage = TranslationManager.GetTranslation("en", "changedLang", data.ChannelID);
                                        }
                                        else if (langsRuAlias.Contains(argument3))
                                        {
                                            UsersData.UserSaveData(UserId, "language", "ru");
                                            resultMessage = TranslationManager.GetTranslation("ru", "changedLang", data.ChannelID);
                                        }
                                        else
                                        {
                                            resultMessage = TranslationManager.GetTranslation(data.User.Lang, "wrongArgs", data.ChannelID);
                                            resultColor = Color.Red;
                                        }
                                    }
                                    else
                                    {
                                        resultMessage = TranslationManager.GetTranslation(data.User.Lang, "lowArgs", data.ChannelID)
                                            .Replace("%commandWorks%", "#bot lang set en/ru");
                                        resultColor = Color.Red;
                                    }
                                }
                                else if (argument2.Contains("get"))
                                {
                                    resultMessage = TranslationManager.GetTranslation(data.User.Lang, "lang", data.ChannelID);
                                }
                                else
                                {
                                    resultMessage = TranslationManager.GetTranslation(data.User.Lang, "wrongArgs", data.ChannelID);
                                    resultColor = Color.Red;
                                }
                            }
                            else
                            {
                                resultMessage = TranslationManager.GetTranslation(data.User.Lang, "lowArgs", data.ChannelID)
                                    .Replace("%commandWorks%", "#bot lang (set en/ru)/get");
                                resultColor = Color.Red;
                            }
                        }
                        else if (argument1.Contains("verify"))
                        {
                            ConsoleServer.SendConsoleMessage("commands", "Отправлен запрос на подтверждения учетной записи!");
                            Guid myuuid = Guid.NewGuid();
                            string myuuidAsString = myuuid.ToString();
                            Dictionary<string, dynamic> userData = new();
                            userData.Add("userLang", data.User.Lang);
                            userData.Add("username", data.User.Name);
                            userData.Add("userID", data.UserUUID);
                            userData.Add("requestUUID", myuuidAsString);
                            userData.Add("requestVerifyDate", DateTime.UtcNow);
                            FileUtil.CreateDirectory(Bot.ProgramPath + $"TwitchVerifyWaiting/");
                            DataManager.SaveData(Bot.ProgramPath + $"TwitchVerifyWaiting/{data.User.Name}.txt", $"verifyRequest{DateTime.UtcNow}", userData);
                            resultMessage = TranslationManager.GetTranslation(data.User.Lang, "botVerified", data.ChannelID);
                        }
                        else if (currencyAlias.Contains(argument1))
                        {
                            if (argsc > 2)
                            {
                                var argument2 = args.ElementAt(1).ToLower();
                                if (addDollarsAlias.Contains(argument2))
                                {
                                    if (is_dev)
                                    {
                                        var converted = FormatUtil.ToNumber(args.ElementAt(2).ToLower());
                                        BotEngine.buttersTotalDollarsInTheBank += converted;
                                        resultMessage = TranslationManager.GetTranslation(data.User.Lang, "currencyDollarsAdded", data.ChannelID)
                                            .Replace("%dollarsAdded%", converted.ToString())
                                            .Replace("%dollarsNOW%", BotEngine.buttersTotalDollarsInTheBank.ToString());
                                    }
                                }
                            }
                            else
                            {
                                // WTF IS THAT
                                // 0-0

                                var date = DateTime.UtcNow.AddDays(-1);
                                string buttersCostProgress = "⬛";
                                string buttersAmountProgress = "⬛";
                                string buttersUsers = "⬛";
                                string buttersDollars = "⬛";
                                string buttersMiddle = "⬛";

                                float oldCurrencyAmount = DataManager.GetData<float>(Bot.CurrencyPath, $"{date.Day}.{date.Month}.{date.Year}totalAmount");
                                float oldCurrencyCost = DataManager.GetData<float>(Bot.CurrencyPath, $"{date.Day}.{date.Month}.{date.Year}butter'sCost");
                                int oldCurrencyUsers = DataManager.GetData<int>(Bot.CurrencyPath, $"{date.Day}.{date.Month}.{date.Year}totalUsers");
                                int oldDollarsInBank = DataManager.GetData<int>(Bot.CurrencyPath, $"{date.Day}.{date.Month}.{date.Year}totalDollarsInTheBank");
                                float oldMiddle = oldCurrencyUsers != 0 ? float.Parse((oldCurrencyAmount / oldCurrencyUsers).ToString("0.00")) : 0;

                                float currencyCost = float.Parse((BotEngine.buttersTotalDollarsInTheBank / BotEngine.buttersTotalAmount).ToString("0.00"));
                                float middleUsersBalance = float.Parse((BotEngine.buttersTotalAmount / BotEngine.buttersTotalUsers).ToString("0.00"));

                                float plusOrMinusCost = currencyCost - oldCurrencyCost;
                                float plusOrMinusAmount = BotEngine.buttersTotalAmount - oldCurrencyAmount;
                                int plusOrMinusUsers = BotEngine.buttersTotalUsers - oldCurrencyUsers;
                                int plusOrMinusDollars = BotEngine.buttersTotalDollarsInTheBank - oldDollarsInBank;
                                float plusOrMinusMiddle = middleUsersBalance - oldMiddle;

                                buttersCostProgress = oldCurrencyCost > currencyCost ? $"🔽 ({plusOrMinusCost.ToString("0.00")})" : oldCurrencyCost == currencyCost ? "⏺️ (0)" : $"🔼 (+{plusOrMinusCost.ToString("0.00")})";
                                buttersAmountProgress = oldCurrencyAmount > BotEngine.buttersTotalAmount ? $"🔽 ({plusOrMinusAmount.ToString("0.00")})" : oldCurrencyAmount == BotEngine.buttersTotalAmount ? "⏺️ (0)" : $"🔼 (+{plusOrMinusAmount.ToString("0.00")})";
                                buttersUsers = oldCurrencyUsers > BotEngine.buttersTotalUsers ? $"🔽 ({plusOrMinusUsers})" : oldCurrencyUsers == BotEngine.buttersTotalUsers ? "⏺️ (0)" : $"🔼 (+{plusOrMinusUsers})";
                                buttersDollars = oldDollarsInBank > BotEngine.buttersTotalDollarsInTheBank ? $"🔽 ({plusOrMinusDollars})" : oldDollarsInBank == BotEngine.buttersTotalDollarsInTheBank ? "⏺️ (0)" : $"🔼 (+{plusOrMinusDollars})";
                                buttersMiddle = oldMiddle > middleUsersBalance ? $"🔽 ({plusOrMinusMiddle.ToString("0.00")})" : oldMiddle == middleUsersBalance ? "⏺️ (0)" : $"🔼 (+{plusOrMinusMiddle.ToString("0.00")})";

                                resultMessage = TranslationManager.GetTranslation(data.User.Lang, "currencyInfo", data.ChannelID)
                                    .Replace("%totalAmount%", BotEngine.buttersTotalAmount.ToString() + " " + buttersAmountProgress)
                                    .Replace("%totalUsers%", BotEngine.buttersTotalUsers.ToString() + " " + buttersUsers)
                                    .Replace("%midleBalance%", (BotEngine.buttersTotalAmount / BotEngine.buttersTotalUsers).ToString("0.00") + " " + buttersMiddle)
                                    .Replace("%buterCost%", (BotEngine.buttersTotalDollarsInTheBank / BotEngine.buttersTotalAmount).ToString("0.00") + "$ " + buttersCostProgress)
                                    .Replace("%buterDollars%", BotEngine.buttersTotalDollarsInTheBank + "$ " + buttersDollars);
                                resultIdSafe = false;
                            }
                        }
                        else if (is_mod)
                        {
                            if (argument1 == "ban")
                            {
                                if (args.Count > 1)
                                {
                                    var argument2 = args.ElementAt(1).ToLower().Replace("@", "").Replace(",", "");
                                    var BanChannelID = NamesUtil.GetUserID(argument2);
                                    var BanReason = data.ArgsAsString.Replace(args.ElementAt(0), "").Replace(args.ElementAt(1), "").Replace("  ", "");
                                    if (BanChannelID == "err")
                                    {
                                        resultMessage = TranslationManager.GetTranslation(data.User.Lang, "noneExistUser", data.ChannelID)
                                            .Replace("%user%", argument2);
                                        resultColor = Color.Red;
                                        resultIdSafe = false;
                                    }
                                    else
                                    {
                                        if (is_dev)
                                        {
                                            UsersData.UserSaveData(BanChannelID, "isBanned", true);
                                            UsersData.UserSaveData(BanChannelID, "banReason", BanReason);
                                            resultMessage = TranslationManager.GetTranslation(data.User.Lang, "userBanned", data.ChannelID)
                                                .Replace("%user%", argument2)
                                                .Replace("%reason%", BanReason);
                                        }
                                        else
                                        {
                                            if (!UsersData.UserGetData<bool>(BanChannelID, "isBotModerator") && !UsersData.UserGetData<bool>(BanChannelID, "isBotDev"))
                                            {
                                                UsersData.UserSaveData(BanChannelID, "isBanned", true);
                                                UsersData.UserSaveData(BanChannelID, "banReason", BanReason);
                                                resultMessage = TranslationManager.GetTranslation(data.User.Lang, "userBanned", data.ChannelID)
                                                    .Replace("%user%", argument2)
                                                    .Replace("%reason%", BanReason);
                                            }
                                            else
                                            {
                                                resultMessage = TranslationManager.GetTranslation(data.User.Lang, "noAccess", data.ChannelID);
                                                resultColor = Color.Red;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    resultMessage = TranslationManager.GetTranslation(data.User.Lang, "lowArgs", data.ChannelID)
                                        .Replace("%commandWorks%", "#bot ban (channel) (reason)");
                                    resultColor = Color.Red;
                                }
                            }
                            else if (argument1 == "pardon")
                            {
                                if (args.Count > 1)
                                {
                                    var argument2 = args.ElementAt(1).ToLower().Replace("@", "").Replace(",", "");
                                    var BanChannelID = NamesUtil.GetUserID(argument2);
                                    if (BanChannelID == "err")
                                    {
                                        resultMessage = TranslationManager.GetTranslation(data.User.Lang, "noneExistUser", data.ChannelID)
                                            .Replace("%user%", argument2);
                                        resultColor = Color.Red;
                                        resultIdSafe = false;
                                    }
                                    else
                                    {
                                        if (is_dev)
                                        {
                                            UsersData.UserSaveData(BanChannelID, "isBanned", false);
                                            resultMessage = TranslationManager.GetTranslation(data.User.Lang, "userPardon", data.ChannelID)
                                                .Replace("%user%", argument2);
                                        }
                                        else
                                        {
                                            if (!UsersData.UserGetData<bool>(BanChannelID, "isBotModerator") && !UsersData.UserGetData<bool>(BanChannelID, "isBotDev"))
                                            {
                                                UsersData.UserSaveData(BanChannelID, "isBanned", false);
                                                resultMessage = TranslationManager.GetTranslation(data.User.Lang, "userPardon", data.ChannelID)
                                                    .Replace("%user%", argument2);
                                            }
                                            else
                                            {
                                                resultMessage = TranslationManager.GetTranslation(data.User.Lang, "noAccess", data.ChannelID);
                                                resultColor = Color.Red;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    resultMessage = TranslationManager.GetTranslation(data.User.Lang, "lowArgs", data.ChannelID)
                                        .Replace("%commandWorks%", "#bot pardon (channel)");
                                }
                            }
                            else if (argument1 == "rejoinchannel" && data.Platform == Platforms.Twitch)
                            {
                                try
                                {
                                    if (TextUtil.FilterTextWithoutSpaces(user) != "")
                                    {
                                        JoinedChannel userRecon = new(user);
                                        if (Bot.client.JoinedChannels.ToList().Contains(userRecon))
                                        {
                                            Bot.client.LeaveChannel(user);
                                        }
                                        Thread.Sleep(1000);
                                        Bot.client.JoinChannel(user);
                                        resultMessage = TranslationManager.GetTranslation(data.User.Lang, "rejoinedChannel", data.ChannelID);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    ConsoleUtil.ErrorOccured(ex.Message, $"(NOTFATAL)Command\\Bot\\Index\\Rejoin#{user}");
                                }
                            }
                            else if (argument1 == "addchannel" && data.Platform == Platforms.Twitch)
                            {
                                try
                                {
                                    if (TextUtil.FilterTextWithoutSpaces(user) != "")
                                    {
                                        var userID = NamesUtil.GetUserID(user);
                                        if (userID != "err")
                                        {
                                            List<string> channels = DataManager.GetData<List<string>>(Bot.SettingsPath, "channels");
                                            channels.Add(userID);
                                            string[] output = channels.ToArray();
                                            DataManager.SaveData(Bot.SettingsPath, "channels", output);
                                            Bot.client.JoinChannel(user);
                                            ChatUtil.SendMsgReply(Channel, RoomId, TranslationManager.GetTranslation(data.User.Lang, "addedChannel", data.ChannelID).Replace("%user%", user), MsgId, data.User.Lang, true);
                                            Thread.Sleep(1000);
                                            CommandUtil.ChangeNicknameColorAsync(ChatColorPresets.DodgerBlue);
                                            ChatUtil.SendMessage(user, TranslationManager.GetTranslation(data.User.Lang, "welcomeChannel", data.ChannelID).Replace("%version%", BotEngine.botVersion), RoomId, MsgId, data.User.Lang, true);
                                        }
                                        else
                                        {
                                            ChatUtil.SendMsgReply(Channel, RoomId, TranslationManager.GetTranslation(data.User.Lang, "noneExistUser", data.ChannelID).Replace("%user%", user), MsgId, data.User.Lang, true);
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    ConsoleUtil.ErrorOccured(ex.Message, $"(NOTFATAL)Command\\Bot\\Index\\Join#{user}");
                                }
                            }
                            else if (argument1 == "delchannel" && data.Platform == Platforms.Twitch)
                            {
                                try
                                {
                                    if (TextUtil.FilterTextWithoutSpaces(user) != "")
                                    {
                                        var userID = NamesUtil.GetUserID(user);
                                        if (userID != "err")
                                        {
                                            List<string> channels = DataManager.GetData<List<string>>(Bot.SettingsPath, "channels");
                                            channels.Remove(userID);
                                            string[] output = channels.ToArray();
                                            DataManager.SaveData(Bot.SettingsPath, "channels", output);
                                            Bot.client.LeaveChannel(user);
                                            ChatUtil.SendMsgReply(Channel, RoomId, TranslationManager.GetTranslation(data.User.Lang, "delChannel", data.ChannelID).Replace("%user%", user), MsgId, data.User.Lang, true);
                                        }
                                        else
                                        {
                                            ChatUtil.SendMsgReply(Channel, RoomId, TranslationManager.GetTranslation(data.User.Lang, "noneExistUser", data.ChannelID).Replace("%user%", user), MsgId, data.User.Lang, true);
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    ConsoleUtil.ErrorOccured(ex.Message, $"(NOTFATAL)Command\\Bot\\Index\\Rejoin#{user}");
                                }
                            }
                            else if (argument1 == "joinchannel" && data.Platform == Platforms.Twitch)
                            {
                                try
                                {
                                    if (TextUtil.FilterTextWithoutSpaces(user) != "")
                                    {
                                        Bot.client.JoinChannel(user);
                                        ChatUtil.SendMsgReply(Channel, RoomId, TranslationManager.GetTranslation(data.User.Lang, "joinedChannel", data.ChannelID), MsgId, data.User.Lang, true);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    ConsoleUtil.ErrorOccured(ex.Message, $"(NOTFATAL)Command\\Bot\\Index\\Join#{user}");
                                }
                            }
                            else if (argument1 == "leavechannel" && data.Platform == Platforms.Twitch)
                            {
                                try
                                {
                                    if (TextUtil.FilterTextWithoutSpaces(user) != "")
                                    {
                                        Bot.client.LeaveChannel(user);
                                        ChatUtil.SendMsgReply(Channel, RoomId, TranslationManager.GetTranslation(data.User.Lang, "leavedChannel", data.ChannelID), MsgId, data.User.Lang, true);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    ConsoleUtil.ErrorOccured(ex.Message, $"(NOTFATAL)Command\\Bot\\Index\\Leave#{user}");
                                }
                            }
                            else if (is_dev)
                            {
                                if (argument1 == "modadd")
                                {
                                    try
                                    {
                                        var userID = NamesUtil.GetUserID(user);
                                        if (userID != "err")
                                        {
                                            UsersData.UserSaveData(userID, "isBotModerator", true);
                                            resultMessage = TranslationManager.GetTranslation(data.User.Lang, "modAdded", data.ChannelID)
                                                .Replace("%user%", user);
                                        }
                                        else
                                        {
                                            resultMessage = TranslationManager.GetTranslation(data.User.Lang, "noneExistUser", data.ChannelID)
                                                .Replace("%user%", user);
                                            resultColor = Color.Red;
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        ConsoleUtil.ErrorOccured(ex.Message, $"(NOTFATAL)Command\\Bot\\Index\\ModAdd#{user}");
                                    }
                                }
                                else if (argument1 == "demod")
                                {
                                    try
                                    {
                                        var userID = NamesUtil.GetUserID(user);
                                        if (userID != "err")
                                        {
                                            UsersData.UserSaveData(userID, "isBotModerator", false);
                                            resultMessage = TranslationManager.GetTranslation(data.User.Lang, "modDel", data.ChannelID)
                                                .Replace("%user%", user);
                                        }
                                        else
                                        {
                                            resultMessage = TranslationManager.GetTranslation(data.User.Lang, "noneExistUser", data.ChannelID)
                                                .Replace("%user%", user);
                                            resultColor = Color.Red;
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        ConsoleUtil.ErrorOccured(ex.Message, $"(NOTFATAL)Command\\Bot\\Index\\ModDelete#{user}");
                                    }
                                }
                                else if (updateTranslaion.Contains(argument1))
                                {
                                    TranslationManager.UpdateTranslation("ru", data.ChannelID);
                                    TranslationManager.UpdateTranslation("en", data.ChannelID);
                                    resultMessage = "MrDestructoid 👍 Го-тово!";
                                }
                            }
                        }
                    }
                    else
                    {
                        resultMessage = TranslationManager.GetTranslation(data.User.Lang, "botInfo", data.ChannelID);
                    }

                    ChatColorPresets resultNicknameColor = ChatColorPresets.YellowGreen;

                    if (resultColor == Color.Red)
                    {
                        resultNicknameColor = ChatColorPresets.Red;
                    }

                    return new()
                    {
                        Message = resultMessage,
                        IsSafeExecute = true,
                        Description = "",
                        Author = "",
                        ImageURL = "",
                        ThumbnailUrl = "",
                        Footer = "",
                        IsEmbed = true,
                        Ephemeral = false,
                        Title = resultMessageTitle,
                        Color = resultColor,
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
