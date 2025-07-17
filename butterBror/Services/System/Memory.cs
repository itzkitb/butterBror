using butterBror.Data;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace butterBror.Services.System
{
    /// <summary>
    /// Provides platform-specific methods to retrieve system memory information and conversion utilities.
    /// </summary>
    public class Memory
    {
        /// <summary>
        /// Gets the total physical memory installed on the system in bytes.
        /// </summary>
        /// <returns>Total physical memory in bytes</returns>
        /// <exception cref="PlatformNotSupportedException">Thrown when running on an unsupported OS platform</exception>
        /// <remarks>
        /// Uses platform-specific implementations:
        /// - Windows: Calls kernel32.dll's GetPhysicallyInstalledSystemMemory
        /// - Linux: Reads from /proc/meminfo
        /// - macOS: Uses sysctl command
        /// </remarks>
        public static ulong GetTotalMemoryBytes()
        {
            Engine.Statistics.FunctionsUsed.Add();
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

        /// <summary>
        /// Gets the total physical memory on Windows systems using kernel32.dll.
        /// </summary>
        /// <returns>Total physical memory in bytes</returns>
        /// <remarks>
        /// Uses Windows API call to GetPhysicallyInstalledSystemMemory.
        /// Memory is reported in kilobytes and converted to bytes.
        /// </remarks>
        private static ulong GetWindowsTotalMemory()
        {
            [DllImport("kernel32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            static extern bool GetPhysicallyInstalledSystemMemory(out long TotalMemoryInKilobytes);

            GetPhysicallyInstalledSystemMemory(out long TotalMemoryInKilobytes);
            return (ulong)TotalMemoryInKilobytes * 1024;
        }

        /// <summary>
        /// Gets the total physical memory on Linux systems by parsing /proc/meminfo.
        /// </summary>
        /// <returns>Total physical memory in bytes</returns>
        /// <remarks>
        /// Reads the first line of /proc/meminfo and extracts the memory value in kilobytes.
        /// Converts the value to bytes using multiplication by 1024.
        /// </remarks>
        private static ulong GetLinuxTotalMemory()
        {
            Engine.Statistics.FunctionsUsed.Add();
            string memInfo = FileUtil.GetFileContent("/proc/meminfo");
            string totalMemoryLine = memInfo.Split('\n')[0];
            string totalMemoryValue = totalMemoryLine.Split([' '], StringSplitOptions.RemoveEmptyEntries)[1];
            return Convert.ToUInt64(totalMemoryValue) * 1024;
        }

        /// <summary>
        /// Gets the total physical memory on macOS systems using sysctl command.
        /// </summary>
        /// <returns>Total physical memory in bytes</returns>
        /// <remarks>
        /// Executes 'sysctl -n hw.memsize' to get memory size and returns it directly.
        /// On macOS, this returns the value in bytes without needing conversion.
        /// </remarks>
        private static ulong GetMacOSTotalMemory()
        {
            Engine.Statistics.FunctionsUsed.Add();
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

        /// <summary>
        /// Converts a byte count to gigabytes.
        /// </summary>
        /// <param name="bytes">The number of bytes to convert</param>
        /// <returns>The equivalent gigabytes value</returns>
        public static double BytesToGB(long bytes)
        {
            Engine.Statistics.FunctionsUsed.Add();
            return bytes / (1024.0 * 1024.0 * 1024.0);
        }

        /// <summary>
        /// Converts an unsigned byte count to gigabytes.
        /// </summary>
        /// <param name="bytes">The number of bytes to convert</param>
        /// <returns>The equivalent gigabytes value</returns>
        public static double BytesToGB(ulong bytes)
        {
            Engine.Statistics.FunctionsUsed.Add();
            return bytes / (1024.0 * 1024.0 * 1024.0);
        }
    }
}
