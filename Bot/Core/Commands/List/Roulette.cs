using bb.Core.Bot;
using bb.Models;
using bb.Utils;
using TwitchLib.Client.Enums;

namespace bb.Core.Commands.List
{
    public class Roulette : CommandBase
    {
        public override string Name => "Roulette";
        public override string Author => "ItzKITb";
        public override string AuthorsGithub => "https://github.com/itzkitb";
        public override string GithubSource => $"{URLs.githubSource}blob/master/butterBror/Core/Commands/List/Roulette.cs";
        public override Version Version => new("1.0.1");
        public override Dictionary<string, string> Description => new() {
            { "ru-RU", "Сыграйте в рулетку! Подробности: https://bit.ly/bb_roulette " },
            { "en-US", "Play Roulette! Details: https://bit.ly/bb_roulette " }
        };
        public override string WikiLink => "https://itzkitb.lol/bot/command?q=roulette";
        public override int CooldownPerUser => 5;
        public override int CooldownPerChannel => 1;
        public override string[] Aliases => ["roulette", "r", "рулетка", "р"];
        public override string HelpArguments => "[select: \"🟩\", \"🟥\", \"⬛\"] [bid]";
        public override DateTime CreationDate => DateTime.Parse("2025-01-29T00:00:00.0000000Z");
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
                string[] selections = ["🟥", "🟩", "⬛"];
                Dictionary<string, int[]> selectionsNumbers = new()
                    {
                        { "🟥", new int[]{ 32, 19, 21, 25, 34, 27, 36, 30, 23, 5, 16, 1, 14, 9, 18, 7, 12, 3 } },
                        { "⬛", new int[]{ 15, 4, 2, 17, 6, 13, 11, 8, 10, 24, 33, 20, 31, 22, 29, 28, 35, 26 } },
                        { "🟩", new int[]{ 0, 36 } }
                    };
                Dictionary<string, double> multipliers = new()
                    {
                        { "🟥", 1.5 },
                        { "⬛", 1.5 },
                        { "🟩", 2 },
                    };

                if (data.Arguments.Count > 1)
                {
                    if (selections.Contains(data.Arguments[0]))
                    {
                        int bid = DataConversion.ToInt(data.Arguments[1]);
                        commandReturn.SetColor(ChatColorPresets.Red);

                        if (bid == 0)
                            commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:roulette_wrong_bid", data.ChannelId, data.Platform));
                        else if (bid < 0)
                            commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:roulette_steal", data.ChannelId, data.Platform));
                        else if (Utils.CurrencyManager.GetBalance(data.User.ID, data.Platform) < bid)
                            commandReturn.SetMessage(LocalizationService.GetString(
                                data.User.Language,
                                "error:roulette_not_enough_coins",
                                data.ChannelId,
                                data.Platform,
                                Utils.CurrencyManager.GetBalance(data.User.ID, data.Platform).ToString()));
                        else
                        {
                            int moves = new Random().Next(38, 380);
                            string result_symbol = "";
                            int result = moves % 38;

                            foreach (var item in selectionsNumbers)
                            {
                                if (item.Value.Contains(result))
                                    result_symbol = item.Key;
                            }

                            if (result_symbol.Equals(data.Arguments[0]))
                            {
                                commandReturn.SetColor(ChatColorPresets.YellowGreen);

                                int win = (int)(bid * multipliers[result_symbol]);
                                Utils.CurrencyManager.Add(data.User.ID, win, 0, data.Platform);
                                commandReturn.SetMessage(LocalizationService.GetString(
                                    data.User.Language,
                                    "command:roulette:result:win",
                                    data.ChannelId,
                                    data.Platform,
                                    result_symbol,
                                    result.ToString(),
                                    win.ToString(),
                                    multipliers[result_symbol].ToString()));
                            }
                            else
                            {
                                Utils.CurrencyManager.Add(data.User.ID, -bid, 0, data.Platform);
                                commandReturn.SetMessage(LocalizationService.GetString(
                                    data.User.Language,
                                    "command:roulette:result:lose",
                                    data.ChannelId,
                                    data.Platform,
                                    result_symbol,
                                    result.ToString(),
                                    bid.ToString()));
                            }
                        }
                    }
                    else
                        commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:roulette_wrong_select", data.ChannelId, data.Platform));
                }
                else
                    commandReturn.SetMessage(LocalizationService.GetString(
                        data.User.Language,
                        "error:not_enough_arguments",
                        data.ChannelId,
                        data.Platform,
                        $"{bb.Bot.DefaultCommandPrefix}roulette [🟩/🟥/⬛] [{LocalizationService.GetString(data.User.Language, "text:bid", data.ChannelId, data.Platform)}]"));
            }
            catch (Exception e)
            {
                commandReturn.SetError(e);
            }

            return commandReturn;
        }
    }
}