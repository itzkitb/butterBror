using butterBib;
using butterBror.Utils;
using butterBror.Utils.DataManagers;

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
                AuthorURL = "twitch.tv/itzkitb",
                AuthorImageURL = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                Description = "Эта комманда поможет уйти вам из чата в афк.",
                UseURL = "https://itzkitb.ru/bot_command/afk",
                UserCooldown = 20,
                GlobalCooldown = 1,
                aliases = ["draw", "drw", "d", "рисовать", "рис", "р", "afk", "афк", "sleep", "goodnight", "gn", "slp", "s", "спать", "храп", "хррр", "с", "rest", "nap", "r", "отдых", "отдохнуть", "о", "lurk", "l", "наблюдатьизтени", "спрятаться", "study", "st", "учеба", "учится", "у", "poop", "p", "туалет", "shower", "sh", "ванная", "душ"],
                ArgsRequired = "(afk сообщение)",
                ResetCooldownIfItHasNotReachedZero = true,
                ForAdmins = false,
                ForBotCreator = false,
                ForChannelAdmins = false,
                CreationDate = DateTime.Parse("07/04/2024")
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
            public static CommandReturn Index(CommandData data)
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
            public static CommandReturn GoToAfk(CommandData data, string afkType)
            {
                string result = TranslationManager.GetTranslation(data.User.Lang, $"{afkType}:start", data.ChannelID).Replace("%user%", data.User.Name);
                string text = data.ArgsAsString;

                if (NoBanwords.fullCheck(text, data.ChannelID))
                {
                    UsersData.UserSaveData(data.UserUUID, "isAfk", true);
                    UsersData.UserSaveData(data.UserUUID, "afkText", text);
                    UsersData.UserSaveData(data.UserUUID, "afkType", afkType);
                    UsersData.UserSaveData(data.UserUUID, "afkTime", DateTime.UtcNow);
                    UsersData.UserSaveData(data.UserUUID, "lastFromAfkResume", DateTime.UtcNow);
                    UsersData.UserSaveData(data.UserUUID, "fromAfkResumeTimes", 0);
                    string send = "";
                    if (TextUtil.FilterTextWithoutSpaces(text) == "")
                    {
                        send = result;
                    }
                    else
                    {
                        send = result + ": " + text;
                    }
                    return new()
                    {
                        Message = send,
                        IsSafeExecute = false,
                        Description = "",
                        Author = "",
                        ImageURL = "",
                        ThumbnailUrl = "",
                        Footer = "",
                        IsEmbed = false,
                        Ephemeral = false,
                        Title = "",
                        Color = Discord.Color.Green,
                        NickNameColor = TwitchLib.Client.Enums.ChatColorPresets.YellowGreen
                    };
                }
                return new()
                {
                    Message = "",
                    IsSafeExecute = false,
                    Description = "",
                    Author = "",
                    ImageURL = "",
                    ThumbnailUrl = "",
                    Footer = "",
                    IsEmbed = false,
                    Ephemeral = false,
                    Title = "",
                    Color = Discord.Color.Green,
                    NickNameColor = TwitchLib.Client.Enums.ChatColorPresets.YellowGreen
                };
            }
        }
    }
}
