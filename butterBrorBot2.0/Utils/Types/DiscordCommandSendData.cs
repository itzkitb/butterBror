using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace butterBror.Utils.Types
{
    public class DiscordCommandSendData
    {
        public required string Description { get; set; }
        public string? Author { get; set; }
        public string? ImageLink { get; set; }
        public string? ThumbnailLink { get; set; }
        public string? Footer { get; set; }
        public required bool IsEmbed { get; set; }
        public required bool IsEphemeral { get; set; }
        public string? Title { get; set; }
        public Discord.Color? EmbedColor { get; set; }
        public required string Server { get; set; }
        public required string ServerID { get; set; }
        public required string ChannelID { get; set; }
        public required string Language { get; set; }
        public string? Message { get; set; }
        public required bool SafeExecute { get; set; }
        public required SocketCommandBase SocketCommandBase { get; set; }
        public required string UserID { get; set; }
    }
}
