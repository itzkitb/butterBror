using butterBror.Utils;
using Discord;
using TwitchLib.Client.Enums;
using butterBror.Utils.Tools;
using butterBror.Utils.Types;

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
                Engine.Statistics.FunctionsUsed.Add();
                CommandReturn commandReturn = new CommandReturn();

                try
                {
                    if (data.Arguments.Count > 0)
                    {
                        string name = Names.GetUsername(data.Arguments[0], Platforms.Twitch);
                        if (name == data.UserID)
                        {
                            commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:name", data.ChannelID, data.Platform).Replace("%name%", data.UserID)); // Fix AB3
                        }
                        else if (name == null)
                        {
                            commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:user_not_found", data.ChannelID, data.Platform).Replace("%user%", data.Arguments[0])); // Fix AB3
                            commandReturn.SetColor(ChatColorPresets.CadetBlue);
                        }
                        else
                        {
                            commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:name:user", data.ChannelID, data.Platform).Replace("%name%", name).Replace("%id%", data.Arguments[0])); // Fix AB3
                        }
                    }
                    else
                    {
                        commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:name", data.ChannelID, data.Platform).Replace("%name%", data.UserID)); // Fix AB3
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
