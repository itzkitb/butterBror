using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace butterBror.Utils.Types.AI
{
    /// <summary>
    /// Represents the request body structure for AI API calls.
    /// </summary>
    internal class RequestBody
    {
        /// <summary>
        /// Gets or sets the AI model to use for processing.
        /// </summary>
        public string model { get; set; }

        /// <summary>
        /// Gets or sets the list of messages in the conversation history.
        /// </summary>
        public List<Message> messages { get; set; }

        /// <summary>
        /// Gets or sets the repetition penalty value to prevent repetitive outputs.
        /// </summary>
        public double repetition_penalty { get; set; }
    }
}
