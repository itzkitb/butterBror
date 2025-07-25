﻿using butterBror.Models;
using butterBror.Utils;
using butterBror.Core.Bot;

namespace butterBror.Core.Commands.List
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
            { "ru", "Эта команда... Просто зачем-то существует" },
            { "en", "This command... Just exists for some reason" }
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
            Engine.Statistics.FunctionsUsed.Add();
            CommandReturn commandReturn = new CommandReturn();

            try
            {
                if (Text.CleanAsciiWithoutSpaces(data.ArgumentsString) != "")
                {
                    string[] blockedEntries = ["/", "$", "#", "+", "-", ">", "<", "*", "\\", ";"];
                    string meMessage = Text.CleanAscii(data.ArgumentsString);
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

                        foreach (string blockedEntry in blockedEntries)
                        {
                            if (meMessage.StartsWith(blockedEntry))
                            {
                                meMessage = string.Join("", meMessage.Skip(blockedEntry.Length)); // AB6 fix
                                break;
                            }
                        }

                        break; // AB5 fix
                    }
                    commandReturn.SetMessage($"/me \u2063 {meMessage}");
                }
                else
                {
                    commandReturn.SetMessage("/me " + TranslationManager.GetTranslation(data.User.Language, "text:ad", data.ChannelID, data.Platform));
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