using bb.Core.Bot;
using bb.Core.Bot.SQLColumnNames;
using bb.Models;
using bb.Utils;
using System.Globalization;
using TwitchLib.Client.Enums;

namespace bb.Core.Commands.List
{
    public class ResumeAfk : CommandBase
    {
        public override string Name => "Rafk";
        public override string Author => "ItzKITb";
        public override string AuthorsGithub => "https://github.com/itzkitb";
        public override string GithubSource => $"{URLs.githubSource}blob/master/butterBror/Core/Commands/List/ResumeAfk.cs";
        public override Version Version => new("1.0.1");
        public override Dictionary<string, string> Description => new() {
            { "ru-RU", "Вернуться в АФК, если вы вышли из него." },
            { "en-US", "Return to AFK if you left it." }
        };
        public override string WikiLink => "https://itzkitb.lol/bot/command?q=rafk";
        public override int CooldownPerUser => 10;
        public override int CooldownPerChannel => 1;
        public override string[] Aliases => ["rafk", "рафк", "вафк", "вернутьафк", "resumeafk"];
        public override string HelpArguments => string.Empty;
        public override DateTime CreationDate => DateTime.Parse("2024-07-07T00:00:00.0000000Z");
        public override bool OnlyBotModerator => false;
        public override bool OnlyBotDeveloper => false;
        public override bool OnlyChannelModerator => false;
        public override PlatformsEnum[] Platforms => [PlatformsEnum.Twitch, PlatformsEnum.Telegram];
        public override bool IsAsync => false;

        public override CommandReturn Execute(CommandData data)
        {
            CommandReturn commandReturn = new CommandReturn();

            try
            {
                if (bb.Bot.UsersBuffer == null || data.ChannelId == null)
                {
                    commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:unknown", string.Empty, data.Platform));
                    return commandReturn;
                }

                long AFKResumeTimes = Convert.ToInt64(bb.Bot.UsersBuffer.GetParameter(data.Platform, DataConversion.ToLong(data.User.Id), Users.AFKResumeTimes));
                DateTime AFKResume = DateTime.Parse((string)bb.Bot.UsersBuffer.GetParameter(data.Platform, DataConversion.ToLong(data.User.Id), Users.AFKResume), null, DateTimeStyles.AdjustToUniversal);

                if (AFKResumeTimes <= 5)
                {
                    TimeSpan cache = DateTime.UtcNow - AFKResume;
                    if (cache.TotalMinutes <= 5)
                    {
                        bb.Bot.UsersBuffer.SetParameter(data.Platform, DataConversion.ToLong(data.User.Id), Users.IsAFK, 1);
                        bb.Bot.UsersBuffer.SetParameter(data.Platform, DataConversion.ToLong(data.User.Id), Users.AFKResumeTimes, AFKResumeTimes + 1);
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
