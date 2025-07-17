using System.Text.Json.Serialization;

namespace butterBror.Models.SevenTVLib
{
    internal class Style
    {
        [JsonPropertyName("color")]
        public long Color { get; set; }
        [JsonPropertyName("paint_id")]
        public string PaintId { get; set; }
    }
}
