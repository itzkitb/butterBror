using System.Net.NetworkInformation;
using Discord;
using TwitchLib.Client.Enums;
using butterBror.Utils;
using butterBror;
using butterBror.Utils.Things;
using butterBror.Utils.Tools;

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
                AuthorLink = "twitch.tv/itzkitb",
                AuthorAvatar = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                Description = new() { 
                    { "ru", "Пинг с Вояджер-1: 243000000 мс" }, 
                    { "en", "Ping to Voyager-1: 243000000 ms" } 
                },
                WikiLink = "https://itzkitb.lol/bot/command?q=ping",
                CooldownPerUser = 30,
                CooldownPerChannel = 5,
                Aliases = ["ping", "пинг", "понг", "пенг", "п"],
                Arguments = "(ISP/dev)",
                CooldownReset = true,
                CreationDate = DateTime.Parse("07/04/2024"),
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
                    string argument = "";
                    if (data.arguments.Count > 0)
                        argument = data.arguments[0].ToLower();

                    if (data.arguments.Count == 0)
                    {
                        var workTime = DateTime.Now - Core.StartTime;
                        string host = "";
                        long pingSpeed = 0;
                        if (data.platform == Platforms.Telegram)
                        {
                            pingSpeed = Utils.Tools.API.Telegram.Ping().Result;
                        }
                        else
                        {
                            if (data.platform == Platforms.Discord) host = URLs.discord;
                            else if (data.platform == Platforms.Twitch) host = URLs.twitch;
                            else if (data.platform == Platforms.Telegram) host = URLs.telegram;

                            PingReply reply = new Ping().Send(host, 1000);
                            pingSpeed = reply.Status == IPStatus.Success ? reply.RoundtripTime : -1;
                        }

                        commandReturn.SetMessage(TranslationManager.GetTranslation(data.user.language, "command:ping", data.channel_id, data.platform)
                                    .Replace("%version%", Core.Version)
                                    .Replace("%workTime%", Text.FormatTimeSpan(workTime, data.user.language))
                                    .Replace("%tabs%", data.platform == Platforms.Twitch ? Core.Bot.Clients.Twitch.JoinedChannels.Count.ToString() : (data.platform == Platforms.Discord ? Core.Bot.Clients.Discord.Guilds.Count.ToString() : (Core.Bot.Clients.Twitch.JoinedChannels.Count + Core.Bot.Clients.Discord.Guilds.Count) + " (Twitch, Discord)"))
                                    .Replace("%loadedCMDs%", Commands.commands.Count.ToString())
                                    .Replace("%completedCMDs%", Core.CompletedCommands.ToString())
                                    .Replace("%ping%", pingSpeed.ToString()));
                    }
                    else if (argument.Equals("isp"))
                    {
                        var workTime = DateTime.Now - Core.StartTime;
                        PingReply reply = new Ping().Send("192.168.1.1", 1000);
                        long pingSpeed = -1;
                        if (reply.Status == IPStatus.Success) pingSpeed = reply.RoundtripTime;
                        else
                        {
                            reply = new Ping().Send("192.168.0.1", 1000);
                            if (reply.Status == IPStatus.Success) pingSpeed = reply.RoundtripTime;
                        }

                        commandReturn.SetMessage(TranslationManager.GetTranslation(data.user.language, "command:ping:isp", data.channel_id, data.platform)
                                    .Replace("%ping%", pingSpeed.ToString()));
                    }
                    else if (argument.Equals("dev"))
                    {
                        var workTime = DateTime.Now - Core.StartTime;
                        string host = "";
                        long pingSpeed = 0;
                        if (data.platform == Platforms.Telegram)
                        {
                            pingSpeed = Utils.Tools.API.Telegram.Ping().Result;
                        }
                        else
                        {
                            if (data.platform == Platforms.Discord) host = URLs.discord;
                            else if (data.platform == Platforms.Twitch) host = URLs.twitch;
                            else if (data.platform == Platforms.Telegram) host = URLs.telegram;

                            PingReply reply = new Ping().Send(host, 1000);
                            pingSpeed = reply.Status == IPStatus.Success ? reply.RoundtripTime : -1;
                        }

                        commandReturn.SetMessage(TranslationManager.GetTranslation(data.user.language, "command:ping:development", data.channel_id, data.platform)
                                    .Replace("%version%", Core.Version)
                                    .Replace("%patch%", Core.Patch)
                                    .Replace("%workTime%", Text.FormatTimeSpan(workTime, data.user.language))
                                    .Replace("%tabs%", (Core.Bot.Clients.Twitch.JoinedChannels.Count + Core.Bot.Clients.Discord.Guilds.Count) + " (Twitch, Discord)")
                                    .Replace("%loadedCMDs%", Commands.commands.Count.ToString())
                                    .Replace("%completedCMDs%", Core.CompletedCommands.ToString())
                                    .Replace("%ping%", pingSpeed.ToString())
                                    .Replace("%tps%", Core.TicksPerSecond.ToString())
                                    .Replace("%max_tps%", Core.Ticks.ToString())
                                    .Replace("%tick_delay%", Core.TickDelay.ToString())
                                    .Replace("%tick_counted%", Core.TicksCounter.ToString())
                                    .Replace("%skiped_ticks%", Core.SkippedTicks.ToString()));
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
