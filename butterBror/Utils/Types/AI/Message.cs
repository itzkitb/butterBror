using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace butterBror.Utils.Types.AI
{
    /// <summary>
    /// Represents a chat message in the AI conversation history.
    /// </summary>
    internal class Message
    {
        /// <summary>
        /// Gets or sets the role of the message sender (system/user/assistant).
        /// </summary>
        public string role { get; set; }

        /// <summary>
        /// Gets or sets the content of the message.
        /// </summary>
        public string content { get; set; }
    }
}
