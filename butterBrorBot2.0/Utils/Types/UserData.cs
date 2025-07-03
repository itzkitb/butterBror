using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace butterBror.Utils.Types
{
    public class UserData
    {
        public required string ID { get; set; }
        public required string Language { get; set; }
        public required string Username { get; set; }
        public int? Balance { get; set; }
        public int? BalanceFloat { get; set; }
        public int? TotalMessages { get; set; }
        public bool? IsBanned { get; set; }
        public bool? Ignored { get; set; }
        public bool? IsModerator { get; set; }
        public bool? IsBroadcaster { get; set; }
        public bool? IsBotModerator { get; set; }
        public bool? IsBotDeveloper { get; set; }
    }
}
