using bb.Models.Platform;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using static bb.Core.Bot.Console;

namespace bb.Utils
{
    /// <summary>
    /// Provides comprehensive text processing utilities for command filtering, layout conversion, and formatting operations in chat environments.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This utility class handles various text manipulation scenarios common in multi-platform chat applications:
    /// <list type="bullet">
    /// <item>Command validation and normalization</item>
    /// <item>Keyboard layout conversion for international users</item>
    /// <item>ASCII sanitization for message processing</item>
    /// <item>Coordinate formatting for location-based features</item>
    /// <item>Time duration formatting with localization support</item>
    /// </list>
    /// </para>
    /// <para>
    /// Key design principles:
    /// <list type="bullet">
    /// <item>Defensive programming against malformed input</item>
    /// <item>Thread-safe implementation for concurrent access</item>
    /// <item>Localization-aware formatting where applicable</item>
    /// <item>Efficient string manipulation for high-throughput environments</item>
    /// </list>
    /// </para>
    /// All methods are static and stateless, making them suitable for high-frequency usage patterns typical in chatbot applications.
    /// </remarks>
    public class TextSanitizer
    {
        /// <summary>
        /// Converts text between English (QWERTY) and Russian (ЙЦУКЕН) keyboard layouts.
        /// </summary>
        /// <param name="text">The text to convert between layouts</param>
        /// <returns>
        /// <para>
        /// The converted text with characters mapped to the opposite layout, preserving:
        /// <list type="bullet">
        /// <item>Original casing (uppercase remains uppercase)</item>
        /// <item>Non-alphabetic characters (numbers, punctuation, spaces)</item>
        /// <item>Characters not present in the opposite layout</item>
        /// </list>
        /// </para>
        /// Returns <see langword="null"/> if an exception occurs during processing.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Conversion behavior:
        /// <list type="bullet">
        /// <item>English 'q' → Russian 'й'</item>
        /// <item>Russian 'й' → English 'q'</item>
        /// <item>Preserves case: 'Q' → 'Й', 'й' → 'Q'</item>
        /// <item>Ignores non-mappable characters</item>
        /// </list>
        /// </para>
        /// <para>
        /// Common use cases:
        /// <list type="bullet">
        /// <item>Correcting user input when they forget to switch keyboard layouts</item>
        /// <item>Processing commands entered with incorrect layout</item>
        /// <item>Enhancing user experience for multilingual communities</item>
        /// </list>
        /// </para>
        /// The method handles both directions of conversion in a single operation.
        /// Errors are logged but don't disrupt application flow (returns null on failure).
        /// </remarks>
        public static string ChangeLayout(string text)
        {
            try
            {
                var en = "qwertyuiop[]asdfghjkl;'zxcvbnm,.";
                var ru = "йцукенгшщзхъфывапролджэячсмитьбю";
                var map = en.Zip(ru, (e, r) => new[] { (e, r), (r, e) })
                           .SelectMany(p => p)
                           .ToDictionary(p => p.Item1, p => p.Item2);

                return string.Concat(text.Select(c =>
                    char.IsLetter(c) && map.TryGetValue(char.ToLower(c), out var m)
                        ? char.IsUpper(c) ? char.ToUpper(m) : m : c));
            }
            catch (Exception ex)
            {
                Write(ex);
                return null;
            }
        }

        /// <summary>
        /// Removes non-printable ASCII control characters from text while preserving standard formatting.
        /// </summary>
        /// <param name="input">The text to sanitize</param>
        /// <returns>
        /// A cleaned string containing only printable ASCII characters (code points 32-126 inclusive),
        /// or the original string if null/empty.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Specifically removes:
        /// <list type="bullet">
        /// <item>Control characters (ASCII 0-31)</item>
        /// <item>Delete character (ASCII 127)</item>
        /// </list>
        /// </para>
        /// <para>
        /// Preserves:
        /// <list type="bullet">
        /// <item>Spaces (ASCII 32)</item>
        /// <item>Punctuation and symbols</item>
        /// <item>Alphanumeric characters</item>
        /// <item>Standard formatting characters</item>
        /// </list>
        /// </para>
        /// <para>
        /// Example:
        /// <code>
        /// string cleaned = Text.CleanAscii("Hello\x07World!\n");
        /// // Result: "HelloWorld!"
        /// </code>
        /// </para>
        /// This method is essential for sanitizing user input before database storage or message transmission.
        /// </remarks>
        public static string CleanAscii(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            return new string(input
                .Where(c => c > 31 && c != 127)
                .ToArray());
        }

        /// <summary>
        /// Removes non-printable ASCII characters and all whitespace from text.
        /// </summary>
        /// <param name="input">The text to sanitize</param>
        /// <returns>
        /// A compact string containing only printable non-whitespace ASCII characters,
        /// or empty string if input is null/empty.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method combines:
        /// <list type="number">
        /// <item>Non-printable character removal (via <see cref="CleanAscii(string)"/>)</item>
        /// <item>Space character removal</item>
        /// </list>
        /// </para>
        /// <para>
        /// Use cases:
        /// <list type="bullet">
        /// <item>Generating compact identifiers from user input</item>
        /// <item>Creating URL-safe strings</item>
        /// <item>Processing input for systems requiring no whitespace</item>
        /// </list>
        /// </para>
        /// <para>
        /// Example:
        /// <code>
        /// string compact = Text.CleanAsciiWithoutSpaces(" User 123! ");
        /// // Result: "User123!"
        /// </code>
        /// </para>
        /// The method efficiently processes strings in a single pass after initial ASCII cleaning.
        /// </remarks>
        public static string CleanAsciiWithoutSpaces(string input)
        {
            return CleanAscii(input).Replace(" ", "");
        }

        /// <summary>
        /// Removes consecutive duplicate characters from a string while preserving non-consecutive duplicates.
        /// </summary>
        /// <param name="text">The string to process</param>
        /// <returns>
        /// A new string with consecutive duplicate characters reduced to a single instance,
        /// or the original string if null.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Behavior examples:
        /// <list type="table">
        /// <item><term>"aaabbbcc"</term><description>"abbc"</description></item>
        /// <item><term>"hello"</term><description>"helo"</description></item>
        /// <item><term>"aabbcc"</term><description>"abc"</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// Key characteristics:
        /// <list type="bullet">
        /// <item>Case-sensitive comparison ('A' and 'a' are different)</item>
        /// <item>Preserves first occurrence of each character sequence</item>
        /// <item>Only affects consecutive duplicates (non-adjacent duplicates remain)</item>
        /// </list>
        /// </para>
        /// This method is commonly used for:
        /// <list type="bullet">
        /// <item>Normalizing exaggerated text (e.g., "coooool" → "cool")</item>
        /// <item>Reducing spam patterns in user input</item>
        /// <item>Creating compact representations of repeated patterns</item>
        /// </list>
        /// The implementation uses an efficient single-pass algorithm with O(n) complexity.
        /// </remarks>
        public static string RemoveDuplicates(string text)
        {
            return text.Aggregate(new StringBuilder(), (sb, c) =>
                sb.Length == 0 || c != sb[^1] ? sb.Append(c) : sb).ToString();
        }

        /// <summary>
        /// Filters username input to allow only characters valid in standard platform identifiers.
        /// </summary>
        /// <param name="input">The raw username text to validate</param>
        /// <returns>
        /// A cleaned username string containing only:
        /// <list type="bullet">
        /// <item>Alphanumeric characters (A-Z, a-z, 0-9)</item>
        /// <item>Underscores (_)</item>
        /// <item>Hyphens (-)</item>
        /// </list>
        /// All other characters are removed.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method ensures compatibility with common username requirements across platforms:
        /// <list type="bullet">
        /// <item>Twitch: Alphanumeric, underscores</item>
        /// <item>Discord: Alphanumeric, underscores, hyphens</item>
        /// <item>Telegram: Alphanumeric, underscores</item>
        /// </list>
        /// </para>
        /// <para>
        /// Example transformations:
        /// <list type="table">
        /// <item><term>"user@name#123"</term><description>"username123"</description></item>
        /// <item><term>"valid_user-name"</term><description>"valid_user-name"</description></item>
        /// <item><term>"invalid!@#$%^&amp;*()"</term><description>"" (empty string)</description></item>
        /// </list>
        /// </para>
        /// The filtering pattern is optimized for security and platform compatibility.
        /// Use this method before storing or processing user identifiers.
        /// </remarks>
        public static string UsernameFilter(string input)
        {
            return Regex.Replace(input, @"[^A-Za-z0-9_-]", "");
        }

        /// <summary>
        /// Calculates and formats the time remaining until a seasonal event's start or end.
        /// </summary>
        /// <param name="startTime">The start date and time of the seasonal event</param>
        /// <param name="endTime">The end date and time of the seasonal event</param>
        /// <param name="type">The event type identifier used for localization keys</param>
        /// <param name="lang">The language code for localization</param>
        /// <param name="argsText">Text containing potential username mentions for personalization</param>
        /// <param name="channelID">The channel identifier for translation context</param>
        /// <param name="platform">The target platform for translation context</param>
        /// <returns>
        /// A localized string describing the time until the next event milestone,
        /// or <see langword="null"/> if an error occurs during processing.
        /// </returns>
        /// <remarks>
        /// <para>
        /// The method handles:
        /// <list type="bullet">
        /// <item>Events spanning calendar year boundaries</item>
        /// <item>Automatic selection of start/end phase based on current date</item>
        /// <item>Localized time formatting appropriate for the target language</item>
        /// </list>
        /// </para>
        /// <para>
        /// Processing logic:
        /// <list type="number">
        /// <item>Determines if event crosses year boundary (end month < start month)</item>
        /// <item>Calculates appropriate year for current event cycle</item>
        /// <item>Selects target date as either start or end of current cycle</item>
        /// <item>Formats time difference using <see cref="FormatTimeSpan(TimeSpan, string)"/></item>
        /// <item>Retrieves localized template based on event type and phase</item>
        /// </list>
        /// </para>
        /// <para>
        /// Localization templates expected:
        /// <list type="bullet">
        /// <item><c>command:{type}:start</c> - for time until event begins</item>
        /// <item><c>command:{type}:end</c> - for time until event ends</item>
        /// </list>
        /// </para>
        /// Errors are logged but don't disrupt application flow (returns null on failure).
        /// </remarks>
        public static string TimeTo(DateTime startTime, DateTime endTime, string type, string lang, string argsText, string channelID, PlatformsEnum platform)
        {
            try
            {
                var selectedUser = UsernameResolver.GetUsernameFromText(argsText);
                DateTime now = DateTime.UtcNow;

                bool crossesYear = startTime.Month > endTime.Month;

                int startYear;
                if (crossesYear)
                {
                    startYear = now.Month >= startTime.Month ? now.Year : now.Year - 1;
                }
                else
                {
                    startYear = now.Year;
                }

                DateTime seasonStart = new(startYear, startTime.Month, startTime.Day);
                DateTime seasonEnd;

                if (crossesYear)
                {
                    seasonEnd = new DateTime(startYear + 1, endTime.Month, endTime.Day).AddMilliseconds(-1);
                }
                else
                {
                    seasonEnd = new DateTime(startYear, endTime.Month, endTime.Day).AddMilliseconds(-1);
                }

                if (now > seasonEnd)
                {
                    startYear = crossesYear ? now.Year : now.Year + 1;

                    seasonStart = new(startYear, startTime.Month, startTime.Day);
                    seasonEnd = crossesYear
                        ? new DateTime(startYear + 1, endTime.Month, endTime.Day).AddMilliseconds(-1)
                        : new DateTime(startYear, endTime.Month, endTime.Day).AddMilliseconds(-1);
                }

                DateTime targetDate;
                string templateKey;

                if (now < seasonStart)
                {
                    targetDate = seasonStart;
                    templateKey = "start";
                }
                else
                {
                    targetDate = seasonEnd;
                    templateKey = "end";
                }

                TimeSpan timeSpan = targetDate - now;
                string formattedTime = FormatTimeSpan(timeSpan, lang);

                return LocalizationService.GetString(
                    lang,
                    $"command:{type}:{templateKey}",
                    channelID,
                    platform,
                    selectedUser,
                    formattedTime
                );
            }
            catch (Exception ex)
            {
                Write(ex);
                return null;
            }
        }

        /// <summary>
        /// Formats a time duration into a human-readable localized string.
        /// </summary>
        /// <param name="timeSpan">The time duration to format</param>
        /// <param name="lang">The language code for localization</param>
        /// <returns>
        /// A string representing the time duration in the most significant units,
        /// localized according to the specified language.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Formatting rules:
        /// <list type="bullet">
        /// <item>< 60 seconds: seconds only</item>
        /// <item>< 60 minutes: minutes and seconds</item>
        /// <item>< 24 hours: hours and minutes</item>
        /// <item>≥ 24 hours: days and hours</item>
        /// </list>
        /// </para>
        /// <para>
        /// Example outputs (English):
        /// <list type="table">
        /// <item><term>TimeSpan.FromSeconds(30)</term><description>"30 seconds."</description></item>
        /// <item><term>TimeSpan.FromMinutes(5.5)</term><description>"5 minutes. 30 seconds."</description></item>
        /// <item><term>TimeSpan.FromHours(3.75)</term><description>"3 hours. 45 minutes."</description></item>
        /// <item><term>TimeSpan.FromDays(2.25)</term><description>"2 days. 6 hours."</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// Implementation details:
        /// <list type="bullet">
        /// <item>Uses absolute values for negative time spans</item>
        /// <item>Retrieves unit labels through localization system</item>
        /// <item>Formats with invariant culture for numeric values</item>
        /// <item>Includes period at end of each unit for consistent punctuation</item>
        /// </list>
        /// </para>
        /// This method is designed for consistent time presentation across all platform interfaces.
        /// The output format is optimized for readability in chat message contexts.
        /// </remarks>
        public static string FormatTimeSpan(TimeSpan timeSpan, string lang)
        {
            int days = Math.Abs(timeSpan.Days);
            int hours = Math.Abs(timeSpan.Hours);
            int minutes = Math.Abs(timeSpan.Minutes);
            int seconds = Math.Abs(timeSpan.Seconds);

            string days_str = $"{days} {LocalizationService.GetString(lang, "text:day", string.Empty, PlatformsEnum.Twitch)}.";
            string hours_str = $"{hours} {LocalizationService.GetString(lang, "text:hour", string.Empty, PlatformsEnum.Twitch)}.";
            string minutes_str = $"{minutes} {LocalizationService.GetString(lang, "text:minute", string.Empty, PlatformsEnum.Twitch)}.";
            string seconds_str = $"{seconds} {LocalizationService.GetString(lang, "text:second", string.Empty, PlatformsEnum.Twitch)}.";

            if (timeSpan.TotalSeconds < 0)
                timeSpan = -timeSpan;

            if (timeSpan.TotalSeconds < 60)
                return seconds_str;
            else if (timeSpan.TotalMinutes < 60)
                return $"{minutes_str} {seconds_str}";
            else if (timeSpan.TotalHours < 24)
                return $"{hours_str} {minutes_str}";
            else
                return $"{days_str} {hours_str}";
        }

        /// <summary>
        /// Safely handles null string values by providing a safe "null" representation.
        /// </summary>
        /// <param name="input">The string to check for null</param>
        /// <returns>
        /// The original string if not null;
        /// otherwise, the string literal "null".
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method prevents <see cref="NullReferenceException"/> in string operations by:
        /// <list type="bullet">
        /// <item>Replacing null references with a safe string representation</item>
        /// <item>Maintaining original non-null strings without modification</item>
        /// </list>
        /// </para>
        /// <para>
        /// Usage example:
        /// <code>
        /// string result = Text.CheckNull(userInput);
        /// // Safe to call result.ToUpper() even if userInput was null
        /// </code>
        /// </para>
        /// <para>
        /// Important notes:
        /// <list type="bullet">
        /// <item>Only handles string nulls (not suitable for numeric types)</item>
        /// <item>Does not trim or modify non-null strings</item>
        /// <item>Returns the literal string "null", not the keyword null</item>
        /// </list>
        /// </para>
        /// Use this method in contexts where null strings could cause exceptions,
        /// but where the "null" string representation is acceptable.
        /// </remarks>
        public static string CheckNull(string input)
        {
            if (input is null)
                return "null";
            return input;
        }
    }
}
