﻿using System.Text.Json.Serialization;

namespace butterBror.Models.SevenTVLib
{
    internal class Host
    {
        [JsonPropertyName("url")]
        public string Url { get; set; }
        [JsonPropertyName("files")]
        public List<File> Files { get; set; }
    }
}
