using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace bb.Services.Internal
{
    /// <summary>
    /// Provides CPU usage monitoring across different platforms (Windows, Linux, macOS)
    /// </summary>
    public sealed class CpuUsage : IDisposable
    {
        private readonly ICpuUsageProvider _provider;
        private bool _disposed;

        /// <summary>
        /// Creates a new instance of CpuUsage monitor
        /// </summary>
        /// <param name="measurementInterval">Interval between measurements in milliseconds (default: 500)</param>
        public CpuUsage(int measurementInterval = 500)
        {
            if (measurementInterval < 100)
                throw new ArgumentException("Interval must be at least 100ms", nameof(measurementInterval));

            _provider = CreateProvider(measurementInterval);
        }

        /// <summary>
        /// Gets current CPU usage percentage
        /// </summary>
        /// <returns>CPU usage in percent (0-100)</returns>
        public float GetUsage()
        {
            CheckDisposed();
            return _provider.GetUsage();
        }

        private ICpuUsageProvider CreateProvider(int interval)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return new WindowsCpuProvider(interval);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return new LinuxCpuProvider(interval);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return new MacCpuProvider(interval);

            throw new PlatformNotSupportedException(
                $"CPU monitoring is not supported on {RuntimeInformation.OSDescription}");
        }

        private void CheckDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(CpuUsage));
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                (_provider as IDisposable)?.Dispose();
                _disposed = true;
            }
        }

        private interface ICpuUsageProvider : IDisposable
        {
            float GetUsage();
        }

        private sealed class WindowsCpuProvider : ICpuUsageProvider
        {
            private readonly PerformanceCounter _cpuCounter;
            private readonly int _interval;

            public WindowsCpuProvider(int interval)
            {
                _interval = interval;
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                // First call returns 0, so we make a dummy call
                _cpuCounter.NextValue();
            }

            public float GetUsage()
            {
                return _cpuCounter.NextValue();
            }

            public void Dispose() => _cpuCounter?.Dispose();
        }

        private sealed class LinuxCpuProvider : ICpuUsageProvider
        {
            private readonly int _interval;
            private (ulong Total, ulong Idle) _previousStats;

            public LinuxCpuProvider(int interval)
            {
                _interval = interval;
                _previousStats = ReadCpuStats();
            }

            public float GetUsage()
            {
                var currentStats = ReadCpuStats();
                var prev = _previousStats;
                _previousStats = currentStats;

                ulong totalDelta = currentStats.Total - prev.Total;
                ulong idleDelta = currentStats.Idle - prev.Idle;

                if (totalDelta == 0)
                    return 0;

                return (float)((totalDelta - idleDelta) / (double)totalDelta * 100.0);
            }

            private (ulong Total, ulong Idle) ReadCpuStats()
            {
                string[] lines = File.ReadAllLines("/proc/stat");
                foreach (string line in lines)
                {
                    if (line.StartsWith("cpu "))
                    {
                        string[] parts = line.Split(
                            new[] { ' ' },
                            StringSplitOptions.RemoveEmptyEntries
                        );

                        // Format: user, nice, system, idle, iowait, irq, softirq, steal, guest, guest_nice
                        if (parts.Length < 5)
                            throw new InvalidDataException("Unexpected /proc/stat format");

                        ulong user = ulong.Parse(parts[1]);
                        ulong nice = ulong.Parse(parts[2]);
                        ulong system = ulong.Parse(parts[3]);
                        ulong idle = ulong.Parse(parts[4]);

                        return (user + nice + system + idle, idle);
                    }
                }

                throw new InvalidOperationException("Could not find CPU stats in /proc/stat");
            }

            public void Dispose() { }
        }

        private sealed class MacCpuProvider : ICpuUsageProvider
        {
            private readonly int _interval;
            private (ulong User, ulong System, ulong Idle, ulong Nice) _previousStats;

            public MacCpuProvider(int interval)
            {
                _interval = interval;
                _previousStats = ReadCpuStats();
            }

            public float GetUsage()
            {
                var currentStats = ReadCpuStats();
                var prev = _previousStats;
                _previousStats = currentStats;

                ulong totalDelta = (currentStats.User + currentStats.System + currentStats.Idle + currentStats.Nice) -
                                   (prev.User + prev.System + prev.Idle + prev.Nice);

                ulong busyDelta = (currentStats.User + currentStats.System + currentStats.Nice) -
                                  (prev.User + prev.System + prev.Nice);

                if (totalDelta == 0)
                    return 0;

                return (float)(busyDelta / (double)totalDelta * 100.0);
            }

            private (ulong User, ulong System, ulong Idle, ulong Nice) ReadCpuStats()
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "sysctl",
                        Arguments = "kern.cp_time",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                // Expected output: "kern.cp_time: 123456 7890 12345 6789 123456"
                string[] parts = output.Split(
                    new[] { ' ', ':' },
                    StringSplitOptions.RemoveEmptyEntries
                );

                if (parts.Length < 6)
                    throw new InvalidDataException("Unexpected sysctl output format");

                return (
                    User: ulong.Parse(parts[1]),
                    System: ulong.Parse(parts[2]),
                    Idle: ulong.Parse(parts[4]),
                    Nice: ulong.Parse(parts[3])
                );
            }

            public void Dispose() { }
        }
    }
}
