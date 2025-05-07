using butterBror.Utils;
using butterBror.Utils.DataManagers;
using butterBror;
using Discord;
using TwitchLib.Client.Enums;

namespace butterBror
{
    public partial class Commands
    {
        public class Afk
        {
            public static CommandInfo Info = new()
            {
                name = "Afk",
                author = "@ItzKITb",
                author_link = "twitch.tv/itzkitb",
                author_avatar = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                description = new(){ { "ru", "Эта комманда поможет вам уйти из чата в афк." }, { "en", "This command will help you leave the chat and go afk." } },
                wiki_link = "https://itzkitb.lol/bot/command?q=afk",
                cooldown_per_user = 20,
                cooldown_global = 1,
                aliases = ["draw", "drw", "d", "рисовать", "рис", "р", "afk", "афк", "sleep", "goodnight", "gn", "slp", "s", "спать", "храп", "хррр", "с", "rest", "nap", "r", "отдых", "отдохнуть", "о", "lurk", "l", "наблюдатьизтени", "спрятаться", "study", "st", "учеба", "учится", "у", "poop", "p", "туалет", "shower", "sh", "ванная", "душ"],
                arguments = "(message)",
                cooldown_reset = true,
                is_for_bot_moderator = false,
                is_for_bot_developer = false,
                is_for_channel_moderator = false,
                creation_date = DateTime.Parse("04/07/2024"),
                platforms = [Platforms.Twitch, Platforms.Telegram]
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
                Engine.Statistics.functions_used.Add();
                try
                {
                    string action = "";
                    switch (data.name)
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
                    return new()
                    {
                        message = "",
                        safe_execute = false,
                        description = "",
                        author = "",
                        image_link = "",
                        thumbnail_link = "",
                        footer = "",
                        is_embed = true,
                        is_ephemeral = false,
                        title = "",
                        embed_color = Color.Green,
                        nickname_color = ChatColorPresets.YellowGreen,
                        is_error = true,
                        exception = e
                    };
                }
            }
            public static CommandReturn GoToAfk(CommandData data, string afkType)
            {
                Engine.Statistics.functions_used.Add();
                try
                {
                    string result = TranslationManager.GetTranslation(data.user.language, $"command:afk:{afkType}:start", data.channel_id, data.platform).Replace("%user%", data.user.username);
                    string text = data.arguments_string;

                    if (NoBanwords.Check(text, data.channel_id, data.platform))
                    {
                        UsersData.Save(data.user_id, "isAfk", true, data.platform);
                        UsersData.Save(data.user_id, "afkText", text, data.platform);
                        UsersData.Save(data.user_id, "afkType", afkType, data.platform);
                        UsersData.Save(data.user_id, "afkTime", DateTime.UtcNow, data.platform);
                        UsersData.Save(data.user_id, "lastFromAfkResume", DateTime.UtcNow, data.platform);
                        UsersData.Save(data.user_id, "fromAfkResumeTimes", 0, data.platform);

                        string send = "";

                        if (TextUtil.CleanAsciiWithoutSpaces(text) == "")
                            send = result;
                        else
                            send = result + ": " + text;

                        return new()
                        {
                            message = send,
                            safe_execute = false,
                            description = "",
                            author = "",
                            image_link = "",
                            thumbnail_link = "",
                            footer = "",
                            is_embed = false,
                            is_ephemeral = false,
                            title = "",
                            embed_color = Color.Green,
                            nickname_color = TwitchLib.Client.Enums.ChatColorPresets.YellowGreen,
                        };
                    }
                    else
                    {
                        return new()
                        {
                            message = TranslationManager.GetTranslation(data.user.language, "error:message_could_not_be_sent", data.channel_id, data.platform),
                            safe_execute = false,
                            description = "",
                            author = "",
                            image_link = "",
                            thumbnail_link = "",
                            footer = "",
                            is_embed = false,
                            is_ephemeral = false,
                            title = "",
                            embed_color = Color.Green,
                            nickname_color = TwitchLib.Client.Enums.ChatColorPresets.YellowGreen,
                        };
                    }
                }
                catch (Exception e)
                {
                    return new()
                    {
                        message = "",
                        safe_execute = false,
                        description = "",
                        author = "",
                        image_link = "",
                        thumbnail_link = "",
                        footer = "",
                        is_embed = true,
                        is_ephemeral = false,
                        title = "",
                        embed_color = Color.Green,
                        nickname_color = ChatColorPresets.YellowGreen,
                        is_error = true,
                        exception = e
                    };
                }
            }
        }
    }
}
