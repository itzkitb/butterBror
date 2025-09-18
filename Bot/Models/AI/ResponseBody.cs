
using Newtonsoft.Json;

namespace bb.Models.AI
{
    /// <summary>
    /// Represents the complete response body from the AI API.
    /// </summary>
    internal class ResponseBody
    {
        /// <summary>
        /// Gets or sets the list of response choices from the AI model.
        /// </summary>
        [JsonProperty("choices")]
        public List<Choice> Choices { get; set; }
    }
}
