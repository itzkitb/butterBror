using bb.Core.Configuration;
using bb.Models.Command;
using bb.Models.Platform;
using bb.Models.Users;
using bb.Utils;
using System.Data;
using TwitchLib.Client.Enums;

namespace bb.Core.Commands.List.Utility
{
    public class Calculator : CommandBase
    {
        public override string Name => "Calculator";
        public override string Author => "https://github.com/itzkitb";
        public override string Source => "Utility/Calculator.cs";
        public override Dictionary<Language, string> Description => new(){
            { Language.RuRu, "Получить ответ на арифметическое выражение." },
            { Language.EnUs, "Get the answer to the arithmetic expression." }
        };
        public override int UserCooldown => 10;
        public override int Cooldown => 1;
        public override string[] Aliases => ["calc", "calculate", "кальк", "math", "матем", "математика", "калькулятор"];
        public override string Help => "<expression>";
        public override DateTime CreationDate => DateTime.Parse("2024-07-04T00:00:00.0000000Z");
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

                string input = data.ArgumentsString;
                Dictionary<string, string> replacements = new() {
                        { ",", "." },
                        { ":", "/" },
                        { "÷", "/" },
                        { "∙", "*" },
                        { "×", "*" }
                    };
                foreach (var replacement in replacements)
                {
                    input = input.Replace(replacement.Key, replacement.Value);
                }

                try
                {
                    double mathResult = Convert.ToDouble(new DataTable().Compute(input, null));

                    if (double.IsInfinity(mathResult))
                    {
                        throw new DivideByZeroException();
                    }

                    commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "command:calculator:result", data.ChannelId, data.Platform, mathResult.ToString()));
                }
                catch (DivideByZeroException)
                {
                    commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:divide_by_zero", data.ChannelId, data.Platform));
                    commandReturn.SetColor(ChatColorPresets.Red);
                }
                catch (Exception)
                {
                    commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:invalid_mathematical_expression", data.ChannelId, data.Platform));
                    commandReturn.SetColor(ChatColorPresets.Red);
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

