using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace butterBror.Utils.Tools
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
            Core.Statistics.FunctionsUsed.Add();
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
            Core.Statistics.FunctionsUsed.Add();
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
            Core.Statistics.FunctionsUsed.Add();
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
            Core.Statistics.FunctionsUsed.Add();
            return double.Parse(Regex.Replace(input, @"[^-1234567890,.]", "").Replace(",", "."));
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
    }
}
