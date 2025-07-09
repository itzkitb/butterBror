using System.Net.NetworkInformation;
using Discord;
using TwitchLib.Client.Enums;
using butterBror.Utils;
using butterBror.Utils.Tools;
using butterBror.Utils.Bot;
using butterBror.Utils.Types;

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
                Engine.Statistics.FunctionsUsed.Add();
                CommandReturn commandReturn = new CommandReturn();

                try
                {
                    string argument = "";
                    if (data.Arguments.Count > 0)
                        argument = data.Arguments[0].ToLower();

                    if (data.Arguments.Count == 0)
                    {
                        var workTime = DateTime.Now - Engine.StartTime;
                        string host = "";
                        long pingSpeed = 0;
                        if (data.Platform == Platforms.Telegram)
                        {
                            pingSpeed = Utils.Tools.API.Telegram.Ping().Result;
                        }
                        else
                        {
                            if (data.Platform == Platforms.Discord) host = URLs.discord;
                            else if (data.Platform == Platforms.Twitch) host = URLs.twitch;
                            else if (data.Platform == Platforms.Telegram) host = URLs.telegram;

                            PingReply reply = new Ping().Send(host, 1000);
                            pingSpeed = reply.Status == IPStatus.Success ? reply.RoundtripTime : -1;
                        }

                        commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:ping", data.ChannelID, data.Platform)
                                    .Replace("%version%", Engine.Version)
                                    .Replace("%workTime%", Text.FormatTimeSpan(workTime, data.User.Language))
                                    .Replace("%tabs%", data.Platform == Platforms.Twitch ? Engine.Bot.Clients.Twitch.JoinedChannels.Count.ToString() : (data.Platform == Platforms.Discord ? Engine.Bot.Clients.Discord.Guilds.Count.ToString() : (Engine.Bot.Clients.Twitch.JoinedChannels.Count + Engine.Bot.Clients.Discord.Guilds.Count) + " (Twitch, Discord)"))
                                    .Replace("%loadedCMDs%", Commands.commands.Count.ToString())
                                    .Replace("%completedCMDs%", Engine.CompletedCommands.ToString())
                                    .Replace("%ping%", pingSpeed.ToString()));
                    }
                    else if (argument.Equals("isp"))
                    {
                        var workTime = DateTime.Now - Engine.StartTime;
                        PingReply reply = new Ping().Send("192.168.1.1", 1000);
                        long pingSpeed = -1;
                        if (reply.Status == IPStatus.Success) pingSpeed = reply.RoundtripTime;
                        else
                        {
                            reply = new Ping().Send("192.168.0.1", 1000);
                            if (reply.Status == IPStatus.Success) pingSpeed = reply.RoundtripTime;
                        }

                        commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:ping:isp", data.ChannelID, data.Platform)
                                    .Replace("%ping%", pingSpeed.ToString()));
                    }
                    else if (argument.Equals("dev"))
                    {
                        var workTime = DateTime.Now - Engine.StartTime;
                        string host = "";
                        long pingSpeed = 0;
                        if (data.Platform == Platforms.Telegram)
                        {
                            pingSpeed = Utils.Tools.API.Telegram.Ping().Result;
                        }
                        else
                        {
                            if (data.Platform == Platforms.Discord) host = URLs.discord;
                            else if (data.Platform == Platforms.Twitch) host = URLs.twitch;
                            else if (data.Platform == Platforms.Telegram) host = URLs.telegram;

                            PingReply reply = new Ping().Send(host, 1000);
                            pingSpeed = reply.Status == IPStatus.Success ? reply.RoundtripTime : -1;
                        }

                        commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:ping:development", data.ChannelID, data.Platform)
                                    .Replace("%version%", Engine.Version)
                                    .Replace("%patch%", Engine.Patch)
                                    .Replace("%workTime%", Text.FormatTimeSpan(workTime, data.User.Language))
                                    .Replace("%tabs%", (Engine.Bot.Clients.Twitch.JoinedChannels.Count + Engine.Bot.Clients.Discord.Guilds.Count) + " (Twitch, Discord)")
                                    .Replace("%loadedCMDs%", Commands.commands.Count.ToString())
                                    .Replace("%completedCMDs%", Engine.CompletedCommands.ToString())
                                    .Replace("%ping%", pingSpeed.ToString())
                                    .Replace("%tps%", Engine.TicksPerSecond.ToString())
                                    .Replace("%max_tps%", Engine.Ticks.ToString())
                                    .Replace("%tick_delay%", Engine.TickDelay.ToString())
                                    .Replace("%tick_counted%", Engine.TicksCounter.ToString())
                                    .Replace("%skiped_ticks%", Engine.SkippedTicks.ToString()));
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
