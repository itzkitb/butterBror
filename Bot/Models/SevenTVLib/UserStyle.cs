using System.Text.Json.Serialization;

namespace bb.Models.SevenTVLib
{
    internal class UserStyle
    {
        [JsonPropertyName("color")]
        public int Color { get; set; }

        [JsonPropertyName("__typename")]
        public string TypeName { get; set; }
    }
}
