using bb.Utils;
using bb.Core.Configuration;
using bb.Models.Command;
using bb.Models.Platform;
using bb.Models.Users;

namespace bb.Core.Commands.List.Utility
{
    public class HelpCommand : CommandBase
    {
        public override string Name => "Help";
        public override string Author => "https://github.com/itzkitb";
        public override string Source => "Utility/Help.cs";
        public override Dictionary<Language, string> Description => new() {
            { Language.RuRu, "Вы только что получили информацию об этой команде, используя эту же команду." },
            { Language.EnUs, "You just got information about this command using this same command." }
        };
        public override int UserCooldown => 10;
        public override int Cooldown => 5;
        public override string[] Aliases => ["help", "помощь", "hlp"];
        public override string Help => "[command]";
        public override DateTime CreationDate => DateTime.Parse("2024-09-12T00:00:00.0000000Z");
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

                if (data.Arguments != null && data.Arguments.Count == 1)
                {
                    string classToFind = data.Arguments[0];
                    commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "command:help:not_found", data.ChannelId, data.Platform));

                    foreach (var command in Program.BotInstance.CommandRunner.commandInstances)
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
                                    aliasesList += $"{Program.BotInstance.DefaultCommandPrefix}{alias}, ";
                                else if (num == numWithoutComma)
                                    aliasesList += $"{Program.BotInstance.DefaultCommandPrefix}{alias}";
                            }
                            commandReturn.SetMessage(LocalizationService.GetString(
                                data.User.Language,
                                "command:help",
                                data.ChannelId,
                                data.Platform,
                                command.Name,
                                aliasesList,
                                command.Help == string.Empty ? "[no_arguments]" : command.Help,
                                command.Description[data.User.Language],
                                command.Cooldown,
                                command.UserCooldown,
                                URLs.wikiSource + command.Name.ToLower()));

                            break;
                        }
                    }
                }
                else if (data.Arguments != null && data.Arguments.Count > 1)
                    commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:a_few_arguments", data.ChannelId, data.Platform, "[command_name]"));
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
