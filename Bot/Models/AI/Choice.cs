
using Newtonsoft.Json;

namespace bb.Models.AI
{
    /// <summary>
    /// Represents a single response choice from the AI model.
    /// </summary>
    internal class Choice
    {
        /// <summary>
        /// Gets or sets the message response from the AI model.
        /// </summary>
        [JsonProperty("message")]
        public Message Message { get; set; }
    }
}
