using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using static butterBror.Utils.Things.Console;

namespace butterBror.Utils.Tools.Device
{
    public class Battery
    {
        [ConsoleSector("butterBror.Utils.Tools.Device.Battery", "GetBatteryCharge")]
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

        [ConsoleSector("butterBror.Utils.Tools.Device.Battery", "IsCharging")]
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
