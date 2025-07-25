﻿using butterBror.Utils;
using butterBror.Data;
using butterBror.Models;
using butterBror.Core.Bot;
using TwitchLib.Client.Enums;
using static butterBror.Core.Bot.Console;

namespace butterBror.Core.Commands.List
{
    public class Tuck : CommandBase
    {
        public override string Name => "Tuck";
        public override string Author => "ItzKITb";
        public override string AuthorsGithub => "https://github.com/itzkitb";
        public override string GithubSource => $"{URLs.githubSource}blob/master/butterBror/Core/Commands/List/Tuck.cs";
        public override Version Version => new("1.0.0");
        public override Dictionary<string, string> Description => new()
        {
            { "ru", "Спокойной ночи... 👁" },
            { "en", "Good night... 👁" }
        };
        public override string WikiLink => "https://itzkitb.lol/bot/command?q=tuck";
        public override int CooldownPerUser => 5;
        public override int CooldownPerChannel => 1;
        public override string[] Aliases => ["tuck", "уложить", "tk", "улож", "тык"];
        public override string HelpArguments => "(name) (text)";
        public override DateTime CreationDate => DateTime.Parse("07/04/2024");
        public override bool OnlyBotModerator => false;
        public override bool OnlyBotDeveloper => false;
        public override bool OnlyChannelModerator => false;
        public override PlatformsEnum[] Platforms => [PlatformsEnum.Twitch, PlatformsEnum.Telegram, PlatformsEnum.Discord];
        public override bool IsAsync => false;

        [ConsoleSector("butterBror.Commands.Tuck", "Index")]
        public override CommandReturn Execute(CommandData data)
        {
            Engine.Statistics.FunctionsUsed.Add();
            CommandReturn commandReturn = new CommandReturn();

            try
            {
                commandReturn.SetColor(ChatColorPresets.HotPink);
                if (data.Arguments.Count >= 1)
                {
                    var username = Text.UsernameFilter(Text.CleanAsciiWithoutSpaces(data.Arguments[0]));
                    var isSelectedUserIsNotIgnored = true;
                    var userID = Names.GetUserID(username.ToLower(), PlatformsEnum.Twitch);
                    try
                    {
                        if (userID != null)
                            isSelectedUserIsNotIgnored = !UsersData.Get<bool>(userID, "isIgnored", data.Platform);
                    }
                    catch (Exception) { }
                    if (username.ToLower() == Engine.Bot.BotName.ToLower())
                    {
                        commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:tuck:bot", data.ChannelID, data.Platform));
                        commandReturn.SetColor(ChatColorPresets.CadetBlue);
                    }
                    else if (isSelectedUserIsNotIgnored)
                    {
                        if (data.Arguments.Count >= 2)
                        {
                            List<string> list = data.Arguments;
                            list.RemoveAt(0);
                            commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:tuck:text", data.ChannelID, data.Platform).Replace("%user%", Names.DontPing(username)).Replace("%text%", string.Join(" ", list)));
                        }
                        else
                        {
                            commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:tuck", data.ChannelID, data.Platform).Replace("%user%", Names.DontPing(username)));
                        }
                    }
                    else
                    {
                        Write($"User @{data.User.Name} tried to put a user to sleep who is in the ignore list", "info");
                        commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:user_ignored", data.ChannelID, data.Platform));
                    }
                }
                else
                {
                    commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:tuck:none", data.ChannelID, data.Platform));
                    commandReturn.SetColor(ChatColorPresets.CadetBlue);
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
