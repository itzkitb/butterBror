using System.Text.Json.Serialization;

namespace butterBror.Models.SevenTVLib
{
    internal class File
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("format")]
        public string Format { get; set; }
        [JsonPropertyName("width")]
        public int Width { get; set; }
        [JsonPropertyName("height")]
        public int Height { get; set; }
    }
}
