using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.Client.Enums;

namespace butterBror.Utils.Types
{
    public class TwitchMessageSendData
    {
        public required string Message { get; set; }
        public required string Channel { get; set; }
        public required string ChannelID { get; set; }
        public required string MessageID { get; set; }
        public required string Language { get; set; }
        public required string Username { get; set; }
        public required bool SafeExecute { get; set; }
        public required ChatColorPresets UsernameColor { get; set; }
    }
}
