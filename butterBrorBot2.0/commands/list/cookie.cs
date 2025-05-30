using butterBror.Utils;
using butterBror;
using Discord;
using TwitchLib.Client.Enums;
using System.Threading.Tasks;
using butterBror.Utils.DataManagers;
using DankDB;

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
                Engine.Statistics.functions_used.Add();
                CommandReturn commandReturn = new CommandReturn();

                try
                {
                    string root_path = Maintenance.path_main + $"GAMES_DATA/{Platform.strings[(int)data.platform]}/COOKIES/";
                    FileUtil.CreateDirectory(root_path);

                    string[] giftAliases = ["gift", "g", "подарить", "подарок"];
                    string[] statsAliases = ["stats", "statistic", "statistics", "стат", "статистика"];
                    string[] topAliases = ["top", "leader", "leaderboard", "топ"];
                    string[] buyAliases = ["buy", "get", "купить", "получить"];

                    string[] eatersAliases = ["eaters", "eat", "поедатели", "едаки"];
                    string[] giftersAliases = ["gifters", "gifter", "gift", "дарители", "подарок", "дарить"];
                    string[] recipientsAliases = ["recipients", "recipient", "recip", "получатели", "получить"];

                    if (data.arguments is not null && data.arguments.Count > 0)
                    {
                        if (statsAliases.Contains(data.arguments[0].ToLower()))
                        {
                            string targetUser = data.user.username;
                            if (data.arguments.Count > 1) targetUser = data.arguments[1];

                            string targetUserId = Names.GetUserID(targetUser, data.platform);
                            if (targetUserId == null)
                            {
                                commandReturn.SetMessage(TranslationManager.GetTranslation(data.user.language, "error:user_not_found", data.channel_id, data.platform, new() { { "user", targetUser } }));
                                return commandReturn;
                            }

                            int eaten = UsersData.Get<int>(targetUserId, "cookie_eaten", data.platform);
                            int gifted = UsersData.Get<int>(targetUserId, "cookie_gifted", data.platform);
                            int received = UsersData.Get<int>(targetUserId, "cookie_received", data.platform);

                            string statsMessage = TranslationManager.GetTranslation(data.user.language, "command:cookie:statistics", data.channel_id, data.platform)
                                .Replace("%user%", targetUser)
                                .Replace("%eaten%", eaten.ToString())
                                .Replace("%gifted%", gifted.ToString())
                                .Replace("%received%", received.ToString());

                            commandReturn.SetMessage(statsMessage);
                            return commandReturn;
                        }
                        else if (giftAliases.Contains(data.arguments[0].ToLower()))
                        {
                            if (data.arguments.Count < 2 || data.arguments.IndexOf("gift") >= data.arguments.Count - 1)
                            {
                                commandReturn.SetMessage(TranslationManager.GetTranslation(data.user.language, "error:not_enough_arguments", data.channel_id, data.platform)
                                    .Replace("%command_example%", $"{Maintenance.executor}cookie gift username"));
                                return commandReturn;
                            }

                            DateTime gifterLastUse = UsersData.Get<DateTime>(data.user.id, "cookie_last_used", data.platform);
                            if (gifterLastUse.Date == DateTime.UtcNow.Date)
                            {
                                commandReturn.SetMessage(TranslationManager.GetTranslation(data.user.language, "error:cookie_gift_cooldown", data.channel_id, data.platform));
                                return commandReturn;
                            }

                            string targetUser = data.arguments[1];
                            string targetUserId = Names.GetUserID(targetUser, data.platform);

                            if (targetUserId == null)
                            {
                                commandReturn.SetMessage(TranslationManager.GetTranslation(data.user.language, "error:user_not_found", data.channel_id, data.platform, new() { { "user", targetUser } }));
                                return commandReturn;
                            }

                            DateTime recipientLastUse = UsersData.Get<DateTime>(targetUserId, "cookie_last_used", data.platform);
                            if (recipientLastUse.Date != DateTime.UtcNow.Date)
                            {
                                commandReturn.SetMessage(TranslationManager.GetTranslation(data.user.language, "error:cookie_already_has", data.channel_id, data.platform)
                                    .Replace("%user%", targetUser));
                                return commandReturn;
                            }

                            UsersData.Save(data.user.id, "cookie_gifted", (UsersData.Get<int>(data.user.id, "cookie_gifted", data.platform)) + 1, data.platform);
                            UsersData.Save(targetUserId, "cookie_received", (UsersData.Get<int>(targetUserId, "cookie_received", data.platform)) + 1, data.platform);

                            var topPathLocal = $"{root_path}/TOP.json";
                            var topGifters = Manager.Get<Dictionary<string, int>>(topPathLocal, "leaderboard_gifters") ?? new Dictionary<string, int>();
                            var topRecipients = Manager.Get<Dictionary<string, int>>(topPathLocal, "leaderboard_recipients") ?? new Dictionary<string, int>();

                            if (!topGifters.ContainsKey(data.user.id))
                                topGifters[data.user.id] = 0;

                            topGifters[data.user.id]++;

                            if (!topRecipients.ContainsKey(targetUserId))
                                topRecipients[targetUserId] = 0;

                            topRecipients[targetUserId]++;

                            Manager.Save(topPathLocal, "leaderboard_gifters", topGifters);
                            Manager.Save(topPathLocal, "leaderboard_recipients", topRecipients);

                            commandReturn.SetMessage(TranslationManager.GetTranslation(data.user.language, "command:cookie:gift", data.channel_id, data.platform)
                                .Replace("%sender%", data.user.username)
                                .Replace("%receiver%", targetUser));
                            return commandReturn;
                        }
                        else if (topAliases.Contains(data.arguments[0].ToLower()))
                        {
                            string topType = "eaters";
                            if (data.arguments.Count > 2)
                            {
                                if (giftAliases.Contains(data.arguments[2].ToLower()))
                                    topType = "gifters";
                                else if (recipientsAliases.Contains(data.arguments[2].ToLower()))
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
                                if (fullSortedList[i].Key == data.user.id)
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
                                    string username = Names.GetUsername(user.Key, data.platform);
                                    topLines[i] = $"{i + 1}. {Names.DontPing(username)} - {user.Value}";
                                }
                                else
                                {
                                    topLines[i] = $"{i + 1}. Empty";
                                }
                            }

                            string topTypeTranslation = TranslationManager.GetTranslation(
                                data.user.language,
                                $"text:{topType}",
                                data.channel_id,
                                data.platform
                            );

                            string topMessage = TranslationManager.GetTranslation(
                                data.user.language,
                                "command:cookie:top",
                                data.channel_id,
                                data.platform
                            )
                            .Replace("%type%", topTypeTranslation)
                            .Replace("%list%", string.Join(", ", topLines))
                            .Replace("%user_position%", userPosition);

                            commandReturn.SetMessage(topMessage);
                            return commandReturn;
                        }
                        else if (buyAliases.Contains(data.arguments[0].ToLower()))
                        {
                            DateTime lastUse = UsersData.Get<DateTime>(data.user.id, "cookie_last_used", data.platform);
                            if (lastUse.Date != DateTime.UtcNow.Date)
                            {
                                commandReturn.SetMessage(TranslationManager.GetTranslation(data.user.language, "error:user_already_has_cookie", data.channel_id, data.platform));
                                return commandReturn;
                            }

                            float currency = Engine.BankDollars / Engine.Coins;
                            float cost = 0.2f / currency;

                            int coins = -(int)cost;
                            int subcoins = -(int)((cost - coins) * 100);

                            if (Utils.Balance.GetBalance(data.user_id, data.platform) + Utils.Balance.GetSubbalance(data.user_id, data.platform) / 100f >= coins + subcoins / 100f)
                            {
                                Utils.Balance.Add(data.user_id, coins, subcoins, data.platform);
                                UsersData.Save(data.user.id, "cookie_last_used", DateTime.UtcNow.AddDays(-1), data.platform);
                                commandReturn.SetMessage(TranslationManager.GetTranslation(data.user.language, "command:cookie:buyed", data.channel_id, data.platform));
                            }
                            else
                            {
                                commandReturn.SetMessage(TranslationManager.GetTranslation(data.user.language, "error:not_enough_coins", data.channel_id, data.platform, new() { { "coins", coins + "." + subcoins } }));
                            }
                            return commandReturn;
                        }
                    }

                    DateTime lastUsed = UsersData.Get<DateTime>(data.user.id, "cookie_last_used", data.platform);
                    if (lastUsed.Date == DateTime.UtcNow.Date)
                    {
                        commandReturn.SetMessage(TranslationManager.GetTranslation(data.user.language, "error:cookie_cooldown", data.channel_id, data.platform));
                        return commandReturn;
                    }

                    string[] result = await Utils.API.AI.Request(
                        "Create a horoscope in an absurdly humorous style, where everyday situations are intertwined with fantastic and provocative advice. Start with phrases like 'The stars demand', 'The moon approves', 'The planets insist' or 'Astrological forecasts warn'. Use unexpected verbs (e.g. 'smear', 'steal', 'tame'), turn everyday objects into magical tools (the refrigerator as a portal to a parallel world, slippers as a means of teleportation), add elements of black humor and sarcasm. Include hyperbolic consequences (e.g. 'you will be arrested for singing too loudly in the shower') and absurd recommendations (e.g. 'feed your neighbors your breakfast to avoid cosmic karma'). Keep the tone light and playful, avoiding real advice or logic. Examples: 'Today you can't wash the dishes without the help of three black cats', 'Take a bottle of yogurt with you on the subway - it will protect you from energy vampires', 'Donate a toothbrush to the altar of random passers-by'. In your answer, write ONLY a prediction of 3-4 sentences, no need to break it up into different zodiac signs. ANSWER IN THE LANGUAGE OF THE USER!",
                        null, data.platform, data.user.username, data.user.id, data.user.language
                    );

                    if (result[0] == "ERR")
                    {
                        commandReturn.SetMessage(TranslationManager.GetTranslation(data.user.language, "error:AI_error", data.channel_id, data.platform, new() { { "reason", result[1] } }));
                        commandReturn.SetColor(ChatColorPresets.Red);
                        return commandReturn;
                    }

                    UsersData.Save(data.user.id, "cookie_eaten", (UsersData.Get<int>(data.user.id, "cookie_eaten", data.platform)) + 1, data.platform);
                    UsersData.Save(data.user.id, "cookie_last_used", DateTime.UtcNow, data.platform);

                    var topPath = $"{root_path}/TOP.json";
                    var top = Manager.Get<Dictionary<string, int>>(topPath, "leaderboard") ?? new Dictionary<string, int>();

                    if (!top.ContainsKey(data.user.id))
                        top[data.user.id] = 0;

                    top[data.user.id]++;
                    Manager.Save(topPath, "leaderboard_eaters", top);

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