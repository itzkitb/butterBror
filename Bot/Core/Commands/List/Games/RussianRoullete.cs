using bb.Core.Configuration;
using bb.Models.Command;
using bb.Models.Platform;
using bb.Models.Users;
using bb.Utils;
using TwitchLib.Client.Enums;

namespace bb.Core.Commands.List.Games
{
    public class RussianRoullete : CommandBase
    {
        public override string Name => "RussianRoullete";
        public override string Author => "https://github.com/itzkitb";
        public override string Source => "Games/RussianRoullete.cs";
        public override Dictionary<Language, string> Description => new() {
            { Language.RuRu, "Mike Klubnika <3" },
            { Language.EnUs, "Mike Klubnika <3" }
        };
        public override int UserCooldown => 10;
        public override int Cooldown => 1;
        public override string[] Aliases => ["rr", "russianroullete", "русскаярулетка", "рр", "br", "buckshotroullete"];
        public override string Help => string.Empty;
        public override DateTime CreationDate => DateTime.Parse("2024-08-08T00:00:00.0000000Z");
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

                int win = new System.Random().Next(1, 3);
                int page2 = new System.Random().Next(1, 5);
                string translationParam = "command:russian_roullete:";
                if (Program.BotInstance.Currency.Get(data.User.Id, data.Platform) > 4)
                {
                    if (win == 1)
                    {
                        // WIN
                        translationParam += "win:" + page2;
                        Program.BotInstance.Currency.Add(data.User.Id, 1, data.Platform);
                    }
                    else
                    {
                        // GAME OVER
                        translationParam += "over:" + page2;
                        if (page2 == 4)
                        {
                            Program.BotInstance.Currency.Add(data.User.Id, -1, data.Platform);
                        }
                        else
                        {
                            Program.BotInstance.Currency.Add(data.User.Id, -5, data.Platform);
                        }
                        commandReturn.SetColor(ChatColorPresets.Red);
                    }
                    commandReturn.SetMessage("🔫 " + LocalizationService.GetString(data.User.Language, translationParam, data.ChannelId, data.Platform));
                }
                else
                {
                    commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:roulette_not_enough_coins", data.ChannelId, data.Platform, Program.BotInstance.Currency.Get(data.User.Id, data.Platform)));
                }
            }
            catch (Exception e)
            {
                commandReturn.SetError(e);
            }

            return commandReturn;
        }
    }
}
