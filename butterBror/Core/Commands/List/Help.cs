using butterBror.Models;
using butterBror.Utils;
using butterBror.Core.Bot;

namespace butterBror.Core.Commands.List
{
    public class Help : CommandBase
    {
        public override string Name => "Help";
        public override string Author => "ItzKITb";
        public override string AuthorsGithub => "https://github.com/itzkitb";
        public override string GithubSource => $"{URLs.githubSource}blob/master/butterBror/Core/Commands/List/Help.cs";
        public override Version Version => new("1.0.0");
        public override Dictionary<string, string> Description => new() {
            { "ru", "Вы только что получили информацию об этой команде, используя эту же команду." },
            { "en", "You just got information about this command using this same command." }
        };
        public override string WikiLink => "https://itzkitb.ru/bot/command?name=help";
        public override int CooldownPerUser => 120;
        public override int CooldownPerChannel => 10;
        public override string[] Aliases => ["help", "помощь", "hlp"];
        public override string HelpArguments => "(command name)";
        public override DateTime CreationDate => DateTime.Parse("09/12/2024");
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
                if (data.Arguments.Count == 1)
                {
                    string classToFind = data.Arguments[0];
                    commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:help:not_found", data.ChannelID, data.Platform));

                    foreach (var command in Runner.commandInstances)
                    {
                        if (command.Aliases.Contains(classToFind))
                        {
                            string aliasesList = "";
                            int num = 0;
                            int numWithoutComma = 5;
                            if (command.Aliases.Length < 5)
                                numWithoutComma = command.Aliases.Length;

                            foreach (string alias in command.Aliases)
                            {
                                num++;
                                if (num < numWithoutComma)
                                    aliasesList += $"{Engine.Bot.Executor}{alias}, ";
                                else if (num == numWithoutComma)
                                    aliasesList += $"{Engine.Bot.Executor}{alias}";
                            }
                            commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:help", data.ChannelID, data.Platform)
                                .Replace("%commandName%", command.Name)
                                .Replace("%Variables%", aliasesList)
                                .Replace("%Args%", command.HelpArguments)
                                .Replace("%Link%", command.WikiLink)
                                .Replace("%Description%", command.Description[data.User.Language])
                                .Replace("%Author%", Names.DontPing(command.Author))
                                .Replace("%creationDate%", command.CreationDate.ToShortDateString())
                                .Replace("%uCooldown%", command.CooldownPerUser.ToString())
                                .Replace("%gCooldown%", command.CooldownPerChannel.ToString()));

                            break;
                        }
                    }
                }
                else if (data.Arguments.Count > 1)
                    commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:a_few_arguments", data.ChannelID, data.Platform).Replace("%args%", "(command_name)"));
                else
                    commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "text:bot_info", data.ChannelID, data.Platform));
            }
            catch (Exception e)
            {
                commandReturn.SetError(e);
            }

            return commandReturn;
        }
    }
}
