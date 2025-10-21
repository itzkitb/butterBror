using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bb.Models.Currency
{
    internal class RandomEvent
    {
        [JsonProperty("text")]
        public string Text { get; set; }
        [JsonProperty("added")]
        public int Added { get; set; }
    }
}
