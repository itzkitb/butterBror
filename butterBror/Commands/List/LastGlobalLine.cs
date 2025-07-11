﻿using butterBror.Utils;
using butterBror.Utils.DataManagers;
using Discord;
using TwitchLib.Client.Enums;
using butterBror.Utils.Tools;
using butterBror.Utils.Types;

namespace butterBror
{
    public partial class Commands
    {
        public class LastGlobalLine
        {
            public static CommandInfo Info = new()
            {
                Name = "LastGlobalLine",
                Author = "@ItzKITb",
                AuthorLink = "twitch.tv/itzkitb",
                AuthorAvatar = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                Description = new() { 
                    { "ru", "Последнее сообщение выбранного пользователя" }, 
                    { "en", "The last message of the selected user" } 
                },
                WikiLink = "https://itzkitb.lol/bot/command?q=lgl",
                CooldownPerUser = 10,
                CooldownPerChannel = 1,
                Aliases = ["lgl", "lastgloballine", "пгс", "последнееглобальноесообщение"],
                Arguments = "[name]",
                CooldownReset = true,
                CreationDate = DateTime.Parse("07/04/2024"),
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
                    if (data.Arguments.Count != 0)
                    {
                        var name = Text.UsernameFilter(data.Arguments.ElementAt(0).ToLower());
                        var userID = Names.GetUserID(name, Platforms.Twitch);
                        if (userID == null)
                        {
                            commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:user_not_found", data.ChannelID, data.Platform)
                                .Replace("%user%", Names.DontPing(name)));
                            commandReturn.SetColor(ChatColorPresets.Red);
                        }
                        else
                        {
                            var lastLine = UsersData.Get<string>(userID, "lastSeenMessage", data.Platform);
                            var lastLineDate = UsersData.Get<DateTime>(userID, "lastSeen", data.Platform);
                            DateTime now = DateTime.UtcNow;
                            if (name == Engine.Bot.BotName.ToLower())
                            {
                                commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:last_global_line:bot", data.ChannelID, data.Platform));
                            }
                            else if (name == data.User.Name.ToLower())
                            {
                                commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "text:you_right_there", data.ChannelID, data.Platform));
                            }
                            else
                            {
                                commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:last_global_line", data.ChannelID, data.Platform)
                                    .Replace("%user%", Names.DontPing(Names.GetUsername(userID, data.Platform)))
                                    .Replace("&timeAgo&", Text.FormatTimeSpan(Utils.Tools.Format.GetTimeTo(lastLineDate, now, false), data.User.Language))
                                    .Replace("%message%", lastLine));
                            }
                        }
                    }
                    else
                    {
                        commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "text:you_right_there", data.ChannelID, data.Platform));
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
