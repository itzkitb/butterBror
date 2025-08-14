using System.Text.Json.Serialization;

namespace butterBror.Models.SevenTVLib
{
    internal class SearchResponse
    {
        [JsonPropertyName("data")]
        public Data Data { get; set; }
    }
}
