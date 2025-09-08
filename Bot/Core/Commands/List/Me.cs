using bb.Core.Bot;
using bb.Models;
using bb.Utils;

namespace bb.Core.Commands.List
{
    public class Me : CommandBase
    {
        public override string Name => "Me";
        public override string Author => "ItzKITb";
        public override string AuthorsGithub => "https://github.com/itzkitb";
        public override string GithubSource => $"{URLs.githubSource}blob/master/butterBror/Core/Commands/List/Me.cs";
        public override Version Version => new("1.0.0");
        public override Dictionary<string, string> Description => new()
        {
            { "ru-RU", "Эта команда... Просто зачем-то существует" },
            { "en-US", "This command... Just exists for some reason" }
        };
        public override string WikiLink => "https://itzkitb.ru/bot/command?name=me";
        public override int CooldownPerUser => 15;
        public override int CooldownPerChannel => 5;
        public override string[] Aliases => ["me", "m", "я"];
        public override string HelpArguments => "[text]";
        public override DateTime CreationDate => DateTime.Parse("07/04/2024");
        public override bool OnlyBotModerator => false;
        public override bool OnlyBotDeveloper => false;
        public override bool OnlyChannelModerator => false;
        public override PlatformsEnum[] Platforms => [PlatformsEnum.Twitch];
        public override bool IsAsync => false;

        public override CommandReturn Execute(CommandData data)
        {
            CommandReturn commandReturn = new CommandReturn();

            try
            {
                if (TextSanitizer.CleanAsciiWithoutSpaces(data.ArgumentsString) != "")
                {
                    string[] blockedEntries = ["/", "$", "#", "+", "-", ">", "<", "*", "\\", ";"];
                    string meMessage = TextSanitizer.CleanAscii(data.ArgumentsString);
                    while (true)
                    {
                        while (meMessage.StartsWith(' '))
                        {
                            meMessage = string.Join("", meMessage.Skip(1)); // AB6 fix
                        }

                        if (meMessage.StartsWith('!'))
                        {
                            meMessage = "❗" + string.Join("", meMessage.Skip(1)); // AB6 fix
                            break;
                        }

                        bool forbiddenSymbolFound = true;
                        while (forbiddenSymbolFound)
                        {
                            forbiddenSymbolFound = false;
                            foreach (string blockedEntry in blockedEntries)
                            {
                                while (meMessage.StartsWith(blockedEntry))
                                {
                                    forbiddenSymbolFound = true;
                                    meMessage = string.Join("", meMessage.Substring(blockedEntry.Length)); // AB6 fix
                                } // repeat symbol fix
                            }
                        } // repeat symbol fix 2

                        break; // AB5 fix
                    }

                    if (string.IsNullOrEmpty(meMessage))
                    {
                        commandReturn.SetMessage("/me " + LocalizationService.GetString(data.User.Language, "text:ad", data.ChannelId, data.Platform));
                    }
                    else
                    {
                        commandReturn.SetMessage($"/me \u034E {meMessage}");
                    }
                }
                else
                {
                    commandReturn.SetMessage("/me " + LocalizationService.GetString(data.User.Language, "text:ad", data.ChannelId, data.Platform));
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