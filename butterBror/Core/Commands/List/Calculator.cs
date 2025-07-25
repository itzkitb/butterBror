﻿using butterBror.Models;
using butterBror.Utils;
using butterBror.Core.Bot;
using System.Data;
using TwitchLib.Client.Enums;

namespace butterBror.Core.Commands.List
{
    public class Calculator : CommandBase
    {
        public override string Name => "Calculator";
        public override string Author => "ItzKITb";
        public override string AuthorsGithub => "https://github.com/itzkitb";
        public override string GithubSource => $"{URLs.githubSource}blob/master/butterBror/Core/Commands/List/Calculator.cs";
        public override Version Version => new("1.0.0");
        public override Dictionary<string, string> Description => new(){
            { "ru", "Получить ответ на арифметическое выражение." },
            { "en", "Get the answer to the arithmetic expression." }
        };
        public override string WikiLink => "https://itzkitb.lol/bot/command?q=math";
        public override int CooldownPerUser => 15;
        public override int CooldownPerChannel => 5;
        public override string[] Aliases => ["calc", "calculate", "кальк", "math", "матем", "математика", "калькулятор"];
        public override string HelpArguments => "[2+2]";
        public override DateTime CreationDate => DateTime.Parse("07/04/2024");
        public override bool OnlyBotModerator => false;
        public override bool OnlyBotDeveloper => false;
        public override bool OnlyChannelModerator => false;
        public override PlatformsEnum[] Platforms => [PlatformsEnum.Twitch, PlatformsEnum.Telegram, PlatformsEnum.Discord];
        public override bool IsAsync => false;

        public override CommandReturn Execute(CommandData data)
        {
            Engine.Statistics.FunctionsUsed.Add();
            CommandReturn commandReturn = new CommandReturn();

            try
            {
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

                    commandReturn.SetMessage(Text.ArgumentReplacement(TranslationManager.GetTranslation(data.User.Language, "command:calculator:result", data.ChannelID, data.Platform), "result", mathResult.ToString()));
                }
                catch (DivideByZeroException)
                {
                    commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:divide_by_zero", data.ChannelID, data.Platform));
                    commandReturn.SetColor(ChatColorPresets.Red);
                }
                catch (Exception)
                {
                    commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:invalid_mathematical_expression", data.ChannelID, data.Platform));
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

