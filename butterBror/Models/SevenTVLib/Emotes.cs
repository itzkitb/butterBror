using System.Text.Json.Serialization;

namespace butterBror.Models.SevenTVLib
{
    internal class Emotes
    {
        [JsonPropertyName("count")]
        public int Count { get; set; }
        [JsonPropertyName("max_page")]
        public int MaxPage { get; set; }
        [JsonPropertyName("items")]
        public List<EmoteItem> Items { get; set; }
    }
}
