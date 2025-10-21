using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bb.Models.Exceptions
{
    public class TwitchClientNullException : Exception
    {
        public TwitchClientNullException()
            : base("Twitch client cannot be null or empty.") { }

        public TwitchClientNullException(string message)
            : base(message) { }

        public TwitchClientNullException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
