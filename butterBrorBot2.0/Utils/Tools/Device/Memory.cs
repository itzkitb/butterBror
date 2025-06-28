using butterBror.Utils.DataManagers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace butterBror.Utils.Tools.Device
{
    public class Memory
    {
        public static ulong GetTotalMemoryBytes()
        {
            Core.Statistics.FunctionsUsed.Add();
            // fckin piece of sht
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return GetWindowsTotalMemory();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return GetLinuxTotalMemory();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return GetMacOSTotalMemory();
            }
            else
            {
                throw new PlatformNotSupportedException("Platform not supported");
            }
        }
        private static ulong GetWindowsTotalMemory()
        {
            [DllImport("kernel32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            static extern bool GetPhysicallyInstalledSystemMemory(out long TotalMemoryInKilobytes);

            GetPhysicallyInstalledSystemMemory(out long TotalMemoryInKilobytes);
            return (ulong)TotalMemoryInKilobytes * 1024;
        }

        private static ulong GetLinuxTotalMemory()
        {
            Core.Statistics.FunctionsUsed.Add();
            string memInfo = FileUtil.GetFileContent("/proc/meminfo");
            string totalMemoryLine = memInfo.Split('\n')[0];
            string totalMemoryValue = totalMemoryLine.Split([' '], StringSplitOptions.RemoveEmptyEntries)[1];
            return Convert.ToUInt64(totalMemoryValue) * 1024;
        }

        private static ulong GetMacOSTotalMemory()
        {
            Core.Statistics.FunctionsUsed.Add();
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "sysctl",
                    Arguments = "-n hw.memsize",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return Convert.ToUInt64(output.Trim());
        }

        public static double BytesToGB(long bytes)
        {
            Core.Statistics.FunctionsUsed.Add();
            return bytes / (1024.0 * 1024.0 * 1024.0);
        }
        public static double BytesToGB(ulong bytes)
        {
            Core.Statistics.FunctionsUsed.Add();
            return bytes / (1024.0 * 1024.0 * 1024.0);
        }
    }
}
