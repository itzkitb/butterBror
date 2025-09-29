using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bb.Models.Exceptions
{
    public class TelegramTokenNullException : Exception
    {
        public TelegramTokenNullException()
            : base("Telegram token cannot be null or empty.") { }

        public TelegramTokenNullException(string message)
            : base(message) { }

        public TelegramTokenNullException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
