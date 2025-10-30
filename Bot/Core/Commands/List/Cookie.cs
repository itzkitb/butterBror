using bb.Core.Configuration;
using bb.Models.Command;
using bb.Models.Platform;
using bb.Models.Users;
using bb.Services.External;
using bb.Utils;
using System.Globalization;
using TwitchLib.Client.Enums;

namespace bb.Core.Commands.List
{
    public class Cookie : CommandBase
    {
        public override string Name => "Cookie";
        public override string Author => "ItzKITb";
        public override string AuthorsGithub => "https://github.com/itzkitb";
        public override string GithubSource => $"{URLs.githubSource}blob/master/butterBror/Core/Commands/List/Cookie.cs";
        public override Version Version => new("1.0.1");
        public override Dictionary<Language, string> Description => new()
        {
            { Language.RuRu, "Получи абсурдное предсказание на день." },
            { Language.EnUs, "Get an absurd horoscope for the day." }
        };
        public override string WikiLink => "https://itzkitb.lol/bot/command?q=cookie";
        public override int CooldownPerUser => 120;
        public override int CooldownPerChannel => 1;
        public override string[] Aliases => ["cookie", "печенье", "horoscope", "гадание"];
        public override string HelpArguments => "[gift <user>] [stats] [stats <user>]";
        public override DateTime CreationDate => DateTime.Parse("2024-08-15T00:00:00.0000000Z");
        public override bool OnlyBotModerator => false;
        public override bool OnlyBotDeveloper => false;
        public override bool OnlyChannelModerator => false;
        public override Platform[] Platforms => [Platform.Twitch, Platform.Telegram, Platform.Discord];
        public override bool IsAsync => true;

        public override async Task<CommandReturn> ExecuteAsync(CommandData data)
        {
            CommandReturn commandReturn = new CommandReturn();
            commandReturn.SetSafe(true);

            try
            {
                if (data.ChannelId == null || bb.Program.BotInstance.UsersBuffer == null || bb.Program.BotInstance.DataBase == null)
                {
                    commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:unknown", string.Empty, data.Platform));
                    return commandReturn;
                }

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

                        string targetUserId = UsernameResolver.GetUserID(targetUser, data.Platform);
                        if (targetUserId == null)
                        {
                            commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:user_not_found", data.ChannelId, data.Platform, targetUser));
                            commandReturn.SetSafe(false);
                            return commandReturn;
                        }

                        long eaten = Convert.ToInt64(bb.Program.BotInstance.UsersBuffer.GetParameter(data.Platform, DataConversion.ToLong(data.User.Id), Users.EatedCookies));
                        long gifted = Convert.ToInt64(bb.Program.BotInstance.UsersBuffer.GetParameter(data.Platform, DataConversion.ToLong(data.User.Id), Users.GiftedCookies));
                        long received = Convert.ToInt64(bb.Program.BotInstance.UsersBuffer.GetParameter(data.Platform, DataConversion.ToLong(data.User.Id), Users.ReceivedCookies));

                        string statsMessage = LocalizationService.GetString(
                            data.User.Language,
                            "command:cookie:statistics",
                            data.ChannelId,
                            data.Platform,
                            targetUser,
                            eaten,
                            gifted,
                            received
                            );

                        commandReturn.SetMessage(statsMessage);
                        return commandReturn;
                    }
                    else if (giftAliases.Contains(data.Arguments[0].ToLower()))
                    {
                        if (data.Arguments.Count < 2 || data.Arguments.IndexOf("gift") >= data.Arguments.Count - 1)
                        {
                            commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:not_enough_arguments", data.ChannelId, data.Platform, $"{bb.Program.BotInstance.DefaultCommandPrefix}cookie gift username"));
                            return commandReturn;
                        }

                        DateTime gifterLastUse = DateTime.Parse((string)bb.Program.BotInstance.UsersBuffer.GetParameter(data.Platform, DataConversion.ToLong(data.User.Id), Users.LastCookie), null, DateTimeStyles.AdjustToUniversal);
                        if (gifterLastUse.Date == DateTime.UtcNow.Date)
                        {
                            commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:cookie_gift_cooldown", data.ChannelId, data.Platform));
                            return commandReturn;
                        }

                        string targetUser = data.Arguments[1];
                        string targetUserId = UsernameResolver.GetUserID(targetUser, data.Platform);

                        if (targetUserId == null)
                        {
                            commandReturn.SetMessage(LocalizationService.GetString(
                                data.User.Language,
                                "error:user_not_found",
                                data.ChannelId,
                                data.Platform,
                                targetUser));
                            commandReturn.SetSafe(false);
                            return commandReturn;
                        }

                        DateTime recipientLastUse = DateTime.Parse((string)bb.Program.BotInstance.UsersBuffer.GetParameter(data.Platform, DataConversion.ToLong(targetUserId), Users.LastCookie), null, DateTimeStyles.AdjustToUniversal);
                        if (recipientLastUse.Date != DateTime.UtcNow.Date)
                        {
                            commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:cookie_already_has", data.ChannelId, data.Platform, targetUser));
                            commandReturn.SetSafe(false);
                            return commandReturn;
                        }

                        bb.Program.BotInstance.UsersBuffer.SetParameter(data.Platform, DataConversion.ToLong(data.User.Id), Users.GiftedCookies, (int)bb.Program.BotInstance.UsersBuffer.GetParameter(data.Platform, DataConversion.ToLong(data.User.Id), Users.GiftedCookies) + 1);
                        bb.Program.BotInstance.UsersBuffer.SetParameter(data.Platform, DataConversion.ToLong(targetUserId), Users.ReceivedCookies, (int)bb.Program.BotInstance.UsersBuffer.GetParameter(data.Platform, DataConversion.ToLong(targetUserId), Users.ReceivedCookies) + 1);
                        bb.Program.BotInstance.UsersBuffer.SetParameter(data.Platform, DataConversion.ToLong(targetUserId), Users.LastCookie, DateTime.UtcNow.AddDays(-1).ToString("o"));

                        commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "command:cookie:gift", data.ChannelId, data.Platform));
                        return commandReturn;
                    }
                    else if (topAliases.Contains(data.Arguments[0].ToLower()))
                    {
                        string topType = "Eaters";
                        if (data.Arguments.Count >= 2)
                        {
                            if (giftersAliases.Contains(data.Arguments[1].ToLower()))
                                topType = "Gifters";
                            else if (recipientsAliases.Contains(data.Arguments[1].ToLower()))
                                topType = "Recipients";
                        }

                        var leaderboard = bb.Program.BotInstance.DataBase.Games.GetLeaderboard("Cookies", data.Platform, $"{topType}Count");

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
                            if (fullSortedList[i].UserId == DataConversion.ToLong(data.User.Id))
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
                                string username = UsernameResolver.GetUsername(user.UserId.ToString(), data.Platform, true);
                                topLines[i] = $"{i + 1}. {UsernameResolver.Unmention(username)} - {user.Value}";
                            }
                            else
                            {
                                topLines[i] = $"{i + 1}. Empty";
                            }
                        }

                        string topTypeTranslation = LocalizationService.GetString(
                            data.User.Language,
                            $"text:{topType}",
                            data.ChannelId,
                            data.Platform
                        );

                        string topMessage = LocalizationService.GetString(
                            data.User.Language,
                            "command:cookie:top",
                            data.ChannelId,
                            data.Platform,
                            topTypeTranslation,
                            topLines,
                            userPosition
                        );

                        commandReturn.SetMessage(topMessage);
                        return commandReturn;
                    }
                    else if (buyAliases.Contains(data.Arguments[0].ToLower()))
                    {
                        DateTime lastUse = DateTime.Parse((string)bb.Program.BotInstance.UsersBuffer.GetParameter(data.Platform, DataConversion.ToLong(data.User.Id), Users.LastCookie), null, DateTimeStyles.AdjustToUniversal);
                        if (lastUse.Date != DateTime.UtcNow.Date)
                        {
                            commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:user_already_has_cookie", data.ChannelId, data.Platform));
                            return commandReturn;
                        }

                        decimal currency = bb.Program.BotInstance.InBankDollars / bb.Program.BotInstance.Coins;
                        decimal cost = 0.2m / currency;

                        if (bb.Program.BotInstance.Currency.GetBalance(data.User.Id, data.Platform) >= cost)
                        {
                            bb.Program.BotInstance.Currency.Add(data.User.Id, cost, data.Platform);
                            bb.Program.BotInstance.UsersBuffer.SetParameter(data.Platform, DataConversion.ToLong(data.User.Id), Users.LastCookie, DateTime.UtcNow.AddDays(-1).ToString("o"));
                            commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "command:cookie:buyed", data.ChannelId, data.Platform));
                        }
                        else
                        {
                            commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:not_enough_coins", data.ChannelId, data.Platform, cost));
                        }
                        return commandReturn;
                    }
                }

                DateTime lastUsed = DateTime.Parse((string)bb.Program.BotInstance.UsersBuffer.GetParameter(data.Platform, DataConversion.ToLong(data.User.Id), Users.LastCookie), null, DateTimeStyles.AdjustToUniversal);
                if (lastUsed.Date == DateTime.UtcNow.Date)
                {
                    commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:cookie_cooldown", data.ChannelId, data.Platform));
                    return commandReturn;
                }

                string[] result = await bb.Program.BotInstance.AiService.Request(
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
                    data.Platform, null, data.User.Name, data.User.Id, data.User.Language, 2, false, false
                );

                if (result[0] == "ERR")
                {
                    commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:AI_error", data.ChannelId, data.Platform, result[1]));
                    commandReturn.SetColor(ChatColorPresets.Red);
                    return commandReturn;
                }

                bb.Program.BotInstance.UsersBuffer.SetParameter(data.Platform, DataConversion.ToLong(data.User.Id), Users.EatedCookies, (int)bb.Program.BotInstance.UsersBuffer.GetParameter(data.Platform, DataConversion.ToLong(data.User.Id), Users.EatedCookies) + 1);
                bb.Program.BotInstance.UsersBuffer.SetParameter(data.Platform, DataConversion.ToLong(data.User.Id), Users.LastCookie, DateTime.UtcNow.ToString("o"));

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