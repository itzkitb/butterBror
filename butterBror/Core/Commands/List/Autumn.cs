﻿using butterBror.Models;
using butterBror.Utils;
using butterBror.Core.Bot;

namespace butterBror.Core.Commands.List
{
    public class Autumn : CommandBase
    {
        public override string Name => "Autumn";
        public override string Author => "ItzKITb";
        public override string AuthorsGithub => "https://github.com/itzkitb";
        public override string GithubSource => $"{URLs.githubSource}blob/master/butterBror/Core/Commands/List/Autumn.cs";
        public override Version Version => new("1.0.0");
        public override Dictionary<string, string> Description => new() {
            { "ru", "Узнать, сколько времени осталось до начала/конца осени." },
            { "en", "Find out how much time is left until the beginning/end of autumn." }
        };
        public override string WikiLink => "https://itzkitb.lol/bot/command?q=autumn";
        public override int CooldownPerUser => 120;
        public override int CooldownPerChannel => 10;
        public override string[] Aliases => ["autumn", "a", "осень", "fall"];
        public override string HelpArguments => string.Empty;
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
                DateTime startDate = new(2000, 9, 1);
                DateTime endDate = new(2000, 12, 1);
                commandReturn.SetMessage(Text.TimeTo(startDate, endDate, "autumn", 0, data.User.Language, data.ArgumentsString, data.ChannelID, data.Platform));
            }
            catch (Exception e)
            {
                commandReturn.SetError(e);
            }

            return commandReturn;
        }
    }
}