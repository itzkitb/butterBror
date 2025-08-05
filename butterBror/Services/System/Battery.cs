using System.Management;
using static butterBror.Core.Bot.Console;

namespace butterBror.Services.System
{
    /// <summary>
    /// Provides functionality to retrieve battery information on Windows systems using WMI.
    /// </summary>
    public class Battery
    {
        /// <summary>
        /// Gets the estimated battery charge percentage.
        /// </summary>
        /// <returns>
        /// A float value representing battery charge percentage (0-100), 
        /// or -1 if no battery is found or an error occurs.
        /// </returns>
        /// <remarks>
        /// Uses Win32_Battery WMI class to retrieve battery information.
        /// Returns -1 if no battery is detected or if there's an access error.
        /// Works only on Windows systems with WMI support.
        /// </remarks>
        
        public static float GetBatteryCharge()
        {
            float charge = -1;

            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Battery"))
                {
                    foreach (ManagementObject battery in searcher.Get())
                    {
                        charge = Convert.ToSingle(battery["EstimatedChargeRemaining"]);
                    }
                }
            }
            catch (Exception ex)
            {
                Write(ex);
            }

            return charge;
        }

        /// <summary>
        /// Determines if the battery is currently charging.
        /// </summary>
        /// <returns>
        /// True if battery is charging (BatteryStatus = 2), 
        /// false otherwise or if no battery is found.
        /// </returns>
        /// <remarks>
        /// Uses Win32_Battery WMI class to check battery status.
        /// Returns false if no battery is detected or if there's an access error.
        /// BatteryStatus value 2 indicates charging according to WMI specification.
        /// </remarks>
        
        public static bool IsCharging()
        {
            bool isCharging = false;

            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Battery"))
                {
                    foreach (ManagementObject battery in searcher.Get())
                    {
                        isCharging = Convert.ToInt32(battery["BatteryStatus"]) == 2;
                    }
                }
            }
            catch (Exception ex)
            {
                Write(ex);
            }

            return isCharging;
        }
    }
}
