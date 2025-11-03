using bb.Utils;
using bb.Core.Configuration;
using bb.Models.Command;
using bb.Models.Platform;
using bb.Models.Users;

namespace bb.Core.Commands.List.Afk
{
    public class Afk : CommandBase
    {
        // AFK
        static string[] draw = ["draw", "drw", "d", "рисовать", "рис", "р"];
        static string[] afk = ["afk", "афк"];
        static string[] sleep = ["sleep", "goodnight", "gn", "slp", "s", "спать", "храп", "хррр", "с"];
        static string[] rest = ["rest", "nap", "r", "отдых", "отдохнуть", "о"];
        static string[] lurk = ["lurk", "l", "наблюдатьизтени", "спрятаться"];
        static string[] study = ["study", "st", "учеба", "учится", "у"];
        static string[] poop = ["poop", "p", "туалет"];
        static string[] shower = ["shower", "sh", "ванная", "душ"];

        public override string Name => "Afk";
        public override string Author => "https://github.com/itzkitb";
        public override string Source => "Afk/Afk.cs";
        public override Dictionary<Language, string> Description => new() {
            { Language.RuRu, "Эта команда поможет вам уйти из чата в афк." },
            { Language.EnUs, "This command will help you leave the chat and go afk." }
        };
        public override int UserCooldown => 5;
        public override int Cooldown => 1;
        public override string[] Aliases => draw.Concat(afk).Concat(sleep).Concat(rest).Concat(lurk).Concat(study).Concat(poop).Concat(shower).ToArray();
        public override string Help => "[message]";
        public override DateTime CreationDate => DateTime.Parse("2024-07-04T00:00:00.0000000Z");
        public override Roles RoleRequired => Roles.Public;
        public override Platform[] Platforms => [Platform.Twitch, Platform.Telegram];
        public override bool IsAsync => false;

        public override CommandReturn Execute(CommandData data)
        {
            try
            {
                string action = "";
                switch (data.Name)
                {
                    case string name when draw.Contains(name):
                        action = "draw";
                        break;
                    case string name when sleep.Contains(name):
                        action = "sleep";
                        break;
                    case string name when rest.Contains(name):
                        action = "rest";
                        break;
                    case string name when lurk.Contains(name):
                        action = "lurk";
                        break;
                    case string name when study.Contains(name):
                        action = "study";
                        break;
                    case string name when poop.Contains(name):
                        action = "poop";
                        break;
                    case string name when shower.Contains(name):
                        action = "shower";
                        break;
                    default:
                        action = "afk";
                        break;
                }
                return GoToAfk(data, action);
            }
            catch (Exception e)
            {
                CommandReturn commandReturn = new CommandReturn();
                commandReturn.SetError(e);
                return commandReturn;
            }
        }
        public static CommandReturn GoToAfk(CommandData data, string afkType)
        {
            CommandReturn commandReturn = new CommandReturn();

            try
            {
                if (Program.BotInstance.UsersBuffer == null || data.ChannelId == null)
                {
                    commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:unknown", string.Empty, data.Platform));
                    return commandReturn;
                }

                string result = LocalizationService.GetString(data.User.Language, $"command:afk:{afkType}:start", data.ChannelId, data.Platform, data.User.Name);
                string text = data.ArgumentsString;

                Program.BotInstance.UsersBuffer.SetParameter(data.Platform, DataConversion.ToLong(data.User.Id), Users.IsAfk, 1);
                Program.BotInstance.UsersBuffer.SetParameter(data.Platform, DataConversion.ToLong(data.User.Id), Users.AfkMessage, text);
                Program.BotInstance.UsersBuffer.SetParameter(data.Platform, DataConversion.ToLong(data.User.Id), Users.AfkType, afkType);
                Program.BotInstance.UsersBuffer.SetParameter(data.Platform, DataConversion.ToLong(data.User.Id), Users.AfkStartTime, DateTime.UtcNow.ToString("o"));
                Program.BotInstance.UsersBuffer.SetParameter(data.Platform, DataConversion.ToLong(data.User.Id), Users.AfkResume, DateTime.UtcNow.ToString("o"));
                Program.BotInstance.UsersBuffer.SetParameter(data.Platform, DataConversion.ToLong(data.User.Id), Users.AfkResumeCount, 0);

                commandReturn.SetMessage(result);
            }
            catch (Exception e)
            {
                commandReturn.SetError(e);
            }

            return commandReturn;
        }
    }
}
