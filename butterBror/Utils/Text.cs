using butterBror.Models;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using static butterBror.Core.Bot.Console;

namespace butterBror.Utils
{
    /// <summary>
    /// Provides text processing utilities for command filtering, layout conversion, and formatting operations.
    /// </summary>
    public class Text
    {
        /// <summary>
        /// Filters command names by removing non-alphanumeric characters from input.
        /// </summary>
        /// <param name="input">The raw command text to filter.</param>
        /// <returns>A cleaned command name with only allowed Latin/Cyrillic characters and numbers.</returns>
        /// <remarks>
        /// Allows both English and Russian keyboard layouts for command input.
        /// Used for command validation and processing.
        /// </remarks>
        
        public static string FilterCommand(string input)
        {
            Engine.Statistics.FunctionsUsed.Add();
            return Regex.Replace(input, @"[^qwertyuiopasdfghjklzxcvbnmйцукенгшщзхъфывапролджэячсмитьбюёQWERTYUIOPASDFGHJKLZXCVBNMЙЦУКЕНГШЩЗХЪФЫВАПРОЛДЖЭЯЧСМИТЬБЮЁ1234567890%]", ""); ;
        }

        /// <summary>
        /// Converts text between English and Russian keyboard layouts.
        /// </summary>
        /// <param name="text">The text to convert.</param>
        /// <returns>Text with characters mapped to the opposite keyboard layout.</returns>
        /// <remarks>
        /// Handles both uppercase and lowercase letters.
        /// Returns null if an exception occurs during conversion.
        /// </remarks>
        
        public static string ChangeLayout(string text)
        {
            Engine.Statistics.FunctionsUsed.Add();
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
        /// Removes non-printable ASCII characters from text.
        /// </summary>
        /// <param name="input">The text to clean.</param>
        /// <returns>Cleaned text containing only printable ASCII characters.</returns>
        /// <remarks>
        /// Preserves spaces and standard punctuation.
        /// Returns empty string for null/empty input.
        /// </remarks>
        
        public static string CleanAscii(string input)
        {
            Engine.Statistics.FunctionsUsed.Add();
            if (string.IsNullOrEmpty(input)) return input;

            return new string(input
                .Where(c => c > 31 && c != 127)
                .ToArray());
        }

        /// <summary>
        /// Removes non-printable ASCII characters and spaces from text.
        /// </summary>
        /// <param name="input">The text to clean.</param>
        /// <returns>Cleaned text without spaces or non-printable characters.</returns>
        /// <remarks>
        /// Uses CleanAscii internally before removing spaces.
        /// Useful for creating compact identifier strings.
        /// </remarks>
        
        public static string CleanAsciiWithoutSpaces(string input)
        {
            Engine.Statistics.FunctionsUsed.Add();
            return CleanAscii(input).Replace(" ", "");
        }

        /// <summary>
        /// Removes consecutive duplicate characters from a string.
        /// </summary>
        /// <param name="text">The text to process.</param>
        /// <returns>A new string with duplicate characters removed.</returns>
        /// <example>
        /// Input: "aaabbbcc" → Output: "abbc"
        /// </example>
        
        public static string RemoveDuplicates(string text)
        {
            Engine.Statistics.FunctionsUsed.Add();
            return text.Aggregate(new StringBuilder(), (sb, c) =>
                sb.Length == 0 || c != sb[^1] ? sb.Append(c) : sb).ToString();
        }

        /// <summary>
        /// Filters username input to allow only valid characters.
        /// </summary>
        /// <param name="input">The raw username text.</param>
        /// <returns>Cleaned username with invalid characters removed.</returns>
        /// <remarks>
        /// Allows alphanumeric characters, underscores, and hyphens.
        /// Used for validating user identifiers.
        /// </remarks>
        
        public static string UsernameFilter(string input)
        {
            Engine.Statistics.FunctionsUsed.Add();
            return Regex.Replace(input, @"[^A-Za-z0-9_-]", "");
        }

        /// <summary>
        /// Shortens coordinate values to one decimal place while preserving direction.
        /// </summary>
        /// <param name="coordinate">The coordinate string to shorten (e.g., "48.8566N").</param>
        /// <returns>
        /// A shortened coordinate string (e.g., "48.9N").
        /// Returns original string if parsing fails.
        /// </returns>
        /// <exception cref="ArgumentException">Thrown when coordinate format is invalid.</exception>
        
        public static string ShortenCoordinate(string coordinate)
        {
            Engine.Statistics.FunctionsUsed.Add();
            if (double.TryParse(coordinate[..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out double number))
                return $"{Math.Round(number, 1).ToString(CultureInfo.InvariantCulture)}{coordinate[^1]}";
            else
                throw new ArgumentException("Invalid coordinate format");
        }

        /// <summary>
        /// Calculates and formats time until specified seasonal event.
        /// </summary>
        /// <param name="startTime">Start date/time of the event.</param>
        /// <param name="endTime">End date/time of the event.</param>
        /// <param name="type">Type of event (used for translation keys).</param>
        /// <param name="endYearAdd">Year adjustment for end date.</param>
        /// <param name="lang">Language code for translation.</param>
        /// <param name="argsText">Text containing potential username mentions.</param>
        /// <param name="channelID">Channel identifier for translation context.</param>
        /// <param name="platform">Target platform context.</param>
        /// <returns>Formatted time string or null if error occurs.</returns>
        /// <remarks>
        /// Uses different translations for start/end phases.
        /// Automatically selects appropriate message based on current date.
        /// </remarks>
        
        public static string TimeTo(DateTime startTime, DateTime endTime, string type, string lang, string argsText, string channelID, PlatformsEnum platform)
        {
            Engine.Statistics.FunctionsUsed.Add();
            try
            {
                var selectedUser = Names.GetUsernameFromText(argsText);
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
        /// Formats a TimeSpan into a human-readable string using specified language.
        /// </summary>
        /// <param name="timeSpan">Time duration to format.</param>
        /// <param name="lang">Language code for translation.</param>
        /// <returns>Localized time string (e.g., "2 days 5 hours").</returns>
        /// <remarks>
        /// Uses translation system for unit labels (day/hour/minute/second).
        /// Handles negative time spans by using absolute values.
        /// </remarks>
        
        public static string FormatTimeSpan(TimeSpan timeSpan, string lang)
        {
            Engine.Statistics.FunctionsUsed.Add();
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
        /// Safely handles null string values by returning "null" string.
        /// </summary>
        /// <param name="input">The string to check.</param>
        /// <returns>Original string or "null" if input is null.</returns>
        /// <remarks>
        /// Prevents NullReferenceExceptions in string operations.
        /// Not intended for numeric null handling.
        /// </remarks>
        
        public static string CheckNull(string input)
        {
            if (input is null)
                return "null";
            return input;
        }
    }
}
