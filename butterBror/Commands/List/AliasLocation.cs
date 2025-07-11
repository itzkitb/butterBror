﻿using butterBror.Utils;
using Discord;
using TwitchLib.Client.Enums;
using butterBror.Utils.Types;

namespace butterBror
{
    public partial class Commands
    {
        public class SetLocation
        {
            public static CommandInfo Info = new()
            {
                Name = "Translation",
                Author = "@ItzKITb",
                AuthorLink = "twitch.tv/itzkitb",
                AuthorAvatar = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                Description = new() {
                    { "ru", "Укажите свое местоположение, чтобы узнать погоду" },
                    { "en", "Set your location to get weather" }
                },
                WikiLink = "https://itzkitb.lol/bot/command?q=setlocation",
                CooldownPerUser = 0,
                CooldownPerChannel = 0,
                Aliases = ["loc", "location", "city", "setlocation", "setloc", "setcity", "улокацию", "угород", "установитьлокацию", "установитьгород"],
                Arguments = "(city name)",
                CooldownReset = false,
                CreationDate = DateTime.Parse("29/04/2025"),
                IsForBotModerator = false,
                IsForBotDeveloper = false,
                IsForChannelModerator = false,
                Platforms = [Platforms.Twitch, Platforms.Telegram, Platforms.Discord]
            };
            public CommandReturn Index(CommandData data)
            {
                Engine.Statistics.FunctionsUsed.Add();
                try
                {
                    var exdata = data;
                    if (exdata.Arguments is not null && exdata.Arguments.Count >= 1)
                    {
                        exdata.Arguments.Insert(0, "set");
                    }
                    else
                    {
                        exdata.Arguments = new List<string>();
                        exdata.Arguments.Insert(0, "get");
                    }
                    var command = new Weather();
                    return command.Index(exdata);
                }
                catch (Exception e)
                {
                    CommandReturn commandReturn = new CommandReturn();
                    commandReturn.SetError(e);
                    return commandReturn;
                }
            }
        }
    }
}