using System.Text.Json.Serialization;

namespace butterBror.Models.SevenTVLib
{
    internal class UserData
    {
        [JsonPropertyName("users")]
        public List<User> Users { get; set; }
    }
}
