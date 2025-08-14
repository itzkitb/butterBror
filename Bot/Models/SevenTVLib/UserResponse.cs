using System.Text.Json.Serialization;

namespace butterBror.Models.SevenTVLib
{
    internal class UserResponse
    {
        [JsonPropertyName("data")]
        public UserData Data { get; set; }
    }
}
