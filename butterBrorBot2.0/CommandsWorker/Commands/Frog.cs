using butterBror.Utils;
using butterBror.Utils.DataManagers;
using butterBib;
using Discord;
using TwitchLib.Client.Enums;

namespace butterBror
{
    public partial class Commands
    {
        public class FrogGame
        {
            public static CommandInfo Info = new()
            {
                Name = "Frogs",
                Author = "@ItzKITb",
                AuthorURL = "twitch.tv/itzkitb",
                AuthorImageURL = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                Description = "🐸☕ Небольшая игра про лягушек!",
                UseURL = "https://itzkitb.ru/bot/command?name=frogs",
                UserCooldown = 10,
                GlobalCooldown = 1,
                aliases = ["frog", "frg", "лягушка", "лягушки", "лягуш"],
                ArgsRequired = "[(info/i)/(statistic/stat/s)/(caught/c)/(gift/g [user] [frogs])]",
                ResetCooldownIfItHasNotReachedZero = true,
                CreationDate = DateTime.Parse("13/10/2024"),
                ForAdmins = false,
                ForBotCreator = false,
                ForChannelAdmins = false
            };
            public static CommandReturn Index(CommandData data)
            {
                try
                {
                    string resultMessage = "";
                    Color resultColor = Color.Green;
                    string resultMessageTitle = TranslationManager.GetTranslation(data.User.Lang, "dsFLTitle", data.ChannelID);
                    ChatColorPresets resultNicknameColor = ChatColorPresets.YellowGreen;
                    string rootPath = Bot.MainPath + "GAMES_DATA\\FROGS\\";
                    string userRootPath = Bot.MainPath + $"GAMES_DATA\\FROGS\\{data.UserUUID}.json";
                    FileUtil.CreateDirectory(rootPath);
                    FileUtil.CreateFile(userRootPath);
                    if (File.ReadAllText(userRootPath) == "")
                    {
                        File.WriteAllText(userRootPath, "{\r\n\t\"frogs\": 0,\r\n\t\"gifted\": 0,\r\n\t\"received\": 0\r\n}"); // Пока-что заглушка
                    }
                    JsonManager jsonManager = new JsonManager(userRootPath);
                    dynamic? frogsBalanceCache = jsonManager.GetData<dynamic>("frogs");
                    dynamic? frogsGiftedCache = jsonManager.GetData<dynamic>("gifted");
                    dynamic? frogsReceivedCache = jsonManager.GetData<dynamic>("received");
                    long frogsBalance = 0;
                    long frogsGifted = 0;
                    long frogsReceived = 0;
                    string[] infoAliases = ["info", "i"];
                    string[] statisticAliases = ["statistic", "stat", "s"];
                    string[] caughtAliases = ["caught", "c"];
                    string[] giftAliases = ["gift", "g"];
                    if (frogsBalanceCache != null && frogsGiftedCache != null && frogsReceivedCache != null)
                    {
                        frogsBalance = FormatUtil.ToLong(frogsBalanceCache.ToString());
                        frogsGifted = FormatUtil.ToLong(frogsGiftedCache.ToString());
                        frogsReceived = FormatUtil.ToLong(frogsReceivedCache.ToString());
                    }
                    if (data.args.Count > 0)
                    {
                        if (infoAliases.Contains(data.args[0].ToLower()))
                        {
                            resultMessage = TranslationManager.GetTranslation(data.User.Lang, "frog:info", data.ChannelID);
                        }
                        else if (statisticAliases.Contains(data.args[0].ToLower()))
                        {
                            resultMessage = TranslationManager.GetTranslation(data.User.Lang, "frog:statistic", data.ChannelID)
                                .Replace("%frogs%", frogsBalance.ToString())
                                .Replace("%frogs_gifted%", frogsGifted.ToString())
                                .Replace("%frogs_received%", frogsReceived.ToString());
                        }
                        else if (caughtAliases.Contains(data.args[0].ToLower()))
                        {
                            if (CommandUtil.IsNotOnCooldown(3600, 0, "FrogsReset", data.UserUUID, data.ChannelID, false, true, true))
                            {
                                Random rand = new Random();
                                long frogCaughtType = rand.Next(0, 4);
                                long frogsCaughted = rand.Next(1, 11);

                                if (frogCaughtType == 0)
                                {
                                    frogsCaughted = rand.Next(1, 11);
                                }
                                else if (frogCaughtType <= 2)
                                {
                                    frogsCaughted = rand.Next(11, 101);
                                }
                                else if (frogCaughtType == 3)
                                {
                                    frogsCaughted = rand.Next(101, 1001);
                                }

                                string caughtText = GetFrogRange(frogsCaughted);

                                if (caughtText == "error:range")
                                {
                                    resultMessage = TranslationManager.GetTranslation(data.User.Lang, "frog:error:range", data.ChannelID);
                                    resultNicknameColor = ChatColorPresets.Red;
                                }
                                else
                                {
                                    resultMessage = TranslationManager.GetTranslation(data.User.Lang, "frog:caught", data.ChannelID)
                                        .Replace("%action%", TranslationManager.GetTranslation(data.User.Lang, $"frog:{caughtText}", data.ChannelID))
                                        .Replace("%old_frogs_count%", frogsBalance.ToString())
                                        .Replace("%new_frogs_count%", (frogsBalance + frogsCaughted).ToString())
                                        .Replace("%added_count%", frogsCaughted.ToString());
                                    jsonManager.SaveData("frogs", (long)(frogsBalance + frogsCaughted));
                                }
                            }
                            else
                            {
                                resultMessage = TranslationManager.GetTranslation(data.User.Lang, "frog:error:caught", data.ChannelID);
                                resultNicknameColor = ChatColorPresets.Red;
                            }
                        }
                        else if (giftAliases.Contains(data.args[0].ToLower()))
                        {
                            if (data.args.Count > 2)
                            {
                                string username = data.args[1].ToLower();
                                long frogs = FormatUtil.ToLong(data.args[2]);
                                string userID = NamesUtil.GetUserID(username);

                                if (userID == "err")
                                {
                                    resultMessage = TranslationManager.GetTranslation(data.User.Lang, "noneExistUser", data.ChannelID)
                                        .Replace("%user%", NamesUtil.DontPingUsername(username));
                                    resultNicknameColor = ChatColorPresets.Red;
                                }
                                else if (frogs == 0)
                                {
                                    resultMessage = TranslationManager.GetTranslation(data.User.Lang, "frog:gift:error:zero", data.ChannelID);
                                    resultNicknameColor = ChatColorPresets.Red;
                                }
                                else if (frogs < 0)
                                {
                                    resultMessage = TranslationManager.GetTranslation(data.User.Lang, "frog:gift:error:lowerThanZero", data.ChannelID);
                                    resultNicknameColor = ChatColorPresets.Red;
                                }
                                else if (frogsBalance >= frogs)
                                {
                                    resultMessage = TranslationManager.GetTranslation(data.User.Lang, "frog:gift", data.ChannelID)
                                        .Replace("%frogs%", frogs.ToString())
                                        .Replace("%gift_user%", NamesUtil.DontPingUsername(username));
                                    string giftUserRootPath = Bot.MainPath + $"GAMES_DATA\\FROGS\\{userID}.json";
                                    FileUtil.CreateFile(giftUserRootPath);
                                    if (File.ReadAllText(giftUserRootPath) == "")
                                    {
                                        File.WriteAllText(giftUserRootPath, "{\r\n\t\"frogs\": 0,\r\n\t\"gifted\": 0,\r\n\t\"received\": 0\r\n}"); // Пока-что заглушка
                                    }
                                    JsonManager giftUserJsonManager = new JsonManager(giftUserRootPath);

                                    dynamic? giftUserFrogsBalanceCache = giftUserJsonManager.GetData<dynamic>("frogs");
                                    dynamic? giftUserFrogsReceivedCache = giftUserJsonManager.GetData<dynamic>("received");

                                    long giftUserFrogsBalance = 0;
                                    long giftUserReceived = 0;

                                    if (giftUserFrogsBalanceCache != null && giftUserFrogsReceivedCache != null)
                                    {
                                        giftUserFrogsBalance = FormatUtil.ToLong(giftUserFrogsBalanceCache.ToString());
                                        giftUserReceived = FormatUtil.ToLong(giftUserFrogsReceivedCache.ToString());
                                    }

                                    giftUserJsonManager.SaveData("frogs", giftUserFrogsBalance + frogs);
                                    giftUserJsonManager.SaveData("received", giftUserReceived + frogs);

                                    jsonManager.SaveData("frogs", frogsBalance - frogs);
                                    jsonManager.SaveData("gifted", frogsGifted + frogs);
                                }
                                else
                                {
                                    resultMessage = TranslationManager.GetTranslation(data.User.Lang, "frog:gift:error:noFrogs", data.ChannelID);
                                    resultNicknameColor = ChatColorPresets.Red;
                                }
                            }
                            else
                            {
                                resultMessage = TranslationManager.GetTranslation(data.User.Lang, "lowArgs", data.ChannelID)
                                    .Replace("%commandWorks%", "#frog gift [user] [frogs]");
                                resultNicknameColor = ChatColorPresets.Red;
                            }
                        }
                    }
                    else
                    {
                        resultMessage = TranslationManager.GetTranslation(data.User.Lang, "lowArgs", data.ChannelID)
                                    .Replace("%commandWorks%", $"#frog {Info.ArgsRequired}");
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
                    return new ()
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

            static string GetFrogRange(long number)
            {
                var ranges = new Dictionary<string, Tuple<long, long>>
                    {
                        {"1", Tuple.Create(1L, 1L)},
                        {"2", Tuple.Create(2L, 2L)},
                        {"3", Tuple.Create(3L, 3L)},
                        {"4-6", Tuple.Create(4L, 6L)},
                        {"7-10", Tuple.Create(7L, 10L)},
                        {"11", Tuple.Create(11L, 11L)},
                        {"12-25", Tuple.Create(12L, 25L)},
                        {"26-50", Tuple.Create(26L, 50L)},
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
                        return "won:" + range.Key;
                    }
                }

                return "error:range";
            }
        }
    }
}
