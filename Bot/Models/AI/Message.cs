
using Newtonsoft.Json;

namespace bb.Models.AI
{
    /// <summary>
    /// Represents a chat message in the AI conversation history.
    /// </summary>
    internal class Message
    {
        /// <summary>
        /// Gets or sets the role of the message sender (system/user/assistant).
        /// </summary>
        [JsonProperty("role")]
        public string Role { get; set; }

        /// <summary>
        /// Gets or sets the content of the message.
        /// </summary>
        [JsonProperty("content")]
        public string Content { get; set; }
    }
}
