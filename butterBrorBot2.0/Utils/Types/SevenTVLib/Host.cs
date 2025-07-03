using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace butterBror.Utils.Types.SevenTVLib
{
    internal class Host
    {
        [JsonPropertyName("url")]
        public string Url { get; set; }
        [JsonPropertyName("files")]
        public List<File> Files { get; set; }
    }
}
