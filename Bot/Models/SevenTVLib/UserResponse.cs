using System.Text.Json.Serialization;

namespace bb.Models.SevenTVLib
{
    internal class UserResponse
    {
        [JsonPropertyName("data")]
        public UserData Data { get; set; }
    }
}
