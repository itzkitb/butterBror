using butterBror.Core.Bot;
using butterBror.Models;
using butterBror.Utils;
using TwitchLib.Client.Enums;

namespace butterBror.Core.Commands.List
{
    public class FrogGame : CommandBase
    {
        public override string Name => "Frogs";
        public override string Author => "ItzKITb";
        public override string AuthorsGithub => "https://github.com/itzkitb";
        public override string GithubSource => $"{URLs.githubSource}blob/master/butterBror/Core/Commands/List/FrogGame.cs";
        public override Version Version => new("1.0.1");
        public override Dictionary<string, string> Description => new() {
            { "ru-RU", "Мини-игра по коллекционированию лягушек." },
            { "en-US", "Minigame about collecting frogs." }
        };
        public override string WikiLink => "https://itzkitb.lol/bot/command?q=frogs";
        public override int CooldownPerUser => 10;
        public override int CooldownPerChannel => 1;
        public override string[] Aliases => ["frog", "frg", "лягушка", "лягушки", "лягуш"];
        public override string HelpArguments => "[(info)/(statistic)/(caught)/(gift [user] [frogs])]";
        public override DateTime CreationDate => DateTime.Parse("13/10/2024");
        public override bool OnlyBotModerator => false;
        public override bool OnlyBotDeveloper => false;
        public override bool OnlyChannelModerator => false;
        public override PlatformsEnum[] Platforms => [PlatformsEnum.Twitch, PlatformsEnum.Telegram, PlatformsEnum.Discord];
        public override bool IsAsync => false;

        public override CommandReturn Execute(CommandData data)
        {
            CommandReturn commandReturn = new CommandReturn();

            try
            {
                long balance = Convert.ToInt64(butterBror.Bot.SQL.Games.GetData("Frogs", data.Platform, DataConversion.ToLong(data.User.ID), "Frogs"));
                long gifted = Convert.ToInt64(butterBror.Bot.SQL.Games.GetData("Frogs", data.Platform, DataConversion.ToLong(data.User.ID), "Gifted"));
                long received = Convert.ToInt64(butterBror.Bot.SQL.Games.GetData("Frogs", data.Platform, DataConversion.ToLong(data.User.ID), "Received"));
                string[] info_aliases = ["info", "i"];
                string[] statistic_aliases = ["statistic", "stat", "s"];
                string[] caught_aliases = ["caught", "c"];
                string[] gift_aliases = ["gift", "g"];
                string[] top_aliases = ["top", "t"];

                if (data.Arguments != null && data.Arguments.Count > 0)
                {
                    if (info_aliases.Contains(data.Arguments[0].ToLower()))
                    {
                        commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "command:frog:info", data.ChannelId, data.Platform));
                    }
                    else if (statistic_aliases.Contains(data.Arguments[0].ToLower()))
                    {
                        commandReturn.SetMessage(LocalizationService.GetString(
                            data.User.Language,
                            "command:frog:statistic",
                            data.ChannelId,
                            data.Platform,
                            LocalizationService.GetPluralString(data.User.Language, "text:frog", data.ChannelId, data.Platform, balance, balance),
                            LocalizationService.GetPluralString(data.User.Language, "text:frog", data.ChannelId, data.Platform, gifted, gifted),
                            LocalizationService.GetPluralString(data.User.Language, "text:frog", data.ChannelId, data.Platform, received, received)));
                    }
                    else if (caught_aliases.Contains(data.Arguments[0].ToLower()))
                    {
                        if (MessageProcessor.CheckCooldown(3600, 0, "FrogsReseter", data.User.ID, data.ChannelId, data.Platform, false, true))
                        {
                            Random rand = new Random();
                            long frog_caught_type = rand.Next(0, 4);
                            long frogs_caughted = rand.Next(1, 11);

                            if (frog_caught_type == 0)
                                frogs_caughted = rand.Next(1, 11);
                            else if (frog_caught_type <= 2)
                                frogs_caughted = rand.Next(10, 101);
                            else if (frog_caught_type == 3)
                                frogs_caughted = rand.Next(100, 1001);

                            string text = GetFrogRange(frogs_caughted);

                            if (text == "error:range")
                            {
                                commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "command:frog:error:range", data.ChannelId, data.Platform));
                                commandReturn.SetColor(ChatColorPresets.Red);
                            }
                            else
                            {
                                long currentBalance = balance + frogs_caughted;
                                commandReturn.SetMessage(LocalizationService.GetString(
                                    data.User.Language,
                                    "command:frog:caught",
                                    data.ChannelId,
                                    data.Platform,
                                    LocalizationService.GetString(data.User.Language, $"command:frog:won:{text}", data.ChannelId, data.Platform),
                                    balance,
                                    currentBalance,
                                    frogs_caughted));

                                butterBror.Bot.SQL.Games.SetData("Frogs", data.Platform, DataConversion.ToLong(data.User.ID), "Gifted", currentBalance);
                            }
                        }
                        else
                        {
                            commandReturn.SetMessage(LocalizationService.GetString(
                                data.User.Language,
                                "command:frog:error:caught",
                                data.ChannelId,
                                data.Platform,
                                TextSanitizer.FormatTimeSpan(MessageProcessor.GetCooldownTime(data.User.ID, "FrogsReseter", 3600, data.Platform), data.User.Language)));
                            commandReturn.SetColor(ChatColorPresets.Red);
                        }
                    }
                    else if (gift_aliases.Contains(data.Arguments[0].ToLower()))
                    {
                        if (data.Arguments.Count > 2)
                        {
                            string username = data.Arguments[1].ToLower();
                            long frogs = Utils.DataConversion.ToLong(data.Arguments[2]);
                            string user_id = UsernameResolver.GetUserID(username, data.Platform);

                            if (user_id == null)
                            {
                                commandReturn.SetMessage(LocalizationService.GetString(
                                    data.User.Language,
                                    "error:user_not_found",
                                    data.ChannelId,
                                    data.Platform,
                                    UsernameResolver.DontPing(username)));
                                commandReturn.SetColor(ChatColorPresets.Red);
                            }
                            else if (frogs == 0)
                            {
                                commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "command:frog:gift:error:zero", data.ChannelId, data.Platform));
                                commandReturn.SetColor(ChatColorPresets.Red);
                            }
                            else if (frogs < 0)
                            {
                                commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "command:frog:gift:error:lowerThanZero", data.ChannelId, data.Platform));
                                commandReturn.SetColor(ChatColorPresets.Red);
                            }
                            else if (balance >= frogs)
                            {
                                commandReturn.SetMessage(LocalizationService.GetString(
                                    data.User.Language,
                                    "command:frog:gift",
                                    data.ChannelId,
                                    data.Platform,
                                    frogs.ToString(),
                                    UsernameResolver.DontPing(username)));

                                long giftUserFrogsBalance = Convert.ToInt64(butterBror.Bot.SQL.Games.GetData("Frogs", data.Platform, DataConversion.ToLong(user_id), "Gifted"));
                                long giftUserReceived = Convert.ToInt64(butterBror.Bot.SQL.Games.GetData("Frogs", data.Platform, DataConversion.ToLong(user_id), "Received"));


                                butterBror.Bot.SQL.Games.SetData("Frogs", data.Platform, DataConversion.ToLong(user_id), "Frogs", giftUserFrogsBalance + frogs);
                                butterBror.Bot.SQL.Games.SetData("Frogs", data.Platform, DataConversion.ToLong(user_id), "Received", giftUserReceived + frogs);
                                butterBror.Bot.SQL.Games.SetData("Frogs", data.Platform, DataConversion.ToLong(data.User.ID), "Frogs", balance - frogs);
                                butterBror.Bot.SQL.Games.SetData("Frogs", data.Platform, DataConversion.ToLong(data.User.ID), "Gifted", gifted + frogs);
                            }
                            else
                            {
                                commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "frog:gift:error:noFrogs", data.ChannelId, data.Platform));
                                commandReturn.SetColor(ChatColorPresets.Red);
                            }
                        }
                        else
                        {
                            commandReturn.SetMessage(LocalizationService.GetString(
                                data.User.Language,
                                "error:not_enough_arguments",
                                data.ChannelId,
                                data.Platform,
                                $"{butterBror.Bot.DefaultExecutor}frog gift [user] [frogs]"));
                            commandReturn.SetColor(ChatColorPresets.Red);
                        }
                    }
                    else
                    {
                        commandReturn.SetMessage(LocalizationService.GetPluralString(data.User.Language, "error:incorrect_parameters", data.ChannelId, data.Platform, data.Arguments.Count)); // Fix AB7
                        commandReturn.SetColor(ChatColorPresets.Red);
                    }
                }
                else
                {
                    commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:not_enough_arguments", data.ChannelId, data.Platform, $"{butterBror.Bot.DefaultExecutor}frog {HelpArguments}"));
                    commandReturn.SetColor(ChatColorPresets.Red);
                }
            }
            catch (Exception e)
            {
                commandReturn.SetError(e);
            }

            return commandReturn;
        }

        string GetFrogRange(long number)
        {
            var ranges = new Dictionary<string, Tuple<long, long>>
            {
                {"1", Tuple.Create(1L, 1L)},
                {"2", Tuple.Create(2L, 2L)},
                {"3", Tuple.Create(3L, 3L)},
                {"4-6", Tuple.Create(4L, 6L)},
                {"7-10", Tuple.Create(7L, 10L)},
                {"26-50", Tuple.Create(10L, 50L)},
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
                    return range.Key;
                }
            }

            return "error:range";
        }
    }
}
