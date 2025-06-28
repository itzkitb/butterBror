using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace butterBror.Utils.Tools
{
    public class Format
    {
        /// <summary>
        /// Text to number
        /// </summary>
        public static int ToInt(string input)
        {
            Core.Statistics.FunctionsUsed.Add();
            return Int32.Parse(Regex.Replace(input, @"[^-1234567890]", ""));
        }
        /// <summary>
        /// Text to long number
        /// </summary>
        public static long ToLong(string input)
        {
            Core.Statistics.FunctionsUsed.Add();
            return long.Parse(Regex.Replace(input, @"[^-1234567890]", ""));
        }

        /// <summary>
        /// Text to ulong number
        /// </summary>
        public static ulong ToUlong(string input)
        {
            Core.Statistics.FunctionsUsed.Add();
            return ulong.Parse(Regex.Replace(input, @"[^-1234567890]", "").Replace(",", "."));
        }

        /// <summary>
        /// Text to double number
        /// </summary>
        public static double ToDouble(string input)
        {
            Core.Statistics.FunctionsUsed.Add();
            return double.Parse(Regex.Replace(input, @"[^-1234567890,.]", "").Replace(",", "."));
        }
        /// <summary>
        /// Get the amount of time until
        /// </summary>
        public static TimeSpan GetTimeTo(DateTime time, DateTime now, bool addYear = true)
        {
            Core.Statistics.FunctionsUsed.Add();
            if (now > time && addYear)
                time.AddYears(1);

            if (now > time)
                return now - time;
            else
                return time - now;
        }
    }
}
