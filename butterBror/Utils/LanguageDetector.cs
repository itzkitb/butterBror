using System;
using System.Collections.Generic;
using System.Linq;

namespace butterBror.Utils
{
    /// <summary>
    /// A utility class for detecting the language of text based on character Unicode ranges.
    /// Currently supports Russian language detection with extensibility for additional languages.
    /// </summary>
    public static class LanguageDetector
    {
        /// <summary>
        /// Collection of language definitions mapping language codes to their respective Unicode character ranges.
        /// Each definition consists of a language code and a list of character ranges that identify the language.
        /// </summary>
        private static readonly List<(string LanguageCode, List<(char Start, char End)> Ranges)> _languageDefinitions =
            new List<(string, List<(char, char)>)>
        {
            ("ru-RU", new List<(char, char)>
            {
                ('\u0410', '\u044F'), // Cyrillic uppercase and lowercase letters
                ('\u0401', '\u0401'), // Cyrillic capital letter Yo
                ('\u0451', '\u0451')  // Cyrillic small letter Yo
            })
        };

        /// <summary>
        /// Detects the language of the provided text by analyzing character Unicode ranges.
        /// Returns the first matching language code based on the presence of characters within defined ranges.
        /// </summary>
        /// <param name="text">The text to analyze for language detection</param>
        /// <returns>
        /// The detected language code (e.g., "ru-RU" for Russian) or "en-US" as default if no match is found or text is empty
        /// </returns>
        public static string DetectLanguage(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "en-US";

            foreach (char c in text)
            {
                foreach (var (languageCode, ranges) in _languageDefinitions)
                {
                    if (IsCharInRanges(c, ranges))
                        return languageCode;
                }
            }

            return "en-US";
        }

        /// <summary>
        /// Determines whether a character falls within any of the specified Unicode ranges.
        /// </summary>
        /// <param name="c">The character to check</param>
        /// <param name="ranges">The collection of character ranges to check against</param>
        /// <returns>
        /// <c>true</c> if the character is within any of the specified ranges; otherwise, <c>false</c>
        /// </returns>
        private static bool IsCharInRanges(char c, List<(char Start, char End)> ranges)
        {
            return ranges.Any(range => c >= range.Start && c <= range.End);
        }

        /// <summary>
        /// Adds a new language definition to the detector with specified Unicode character ranges.
        /// This allows extending the detector to recognize additional languages.
        /// </summary>
        /// <param name="languageCode">The IETF language tag for the new language (e.g., "zh-CN" for Chinese)</param>
        /// <param name="ranges">
        /// One or more Unicode character ranges that uniquely identify the language.
        /// Each range is specified as a tuple of start and end characters.
        /// </param>
        public static void AddLanguageDefinition(string languageCode, params (char Start, char End)[] ranges)
        {
            _languageDefinitions.Add((languageCode, new List<(char, char)>(ranges)));
        }
    }
}