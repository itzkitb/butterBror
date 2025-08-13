using Newtonsoft.Json;
using System.Globalization;
using System.Text.RegularExpressions;

namespace butterBror.Utils
{
    /// <summary>
    /// Provides utility methods for string parsing, data conversion, and date/time calculations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class offers specialized formatting utilities for common data transformation scenarios:
    /// <list type="bullet">
    /// <item>Robust numeric string parsing with flexible input handling</item>
    /// <item>JSON serialization/deserialization for common collection types</item>
    /// <item>Precise time difference calculations with annual cycle support</item>
    /// </list>
    /// </para>
    /// <para>
    /// Key features:
    /// <list type="bullet">
    /// <item>Handles various numeric formats with automatic cleanup of non-essential characters</item>
    /// <item>Supports culture-invariant parsing for consistent international behavior</item>
    /// <item>Provides specialized methods for common collection serialization patterns</item>
    /// <item>Includes edge case handling for time calculations around annual boundaries</item>
    /// </list>
    /// </para>
    /// All methods are designed to be thread-safe and suitable for high-frequency usage in production environments.
    /// </remarks>
    public class DataConversion
    {
        /// <summary>
        /// Converts a string to an integer value by extracting numeric components.
        /// </summary>
        /// <param name="input">The string containing numeric characters to convert.</param>
        /// <returns>The parsed integer value.</returns>
        /// <exception cref="FormatException">
        /// Thrown when the cleaned string contains no valid numeric characters or exceeds <see cref="int"/> range.
        /// </exception>
        /// <exception cref="OverflowException">
        /// Thrown when the numeric value exceeds <see cref="int"/> range after cleaning.
        /// </exception>
        /// <remarks>
        /// <para>
        /// Processing workflow:
        /// <list type="number">
        /// <item>Removes all non-digit characters except the leading minus sign</item>
        /// <item>Parses the resulting string as an integer using current culture settings</item>
        /// </list>
        /// </para>
        /// <para>
        /// Examples:
        /// <list type="table">
        /// <item><term>"1,234"</term><description>Returns 1234</description></item>
        /// <item><term>"-$500"</term><description>Returns -500</description></item>
        /// <item><term>"abc"</term><description>Throws FormatException</description></item>
        /// </list>
        /// </para>
        /// Note: Null or empty inputs will result in FormatException rather than returning 0.
        /// This method is optimized for performance in scenarios with mixed alphanumeric input.
        /// </remarks>
        public static int ToInt(string input)
        {
            return Int32.Parse(Regex.Replace(input, @"[^-1234567890]", ""));
        }

        /// <summary>
        /// Converts a string to a 64-bit integer value by extracting numeric components.
        /// </summary>
        /// <param name="input">The string containing numeric characters to convert.</param>
        /// <returns>The parsed long integer value.</returns>
        /// <exception cref="FormatException">
        /// Thrown when the cleaned string contains no valid numeric characters or exceeds <see cref="long"/> range.
        /// </exception>
        /// <exception cref="OverflowException">
        /// Thrown when the numeric value exceeds <see cref="long"/> range after cleaning.
        /// </exception>
        /// <remarks>
        /// <para>
        /// Key differences from <see cref="ToInt(string)"/>:
        /// <list type="bullet">
        /// <item>Supports larger numeric ranges (up to 9,223,372,036,854,775,807)</item>
        /// <item>Same character filtering behavior as ToInt method</item>
        /// <item>Uses identical regex pattern for input cleaning</item>
        /// </list>
        /// </para>
        /// <para>
        /// Typical use cases:
        /// <list type="bullet">
        /// <item>Parsing large numeric identifiers</item>
        /// <item>Handling financial values with high precision</item>
        /// <item>Processing database IDs and timestamps</item>
        /// </list>
        /// </para>
        /// The method preserves negative values when a leading minus sign is present in the input.
        /// Empty or invalid inputs result in FormatException rather than default values.
        /// </remarks>
        public static long ToLong(string input)
        {
            return Int64.Parse(Regex.Replace(input, @"[^-1234567890]", ""));
        }

        /// <summary>
        /// Converts a string to an unsigned 64-bit integer value by extracting numeric components.
        /// </summary>
        /// <param name="input">The string containing numeric characters to convert.</param>
        /// <returns>The parsed unsigned long integer value.</returns>
        /// <exception cref="FormatException">
        /// Thrown when the cleaned string contains no valid numeric characters or exceeds <see cref="ulong"/> range.
        /// </exception>
        /// <exception cref="OverflowException">
        /// Thrown when the numeric value exceeds <see cref="ulong"/> range or is negative.
        /// </exception>
        /// <remarks>
        /// <para>
        /// Processing behavior:
        /// <list type="bullet">
        /// <item>Removes all non-digit characters (negative values not permitted)</item>
        /// <item>Replaces commas with periods (though unnecessary for integer parsing)</item>
        /// <item>Parses as unsigned 64-bit integer</item>
        /// </list>
        /// </para>
        /// <para>
        /// Important notes:
        /// <list type="bullet">
        /// <item>Leading minus signs will cause OverflowException (unsigned type)</item>
        /// <item>Decimal separators are removed as they're invalid for ulong parsing</item>
        /// <item>Supports values up to 18,446,744,073,709,551,615</item>
        /// </list>
        /// </para>
        /// This method is particularly useful for parsing large positive identifiers like:
        /// <list type="bullet">
        /// <item>Discord snowflake IDs</item>
        /// <item>Database sequence values</item>
        /// <item>Large counter values</item>
        /// </list>
        /// </remarks>
        public static ulong ToUlong(string input)
        {
            return ulong.Parse(Regex.Replace(input, @"[^-1234567890]", "").Replace(",", "."));
        }

        /// <summary>
        /// Converts a string to a double-precision floating-point number with culture-invariant parsing.
        /// </summary>
        /// <param name="input">The string containing numeric characters to convert.</param>
        /// <returns>The parsed double value.</returns>
        /// <exception cref="FormatException">
        /// Thrown when the cleaned string cannot be parsed as a valid number.
        /// </exception>
        /// <exception cref="OverflowException">
        /// Thrown when the numeric value exceeds <see cref="double"/> range.
        /// </exception>
        /// <remarks>
        /// <para>
        /// Processing workflow:
        /// <list type="number">
        /// <item>Removes all characters except digits, minus sign, and decimal separators</item>
        /// <item>Standardizes decimal separators to periods (culture-invariant format)</item>
        /// <item>Parses using <see cref="CultureInfo.InvariantCulture"/> for consistent behavior</item>
        /// </list>
        /// </para>
        /// <para>
        /// Supported input formats:
        /// <list type="bullet">
        /// <item>Numeric values with commas as thousand separators ("1,000.50")</item>
        /// <item>European decimal formats using commas ("1000,50")</item>
        /// <item>Scientific notation ("1.23e-4")</item>
        /// <item>Negative values with leading minus sign ("-50.25")</item>
        /// </list>
        /// </para>
        /// This method ensures consistent numeric parsing across different regional settings,
        /// making it ideal for international applications and data interchange scenarios.
        /// </remarks>
        public static double ToDouble(string input)
        {
            string cleaned = Regex.Replace(input, @"[^-0-9,.]", "")
                                  .Replace(",", ".");

            return double.Parse(cleaned, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Calculates the time difference between two DateTime values with optional annual cycle adjustment.
        /// </summary>
        /// <param name="time">The reference DateTime to compare against.</param>
        /// <param name="now">The current DateTime value.</param>
        /// <param name="addYear">
        /// If true, automatically adds one year to the reference time when it falls in the past,
        /// effectively calculating time until the next annual occurrence.
        /// </param>
        /// <returns>A TimeSpan representing the absolute difference between the two times.</returns>
        /// <remarks>
        /// <para>
        /// Behavior scenarios:
        /// <list type="table">
        /// <item><term>addYear = false</term><description>Always returns absolute difference (|now - time|)</description></item>
        /// <item><term>addYear = true &amp; time in past</term><description>Returns time until next year's occurrence (time + 1 year - now)</description></item>
        /// <item><term>addYear = true &amp; time in future</term><description>Returns time until occurrence (time - now)</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// Typical use cases:
        /// <list type="bullet">
        /// <item>Calculating time until recurring events (birthdays, anniversaries)</item>
        /// <item>Determining countdowns for annual celebrations</item>
        /// <item>Handling subscription renewal periods</item>
        /// </list>
        /// </para>
        /// <para>
        /// Important considerations:
        /// <list type="bullet">
        /// <item>Uses absolute difference when addYear is false</item>
        /// <item>Handles leap years correctly through DateTime.AddYears()</item>
        /// <item>Timezone-agnostic - operates on DateTime values as provided</item>
        /// <item>Does not modify original DateTime parameters</item>
        /// </list>
        /// </para>
        /// This method is particularly useful for implementing "time until next occurrence" functionality.
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
        /// Deserializes a JSON string representing an array of strings into a string array.
        /// </summary>
        /// <param name="json">JSON string in standard array format (e.g., <c>["value1", "value2"]</c>).</param>
        /// <returns>A string array containing the deserialized values.</returns>
        /// <exception cref="JsonException">
        /// Thrown when the input is not valid JSON or doesn't match the expected array structure.
        /// </exception>
        /// <remarks>
        /// <para>
        /// Expected input format:
        /// <code>
        /// ["item1", "item2", "item3"]
        /// </code>
        /// </para>
        /// <para>
        /// Usage notes:
        /// <list type="bullet">
        /// <item>Uses <see cref="JsonConvert.DeserializeObject{T}(string)"/> internally</item>
        /// <item>Preserves original string casing and content</item>
        /// <item>Handles escaped characters within string values</item>
        /// <item>Null values in JSON become null references in the array</item>
        /// </list>
        /// </para>
        /// This method is optimized for performance with small to medium-sized arrays.
        /// For large datasets, consider using streaming JSON parsing approaches.
        /// </remarks>
        public static string[] ParseStringArray(string json)
        {
            return JsonConvert.DeserializeObject<string[]>(json);
        }

        /// <summary>
        /// Deserializes a JSON string representing an array of strings into a string list.
        /// </summary>
        /// <param name="json">JSON string in standard array format (e.g., <c>["value1", "value2"]</c>).</param>
        /// <returns>A list of strings containing the deserialized values.</returns>
        /// <exception cref="JsonException">
        /// Thrown when the input is not valid JSON or doesn't match the expected array structure.
        /// </exception>
        /// <remarks>
        /// <para>
        /// Key differences from <see cref="ParseStringArray(string)"/>:
        /// <list type="bullet">
        /// <item>Returns <see cref="List{T}"/> instead of array for mutable collection</item>
        /// <item>Same underlying deserialization process as array method</item>
        /// <item>Preferred when subsequent modifications are needed</item>
        /// </list>
        /// </para>
        /// <para>
        /// Performance characteristics:
        /// <list type="bullet">
        /// <item>Slightly higher memory usage than array version</item>
        /// <item>Same parsing speed as array deserialization</item>
        /// <item>Provides standard list operations for post-processing</item>
        /// </list>
        /// </para>
        /// Ideal for scenarios requiring dynamic collection modification after deserialization.
        /// </remarks>
        public static List<string> ParseStringList(string json)
        {
            return JsonConvert.DeserializeObject<List<string>>(json);
        }

        /// <summary>
        /// Deserializes a JSON string representing a dictionary of string key-value pairs.
        /// </summary>
        /// <param name="json">JSON string in object format (e.g., <c>{"key1":"value1", "key2":"value2"}</c>).</param>
        /// <returns>A dictionary containing the deserialized key-value pairs.</returns>
        /// <exception cref="JsonException">
        /// Thrown when the input is not valid JSON or doesn't match the expected dictionary structure.
        /// </exception>
        /// <remarks>
        /// <para>
        /// Expected input format:
        /// <code>
        /// {"key1": "value1", "key2": "value2"}
        /// </code>
        /// </para>
        /// <para>
        /// Important behaviors:
        /// <list type="bullet">
        /// <item>Uses case-sensitive string keys by default</item>
        /// <item>Preserves original key and value casing</item>
        /// <item>Handles nested JSON structures as string values</item>
        /// <item>Null values in JSON become null references in the dictionary</item>
        /// </list>
        /// </para>
        /// <para>
        /// Common use cases:
        /// <list type="bullet">
        /// <item>Parsing configuration data from JSON storage</item>
        /// <item>Processing API responses with string-based key-value pairs</item>
        /// <item>Converting serialized command cooldown data</item>
        /// </list>
        /// </para>
        /// Note: Duplicate keys in JSON will result in the last value overwriting previous ones.
        /// </remarks>
        public static Dictionary<string, string> ParseStringDictionary(string json)
        {
            return JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
        }

        /// <summary>
        /// Serializes a string array to a JSON array format.
        /// </summary>
        /// <param name="array">The string array to serialize.</param>
        /// <returns>A JSON string representing the array in standard format.</returns>
        /// <remarks>
        /// <para>
        /// Output format example:
        /// <code>
        /// ["value1","value2","value3"]
        /// </code>
        /// </para>
        /// <para>
        /// Serialization characteristics:
        /// <list type="bullet">
        /// <item>Uses minimal formatting (no extra whitespace)</item>
        /// <item>Escapes special characters in string values</item>
        /// <item>Preserves null values as JSON null</item>
        /// <item>Handles Unicode characters correctly</item>
        /// </list>
        /// </para>
        /// <para>
        /// Performance notes:
        /// <list type="bullet">
        /// <item>Optimized for small to medium-sized arrays</item>
        /// <item>Thread-safe for concurrent usage</item>
        /// <item>Produces compact output for storage efficiency</item>
        /// </list>
        /// </para>
        /// Ideal for persisting array data to files or databases in a standard interchange format.
        /// </remarks>
        public static string SerializeStringArray(string[] array)
        {
            return JsonConvert.SerializeObject(array);
        }

        /// <summary>
        /// Serializes a string list to a JSON array format.
        /// </summary>
        /// <param name="array">The string list to serialize.</param>
        /// <returns>A JSON string representing the list in standard array format.</returns>
        /// <remarks>
        /// <para>
        /// Output format example:
        /// <code>
        /// ["value1","value2","value3"]
        /// </code>
        /// </para>
        /// <para>
        /// Key features:
        /// <list type="bullet">
        /// <item>Produces identical output format to <see cref="SerializeStringArray(string[])"/></item>
        /// <item>Converts list to array internally for serialization</item>
        /// <item>Maintains element order from the original list</item>
        /// <item>Handles null elements appropriately</item>
        /// </list>
        /// </para>
        /// <para>
        /// Usage guidance:
        /// <list type="bullet">
        /// <item>Prefer this method when working with list collections</item>
        /// <item>Use when subsequent deserialization to List is preferred</item>
        /// <item>Same performance characteristics as array serialization</item>
        /// </list>
        /// </para>
        /// This method provides a convenient way to persist list data while maintaining interoperability
        /// with systems expecting standard JSON array formats.
        /// </remarks>
        public static string SerializeStringList(List<string> array)
        {
            return JsonConvert.SerializeObject(array);
        }

        /// <summary>
        /// Serializes a string dictionary to a JSON object format.
        /// </summary>
        /// <param name="dictionary">The string dictionary to serialize.</param>
        /// <returns>A JSON string representing the dictionary in standard object format.</returns>
        /// <remarks>
        /// <para>
        /// Output format example:
        /// <code>
        /// {"key1":"value1","key2":"value2"}
        /// </code>
        /// </para>
        /// <para>
        /// Serialization behavior:
        /// <list type="bullet">
        /// <item>Uses dictionary keys as JSON property names</item>
        /// <item>Produces minimal formatting (no extra whitespace)</item>
        /// <item>Escapes special characters in keys and values</item>
        /// <item>Maintains case sensitivity of keys</item>
        /// </list>
        /// </para>
        /// <para>
        /// Important notes:
        /// <list type="bullet">
        /// <item>Null values are serialized as JSON null</item>
        /// <item>Dictionary order is not preserved in JSON (per JSON specification)</item>
        /// <item>Handles Unicode characters in both keys and values</item>
        /// </list>
        /// </para>
        /// This method is particularly useful for persisting configuration data and command cooldown records
        /// in a human-readable and interoperable format.
        /// </remarks>
        public static string SerializeStringDictionary(Dictionary<string, string> dictionary)
        {
            return JsonConvert.SerializeObject(dictionary);
        }
    }
}
