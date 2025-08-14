using butterBror.Core.Bot;
using butterBror.Models;
using butterBror.Utils;

namespace butterBror.Core.Commands.List
{
    public class Help : CommandBase
    {
        public override string Name => "Help";
        public override string Author => "ItzKITb";
        public override string AuthorsGithub => "https://github.com/itzkitb";
        public override string GithubSource => $"{URLs.githubSource}blob/master/butterBror/Core/Commands/List/Help.cs";
        public override Version Version => new("1.0.1");
        public override Dictionary<string, string> Description => new() {
            { "ru-RU", "Вы только что получили информацию об этой команде, используя эту же команду." },
            { "en-US", "You just got information about this command using this same command." }
        };
        public override string WikiLink => "https://itzkitb.lol/bot/command?q=help";
        public override int CooldownPerUser => 10;
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
            CommandReturn commandReturn = new CommandReturn();

            try
            {
                if (data.Arguments.Count == 1)
                {
                    string classToFind = data.Arguments[0];
                    commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "command:help:not_found", data.ChannelId, data.Platform));

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
                                    aliasesList += $"{butterBror.Bot.DefaultExecutor}{alias}, ";
                                else if (num == numWithoutComma)
                                    aliasesList += $"{butterBror.Bot.DefaultExecutor}{alias}";
                            }
                            commandReturn.SetMessage(LocalizationService.GetString(
                                data.User.Language,
                                "command:help",
                                data.ChannelId,
                                data.Platform,
                                command.Name,
                                aliasesList,
                                command.HelpArguments,
                                command.Description[data.User.Language],
                                command.CooldownPerChannel,
                                command.CooldownPerUser,
                                command.WikiLink));

                            break;
                        }
                    }
                }
                else if (data.Arguments.Count > 1)
                    commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:a_few_arguments", data.ChannelId, data.Platform).Replace("%args%", "(command_name)"));
                else
                    commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "text:bot_info", data.ChannelId, data.Platform));
            }
            catch (Exception e)
            {
                commandReturn.SetError(e);
            }

            return commandReturn;
        }
    }
}
