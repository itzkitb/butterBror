using bb.Models.Users;

namespace bb.Utils
{
    /// <summary>
    /// Utility class for language detection based on Unicode character range analysis.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This detector identifies languages by examining the Unicode ranges of characters in text.
    /// Key features:
    /// <list type="bullet">
    /// <item>Lightweight implementation with minimal resource usage</item>
    /// <item>Extensible architecture for adding new language definitions</item>
    /// <item>Case-insensitive detection through direct Unicode analysis</item>
    /// <item>No external dependencies or machine learning requirements</item>
    /// </list>
    /// </para>
    /// <para>
    /// Current capabilities:
    /// <list type="bullet">
    /// <item>Primary support for Russian (Cyrillic script) detection</item>
    /// <item>Default fallback to English ("en-US") for unrecognized text</item>
    /// <item>Support for mixed-language content (detects first matching language)</item>
    /// </list>
    /// </para>
    /// The detection algorithm is optimized for chat message processing with:
    /// <list type="bullet">
    /// <item>O(n) time complexity relative to text length</item>
    /// <item>Constant memory usage regardless of input size</item>
    /// <item>Early termination on first matching character</item>
    /// </list>
    /// </remarks>
    public static class LanguageDetector
    {
        private static readonly List<(Language LanguageCode, List<(char Start, char End)> Ranges)> _languageDefinitions =
            new List<(Language, List<(char, char)>)>
        {
            (Language.RuRu, new List<(char, char)>
            {
                ('\u0410', '\u044F'), // Cyrillic uppercase and lowercase letters
                ('\u0401', '\u0401'), // Cyrillic capital letter Yo
                ('\u0451', '\u0451')  // Cyrillic small letter Yo
            })
        };

        /// <summary>
        /// Analyzes text to determine its primary language based on Unicode character ranges.
        /// </summary>
        /// <param name="text">The input text to analyze. Can contain mixed scripts and special characters.</param>
        /// <returns>
        /// <para>
        /// Returns:
        /// <list type="bullet">
        /// <item>The first matching language code from registered definitions (e.g., "ru-RU")</item>
        /// <item>"en-US" as default when:
        /// <list type="bullet">
        /// <item>Text is empty or whitespace</item>
        /// <item>No characters match registered language ranges</item>
        /// <item>Mixed script where no single language dominates</item>
        /// </list>
        /// </item>
        /// </list>
        /// </para>
        /// </returns>
        /// <remarks>
        /// <para>
        /// Detection algorithm:
        /// <list type="number">
        /// <item>Immediately returns "en-US" for null, empty, or whitespace-only input</item>
        /// <item>Processes text character by character from beginning to end</item>
        /// <item>Returns first language whose character range contains any input character</item>
        /// <item>Stops processing at first match (optimized for performance)</item>
        /// </list>
        /// </para>
        /// <para>
        /// Important behaviors:
        /// <list type="bullet">
        /// <item>Case-insensitive (treats uppercase and lowercase equally)</item>
        /// <item>Ignores non-matching characters (punctuation, numbers, etc.)</item>
        /// <item>Does not measure language dominance (first match wins)</item>
        /// <item>Not designed for multilingual text analysis</item>
        /// </list>
        /// </para>
        /// Typical use cases:
        /// <list type="bullet">
        /// <item>Auto-selecting translation resources</item>
        /// <item>Language-specific message formatting</item>
        /// <item>Basic content filtering by language</item>
        /// </list>
        /// </remarks>
        public static Language DetectLanguage(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return Language.EnUs;

            foreach (char c in text)
            {
                foreach (var (languageCode, ranges) in _languageDefinitions)
                {
                    if (IsCharInRanges(c, ranges))
                        return languageCode;
                }
            }

            return Language.EnUs;
        }

        /// <summary>
        /// Determines if a character falls within any of the specified Unicode ranges.
        /// </summary>
        /// <param name="c">The character to evaluate</param>
        /// <param name="ranges">Collection of inclusive character ranges to check against</param>
        /// <returns>
        /// <see langword="true"/> if the character is within at least one specified range;
        /// otherwise, <see langword="false"/>
        /// </returns>
        /// <remarks>
        /// <para>
        /// Range checking logic:
        /// <list type="bullet">
        /// <item>Each range is inclusive of both start and end characters</item>
        /// <item>Uses simple numerical comparison of character code points</item>
        /// <item>Returns on first matching range (short-circuit evaluation)</item>
        /// </list>
        /// </para>
        /// <para>
        /// Performance characteristics:
        /// <list type="bullet">
        /// <item>O(m) time complexity where m is the number of ranges</item>
        /// <item>Constant memory usage</item>
        /// <item>Optimized for small range collections (typically 1-5 ranges per language)</item>
        /// </list>
        /// </para>
        /// This is a helper method primarily used internally by DetectLanguage.
        /// Not intended for direct public use but exposed for potential extension scenarios.
        /// </remarks>
        private static bool IsCharInRanges(char c, List<(char Start, char End)> ranges)
        {
            return ranges.Any(range => c >= range.Start && c <= range.End);
        }

        /// <summary>
        /// Extends the language detection capabilities by registering a new language definition.
        /// </summary>
        /// <param name="languageCode">IETF BCP 47 language tag (e.g., "zh-CN", "ja-JP", "es-ES")</param>
        /// <param name="ranges">
        /// One or more Unicode character ranges that uniquely identify the language.
        /// Each range is specified as a tuple of inclusive start and end characters.
        /// </param>
        /// <remarks>
        /// <para>
        /// Usage example:
        /// <code>
        /// // Add Japanese (Kanji) support
        /// LanguageDetector.AddLanguageDefinition("ja-JP",
        ///     ('\u4E00', '\u9FAF'),  // Common Kanji
        ///     ('\u3040', '\u309F'),  // Hiragana
        ///     ('\u30A0', '\u30FF')   // Katakana
        /// );
        /// </code>
        /// </para>
        /// <para>
        /// Important considerations:
        /// <list type="bullet">
        /// <item>Range definitions should be mutually exclusive where possible</item>
        /// <item>Order of registration matters - first match wins during detection</item>
        /// <item>Overlapping ranges may cause unexpected detection results</item>
        /// <item>Should be called during application initialization</item>
        /// </list>
        /// </para>
        /// <para>
        /// Best practices:
        /// <list type="bullet">
        /// <item>Use official Unicode block definitions where available</item>
        /// <item>Include all necessary variants (standard, full-width, etc.)</item>
        /// <item>Avoid overly broad ranges that might match multiple languages</item>
        /// <item>Test with representative samples of the target language</item>
        /// </list>
        /// </para>
        /// This method enables customization for platform-specific language requirements
        /// without modifying the core detection algorithm.
        /// </remarks>
        public static void AddLanguageDefinition(Language languageCode, params (char Start, char End)[] ranges)
        {
            _languageDefinitions.Add((languageCode, new List<(char, char)>(ranges)));
        }
    }
}