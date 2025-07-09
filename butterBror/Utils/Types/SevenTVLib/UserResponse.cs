using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace butterBror.Utils.Types.SevenTVLib
{
    internal class UserResponse
    {
        [JsonPropertyName("data")]
        public UserData Data { get; set; }
    }
}
