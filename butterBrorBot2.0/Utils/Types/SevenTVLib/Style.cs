using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace butterBror.Utils.Types.SevenTVLib
{
    internal class Style
    {
        [JsonPropertyName("color")]
        public long Color { get; set; }
        [JsonPropertyName("paint_id")]
        public string PaintId { get; set; }
    }
}
