
using Newtonsoft.Json;

namespace bb.Models.AI
{
    /// <summary>
    /// Represents the request body structure for AI API calls.
    /// </summary>
    internal class RequestBody
    {
        /// <summary>
        /// Gets or sets the AI model to use for processing.
        /// </summary>
        [JsonProperty("model")]
        public string Model { get; set; }

        /// <summary>
        /// Gets or sets the list of messages in the conversation history.
        /// </summary>
        [JsonProperty("messages")]
        public List<Message> Messages { get; set; }

        /// <summary>
        /// Gets or sets the repetition penalty value to prevent repetitive outputs.
        /// </summary>
        [JsonProperty("repetition_penalty")]
        public double RepetitionPenalty { get; set; }

        [JsonProperty("temperature")]
        public double Temperature { get; set; }
    }
}
