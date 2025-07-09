using butterBror.Utils;
using Discord;
using TwitchLib.Client.Enums;
using System.Threading.Tasks;
using butterBror.Utils.DataManagers;
using DankDB;
using butterBror.Utils.Tools;
using butterBror.Utils.Types;

namespace butterBror
{
    public partial class Commands
    {
        public class Cookie
        {
            public static CommandInfo Info = new()
            {
                Name = "Cookie",
                Author = "@ItzKITb",
                AuthorLink = "twitch.tv/itzkitb",
                AuthorAvatar = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png ",
                Description = new()
                {
                    { "ru", "Получи абсурдное предсказание на день с возможностью подарить печенье другим!" },
                    { "en", "Get an absurd horoscope for the day with the ability to gift cookies to others!" }
                },
                WikiLink = "https://itzkitb.lol/bot/command?q=cookie ",
                CooldownPerUser = 0,
                CooldownPerChannel = 0,
                Aliases = ["cookie", "печенье", "horoscope", "гадание"],
                Arguments = "[gift <user>] [stats] [stats <user>]",
                CooldownReset = false,
                CreationDate = DateTime.Parse("15/08/2024"),
                IsForBotModerator = false,
                IsForBotDeveloper = false,
                IsForChannelModerator = false,
                Platforms = [Platforms.Twitch, Platforms.Telegram, Platforms.Discord]
            };

            public async Task<CommandReturn> Index(CommandData data)
            {
                Engine.Statistics.FunctionsUsed.Add();
                CommandReturn commandReturn = new CommandReturn();

                try
                {
                    string root_path = Engine.Bot.Pathes.Main + $"GAMES_DATA/{Platform.strings[(int)data.Platform]}/COOKIES/";
                    FileUtil.CreateDirectory(root_path);

                    string[] giftAliases = ["gift", "g", "подарить", "подарок"];
                    string[] statsAliases = ["stats", "statistic", "statistics", "стат", "статистика"];
                    string[] topAliases = ["top", "leader", "leaderboard", "топ"];
                    string[] buyAliases = ["buy", "get", "купить", "получить"];

                    string[] eatersAliases = ["eaters", "eat", "поедатели", "едаки"];
                    string[] giftersAliases = ["gifters", "gifter", "gift", "дарители", "подарок", "дарить"];
                    string[] recipientsAliases = ["recipients", "recipient", "recip", "получатели", "получить"];

                    if (data.Arguments is not null && data.Arguments.Count > 0)
                    {
                        if (statsAliases.Contains(data.Arguments[0].ToLower()))
                        {
                            string targetUser = data.User.Name;
                            if (data.Arguments.Count > 1) targetUser = data.Arguments[1];

                            string targetUserId = Names.GetUserID(targetUser, data.Platform);
                            if (targetUserId == null)
                            {
                                commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:user_not_found", data.ChannelID, data.Platform, new() { { "user", targetUser } }));
                                return commandReturn;
                            }

                            int eaten = UsersData.Get<int>(targetUserId, "cookie_eaten", data.Platform);
                            int gifted = UsersData.Get<int>(targetUserId, "cookie_gifted", data.Platform);
                            int received = UsersData.Get<int>(targetUserId, "cookie_received", data.Platform);

                            string statsMessage = TranslationManager.GetTranslation(data.User.Language, "command:cookie:statistics", data.ChannelID, data.Platform)
                                .Replace("%user%", targetUser)
                                .Replace("%eaten%", eaten.ToString())
                                .Replace("%gifted%", gifted.ToString())
                                .Replace("%received%", received.ToString());

                            commandReturn.SetMessage(statsMessage);
                            return commandReturn;
                        }
                        else if (giftAliases.Contains(data.Arguments[0].ToLower()))
                        {
                            if (data.Arguments.Count < 2 || data.Arguments.IndexOf("gift") >= data.Arguments.Count - 1)
                            {
                                commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:not_enough_arguments", data.ChannelID, data.Platform)
                                    .Replace("%command_example%", $"{Engine.Bot.Executor}cookie gift username"));
                                return commandReturn;
                            }

                            DateTime gifterLastUse = UsersData.Get<DateTime>(data.User.ID, "cookie_last_used", data.Platform);
                            if (gifterLastUse.Date == DateTime.UtcNow.Date)
                            {
                                commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:cookie_gift_cooldown", data.ChannelID, data.Platform));
                                return commandReturn;
                            }

                            string targetUser = data.Arguments[1];
                            string targetUserId = Names.GetUserID(targetUser, data.Platform);

                            if (targetUserId == null)
                            {
                                commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:user_not_found", data.ChannelID, data.Platform, new() { { "user", targetUser } }));
                                return commandReturn;
                            }

                            DateTime recipientLastUse = UsersData.Get<DateTime>(targetUserId, "cookie_last_used", data.Platform);
                            if (recipientLastUse.Date != DateTime.UtcNow.Date)
                            {
                                commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:cookie_already_has", data.ChannelID, data.Platform)
                                    .Replace("%user%", targetUser));
                                return commandReturn;
                            }

                            UsersData.Save(data.User.ID, "cookie_gifted", (UsersData.Get<int>(data.User.ID, "cookie_gifted", data.Platform)) + 1, data.Platform);
                            UsersData.Save(targetUserId, "cookie_received", (UsersData.Get<int>(targetUserId, "cookie_received", data.Platform)) + 1, data.Platform);
                            UsersData.Save(targetUserId, "cookie_last_used", DateTime.UtcNow.AddDays(-1), data.Platform);

                            var topPathLocal = $"{root_path}/TOP.json";
                            var topGifters = Manager.Get<Dictionary<string, int>>(topPathLocal, "leaderboard_gifters") ?? new Dictionary<string, int>();
                            var topRecipients = Manager.Get<Dictionary<string, int>>(topPathLocal, "leaderboard_recipients") ?? new Dictionary<string, int>();

                            if (!topGifters.ContainsKey(data.User.ID))
                                topGifters[data.User.ID] = 0;

                            topGifters[data.User.ID]++;

                            if (!topRecipients.ContainsKey(targetUserId))
                                topRecipients[targetUserId] = 0;

                            topRecipients[targetUserId]++;

                            SafeManager.Save(topPathLocal, "leaderboard_gifters", topGifters, false);
                            SafeManager.Save(topPathLocal, "leaderboard_recipients", topRecipients);

                            commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:cookie:gift", data.ChannelID, data.Platform)
                                .Replace("%sender%", data.User.Name)
                                .Replace("%receiver%", targetUser));
                            return commandReturn;
                        }
                        else if (topAliases.Contains(data.Arguments[0].ToLower()))
                        {
                            string topType = "eaters";
                            if (data.Arguments.Count >= 2)
                            {
                                if (giftersAliases.Contains(data.Arguments[1].ToLower()))
                                    topType = "gifters";
                                else if (recipientsAliases.Contains(data.Arguments[1].ToLower()))
                                    topType = "recipients";
                            }

                            var topPathL = $"{root_path}/TOP.json";

                            Dictionary<string, int> leaderboard = Manager.Get<Dictionary<string, int>>(topPathL, $"leaderboard_{topType}") ?? new Dictionary<string, int>();

                            var sortedList = leaderboard
                                .OrderByDescending(kvp => kvp.Value)
                                .Take(5)
                                .ToList();

                            var fullSortedList = leaderboard
                                .OrderByDescending(kvp => kvp.Value)
                                .ToList();

                            string userPosition = "Empty";
                            int? userRank = null;

                            for (int i = 0; i < fullSortedList.Count; i++)
                            {
                                if (fullSortedList[i].Key == data.User.ID)
                                {
                                    userRank = i + 1;
                                    userPosition = $"{userRank}.";
                                    break;
                                }
                            }

                            string[] topLines = new string[5];
                            for (int i = 0; i < 5; i++)
                            {
                                if (i < sortedList.Count)
                                {
                                    var user = sortedList[i];
                                    string username = Names.GetUsername(user.Key, data.Platform);
                                    topLines[i] = $"{i + 1}. {Names.DontPing(username)} - {user.Value}";
                                }
                                else
                                {
                                    topLines[i] = $"{i + 1}. Empty";
                                }
                            }

                            string topTypeTranslation = TranslationManager.GetTranslation(
                                data.User.Language,
                                $"text:{topType}",
                                data.ChannelID,
                                data.Platform
                            );

                            string topMessage = TranslationManager.GetTranslation(
                                data.User.Language,
                                "command:cookie:top",
                                data.ChannelID,
                                data.Platform
                            )
                            .Replace("%type%", topTypeTranslation)
                            .Replace("%list%", string.Join(", ", topLines))
                            .Replace("%user_position%", userPosition);

                            commandReturn.SetMessage(topMessage);
                            return commandReturn;
                        }
                        else if (buyAliases.Contains(data.Arguments[0].ToLower()))
                        {
                            DateTime lastUse = UsersData.Get<DateTime>(data.User.ID, "cookie_last_used", data.Platform);
                            if (lastUse.Date != DateTime.UtcNow.Date)
                            {
                                commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:user_already_has_cookie", data.ChannelID, data.Platform));
                                return commandReturn;
                            }

                            float currency = Engine.BankDollars / Engine.Coins;
                            float cost = 0.2f / currency;

                            int coins = -(int)cost;
                            int subcoins = -(int)((cost - coins) * 100);

                            if (Utils.Tools.Balance.GetBalance(data.UserID, data.Platform) + Utils.Tools.Balance.GetSubbalance(data.UserID, data.Platform) / 100f >= coins + subcoins / 100f)
                            {
                                Utils.Tools.Balance.Add(data.UserID, coins, subcoins, data.Platform);
                                UsersData.Save(data.User.ID, "cookie_last_used", DateTime.UtcNow.AddDays(-1), data.Platform);
                                commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:cookie:buyed", data.ChannelID, data.Platform));
                            }
                            else
                            {
                                commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:not_enough_coins", data.ChannelID, data.Platform, new() { { "coins", coins + "." + subcoins } }));
                            }
                            return commandReturn;
                        }
                    }

                    DateTime lastUsed = UsersData.Get<DateTime>(data.User.ID, "cookie_last_used", data.Platform);
                    if (lastUsed.Date == DateTime.UtcNow.Date)
                    {
                        commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:cookie_cooldown", data.ChannelID, data.Platform));
                        return commandReturn;
                    }

                    string[] result = await Utils.Tools.API.AI.Request(
                        @"Create original absurdly humorous texts in the style of 'horoscopes', as in the example below. Each text should contain:

1. Advice/warning with an unexpected or ridiculous twist (e.g. 'Avoid people over 20 - they can be aggressive!').
2. Elements of sarcasm, grotesqueness and a mix of unrelated topics (e.g. 'You can turn into a toad with a mustache or start selling urine at the market').
3. Mentioning historical/pop-culture characters in meaningless scenarios.
4. Conversational style with exclamations, jokes and provocative wording.
Requirements:
- Texts should be unique, do not repeat examples from the source code.
- Maintain the structure: 3-5 sentences with abrupt transitions between ideas.
— Add absurd recommendations ('Don't forget to rub your forehead with garlic!') and exaggerated consequences ('You may be accused of witchcraft against tomatoes').

Example (just to understand the style):
'Today, it is recommended to put on a clown costume and go online. If you have problems, hit yourself on the corner of the refrigerator. Attacks by Jedi midwives are possible!'

Generate ONLY 4 sentence variations following these rules. The answer must be in the user's language! In your answer write ONLY text! DO NOT indicate sentence numbers! DO NOT write more than 4 sentences!",
                        null, data.Platform, data.User.Name, data.User.ID, data.User.Language, 2, false
                    );

                    if (result[0] == "ERR")
                    {
                        commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:AI_error", data.ChannelID, data.Platform, new() { { "reason", result[1] } }));
                        commandReturn.SetColor(ChatColorPresets.Red);
                        return commandReturn;
                    }

                    UsersData.Save(data.User.ID, "cookie_eaten", (UsersData.Get<int>(data.User.ID, "cookie_eaten", data.Platform)) + 1, data.Platform);
                    UsersData.Save(data.User.ID, "cookie_last_used", DateTime.UtcNow, data.Platform);

                    var topPath = $"{root_path}/TOP.json";
                    var top = Manager.Get<Dictionary<string, int>>(topPath, "leaderboard_eaters") ?? new Dictionary<string, int>();

                    if (!top.ContainsKey(data.User.ID))
                        top[data.User.ID] = 0;

                    top[data.User.ID]++;
                    SafeManager.Save(topPath, "leaderboard_eaters", top);

                    string horoscope = "🍪 " + result[1];
                    commandReturn.SetMessage(horoscope);
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