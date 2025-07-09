using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using TwitchLib.Client.Events;

namespace butterBror.Utils.Types
{
    public class CommandData
    {
        public required string Name { set; get; }
        public List<string>? Arguments { get; set; }
        public OnChatCommandReceivedArgs? TwitchArguments { get; set; }
        public Dictionary<string, dynamic>? DiscordArguments { get; set; }
        public string MessageID { get; set; }
        public required string UserID { get; set; }
        public string? Channel { get; set; }
        public string? ChannelID { get; set; }
        public required string ArgumentsString { get; set; }
        public SocketCommandBase? DiscordCommandBase { get; set; }
        public required Platforms Platform { get; set; }
        public required UserData User { get; set; }
        public required string CommandInstanceID { get; set; }
        public Message? TelegramMessage { get; set; }
        public string ServerID { get; set; }
        public string Server { get; set; }
    }
}
