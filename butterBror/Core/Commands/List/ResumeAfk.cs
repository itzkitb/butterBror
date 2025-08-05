﻿using butterBror.Core.Bot;
using butterBror.Core.Bot.SQLColumnNames;
using butterBror.Data;
using butterBror.Models;
using butterBror.Utils;
using System.Globalization;
using TwitchLib.Client.Enums;

namespace butterBror.Core.Commands.List
{
    public class ResumeAfk : CommandBase
    {
        public override string Name => "Rafk";
        public override string Author => "ItzKITb";
        public override string AuthorsGithub => "https://github.com/itzkitb";
        public override string GithubSource => $"{URLs.githubSource}blob/master/butterBror/Core/Commands/List/ResumeAfk.cs";
        public override Version Version => new("1.0.0");
        public override Dictionary<string, string> Description => new() {
            { "ru-RU", "Вернуться в АФК, если вы вышли из него." },
            { "en-US", "Return to AFK if you left it." }
        };
        public override string WikiLink => "https://itzkitb.lol/bot/command?q=rafk";
        public override int CooldownPerUser => 30;
        public override int CooldownPerChannel => 5;
        public override string[] Aliases => ["rafk", "рафк", "вафк", "вернутьафк", "resumeafk"];
        public override string HelpArguments => string.Empty;
        public override DateTime CreationDate => DateTime.Parse("07/07/2024");
        public override bool OnlyBotModerator => false;
        public override bool OnlyBotDeveloper => false;
        public override bool OnlyChannelModerator => false;
        public override PlatformsEnum[] Platforms => [PlatformsEnum.Twitch, PlatformsEnum.Telegram];
        public override bool IsAsync => false;

        public override CommandReturn Execute(CommandData data)
        {
            Engine.Statistics.FunctionsUsed.Add();
            CommandReturn commandReturn = new CommandReturn();

            try
            {
                long AFKResumeTimes = (long)Engine.Bot.SQL.Users.GetParameter(data.Platform, Format.ToLong(data.UserID), Users.AFKResumeTimes);
                DateTime AFKResume = DateTime.Parse((string)Engine.Bot.SQL.Users.GetParameter(data.Platform, Format.ToLong(data.UserID), Users.AFKResume), null, DateTimeStyles.AdjustToUniversal);

                if (AFKResumeTimes <= 5)
                {
                    TimeSpan cache = DateTime.UtcNow - AFKResume;
                    if (cache.TotalMinutes <= 5)
                    {
                        Engine.Bot.SQL.Users.SetParameter(data.Platform, Format.ToLong(data.UserID), Users.IsAFK, 1);
                        Engine.Bot.SQL.Users.SetParameter(data.Platform, Format.ToLong(data.UserID), Users.AFKResumeTimes, AFKResumeTimes + 1);
                        commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "command:rafk", data.ChannelId, data.Platform));
                        commandReturn.SetColor(ChatColorPresets.YellowGreen);
                    }
                    else
                    {
                        commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:afk_resume_after_5_minutes", data.ChannelId, data.Platform));
                    }
                }
                else
                {
                    commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:afk_resume", data.ChannelId, data.Platform));
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
