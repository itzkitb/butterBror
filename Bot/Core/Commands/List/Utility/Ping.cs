using bb.Utils;
using bb.Core.Configuration;
using System.Net.NetworkInformation;
using bb.Models.Command;
using bb.Models.Platform;
using bb.Models.Users;

namespace bb.Core.Commands.List.Utility
{
    public class Ping : CommandBase
    {
        public override string Name => "Ping";
        public override string Author => "https://github.com/itzkitb";
        public override string Source => "Utility/Ping.cs";
        public override Dictionary<Language, string> Description => new() {
            { Language.RuRu, "Узнать пинг бота." },
            { Language.EnUs, "Find out the bot's ping." }
        };
        public override int UserCooldown => 10;
        public override int Cooldown => 1;
        public override string[] Aliases => ["ping", "пинг", "понг", "пенг", "п"];
        public override string Help => "[host]";
        public override DateTime CreationDate => DateTime.Parse("2024-07-04T00:00:00.0000000Z");
        public override Roles RoleRequired => Roles.Public;
        public override Platform[] Platforms => [Platform.Twitch, Platform.Telegram, Platform.Discord];
        public override bool IsAsync => false;

        public override CommandReturn Execute(CommandData data)
        {
            CommandReturn commandReturn = new CommandReturn();

            try
            {
                if (data.ChannelId == null || Program.BotInstance.Clients.Twitch == null || Program.BotInstance.Clients.Discord == null)
                {
                    commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:unknown", string.Empty, data.Platform));
                    return commandReturn;
                }

                string argument = "";
                if (data.Arguments != null && data.Arguments.Count > 0)
                {
                    argument = data.Arguments[0].ToLower();
                }

                if (data.Arguments != null && data.Arguments.Count == 0)
                {
                    var workTime = DateTime.UtcNow - Program.BotInstance.StartTime;
                    string host = "";
                    long pingSpeed = 0;
                    if (data.Platform == Platform.Telegram)
                    {
                        pingSpeed = bb.Services.External.TelegramPing.Ping().Result;
                    }
                    else
                    {
                        if (data.Platform == Platform.Discord) host = URLs.discord;
                        else if (data.Platform == Platform.Twitch) host = URLs.twitch;
                        else if (data.Platform == Platform.Telegram) host = URLs.telegram;

                        PingReply reply = new System.Net.NetworkInformation.Ping().Send(host, 1000);
                        pingSpeed = reply.Status == IPStatus.Success ? reply.RoundtripTime : -1;
                    }

                    long joinedTabs = data.Platform == Platform.Twitch ? Program.BotInstance.Clients.Twitch.JoinedChannels.Count : data.Platform == Platform.Discord ? Program.BotInstance.Clients.Discord.Guilds.Count : Program.BotInstance.Clients.Twitch.JoinedChannels.Count + Program.BotInstance.Clients.Discord.Guilds.Count;

                    commandReturn.SetMessage(LocalizationService.GetString(
                        data.User.Language,
                        "command:ping",
                        data.ChannelId,
                        data.Platform,
                        Program.BotInstance.Version + $" ({Program.BotInstance.Branch}/{Program.BotInstance.Commit})",
                        TextSanitizer.FormatTimeSpan(workTime, data.User.Language),
                        LocalizationService.GetPluralString(data.User.Language, "text:tab", data.ChannelId, data.Platform, joinedTabs, joinedTabs),
                        LocalizationService.GetPluralString(data.User.Language, "text:commands", data.ChannelId, data.Platform, Program.BotInstance.CommandRunner.commandInstances.Count, Program.BotInstance.CommandRunner.commandInstances.Count),
                        Program.BotInstance.CompletedCommands,
                        pingSpeed.ToString()));
                }
                else if (argument.Equals("host"))
                {
                    var workTime = DateTime.UtcNow - Program.BotInstance.StartTime;
                    PingReply reply = new System.Net.NetworkInformation.Ping().Send("192.168.1.1", 1000);
                    long pingSpeed = -1;
                    if (reply.Status == IPStatus.Success) pingSpeed = reply.RoundtripTime;
                    else
                    {
                        reply = new System.Net.NetworkInformation.Ping().Send("192.168.0.1", 1000);
                        if (reply.Status == IPStatus.Success) pingSpeed = reply.RoundtripTime;
                    }

                    commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "command:ping:isp", data.ChannelId, data.Platform, pingSpeed.ToString()));
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
