using butterBror.Utils.DataManagers;
using butterBror.Utils.Tools;
using butterBror.Utils.Tools.Device;
using butterBror.Utils.Types;
using Microsoft.TeamFoundation.Common;
using Pastel;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using static butterBror.Utils.Bot.Console;

namespace butterBror
{
    /// <summary>
    /// Central class managing bot statistics, performance metrics, and system-wide operations.
    /// </summary>
    public class Core
    {
        /// <summary>
        /// Gets the number of times the bot has been restarted.
        /// </summary>
        public static int RestartedTimes = 0;

        /// <summary>
        /// Gets the count of completed commands.
        /// </summary>
        public static int CompletedCommands = 0;

        /// <summary>
        /// Gets or sets the total number of registered users.
        /// </summary>
        public static int Users = 0;

        /// <summary>
        /// Gets or sets total dollars in the virtual economy.
        /// </summary>
        public static int BankDollars = 0;

        /// <summary>
        /// Gets or sets base ticks per second for bot operations.
        /// </summary>
        public static int Ticks = 20;

        /// <summary>
        /// Gets or sets bot readiness status.
        /// </summary>
        public static bool Ready = false;

        /// <summary>
        /// Gets the current version string.
        /// </summary>
        public static string Version = "2.16";

        /// <summary>
        /// Gets the current patch version.
        /// </summary>
        public static string Patch = "3";

        /// <summary>
        /// Gets or sets the previous version string.
        /// </summary>
        public static string PreviousVersion = "";

        /// <summary>
        /// Gets or sets TPS counter value.
        /// </summary>
        public static long TPSCounter = 0;

        /// <summary>
        /// Gets or sets CPU counter items count.
        /// </summary>
        public static long CPUCounterItems = 0;

        /// <summary>
        /// Gets or sets TPS counter items count.
        /// </summary>
        public static long TPSCounterItems = 0;

        /// <summary>
        /// Gets or sets total tick count since startup.
        /// </summary>
        public static long TicksCounter = 0;

        /// <summary>
        /// Gets or sets skipped tick count.
        /// </summary>
        public static long SkippedTicks = 0;

        /// <summary>
        /// Gets or sets ticks per second value.
        /// </summary>
        public static long TicksPerSecond = 0;

        /// <summary>
        /// Gets or sets current CPU percentage usage.
        /// </summary>
        public static float CPUPercentage = 0;

        /// <summary>
        /// Gets or sets current coin balance.
        /// </summary>
        public static float Coins = 0;

        /// <summary>
        /// Gets or sets average tick delay in milliseconds.
        /// </summary>
        public static double TickDelay = 0;

        /// <summary>
        /// Gets or sets CPU counter value.
        /// </summary>
        public static decimal CPUCounter = 0;

        /// <summary>
        /// Gets or sets application start time.
        /// </summary>
        public static DateTime StartTime = new();

        /// <summary>
        /// Gets or sets the main bot instance.
        /// </summary>
        public static InternalBot Bot = new InternalBot();

        private static float _lastCoinAmount = 0;
        private static bool _isTickEnded = true;
        private static long _lastTickCount = 0;
        private static long _lastSendTick = 0;
        private static PerformanceCounter _CPU = new PerformanceCounter("Processor", "% Processor Time", "_Total");


        private class DankDBPreviousStatistics
        {
            public static long FileReads = 0;
            public static long CacheReads = 0;
            public static long CacheWrites = 0;
            public static long FileWrites = 0;
            public static long Checks = 0;
        }

        private static Timer _ticksTimer;
        private static Timer _secondTimer;
        private static Task _botTask;

        public class Statistics
        {
            public static StatisticItem FunctionsUsed = new();
            public static StatisticItem MessagesReaded = new();
            public class DataBase
            {
                public static StatisticItem Operations = new();
                public static StatisticItem FileReads = new();
                public static StatisticItem CacheReads = new();
                public static StatisticItem CacheWrites = new();
                public static StatisticItem FileWrites = new();
                public static StatisticItem Checks = new();
            }
        }

        /// <summary>
        /// Starts the bot core with specified configuration.
        /// </summary>
        /// <param name="mainPath">Optional custom path for data storage.</param>
        /// <param name="customTickSpeed">Optional TPS override (default: 20).</param>
        /// <param name="runsInConsole">Indicates whether running in console mode.</param>
        /// <remarks>
        /// - Initializes system metrics tracking
        /// - Sets up hardware information gathering
        /// - Configures performance counters
        /// - Starts periodic tick timer
        /// </remarks>
        [ConsoleSector("butterBror.Core", "Start")]
        public static async void Start(string? mainPath = null, int customTickSpeed = 20, bool runsInConsole = true)
        {
            Statistics.FunctionsUsed.Add();
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                _CPU.NextValue();
            }

            Ticks = customTickSpeed;
            if (customTickSpeed > 1000)
            {
                Write(new Exception("Ticks cannot exceed 1000 per second!"));
                return;
            }
            else if (customTickSpeed < 1)
            {
                Write(new Exception("Ticks cannot be less than 1 per second!"));
                return;
            }
            int ticks_time = 1000 / Ticks;

            Write("Please wait...", "kernel");

            string proccessor = "Unnamed processor";
            string OSName = "Unknown";
            double memory = 0;
            long totalDiskSpace = 0;
            long totalFreeDiskSpace = 0;

            try
            {
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");
                    foreach (ManagementObject cpu in searcher.Get())
                    {
                        proccessor = cpu["Name"].ToString();
                    }
                }
            }
            catch (Exception)
            {
                Write("Unable to get operating system name", "kernel");
            }

            try
            {
                memory = Memory.BytesToGB(Memory.GetTotalMemoryBytes());
            }
            catch (Exception)
            {
                Write("Unable to get RAM size", "kernel");
            }

            try
            {
                foreach (DriveInfo drive in Drives.Get())
                {
                    if (drive.IsReady)
                    {
                        totalDiskSpace += drive.TotalFreeSpace;
                        totalFreeDiskSpace += drive.AvailableFreeSpace;
                    }
                }
            }
            catch (Exception)
            {
                Write("Unable to get disks sizes", "kernel");
            }

            try
            {
                var name = (from x in new ManagementObjectSearcher("SELECT Caption FROM Win32_OperatingSystem").Get().Cast<ManagementObject>()
                            select x.GetPropertyValue("Caption")).FirstOrDefault();
                OSName = name != null ? name.ToString() : "Unknown";
            }
            catch (Exception ex)
            {
                Write("Unable to get OS name", "kernel");
            }

            Write($@"ButterBror

            :::::::::                  
        :::::::::::::::::              
      ::i:::::::::::::::::::           
  :::::::::::::::::::::::::::::        
 ::::::::::::::::::::::::::::::::      Framework: {(runsInConsole ? RuntimeInformation.FrameworkDescription.Pastel("#ff7b42") : RuntimeInformation.FrameworkDescription)}
 {{~:::::::::::::::::::::::::::::::     v.{(runsInConsole ? Version.ToString().Pastel("#ff7b42") : Version.ToString())}.{Patch}
 0000XI::::::::::::::::::::::tC00:     {(runsInConsole ? customTickSpeed.ToString().Pastel("#ff7b42") : customTickSpeed)} TPS
 ::c0000nI::::::::::::::::(v1::<l      {(runsInConsole ? OSName.Pastel("#ff7b42") : OSName)}
 ((((:n0000f-::::::::}}x00(::n000(:     {(runsInConsole ? proccessor.Pastel("#ff7b42") : proccessor)}
 n0((::::c0000f(:::>}}X(l!00QQ0((::     RAM: {(runsInConsole ? memory.ToString().Pastel("#ff7b42") : memory)} GB
  :():::::::C000000000000:::::+l:      Total disks space: {(runsInConsole ? Math.Round(Memory.BytesToGB(totalDiskSpace)).ToString().Pastel("#ff7b42") : Math.Round(Memory.BytesToGB(totalDiskSpace)))} GB
     Ix:(((((((:-}}-:((:::100_:         Available disks space: {(runsInConsole ? Math.Round(Memory.BytesToGB(totalFreeDiskSpace)).ToString().Pastel("#ff7b42") : Math.Round(Memory.BytesToGB(totalFreeDiskSpace)))} GB
        :X00:((:::::]000x;:            
            :x0000000n:                
              :::::::
", "kernel");

            Write($"The engine is currently starting...", "kernel");

            Bot = new InternalBot();

            if (mainPath != null)
            {
                Bot.Pathes.General = mainPath;
                Bot.Pathes.Main = mainPath + "butterBror/";

                Write($"Main path: {Bot.Pathes.Main}", "kernel");
                Write($"The paths are set!", "kernel");
            }
            else
            {
                Bot.Pathes.General = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}/ItzKITb/";
                Bot.Pathes.Main = Bot.Pathes.General + "butterBror/";
            }

            _ticksTimer = new(OnTick, null, 0, ticks_time);
            _secondTimer = new((object? timer) =>
            {
                TicksPerSecond = TicksCounter - _lastTickCount;
                _lastTickCount = TicksCounter;

                TPSCounter += TicksPerSecond;
                TPSCounterItems++;

                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    CPUPercentage = _CPU.NextValue();
                    CPUCounter += (decimal)CPUPercentage;
                    CPUCounterItems++;
                }
            }, null, 0, 1000);
            Write($"TPS counter successfully started.", "kernel");

            try
            {
                StartTime = DateTime.Now;
                _botTask = Task.Run(() => { Bot.Start(0); });
            }
            catch (Exception e)
            {
                Write(e);
            }
        }

        /// <summary>
        /// Periodic task handler for system metrics and data persistence.
        /// </summary>
        /// <param name="timer">Timer state object.</param>
        /// <remarks>
        /// - Updates database statistics
        /// - Tracks currency metrics
        /// - Manages semaphore cleanup
        /// - Sends telemetry data every 10 minutes
        /// - Handles bot restart operations
        /// </remarks>
        [ConsoleSector("butterBror.Core", "OnTick")]
        public static async void OnTick(object? timer)
        {
            Statistics.FunctionsUsed.Add();

            if (!_isTickEnded)
            {
                SkippedTicks++;
                return;
            }

            try
            {
                if (Bot.Initialized)
                {
                    int cache_reads = (int)((long)DankDB.Statistics.cache_reads - DankDBPreviousStatistics.CacheReads);
                    int cache_writes = (int)((long)DankDB.Statistics.cache_writes - DankDBPreviousStatistics.CacheWrites);
                    int writes = (int)((long)DankDB.Statistics.writes - DankDBPreviousStatistics.FileWrites);
                    int reads = (int)((long)DankDB.Statistics.reads - DankDBPreviousStatistics.FileReads);
                    int checks = (int)((long)DankDB.Statistics.checks - DankDBPreviousStatistics.Checks);

                    Statistics.DataBase.CacheReads.Add(cache_reads);
                    Statistics.DataBase.CacheWrites.Add(cache_writes);
                    Statistics.DataBase.FileWrites.Add(writes);
                    Statistics.DataBase.FileReads.Add(reads);
                    Statistics.DataBase.Checks.Add(checks);
                    Statistics.DataBase.Operations.Add(cache_reads + cache_writes + writes + reads + checks);

                    DankDBPreviousStatistics.CacheReads = (long)DankDB.Statistics.cache_reads;
                    DankDBPreviousStatistics.CacheWrites = (long)DankDB.Statistics.cache_writes;
                    DankDBPreviousStatistics.FileReads = (long)DankDB.Statistics.reads;
                    DankDBPreviousStatistics.FileWrites = (long)DankDB.Statistics.writes;
                    DankDBPreviousStatistics.Checks = (long)DankDB.Statistics.checks;

                    DateTime startTime = DateTime.Now;

                    if (!Bot.NeedRestart && Bot.Initialized && Coins != 0 && Users != 0 && _lastCoinAmount != Coins)
                    {
                        var date = DateTime.UtcNow;
                        Dictionary<string, dynamic> currencyData = new()
                            {
                                    { "amount", Coins },
                                    { "users", Users },
                                    { "dollars", BankDollars },
                                    { "cost", BankDollars / Coins },
                                    { "middleBalance", Coins / Users }
                            };

                        if (!Bot.Pathes.Currency.IsNullOrEmpty())
                        {
                            SafeManager.Save(Bot.Pathes.Currency, "totalAmount", Coins, false);
                            SafeManager.Save(Bot.Pathes.Currency, "totalUsers", Users, false);
                            SafeManager.Save(Bot.Pathes.Currency, "totalDollarsInTheBank", BankDollars, false);
                            SafeManager.Save(Bot.Pathes.Currency, $"[{date.Day}.{date.Month}.{date.Year}]", "", false);
                            SafeManager.Save(Bot.Pathes.Currency, $"[{date.Day}.{date.Month}.{date.Year}] cost", (BankDollars / Coins), false);
                            SafeManager.Save(Bot.Pathes.Currency, $"[{date.Day}.{date.Month}.{date.Year}] amount", Coins, false);
                            SafeManager.Save(Bot.Pathes.Currency, $"[{date.Day}.{date.Month}.{date.Year}] users", Users, false);
                            SafeManager.Save(Bot.Pathes.Currency, $"[{date.Day}.{date.Month}.{date.Year}] dollars", BankDollars);
                        }

                        _lastCoinAmount = Coins;
                    }

                    if (DateTime.UtcNow.Minute % 10 == 0 && DateTime.UtcNow.Second == 0 && TicksCounter - _lastSendTick > Ticks)
                    {
                        _lastSendTick = TicksCounter;
                        Bot.Pathes.UpdatePaths();
                        Bot.SaveEmoteCache();
                        await Bot.SendTelemetry();

                        // Clearing semaphore
                        var now = DateTime.UtcNow;
                        var timeout = TimeSpan.FromMinutes(10);

                        foreach (var (userId, (semaphore, lastUsed)) in Command.messagesSemaphores.ToList())
                        {
                            if (now - lastUsed > timeout)
                            {
                                if (Command.messagesSemaphores.TryRemove(userId, out var entry))
                                {
                                    try
                                    {
                                        if (entry.Semaphore.CurrentCount == 0)
                                            entry.Semaphore.Release();
                                    }
                                    finally
                                    {
                                        entry.Semaphore.Dispose();
                                    }
                                }
                            }
                        }
                    }

                    _isTickEnded = false;
                    TicksCounter++;
                    TickDelay = (DateTime.Now - startTime).TotalMilliseconds;
                }
                else if (Bot.NeedRestart)
                {
                    Restart();
                }
            }
            catch (Exception e)
            {
                Write(e);
            }
            finally
            {
                _isTickEnded = true;
            }
        }

        /// <summary>
        /// Restarts the bot instance with preserved state.
        /// </summary>
        /// <remarks>
        /// - Increments restart counter
        /// - Disposes old timers
        /// - Preserves statistics between restarts
        /// </remarks>
        [ConsoleSector("butterBror.Core", "Restart")]
        private static void Restart()
        {
            Statistics.FunctionsUsed.Add();

            RestartedTimes++;
            _ticksTimer?.Dispose();
            _secondTimer?.Dispose();
            _botTask.Dispose();

            _botTask = Task.Run(() => { Bot.Start(RestartedTimes); });

            int ticks_time = 1000 / Ticks;
            _ticksTimer = new Timer(OnTick, null, 0, ticks_time);
            _secondTimer = new((object? timer) =>
            {
                TicksPerSecond = TicksCounter - _lastTickCount;
                _lastTickCount = TicksCounter;

                TPSCounter += TicksPerSecond;
                TPSCounterItems++;

                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    CPUCounter += (decimal)_CPU.NextValue();
                    CPUCounterItems++;
                }
            }, null, 0, 1000);
        }

        /// <summary>
        /// Responds to ping requests with status confirmation.
        /// </summary>
        /// <returns>"Pong!" status response</returns>
        [ConsoleSector("butterBror.Core", "Ping")]
        public static string Ping()
        {
            return "Pong!";
        }
    }
}