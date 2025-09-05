
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
