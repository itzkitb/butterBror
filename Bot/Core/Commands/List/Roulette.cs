using bb.Utils;
using bb.Core.Configuration;
using TwitchLib.Client.Enums;
using bb.Models.Command;
using bb.Models.Platform;

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
        public override int CooldownPerUser => 10;
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
                if (data.ChannelId == null)
                {
                    commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:unknown", string.Empty, data.Platform));
                    return commandReturn;
                }

                Dictionary<string, List<string>> aliases = new(){
                    { "red", ["red", "r", "красный", "красное", "к", "🔴", "🟥", "❤️"] },
                    { "green", ["green", "g", "зеленый", "зеленое", "з", "🟢", "🟩", "💚"] },
                    { "black", ["black", "b", "черный", "черное", "ч", "⚫", "⬛", "◼️", "◾", "🖤"] }
                };
                string[] selections = ["red", "green", "black"];
                Dictionary<string, int[]> selectionsNumbers = new()
                    {
                        { "red", new int[]{ 32, 19, 21, 25, 34, 27, 36, 30, 23, 5, 16, 1, 14, 9, 18, 7, 12, 3 } },
                        { "black", new int[]{ 15, 4, 2, 17, 6, 13, 11, 8, 10, 24, 33, 20, 31, 22, 29, 28, 35, 26 } },
                        { "green", new int[]{ 0, 36 } }
                    };
                Dictionary<string, double> multipliers = new()
                    {
                        { "red", 1.5 },
                        { "black", 1.5 },
                        { "green", 2 },
                    };

                if (data.Arguments != null && data.Arguments.Count > 1)
                {
                    string? selected = null;
                    foreach (KeyValuePair<string, List<string>> alias in aliases)
                    {
                        if (alias.Value.Contains(data.Arguments[0]))
                        {
                            selected = alias.Key;
                            break;
                        }
                    }

                    if (selected != null)
                    {
                        int bid = DataConversion.ToInt(data.Arguments[1]);
                        commandReturn.SetColor(ChatColorPresets.Red);

                        if (bid == 0)
                            commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:roulette_wrong_bid", data.ChannelId, data.Platform));
                        else if (bid < 0)
                            commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:roulette_steal", data.ChannelId, data.Platform));
                        else if (bb.Program.BotInstance.Currency.GetBalance(data.User.Id, data.Platform) < bid)
                            commandReturn.SetMessage(LocalizationService.GetString(
                                data.User.Language,
                                "error:roulette_not_enough_coins",
                                data.ChannelId,
                                data.Platform,
                                bb.Program.BotInstance.Currency.GetBalance(data.User.Id, data.Platform).ToString()));
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
                                bb.Program.BotInstance.Currency.Add(data.User.Id, win, 0, data.Platform);
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
                                bb.Program.BotInstance.Currency.Add(data.User.Id, -bid, 0, data.Platform);
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
                        $"{bb.Program.BotInstance.DefaultCommandPrefix}roulette [red/green/black] [{LocalizationService.GetString(data.User.Language, "text:bid", data.ChannelId, data.Platform)}]"));
            }
            catch (Exception e)
            {
                commandReturn.SetError(e);
            }

            return commandReturn;
        }
    }
}