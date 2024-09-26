using System.Net.NetworkInformation;
using Discord;
using butterBib;
using Discord.Rest;
using TwitchLib.Client.Enums;
using butterBror.Utils;

namespace butterBror
{
    public partial class Commands
    {
        public class Pinger
        {
            public static CommandInfo Info = new()
            {
                Name = "Ping",
                Author = "@ItzKITb",
                AuthorURL = "twitch.tv/itzkitb",
                AuthorImageURL = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                Description = "Этой командой можно узнать, функционирует ли бот и получить краткую информацию о нем.",
                UseURL = "https://itzkitb.ru/bot_command/ping",
                UserCooldown = 30,
                GlobalCooldown = 5,
                aliases = ["ping", "пинг", "понг", "пенг", "п"],
                ArgsRequired = "(Нету)",
                ResetCooldownIfItHasNotReachedZero = true,
                CreationDate = DateTime.Parse("07/04/2024"),
                ForAdmins = false,
                ForBotCreator = false,
                ForChannelAdmins = false
            };
            public static CommandReturn Index(CommandData data)
            {
                string returnMessage = "";
                Random rand = new();
                var workTime = DateTime.Now - BotEngine.StartTime;
                string host = "";
                if (data.Platform == Platforms.Discord)
                {
                    host = "discord.com";
                }
                else if (data.Platform == Platforms.Twitch)
                {
                    host = "twitch.tv";
                }
                int timeout = 1000;
                Ping ping = new Ping();
                PingReply reply = ping.Send(host, timeout);
                long pingSpeed = 0;
                if (reply.Status == IPStatus.Success)
                {
                    pingSpeed = reply.RoundtripTime;
                }
                if (pingSpeed >= 60 && pingSpeed <= 70 && rand.Next(0, 2) == 1)
                {
                    pingSpeed = 69;
                }
                returnMessage = TranslationManager.GetTranslation(data.User.Lang, "pingText", data.ChannelID)
                            .Replace("%version%", BotEngine.botVersion)
                            .Replace("%workTime%", TextUtil.FormatTimeSpan(workTime, data.User.Lang))
                            .Replace("%tabs%", Bot.Channels.Length.ToString())
                            .Replace("%loadedCMDs%", Bot.CommandsActive.ToString())
                            .Replace("%completedCMDs%", BotEngine.CompletedCommands.ToString())
                            .Replace("%ping%", pingSpeed.ToString());
                return new()
                {
                    Message = returnMessage,
                    IsSafeExecute = false,
                    Description = "",
                    Author = "",
                    ImageURL = "",
                    ThumbnailUrl = "",
                    Footer = "",
                    IsEmbed = true,
                    Ephemeral = false,
                    Title = TranslationManager.GetTranslation(data.User.Lang, "dsPingTitle", data.ChannelID),
                    Color = Color.Green,
                    NickNameColor = ChatColorPresets.YellowGreen
                };
            }
        }
    }
}
