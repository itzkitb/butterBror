using System.Text.Json.Serialization;

namespace butterBror.Models.SevenTVLib
{
    internal class Data
    {
        [JsonPropertyName("emotes")]
        public Emotes Emotes { get; set; }
    }
}
