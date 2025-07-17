
namespace butterBror.Models.AI
{
    /// <summary>
    /// Represents the complete response body from the AI API.
    /// </summary>
    internal class ResponseBody
    {
        /// <summary>
        /// Gets or sets the list of response choices from the AI model.
        /// </summary>
        public List<Choice> choices { get; set; }
    }
}
