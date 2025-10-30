using bb.Models.Platform;

namespace bb.Data.Entities
{
    public class UserChange
    {
        public Platform Platform { get; set; }
        public long UserId { get; set; }
        public Dictionary<string, object> Changes { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, int> ChannelMessageCounts { get; set; } = new Dictionary<string, int>();
        public int GlobalMessageCountIncrement { get; set; }
        public int GlobalMessageLengthIncrement { get; set; }
    }
}
