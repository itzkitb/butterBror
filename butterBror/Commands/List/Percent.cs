﻿using butterBror.Utils.Types;
using Discord;
using TwitchLib.Client.Enums;

namespace butterBror
{
    public partial class Commands
    {
        public class Percent
        {
            public static CommandInfo Info = new()
            {
                Name = "Percent",
                Author = "@ItzKITb",
                AuthorLink = "twitch.tv/itzkitb",
                AuthorAvatar = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                Description = new() { 
                    { "ru", "MrDestructoid Вероятность уничтожения змели в ближайшие 5 минут: 99.9%" }, 
                    { "en", "MrDestructoid Chance of destroying the snake in the next 5 minutes: 99.9%" } 
                },
                WikiLink = "https://itzkitb.lol/bot/command?q=percent",
                CooldownPerUser = 5,
                CooldownPerChannel = 1,
                Aliases = ["%", "percent", "процент", "perc", "проц"],
                Arguments = string.Empty,
                CooldownReset = true,
                CreationDate = DateTime.Parse("08/08/2024"),
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
                    float percent = (float)new Random().Next(10000) / 100;
                    commandReturn.SetMessage($"🤔 {percent}%");
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