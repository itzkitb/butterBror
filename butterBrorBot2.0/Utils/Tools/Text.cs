using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using static butterBror.Utils.Things.Console;

namespace butterBror.Utils.Tools
{
    public class Text
    {
        /// <summary>
        /// Filter command name
        /// </summary>
        [ConsoleSector("butterBror.Utils.Tools.Text", "FilterCommand")]
        public static string FilterCommand(string input)
        {
            Core.Statistics.FunctionsUsed.Add();
            return Regex.Replace(input, @"[^qwertyuiopasdfghjklzxcvbnmйцукенгшщзхъфывапролджэячсмитьбюёQWERTYUIOPASDFGHJKLZXCVBNMЙЦУКЕНГШЩЗХЪФЫВАПРОЛДЖЭЯЧСМИТЬБЮЁ1234567890%]", ""); ;
        }
        /// <summary>
        /// Change text layout
        /// </summary>
        [ConsoleSector("butterBror.Utils.Tools.Text", "ChangeLayout")]
        public static string ChangeLayout(string text)
        {
            Core.Statistics.FunctionsUsed.Add();
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
        /// Filter text from ASCII
        /// </summary>
        [ConsoleSector("butterBror.Utils.Tools.Text", "CleanAscii")]
        public static string CleanAscii(string input)
        {
            Core.Statistics.FunctionsUsed.Add();
            if (string.IsNullOrEmpty(input)) return input;

            return new string(input
                .Where(c => c > 31 && c != 127)
                .ToArray());
        }
        /// <summary>
        /// Filter text from ASCII without spaces
        /// </summary>
        [ConsoleSector("butterBror.Utils.Tools.Text", "CleanAsciiWithoutSpaces")]
        public static string CleanAsciiWithoutSpaces(string input)
        {
            Core.Statistics.FunctionsUsed.Add();
            return CleanAscii(input).Replace(" ", "");
        }
        /// <summary>
        /// Remove duplicate characters from text
        /// </summary>
        [ConsoleSector("butterBror.Utils.Tools.Text", "RemoveDuplicates")]
        public static string RemoveDuplicates(string text)
        {
            Core.Statistics.FunctionsUsed.Add();
            return text.Aggregate(new StringBuilder(), (sb, c) =>
                sb.Length == 0 || c != sb[^1] ? sb.Append(c) : sb).ToString();
        }
        /// <summary>
        /// Filter nickname
        /// </summary>
        [ConsoleSector("butterBror.Utils.Tools.Text", "UsernameFilter")]
        public static string UsernameFilter(string input)
        {
            Core.Statistics.FunctionsUsed.Add();
            return Regex.Replace(input, @"[^A-Za-z0-9_-]", "");
        }
        /// <summary>
        /// Reduce coordinates
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        [ConsoleSector("butterBror.Utils.Tools.Text", "ShortenCoordinate")]
        public static string ShortenCoordinate(string coordinate)
        {
            Core.Statistics.FunctionsUsed.Add();
            if (double.TryParse(coordinate[..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out double number))
                return $"{Math.Round(number, 1).ToString(CultureInfo.InvariantCulture)}{coordinate[^1]}";
            else
                throw new ArgumentException("Invalid coordinate format");
        }
        /// <summary>
        /// Print time until
        /// </summary>
        [ConsoleSector("butterBror.Utils.Tools.Text", "TimeTo")]
        public static string TimeTo(DateTime startTime, DateTime endTime, string type, int endYearAdd, string lang, string argsText, string channelID, Platforms platform)
        {
            Core.Statistics.FunctionsUsed.Add();
            try
            {
                var selectedUser = Names.GetUsernameFromText(argsText);
                DateTime now = DateTime.UtcNow;
                DateTime winterStart = new(now.Year, startTime.Month, startTime.Day);
                DateTime winterEnd = new(now.Year + endYearAdd, endTime.Month, endTime.Day);
                winterEnd.AddDays(-1);
                DateTime winter = now < winterStart ? winterStart : winterEnd;
                if (now < winterStart)
                    return ArgumentsReplacement(TranslationManager.GetTranslation(lang, $"command:{type}:start", channelID, platform),
                        new(){ { "time", FormatTimeSpan(Format.GetTimeTo(winter, now), lang) },
                            { "sUser", selectedUser } });
                else
                    return ArgumentsReplacement(TranslationManager.GetTranslation(lang, $"command:{type}:end", channelID, platform),
                        new(){ { "time", FormatTimeSpan(Format.GetTimeTo(winter, now), lang) },
                            { "sUser", selectedUser } });
            }
            catch (Exception ex)
            {
                Write(ex);
                return null;
            }
        }
        /// <summary>
        /// Format time to text
        /// </summary>
        [ConsoleSector("butterBror.Utils.Tools.Text", "FormatTimeSpan")]
        public static string FormatTimeSpan(TimeSpan timeSpan, string lang)
        {
            Core.Statistics.FunctionsUsed.Add();
            int days = Math.Abs(timeSpan.Days);
            int hours = Math.Abs(timeSpan.Hours);
            int minutes = Math.Abs(timeSpan.Minutes);
            int seconds = Math.Abs(timeSpan.Seconds);

            string days_str = $"{days} {TranslationManager.GetTranslation(lang, "text:day", "", Platforms.Twitch)}.";
            string hours_str = $"{hours} {TranslationManager.GetTranslation(lang, "text:hour", "", Platforms.Twitch)}.";
            string minutes_str = $"{minutes} {TranslationManager.GetTranslation(lang, "text:minute", "", Platforms.Twitch)}.";
            string seconds_str = $"{seconds} {TranslationManager.GetTranslation(lang, "text:second", "", Platforms.Twitch)}.";

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
        /// Replaces arguments
        /// </summary>
        /// <param name="original"></param>
        /// <param name="argument"></param>
        /// <param name="replace"></param>
        /// <returns></returns>
        [ConsoleSector("butterBror.Utils.Tools.Text", "ArgumentsReplacement")]
        public static string ArgumentsReplacement(string original, Dictionary<string, string> replacements)
        {
            string result = original;

            foreach (var replace in replacements)
            {
                result = result.Replace($"%{replace.Key}%", replace.Value);
            }

            return result;
        }
        /// <summary>
        /// Replaces argument
        /// </summary>
        /// <param name="original"></param>
        /// <param name="argument"></param>
        /// <param name="replace"></param>
        /// <returns></returns>
        [ConsoleSector("butterBror.Utils.Tools.Text", "ArgumentReplacement")]
        public static string ArgumentReplacement(string original, string key, string value)
        {
            return original.Replace($"%{key}%", value);
        }

        [ConsoleSector("butterBror.Utils.Tools.Text", "CheckNull")]
        public static string CheckNull(string input)
        {
            if (input is null)
                return "null";
            return input;
        }
    }
}
