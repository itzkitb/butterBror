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
            { "ru-RU", "Узнать пинг бота." },
            { "en-US", "Find out the bot's ping." }
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

                    long joinedTabs = data.Platform == PlatformsEnum.Twitch ? Engine.Bot.Clients.Twitch.JoinedChannels.Count : (data.Platform == PlatformsEnum.Discord ? Engine.Bot.Clients.Discord.Guilds.Count : (Engine.Bot.Clients.Twitch.JoinedChannels.Count + Engine.Bot.Clients.Discord.Guilds.Count));

                    commandReturn.SetMessage(LocalizationService.GetString(
                        data.User.Language,
                        "command:ping",
                        data.ChannelId,
                        data.Platform,
                        Engine.Version,
                        Engine.Patch,
                        Text.FormatTimeSpan(workTime, data.User.Language),
                        LocalizationService.GetPluralString(data.User.Language, "text:tab", data.ChannelId, data.Platform, joinedTabs, joinedTabs),
                        LocalizationService.GetPluralString(data.User.Language, "text:commands", data.ChannelId, data.Platform, Runner.commandInstances.Count, Runner.commandInstances.Count),
                        Engine.CompletedCommands,
                        pingSpeed.ToString()));
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

                    commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "command:ping:isp", data.ChannelId, data.Platform, pingSpeed.ToString()));
                }
                /*else if (argument.Equals("dev"))
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

                    commandReturn.SetMessage(LocalizationService.GetString(
                        data.User.Language,
                        "command:ping:development",
                        data.ChannelId,
                        data.Platform,
                        Engine.Version,
                        Engine.Patch,
                        Text.FormatTimeSpan(workTime, data.User.Language),
                        (Engine.Bot.Clients.Twitch.JoinedChannels.Count + Engine.Bot.Clients.Discord.Guilds.Count) + " (Twitch, Discord)",
                        Runner.commandInstances.Count,
                        pingSpeed.ToString(),
                        Engine.TicksPerSecond.ToString(),
                        Engine.Ticks.ToString(),
                        Engine.TickDelay.ToString(),
                        Engine.TicksCounter.ToString(),
                        Engine.SkippedTicks.ToString()));
                }*/
            }
            catch (Exception e)
            {
                commandReturn.SetError(e);
            }

            return commandReturn;
        }
    }
}
