using System.Text.Json.Serialization;

namespace bb.Models.SevenTVLib
{
    internal class SearchResponse
    {
        [JsonPropertyName("data")]
        public Data Data { get; set; }
    }
}
