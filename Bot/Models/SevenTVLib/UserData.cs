using System.Text.Json.Serialization;

namespace bb.Models.SevenTVLib
{
    internal class UserData
    {
        [JsonPropertyName("users")]
        public List<User> Users { get; set; }
    }
}
