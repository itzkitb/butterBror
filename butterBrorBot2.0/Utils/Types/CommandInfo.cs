﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace butterBror.Utils.Types
{
    public class CommandInfo
    {
        public required string Name { get; set; }
        public required string Author { get; set; }
        public required string AuthorLink { get; set; }
        public required string AuthorAvatar { get; set; }
        public required string[] Aliases { get; set; }
        public required int CooldownPerChannel { get; set; }
        public required int CooldownPerUser { get; set; }
        public required Dictionary<string, string> Description { get; set; }
        public required string WikiLink { get; set; }
        public required string Arguments { get; set; }
        public required bool CooldownReset { get; set; }
        public required DateTime CreationDate { get; set; }
        public required bool IsForBotModerator { get; set; }
        public required bool IsForChannelModerator { get; set; }
        public required bool IsForBotDeveloper { get; set; }
        public double? cost { get; set; }
        public required Platforms[] Platforms { get; set; }

        public bool is_on_development { get; set; }
    }
}
