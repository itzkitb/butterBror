using butterBror.Utils;
using butterBror.Utils.DataManagers;
using Discord;
using TwitchLib.Client.Enums;
using butterBror.Utils.Tools;
using butterBror.Utils.Types;

namespace butterBror
{
    public partial class Commands
    {
        public class Afk
        {
            public static CommandInfo Info = new()
            {
                Name = "Afk",
                Author = "@ItzKITb",
                AuthorLink = "twitch.tv/itzkitb",
                AuthorAvatar = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                Description = new(){ { "ru", "Эта комманда поможет вам уйти из чата в афк." }, { "en", "This command will help you leave the chat and go afk." } },
                WikiLink = "https://itzkitb.lol/bot/command?q=afk",
                CooldownPerUser = 20,
                CooldownPerChannel = 1,
                Aliases = ["draw", "drw", "d", "рисовать", "рис", "р", "afk", "афк", "sleep", "goodnight", "gn", "slp", "s", "спать", "храп", "хррр", "с", "rest", "nap", "r", "отдых", "отдохнуть", "о", "lurk", "l", "наблюдатьизтени", "спрятаться", "study", "st", "учеба", "учится", "у", "poop", "p", "туалет", "shower", "sh", "ванная", "душ"],
                Arguments = "(message)",
                CooldownReset = true,
                IsForBotModerator = false,
                IsForBotDeveloper = false,
                IsForChannelModerator = false,
                CreationDate = DateTime.Parse("04/07/2024"),
                Platforms = [Platforms.Twitch, Platforms.Telegram]
            };
            // AFK
            static string[] draw = ["draw", "drw", "d", "рисовать", "рис", "р"];
            static string[] afk = ["afk", "афк"];
            static string[] sleep = ["sleep", "goodnight", "gn", "slp", "s", "спать", "храп", "хррр", "с"];
            static string[] rest = ["rest", "nap", "r", "отдых", "отдохнуть", "о"];
            static string[] lurk = ["lurk", "l", "наблюдатьизтени", "спрятаться"];
            static string[] study = ["study", "st", "учеба", "учится", "у"];
            static string[] poop = ["poop", "p", "😳", "туалет", "🚽"];
            static string[] shower = ["shower", "sh", "ванная", "душ"];

            public CommandReturn Index(CommandData data)
            {
                Core.Statistics.FunctionsUsed.Add();
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
                Core.Statistics.FunctionsUsed.Add();
                CommandReturn commandReturn = new CommandReturn();

                try
                {
                    string result = TranslationManager.GetTranslation(data.User.Language, $"command:afk:{afkType}:start", data.ChannelID, data.Platform).Replace("%user%", data.User.Username);
                    string text = data.ArgumentsString;

                    if (new NoBanwords().Check(text, data.ChannelID, data.Platform))
                    {
                        UsersData.Save(data.UserID, "isAfk", true, data.Platform);
                        UsersData.Save(data.UserID, "afkText", text, data.Platform);
                        UsersData.Save(data.UserID, "afkType", afkType, data.Platform);
                        UsersData.Save(data.UserID, "afkTime", DateTime.UtcNow, data.Platform);
                        UsersData.Save(data.UserID, "lastFromAfkResume", DateTime.UtcNow, data.Platform);
                        UsersData.Save(data.UserID, "fromAfkResumeTimes", 0, data.Platform);

                        if (Text.CleanAsciiWithoutSpaces(text) == "")
                            commandReturn.SetMessage(result);
                        else
                            commandReturn.SetMessage(result + ": " + text);
                    }
                    else
                    {
                        
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
}
