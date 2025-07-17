using butterBror.Utils;
using butterBror.Data;
using butterBror.Models;
using butterBror.Core.Bot;
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
            { "ru", "Вернуться в АФК, если вы вышли из него." },
            { "en", "Return to AFK if you left it." }
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
                if (UsersData.Contains(data.UserID, "fromAfkResumeTimes", data.Platform) && UsersData.Contains(data.UserID, "lastFromAfkResume", data.Platform))
                {
                    var resumeTimes = UsersData.Get<int>(data.UserID, "fromAfkResumeTimes", data.Platform);
                    if (resumeTimes <= 5)
                    {
                        DateTime lastResume = UsersData.Get<DateTime>(data.UserID, "lastFromAfkResume", data.Platform);
                        TimeSpan cache = DateTime.UtcNow - lastResume;
                        if (cache.TotalMinutes <= 5)
                        {
                            UsersData.Save(data.UserID, "isAfk", true, data.Platform);
                            UsersData.Save(data.UserID, "fromAfkResumeTimes", resumeTimes + 1, data.Platform);
                            commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:rafk", data.ChannelID, data.Platform));
                            commandReturn.SetColor(ChatColorPresets.YellowGreen);
                        }
                        else
                        {
                            commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:afk_resume_after_5_minutes", data.ChannelID, data.Platform));
                        }
                    }
                    else
                    {
                        commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:afk_resume", data.ChannelID, data.Platform));
                    }
                }
                else
                {
                    commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:no_afk", data.ChannelID, data.Platform));
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
