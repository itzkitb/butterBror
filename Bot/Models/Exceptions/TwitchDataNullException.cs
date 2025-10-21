using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bb.Models.Exceptions
{
    public class TwitchDataNullException : Exception
    {
        public TwitchDataNullException()
            : base("Twitch data cannot be null or empty.") { }

        public TwitchDataNullException(string message)
            : base(message) { }

        public TwitchDataNullException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
