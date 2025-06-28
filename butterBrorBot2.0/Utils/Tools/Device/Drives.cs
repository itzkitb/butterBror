using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace butterBror.Utils.Tools.Device
{
    public class Drives
    {
        public static DriveInfo[] Get()
        {
            Core.Statistics.FunctionsUsed.Add();
            DriveInfo[] drives = DriveInfo.GetDrives();
            return drives;
        }
    }
}
