using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace butterBror.Utils.Types.AI
{
    /// <summary>
    /// Represents a single response choice from the AI model.
    /// </summary>
    internal class Choice
    {
        /// <summary>
        /// Gets or sets the message response from the AI model.
        /// </summary>
        public Message message { get; set; }
    }
}
