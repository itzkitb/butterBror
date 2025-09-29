using bb.Utils;
using bb.Core.Configuration;
using System.Data;
using TwitchLib.Client.Enums;
using bb.Models.Command;
using bb.Models.Platform;

namespace bb.Core.Commands.List
{
    public class Calculator : CommandBase
    {
        public override string Name => "Calculator";
        public override string Author => "ItzKITb";
        public override string AuthorsGithub => "https://github.com/itzkitb";
        public override string GithubSource => $"{URLs.githubSource}blob/master/butterBror/Core/Commands/List/Calculator.cs";
        public override Version Version => new("1.0.0");
        public override Dictionary<string, string> Description => new(){
            { "ru-RU", "Получить ответ на арифметическое выражение." },
            { "en-US", "Get the answer to the arithmetic expression." }
        };
        public override string WikiLink => "https://itzkitb.lol/bot/command?q=math";
        public override int CooldownPerUser => 10;
        public override int CooldownPerChannel => 1;
        public override string[] Aliases => ["calc", "calculate", "кальк", "math", "матем", "математика", "калькулятор"];
        public override string HelpArguments => "[2+2]";
        public override DateTime CreationDate => DateTime.Parse("2024-07-04T00:00:00.0000000Z");
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
                    input.Replace(replacement.Key, replacement.Value);
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

