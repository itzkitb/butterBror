using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace butterBror.Utils.Types.SevenTVLib
{
    internal class Emotes
    {
        [JsonPropertyName("count")]
        public int Count { get; set; }
        [JsonPropertyName("max_page")]
        public int MaxPage { get; set; }
        [JsonPropertyName("items")]
        public List<EmoteItem> Items { get; set; }
    }
}
