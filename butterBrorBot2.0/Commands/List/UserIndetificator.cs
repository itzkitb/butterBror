using butterBror.Utils;
using Discord;
using TwitchLib.Client.Enums;
using butterBror.Utils.Tools;
using butterBror.Utils.Types;

namespace butterBror
{
    public partial class Commands
    {
        public class ID
        {
            public static CommandInfo Info = new()
            {
                Name = "ID",
                Author = "@ItzKITb",
                AuthorLink = "twitch.tv/itzkitb",
                AuthorAvatar = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                Description = new()
                {
                    { "ru", "Узнать ID пользователя" },
                    { "en", "Find out user ID" }
                },
                WikiLink = "https://itzkitb.lol/bot/command?q=id",
                CooldownPerUser = 5,
                CooldownPerChannel = 1,
                Aliases = ["id", "indetificator", "ид"],
                Arguments = "(name)",
                CooldownReset = true,
                CreationDate = DateTime.Parse("08/08/2024"),
                IsForBotModerator = false,
                IsForBotDeveloper = false,
                IsForChannelModerator = false,
                Platforms = [Platforms.Twitch, Platforms.Telegram, Platforms.Discord]
            };
            public CommandReturn Index(CommandData data)
            {
                Core.Statistics.FunctionsUsed.Add();
                CommandReturn commandReturn = new CommandReturn();

                try
                {
                    if (data.Arguments.Count > 0)
                    {
                        string username = Text.UsernameFilter(data.Arguments[0].ToLower());
                        string ID = Names.GetUserID(username, data.Platform);
                        if (ID == data.UserID)
                        {
                            commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:id", data.ChannelID, data.Platform).Replace("%id%", data.UserID));
                        }
                        else if (ID == null)
                        {
                            commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:user_not_found", data.ChannelID, data.Platform).Replace("%user%", username));
                            commandReturn.SetColor(ChatColorPresets.CadetBlue);
                        }
                        else
                        {
                            commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:id:user", data.ChannelID, data.Platform).Replace("%id%", ID).Replace("%user%", Names.DontPing(username)));
                        }
                    }
                    else
                    {
                        commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:id", data.ChannelID, data.Platform).Replace("%id%", data.UserID));
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
