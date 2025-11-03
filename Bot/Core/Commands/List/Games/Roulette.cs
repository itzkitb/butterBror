using bb.Utils;
using bb.Core.Configuration;
using TwitchLib.Client.Enums;
using bb.Models.Command;
using bb.Models.Platform;
using bb.Models.Users;

namespace bb.Core.Commands.List.Games
{
    public class Roulette : CommandBase
    {
        public override string Name => "Roulette";
        public override string Author => "https://github.com/itzkitb";
        public override string Source => "Games/Roulette.cs";
        public override Dictionary<Language, string> Description => new() {
            { Language.RuRu, "Сыграйте в рулетку! Подробности: https://bit.ly/bb_roulette " },
            { Language.EnUs, "Play Roulette! Details: https://bit.ly/bb_roulette " }
        };
        public override int UserCooldown => 10;
        public override int Cooldown => 1;
        public override string[] Aliases => ["roulette", "r", "рулетка", "р"];
        public override string Help => "<green|red|black> <bid>";
        public override DateTime CreationDate => DateTime.Parse("2025-01-29T00:00:00.0000000Z");
        public override Roles RoleRequired => Roles.Public;
        public override Platform[] Platforms => [Platform.Twitch, Platform.Telegram, Platform.Discord];
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
                        else if (Program.BotInstance.Currency.Get(data.User.Id, data.Platform) < bid)
                            commandReturn.SetMessage(LocalizationService.GetString(
                                data.User.Language,
                                "error:roulette_not_enough_coins",
                                data.ChannelId,
                                data.Platform,
                                Program.BotInstance.Currency.Get(data.User.Id, data.Platform).ToString()));
                        else
                        {
                            int moves = new System.Random().Next(38, 380);
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
                                Program.BotInstance.Currency.Add(data.User.Id, win, data.Platform);
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
                                Program.BotInstance.Currency.Add(data.User.Id, -bid, data.Platform);
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
                        $"{Program.BotInstance.DataBase.Channels.GetCommandPrefix(Platform.Twitch, data.ChatID)}{Aliases[0]} [red/green/black] [{LocalizationService.GetString(data.User.Language, "text:bid", data.ChannelId, data.Platform)}]"));
            }
            catch (Exception e)
            {
                commandReturn.SetError(e);
            }

            return commandReturn;
        }
    }
}