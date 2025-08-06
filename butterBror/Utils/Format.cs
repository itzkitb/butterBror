using Newtonsoft.Json;
using System.Globalization;
using System.Text.RegularExpressions;

namespace butterBror.Utils
{
    /// <summary>
    /// Provides utility methods for string formatting and date/time calculations.
    /// </summary>
    public class Format
    {
        /// <summary>
        /// Converts a string to an integer value by removing non-numeric characters.
        /// </summary>
        /// <param name="input">The string to convert.</param>
        /// <returns>The parsed integer value.</returns>
        /// <exception cref="FormatException">Thrown if input contains no valid numeric characters.</exception>
        /// <remarks>
        /// Removes all non-digit characters except the minus sign before parsing.
        /// Returns 0 if input is null or empty.
        /// </remarks>
        public static int ToInt(string input)
        {
            return Int32.Parse(Regex.Replace(input, @"[^-1234567890]", ""));
        }

        /// <summary>
        /// Converts a string to a long integer value by removing non-numeric characters.
        /// </summary>
        /// <param name="input">The string to convert.</param>
        /// <returns>The parsed long value.</returns>
        /// <exception cref="FormatException">Thrown if input contains no valid numeric characters.</exception>
        /// <remarks>
        /// Removes all non-digit characters except the minus sign before parsing.
        /// Returns 0 if input is null or empty.
        /// </remarks>
        public static long ToLong(string input)
        {
            return long.Parse(Regex.Replace(input, @"[^-1234567890]", ""));
        }

        /// <summary>
        /// Converts a string to an unsigned long integer value by removing non-numeric characters.
        /// </summary>
        /// <param name="input">The string to convert.</param>
        /// <returns>The parsed unsigned long value.</returns>
        /// <exception cref="FormatException">Thrown if input contains no valid numeric characters.</exception>
        /// <remarks>
        /// Removes all non-digit characters except the minus sign before parsing.
        /// Returns 0 if input is null or empty.
        /// Converts commas to periods before parsing.
        /// </remarks>
        public static ulong ToUlong(string input)
        {
            return ulong.Parse(Regex.Replace(input, @"[^-1234567890]", "").Replace(",", "."));
        }

        /// <summary>
        /// Converts a string to a double-precision floating-point number.
        /// </summary>
        /// <param name="input">The string to convert.</param>
        /// <returns>The parsed double value.</returns>
        /// <exception cref="FormatException">Thrown if input contains no valid numeric characters.</exception>
        /// <remarks>
        /// Removes all non-numeric characters except -, . and , before parsing.
        /// Converts commas to periods for decimal parsing.
        /// Returns 0 if input is null or empty.
        /// </remarks>
        public static double ToDouble(string input)
        {
            string cleaned = Regex.Replace(input, @"[^-0-9,.]", "")
                                  .Replace(",", ".");

            return double.Parse(cleaned, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Calculates the time difference between two DateTime values with optional annual cycle handling.
        /// </summary>
        /// <param name="time">The reference DateTime to compare against.</param>
        /// <param name="now">The current DateTime value.</param>
        /// <param name="addYear">If true, adds one year to reference time if it's in the past</param>
        /// <returns>A TimeSpan representing the difference between the two times.</returns>
        /// <remarks>
        /// If addYear is true and the reference time is in the past, the method will add one year to the reference time
        /// before calculating the difference. This is useful for recurring annual events like birthdays.
        /// </remarks>
        public static TimeSpan GetTimeTo(DateTime time, DateTime now, bool addYear = true)
        {
            // Fix: Save the result of AddYears operation
            if (now > time && addYear)
                time = time.AddYears(1);  // Fixed to update the time value

            if (now > time)
                return now - time;
            else
                return time - now;
        }

        /// <summary>
        /// Deserializes a JSON string representing an array of strings into a string[]
        /// </summary>
        /// <param name="json">JSON in the format ["value1", "value2", ...]</param>
        /// <returns>Array of strings</returns>
        /// <exception cref="JsonException">Thrown if the JSON is not in a valid format</exception>
        public static string[] ParseStringArray(string json)
        {
            return JsonConvert.DeserializeObject<string[]>(json);
        }

        /// <summary>
        /// Deserializes a JSON string representing an array of strings into a List&lt;string&gt;
        /// </summary>
        /// <param name="json">JSON in the format ["value1", "value2", ...]</param>
        /// <returns>Array of strings</returns>
        /// <exception cref="JsonException">Thrown if the JSON is not in a valid format</exception>
        public static List<string> ParseStringList(string json)
        {
            return JsonConvert.DeserializeObject<List<string>>(json);
        }

        /// <summary>
        /// Deserializes a JSON string representing an array of strings into a Dictionary&lt;string, string&gt;
        /// </summary>
        /// <param name="json">JSON in the format ["value1", "value2", ...]</param>
        /// <returns>Disctionary of strings</returns>
        /// <exception cref="JsonException">Thrown if the JSON is not in a valid format</exception>
        public static Dictionary<string, string> ParseStringDictionary(string json)
        {
            return JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
        }

        /// <summary>
        /// Serializes an array of strings to a JSON string of the format ["value1", "value2", ...]
        /// </summary>
        /// <param name="array">The array of strings to convert</param>
        /// <returns>The JSON string in array format</returns>
        /// <example>
        /// string[] arr = { "123", "text", "lol" };
        /// string json = SerializeStringArray(arr); // Returns: ["123","text","lol"]
        /// </example>
        public static string SerializeStringArray(string[] array)
        {
            return JsonConvert.SerializeObject(array);
        }

        /// <summary>
        /// Serializes an list of strings to a JSON string of the format ["value1", "value2", ...]
        /// </summary>
        /// <param name="array">The list of strings to convert</param>
        /// <returns>The JSON string in list format</returns>
        /// <example>
        /// List&lt;string&gt; arr = { "123", "text", "lol" };
        /// string json = SerializeStringArray(arr); // Returns: ["123","text","lol"]
        /// </example>
        public static string SerializeStringList(List<string> array)
        {
            return JsonConvert.SerializeObject(array);
        }

        /// <summary>
        /// Serializes an array of strings to a JSON string of the format ["value1", "value2", ...]
        /// </summary>
        /// <param name="array">The array of strings to convert</param>
        /// <returns>The JSON string in array format</returns>
        /// <example>
        /// string[] arr = { "123", "text", "lol" };
        /// string json = SerializeStringArray(arr); // Returns: ["123","text","lol"]
        /// </example>
        public static string SerializeStringDictionary(Dictionary<string, string> dictionary)
        {
            return JsonConvert.SerializeObject(dictionary);
        }
    }
}
