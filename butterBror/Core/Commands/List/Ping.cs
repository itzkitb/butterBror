using butterBror.Utils;
using butterBror.Core.Bot;
using butterBror.Models;
using System.Net.NetworkInformation;

namespace butterBror.Core.Commands.List
{
    public class Pinger : CommandBase
    {
        public override string Name => "Ping";
        public override string Author => "ItzKITb";
        public override string AuthorsGithub => "https://github.com/itzkitb";
        public override string GithubSource => $"{URLs.githubSource}blob/master/butterBror/Core/Commands/List/Pinger.cs";
        public override Version Version => new("1.0.0");
        public override Dictionary<string, string> Description => new() {
            { "ru", "Узнать пинг бота." },
            { "en", "Find out the bot's ping." }
        };
        public override string WikiLink => "https://itzkitb.lol/bot/command?q=ping";
        public override int CooldownPerUser => 30;
        public override int CooldownPerChannel => 5;
        public override string[] Aliases => ["ping", "пинг", "понг", "пенг", "п"];
        public override string HelpArguments => "(ISP/dev)";
        public override DateTime CreationDate => DateTime.Parse("07/04/2024");
        public override bool OnlyBotModerator => false;
        public override bool OnlyBotDeveloper => false;
        public override bool OnlyChannelModerator => false;
        public override PlatformsEnum[] Platforms => [PlatformsEnum.Twitch, PlatformsEnum.Telegram, PlatformsEnum.Discord];
        public override bool IsAsync => false;

        public override CommandReturn Execute(CommandData data)
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
                    if (data.Platform == PlatformsEnum.Telegram)
                    {
                        pingSpeed = Services.External.TelegramService.Ping().Result;
                    }
                    else
                    {
                        if (data.Platform == PlatformsEnum.Discord) host = URLs.discord;
                        else if (data.Platform == PlatformsEnum.Twitch) host = URLs.twitch;
                        else if (data.Platform == PlatformsEnum.Telegram) host = URLs.telegram;

                        PingReply reply = new Ping().Send(host, 1000);
                        pingSpeed = reply.Status == IPStatus.Success ? reply.RoundtripTime : -1;
                    }

                    commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:ping", data.ChannelID, data.Platform)
                                .Replace("%version%", Engine.Version)
                                .Replace("%workTime%", Text.FormatTimeSpan(workTime, data.User.Language))
                                .Replace("%tabs%", data.Platform == PlatformsEnum.Twitch ? Engine.Bot.Clients.Twitch.JoinedChannels.Count.ToString() : (data.Platform == PlatformsEnum.Discord ? Engine.Bot.Clients.Discord.Guilds.Count.ToString() : (Engine.Bot.Clients.Twitch.JoinedChannels.Count + Engine.Bot.Clients.Discord.Guilds.Count) + " (Twitch, Discord)"))
                                .Replace("%loadedCMDs%", Runner.commandInstances.Count.ToString())
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
                    if (data.Platform == PlatformsEnum.Telegram)
                    {
                        pingSpeed = Services.External.TelegramService.Ping().Result;
                    }
                    else
                    {
                        if (data.Platform == PlatformsEnum.Discord) host = URLs.discord;
                        else if (data.Platform == PlatformsEnum.Twitch) host = URLs.twitch;
                        else if (data.Platform == PlatformsEnum.Telegram) host = URLs.telegram;

                        PingReply reply = new Ping().Send(host, 1000);
                        pingSpeed = reply.Status == IPStatus.Success ? reply.RoundtripTime : -1;
                    }

                    commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:ping:development", data.ChannelID, data.Platform)
                                .Replace("%version%", Engine.Version)
                                .Replace("%patch%", Engine.Patch)
                                .Replace("%workTime%", Text.FormatTimeSpan(workTime, data.User.Language))
                                .Replace("%tabs%", (Engine.Bot.Clients.Twitch.JoinedChannels.Count + Engine.Bot.Clients.Discord.Guilds.Count) + " (Twitch, Discord)")
                                .Replace("%loadedCMDs%", Runner.commandInstances.Count.ToString())
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
