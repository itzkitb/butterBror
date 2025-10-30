using bb.Models.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bb.Utils
{
    public static class LanguageExtensions
    {
        private static readonly Dictionary<Language, string> LanguageToStringMap = new()
            {
                { Language.EnUs, "en-US" },
                { Language.RuRu, "ru-RU" }
            };

        public static string ToStringFormat(this Language language)
        {
            return LanguageToStringMap.TryGetValue(language, out var result)
                ? result
                : "en-US";
        }
    }
}
