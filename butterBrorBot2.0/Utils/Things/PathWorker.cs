using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static butterBror.Core.Statistics;

namespace butterBror.Utils.Things
{
    public class PathWorker
    {
        public string General { get; set; } = string.Empty;
        private string _main_path;

        public string Main
        {
            get => _main_path;
            set
            {
                _main_path = Format(value);
                UpdatePaths();
            }
        }
        public string Channels { get; private set; } = string.Empty;
        public string Users { get; private set; } = string.Empty;
        public string NicknamesData { get; private set; } = string.Empty;
        public string Nick2ID { get; private set; } = string.Empty;
        public string ID2Nick { get; private set; } = string.Empty;
        public string Settings { get; private set; } = string.Empty;
        public string Cookies { get; private set; } = string.Empty;
        public string Translations { get; private set; } = string.Empty;
        public string TranslateDefault { get; private set; } = string.Empty;
        public string TranslateCustom { get; private set; } = string.Empty;
        public string BlacklistWords { get; private set; } = string.Empty;
        public string BlacklistReplacements { get; private set; } = string.Empty;
        public string APIUses { get; private set; } = string.Empty;
        public string Logs { get; private set; } = string.Empty;
        public string Errors { get; private set; } = string.Empty;
        public string Cache { get; private set; } = string.Empty;
        public string Currency { get; private set; } = string.Empty;
        public string SevenTVCache { get; private set; } = string.Empty;
        public string Reserve { get; private set; } = string.Empty;

        public void UpdatePaths()
        {
            FunctionsUsed.Add();

            Channels = Format(Path.Combine(Main, "CHNLS/"));
            Users = Format(Path.Combine(Main, "USERSDB/"));
            NicknamesData = Format(Path.Combine(Main, "CONVRT/"));
            Nick2ID = Format(Path.Combine(NicknamesData, "N2I/"));
            ID2Nick = Format(Path.Combine(NicknamesData, "I2N/"));
            Settings = Format(Path.Combine(Main, "SETTINGS.json"));
            Cookies = Format(Path.Combine(Main, "COOKIES.MDS"));
            Translations = Format(Path.Combine(Main, "TRNSLT/"));
            TranslateDefault = Format(Path.Combine(Translations, "DEFAULT/"));
            TranslateCustom = Format(Path.Combine(Translations, "CUSTOM/"));
            BlacklistWords = Format(Path.Combine(Main, "BNWORDS.txt"));
            BlacklistReplacements = Format(Path.Combine(Main, "BNWORDSREP.txt"));
            APIUses = Format(Path.Combine(Main, "API.json"));
            Logs = Format(Path.Combine(Main, "LOGS", $"{DateTime.UtcNow.ToString("dd_MM_yyyy HH.mm.ss")}.log"));
            Errors = Format(Path.Combine(Main, "ERRORS.log"));
            Cache = Format(Path.Combine(Main, "LOC.cache"));
            Currency = Format(Path.Combine(Main, "CURR.json"));
            SevenTVCache = Format(Path.Combine(Main, "7TV.json"));
            Reserve = Format(Path.Combine(General, "butterbror_reserves/", $"{DateTime.UtcNow.ToString("dd_MM_yyyy")}/"));
        }

        public string Format(string input)
        {
            return input.Replace("/", "\\");
        }
    }
}
