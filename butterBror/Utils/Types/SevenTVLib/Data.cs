using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace butterBror.Utils.Types.SevenTVLib
{
    internal class Data
    {
        [JsonPropertyName("emotes")]
        public Emotes Emotes { get; set; }
    }
}
