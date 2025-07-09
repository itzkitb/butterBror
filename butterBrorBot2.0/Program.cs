using butterBror.Utils.DataManagers;
using butterBror.Utils.Tools;
using butterBror.Utils.Tools.Device;
using butterBror.Utils.Types;
using Microsoft.TeamFoundation.Common;
using Microsoft.VisualStudio.Services.Common;
using Pastel;
using System;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using static butterBror.Utils.Bot.Console;

namespace butterBror
{
    /// <summary>
    /// Central class managing bot statistics, performance metrics, and system-wide operations.
    /// </summary>
    public class Engine
    {
        #region Variables
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
        public static string Patch = "4";

        /// <summary>
        /// Gets or sets the previous version string.
        /// </summary>
        public static string PreviousVersion = "";

        /// <summary>
        /// Gets or sets TPS counter value.
        /// </summary>
        public static long TelemetryTPS = 0;

        /// <summary>
        /// Gets or sets CPU counter items count.
        /// </summary>
        public static long TelemetryCPUItems = 0;

        /// <summary>
        /// Gets or sets TPS counter items count.
        /// </summary>
        public static long TelemetryTPSItems = 0;

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
        public static decimal TelemetryCPU = 0;

        /// <summary>
        /// Gets or sets application start time.
        /// </summary>
        public static DateTime StartTime = new();

        /// <summary>
        /// Gets or sets the main bot instance.
        /// </summary>
        public static InternalBot Bot = new InternalBot();

        /// <summary>
        /// Gets or sets the core name.
        /// </summary>
        public static string hostName = null;

        /// <summary>
        /// Gets or sets the core version.
        /// </summary>
        public static string hostVersion = null;

        private static float _lastCoinAmount = 0;
        private static long _tpsCounter = 0;
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

        private static Task _ticksTimer;
        private static Timer _secondTimer;

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
        #endregion

        /// <summary>
        /// Starts the bot core with specified configuration.
        /// </summary>
        /// <param name="mainPath">Optional custom path for data storage.</param>
        /// <param name="customTickSpeed">Optional TPS override (default: 20).</param>
        /// <remarks>
        /// - Initializes system metrics tracking
        /// - Sets up hardware information gathering
        /// - Configures performance counters
        /// - Starts periodic tick timer
        /// </remarks>
        [ConsoleSector("butterBror.Core", "Start")]
        public static void Main(string[] args)
        {
            Statistics.FunctionsUsed.Add();

            Initialize(args);
            ButterBrorFetch(Ticks);
            StartEngine();

            Console.ReadLine();
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
        [ConsoleSector("butterBror.Engine", "StartTickLoop")]
        private static async Task StartTickLoop(int ticksTime)
        {
            Statistics.FunctionsUsed.Add();

            while (true)
            {
                Stopwatch elapsedTime = new Stopwatch();
                elapsedTime.Start();

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

                        if (Bot is not null && Bot.Initialized && Coins != 0 && Users != 0 && _lastCoinAmount != Coins)
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
                    }

                    TicksCounter++;
                    TickDelay = elapsedTime.ElapsedMilliseconds;
                }
                catch (Exception e)
                {
                    Write(e);
                }
                finally
                {
                    elapsedTime.Stop();
                    var delayTime = Math.Max(0, ticksTime - (int)elapsedTime.ElapsedMilliseconds);
                    await Task.Delay(delayTime);
                }
            }
        }

        /// <summary>
        /// Responds to ping requests with status confirmation.
        /// </summary>
        /// <returns>"Pong!" status response</returns>
        [ConsoleSector("butterBror.Engine", "Ping")]
        public static string Ping()
        {
            Statistics.FunctionsUsed.Add();
            return "Pong!";
        }

        /// <summary>
        /// Closes the application with a specific code.
        /// </summary>
        [ConsoleSector("butterBror.Engine", "Exit")]
        public static void Exit(int exitCode)
        {
            Statistics.FunctionsUsed.Add();
            Environment.Exit(exitCode);
        }

        /// <summary>
        /// Fetches and displays system hardware and software information for the ButterBror bot.
        /// </summary>
        /// <param name="customTickSpeed">The custom tick speed (TPS - ticks per second) set for the bot's operation.</param>
        /// <remarks>
        /// This method gathers detailed system information including processor name, operating system details, 
        /// total and available memory (RAM), and disk space statistics. It then formats and displays this 
        /// information in a stylized console output along with bot version, framework, and host details.
        /// If any information cannot be retrieved due to exceptions, appropriate error messages are logged.
        /// </remarks>
        [ConsoleSector("butterBror.Engine", "ButterBrorFetch")]
        private static void ButterBrorFetch(int customTickSpeed)
        {
            Statistics.FunctionsUsed.Add();
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
                        totalDiskSpace += drive.TotalSize;
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
  :::::::::::::::::::::::::::::        Host: {hostName} v.{hostVersion}
 ::::::::::::::::::::::::::::::::      Framework: {RuntimeInformation.FrameworkDescription.Pastel("#ff7b42")}
 {{~:::::::::::::::::::::::::::::::     v.{Version.ToString().Pastel("#ff7b42")}.{Patch}
 0000XI::::::::::::::::::::::tC00:     {customTickSpeed.ToString().Pastel("#ff7b42")} TPS
 ::c0000nI::::::::::::::::(v1::<l      {OSName.Pastel("#ff7b42")}
 ((((:n0000f-::::::::}}x00(::n000(:     {proccessor.Pastel("#ff7b42")}
 n0((::::c0000f(:::>}}X(l!00QQ0((::     RAM: {memory.ToString().Pastel("#ff7b42")} GB
  :():::::::C000000000000:::::+l:      Total disks space: {Math.Round(Memory.BytesToGB(totalDiskSpace)).ToString().Pastel("#ff7b42")} GB
     Ix:(((((((:-}}-:((:::100_:         Available disks space: {Math.Round(Memory.BytesToGB(totalFreeDiskSpace)).ToString().Pastel("#ff7b42")} GB
        :X00:((:::::]000x;:            
            :x0000000n:                
              :::::::
", "kernel");
        }

        /// <summary>
        /// Initializes the core settings and environment for the ButterBror bot.
        /// </summary>
        /// <param name="args">Command-line arguments passed to the application.</param>
        /// <remarks>
        /// This method sets up the console title, parses command-line arguments to configure core name and version,
        /// and initializes performance counters for CPU usage monitoring on Windows. It also validates the tick speed 
        /// (TPS) to ensure it falls within acceptable limits (1 to 1000 ticks per second). If critical parameters like 
        /// core name or version are missing, or if tick speed validation fails, the method logs errors and halts execution.
        /// </remarks>
        [ConsoleSector("butterBror.Engine", "Initialize")]
        private static void Initialize(string[] args)
        {
            Console.Title = $"butterBror | v.{Version}.{Patch}";

            int customTickSpeed = 20;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--core-version" && i + 1 < args.Length)
                {
                    hostVersion = args[i + 1];
                }

                if (args[i] == "--core-name" && i + 1 < args.Length)
                {
                    hostName = args[i + 1];
                }
            }

            if (hostName is null || hostVersion is null)
            {
                Write("The bot is running without a host! Please run it from the host, not directly.", "kernel");
                Console.ReadLine();
                return;
            }

            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                _CPU.NextValue();
            }

            Ticks = customTickSpeed;
            if (customTickSpeed > 1000)
            {
                Write(new Exception("Ticks cannot exceed 1000 per second!"));
                Console.ReadLine();
                return;
            }
            else if (customTickSpeed < 1)
            {
                Write(new Exception("Ticks cannot be less than 1 per second!"));
                Console.ReadLine();
                return;
            }
        }

        /// <summary>
        /// Starts the core engine of the ButterBror bot, initializing timers and bot instance.
        /// </summary>
        /// <remarks>
        /// This method initializes the main bot instance, sets up file paths for data storage, and starts the tick loop 
        /// with a calculated interval based on the specified ticks per second (TPS). It also sets up a secondary timer 
        /// for periodic updates of performance metrics like TPS and CPU usage. Finally, it records the application start 
        /// time and triggers the bot's startup process, logging progress to the console.
        /// </remarks>
        [ConsoleSector("butterBror.Engine", "StartEngine")]
        private static void StartEngine()
        {
            Write($"The engine is currently starting...", "kernel");

            Bot = new InternalBot();
            Bot.Pathes.General = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}/ItzKITb/";
            Bot.Pathes.Main = Bot.Pathes.General + "butterBror/";

            int ticks_time = 1000 / Ticks;
            _ticksTimer = Task.Run(() => StartTickLoop(ticks_time));
            _secondTimer = new((object? timer) =>
            {
                TicksPerSecond = _tpsCounter;
                _tpsCounter = 0;

                TelemetryTPS += TicksPerSecond;
                TelemetryTPSItems++;

                CPUPercentage = _CPU.NextValue();
                TelemetryCPU += (decimal)CPUPercentage;
                TelemetryCPUItems++;
            }, null, 0, 1000);
            Write($"TPS counter successfully started.", "kernel");

            StartTime = DateTime.Now;
            Bot.Start();
        }
    }
}