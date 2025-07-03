using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace butterBror.Utils.Types.SevenTVLib
{
    internal class UserStyle
    {
        [JsonPropertyName("color")]
        public int Color { get; set; }

        [JsonPropertyName("__typename")]
        public string TypeName { get; set; }
    }
}
