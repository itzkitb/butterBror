using System.Text.Json.Serialization;

namespace butterBror.Models.SevenTVLib
{
    internal class Owner
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        [JsonPropertyName("username")]
        public string Username { get; set; }
        [JsonPropertyName("display_name")]
        public string DisplayName { get; set; }
        [JsonPropertyName("style")]
        public Style Style { get; set; }
    }
}
