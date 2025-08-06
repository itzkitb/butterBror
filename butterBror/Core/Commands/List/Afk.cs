using butterBror.Core.Bot;
using butterBror.Core.Bot.SQLColumnNames;
using butterBror.Data;
using butterBror.Models;
using butterBror.Utils;
using Microsoft.CodeAnalysis;

namespace butterBror.Core.Commands.List
{
    public class Afk : CommandBase
    {
        public override string Name => "Afk";
        public override string Author => "ItzKITb";
        public override string AuthorsGithub => "https://github.com/itzkitb";
        public override string GithubSource => $"{URLs.githubSource}blob/master/butterBror/Core/Commands/List/Afk.cs";
        public override Version Version => new Version("1.0.0");
        public override Dictionary<string, string> Description => new() {
            { "ru-RU", "Эта команда поможет вам уйти из чата в афк." },
            { "en-US", "This command will help you leave the chat and go afk." }
        };
        public override string WikiLink => "https://itzkitb.lol/bot/command?q=afk";
        public override int CooldownPerUser => 20;
        public override int CooldownPerChannel => 1;
        public override string[] Aliases => ["draw", "drw", "d", "рисовать", "рис", "р", "afk", "афк", "sleep", "goodnight", "gn", "slp", "s", "спать", "храп", "хррр", "с", "rest", "nap", "r", "отдых", "отдохнуть", "о", "lurk", "l", "наблюдатьизтени", "спрятаться", "study", "st", "учеба", "учится", "у", "poop", "p", "туалет", "shower", "sh", "ванная", "душ"];
        public override string HelpArguments => "(message)";
        public override DateTime CreationDate => DateTime.Parse("04/07/2024");
        public override bool OnlyBotDeveloper => false;
        public override bool OnlyBotModerator => false;
        public override bool OnlyChannelModerator => false;
        public override PlatformsEnum[] Platforms => [PlatformsEnum.Twitch, PlatformsEnum.Telegram];
        public override bool IsAsync => false;

        // AFK
        static string[] draw = ["draw", "drw", "d", "рисовать", "рис", "р"];
        static string[] afk = ["afk", "афк"];
        static string[] sleep = ["sleep", "goodnight", "gn", "slp", "s", "спать", "храп", "хррр", "с"];
        static string[] rest = ["rest", "nap", "r", "отдых", "отдохнуть", "о"];
        static string[] lurk = ["lurk", "l", "наблюдатьизтени", "спрятаться"];
        static string[] study = ["study", "st", "учеба", "учится", "у"];
        static string[] poop = ["poop", "p", "😳", "туалет", "🚽"];
        static string[] shower = ["shower", "sh", "ванная", "душ"];

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
                string result = LocalizationService.GetString(data.User.Language, $"command:afk:{afkType}:start", data.ChannelId, data.Platform, data.User.Name);
                string text = data.ArgumentsString;

                Engine.Bot.SQL.Users.SetParameter(data.Platform, Format.ToLong(data.UserID), Users.IsAFK, 1);
                Engine.Bot.SQL.Users.SetParameter(data.Platform, Format.ToLong(data.UserID), Users.AFKText, text);
                Engine.Bot.SQL.Users.SetParameter(data.Platform, Format.ToLong(data.UserID), Users.AFKType, afkType);
                Engine.Bot.SQL.Users.SetParameter(data.Platform, Format.ToLong(data.UserID), Users.AFKStart, DateTime.UtcNow.ToString("o"));
                Engine.Bot.SQL.Users.SetParameter(data.Platform, Format.ToLong(data.UserID), Users.AFKResume, DateTime.UtcNow.ToString("o"));
                Engine.Bot.SQL.Users.SetParameter(data.Platform, Format.ToLong(data.UserID), Users.AFKResumeTimes, 0);

                if (Text.CleanAsciiWithoutSpaces(text) == "")
                    commandReturn.SetMessage(result);
                else
                    commandReturn.SetMessage(result + ": " + text);
            }
            catch (Exception e)
            {
                commandReturn.SetError(e);
            }

            return commandReturn;
        }
    }
}
