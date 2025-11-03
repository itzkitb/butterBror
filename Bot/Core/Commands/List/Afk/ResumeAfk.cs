using bb.Utils;
using bb.Core.Configuration;
using System.Globalization;
using TwitchLib.Client.Enums;
using bb.Models.Command;
using bb.Models.Platform;
using bb.Models.Users;

namespace bb.Core.Commands.List.Afk
{
    public class ResumeAfk : CommandBase
    {
        public override string Name => "Rafk";
        public override string Author => "https://github.com/itzkitb";
        public override string Source => "Afk/ResumeAfk.cs";
        public override Dictionary<Language, string> Description => new() {
            { Language.RuRu, "Вернуться в АФК, если вы вышли из него." },
            { Language.EnUs, "Return to AFK if you left it." }
        };
        public override int UserCooldown => 10;
        public override int Cooldown => 1;
        public override string[] Aliases => ["rafk", "рафк", "вафк", "вернутьафк", "resumeafk"];
        public override string Help => string.Empty;
        public override DateTime CreationDate => DateTime.Parse("2024-07-07T00:00:00.0000000Z");
        public override Roles RoleRequired => Roles.Public;
        public override Platform[] Platforms => [Platform.Twitch, Platform.Telegram];
        public override bool IsAsync => false;

        public override CommandReturn Execute(CommandData data)
        {
            CommandReturn commandReturn = new CommandReturn();

            try
            {
                if (Program.BotInstance.UsersBuffer == null || data.ChannelId == null)
                {
                    commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:unknown", string.Empty, data.Platform));
                    return commandReturn;
                }

                long AFKResumeTimes = Convert.ToInt64(Program.BotInstance.UsersBuffer.GetParameter(data.Platform, DataConversion.ToLong(data.User.Id), Users.AfkResumeCount));
                DateTime AFKResume = DateTime.Parse((string)Program.BotInstance.UsersBuffer.GetParameter(data.Platform, DataConversion.ToLong(data.User.Id), Users.AfkResume), null, DateTimeStyles.AdjustToUniversal);

                if (AFKResumeTimes <= 5)
                {
                    TimeSpan cache = DateTime.UtcNow - AFKResume;
                    if (cache.TotalMinutes <= 5)
                    {
                        Program.BotInstance.UsersBuffer.SetParameter(data.Platform, DataConversion.ToLong(data.User.Id), Users.IsAfk, 1);
                        Program.BotInstance.UsersBuffer.SetParameter(data.Platform, DataConversion.ToLong(data.User.Id), Users.AfkResumeCount, AFKResumeTimes + 1);
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
