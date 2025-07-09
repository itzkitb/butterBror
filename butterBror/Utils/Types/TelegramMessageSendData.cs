using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace butterBror.Utils.Types
{
    public class TelegramMessageSendData
    {
        public required string MessageID { get; set; }
        public required string Message { get; set; }
        public required string Channel { get; set; }
        public required string ChannelID { get; set; }
        public required string Language { get; set; }
        public required string Username { get; set; }
        public required bool SafeExecute { get; set; }
    }
}
