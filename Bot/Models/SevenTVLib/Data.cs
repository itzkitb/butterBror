using System.Text.Json.Serialization;

namespace bb.Models.SevenTVLib
{
    internal class Data
    {
        [JsonPropertyName("emotes")]
        public Emotes Emotes { get; set; }
    }
}
