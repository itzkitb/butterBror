using bb.Models.Platform;
using bb.Models.Users;
using Discord.WebSocket;
using Telegram.Bot.Types;
using TwitchLib.Client.Events;

namespace bb.Models.Command
{
    public class CommandData
    {
        public required string Name { set; get; }
        public List<string>? Arguments { get; set; }
        public OnMessageReceivedArgs? TwitchMessage { get; set; }
        public Dictionary<string, dynamic>? DiscordArguments { get; set; }
        public string MessageID { get; set; }
        public string? Channel { get; set; }
        public string? ChannelId { get; set; }
        public required string ArgumentsString { get; set; }
        public SocketCommandBase? DiscordCommandBase { get; set; }
        public required PlatformsEnum Platform { get; set; }
        public required UserData User { get; set; }
        public required string CommandInstanceID { get; set; }
        public Message? TelegramMessage { get; set; }
        public string ServerID { get; set; }
        public string Server { get; set; }
        public string ChatID { get; set; }
    }
}
