using System.Text.Json.Serialization;

namespace bb.Models.SevenTVLib
{
    internal class EmoteItem
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("state")]
        public List<string> State { get; set; }
        [JsonPropertyName("trending")]
        public dynamic? Trending { get; set; }
        [JsonPropertyName("owner")]
        public Owner Owner { get; set; }
        [JsonPropertyName("flags")]
        public int Flags { get; set; }
        [JsonPropertyName("host")]
        public Host Host { get; set; }
    }
}
