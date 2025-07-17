﻿using TwitchLib.Client.Enums;

namespace butterBror.Models
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
