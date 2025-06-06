using butterBror.Utils;
using butterBror;
using Discord;
using TwitchLib.Client.Enums;

namespace butterBror
{
    public partial class Commands
    {
        public class Name
        {
            public static CommandInfo Info = new()
            {
                Name = "Name",
                Author = "@ItzKITb",
                AuthorLink = "twitch.tv/itzkitb",
                AuthorAvatar = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                Description = new()
                {
                    { "ru", "Получить имя из ID" },
                    { "en", "Get name from ID" }
                },
                WikiLink = "https://itzkitb.lol/bot/command?q=name",
                CooldownPerUser = 5,
                CooldownPerChannel = 1,
                Aliases = ["name", "nick", "nickname", "никнейм", "ник", "имя"],
                Arguments = "[user ID]",
                CooldownReset = true,
                CreationDate = DateTime.Parse("25/10/2024"),
                IsForBotModerator = false,
                IsForBotDeveloper = false,
                IsForChannelModerator = false,
                Platforms = [Platforms.Twitch, Platforms.Telegram, Platforms.Discord]
            };
            public CommandReturn Index(CommandData data)
            {
                Engine.Statistics.functions_used.Add();
                CommandReturn commandReturn = new CommandReturn();

                try
                {
                    if (data.arguments.Count > 0)
                    {
                        string name = Names.GetUsername(data.arguments[0], Platforms.Twitch);
                        if (name == data.user_id)
                        {
                            commandReturn.SetMessage(TranslationManager.GetTranslation(data.user.language, "command:name", data.channel_id, data.platform).Replace("%name%", data.user_id)); // Fix AB3
                        }
                        else if (name == null)
                        {
                            commandReturn.SetMessage(TranslationManager.GetTranslation(data.user.language, "error:user_not_found", data.channel_id, data.platform).Replace("%user%", data.arguments[0])); // Fix AB3
                            commandReturn.SetColor(ChatColorPresets.CadetBlue);
                        }
                        else
                        {
                            commandReturn.SetMessage(TranslationManager.GetTranslation(data.user.language, "command:name:user", data.channel_id, data.platform).Replace("%name%", name).Replace("%id%", data.arguments[0])); // Fix AB3
                        }
                    }
                    else
                    {
                        commandReturn.SetMessage(TranslationManager.GetTranslation(data.user.language, "command:name", data.channel_id, data.platform).Replace("%name%", data.user_id)); // Fix AB3
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
