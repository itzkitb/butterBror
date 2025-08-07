using butterBror.Core.Bot;
using butterBror.Data;
using butterBror.Models;
using butterBror.Services.System;
using butterBror.Utils;
using DankDB;
using Microsoft.TeamFoundation.Common;
using Microsoft.VisualStudio.Services.Common;
using Pastel;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using static butterBror.Core.Bot.Console;

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
        /// Gets or sets bot readiness status.
        /// </summary>
        public static bool Ready = false;

        /// <summary>
        /// Gets the current version string.
        /// </summary>
        public static string Version = "2.17";

        /// <summary>
        /// Gets the current patch version.
        /// </summary>
        public static string Patch = "5";

        /// <summary>
        /// Gets or sets the previous version string.
        /// </summary>
        public static string PreviousVersion = "";

        /// <summary>
        /// Gets or sets CPU counter items count.
        /// </summary>
        public static long TelemetryCPUItems = 0;

        /// <summary>
        /// Gets or sets current coin balance.
        /// </summary>
        public static float Coins = 0;

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
        private static PerformanceCounter _CPU = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        private static Task _repeater;
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
        public static void Main(string[] args)
        {
            System.Console.Title = "Loading libraries...";
            System.Console.WriteLine("Loading libraries...");
            
            Initialize(args);
            ButterBrorFetch();
            StartEngine();

            System.Console.ReadLine();
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
        private static async Task StartRepeater()
        {
            await Task.Delay(1000 - DateTime.UtcNow.Millisecond);

            while (true)
            {
                try
                {
                    if (Bot.Initialized)
                    {
                        TelemetryCPUItems++;
                        TelemetryCPU += (decimal)_CPU.NextValue();

                        if (Coins != 0 && Users != 0 && _lastCoinAmount != Coins)
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
                                Manager.Save(Bot.Pathes.Currency, "totalAmount", Coins);
                                Manager.Save(Bot.Pathes.Currency, "totalUsers", Users);
                                Manager.Save(Bot.Pathes.Currency, "totalDollarsInTheBank", BankDollars);
                                Manager.Save(Bot.Pathes.Currency, $"[{date.Day}.{date.Month}.{date.Year}]", "");
                                Manager.Save(Bot.Pathes.Currency, $"[{date.Day}.{date.Month}.{date.Year}] cost", (BankDollars / Coins));
                                Manager.Save(Bot.Pathes.Currency, $"[{date.Day}.{date.Month}.{date.Year}] amount", Coins);
                                Manager.Save(Bot.Pathes.Currency, $"[{date.Day}.{date.Month}.{date.Year}] users", Users);
                                Manager.Save(Bot.Pathes.Currency, $"[{date.Day}.{date.Month}.{date.Year}] dollars", BankDollars);
                            }

                            _lastCoinAmount = Coins;
                        }

                        if (DateTime.UtcNow.Minute % 10 == 0 && DateTime.UtcNow.Second == 0)
                        {
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

                        if (DateTime.UtcNow.Hour == 0 && DateTime.UtcNow.Minute == 0 && DateTime.UtcNow.Second == 0)
                        {
                            _ = Bot.BackupDataAsync();
                        }

                        if (DateTime.UtcNow.Second == 0 && (Bot.allMessages.Count > 0 || Bot.allFirstMessages.Count > 0))
                        {
                            Stopwatch stopwatch = Stopwatch.StartNew();
                            int messages = Bot.allMessages.Count + Bot.allFirstMessages.Count;

                            if (Bot.allMessages.Count > 0)
                            {
                                Bot.SQL.Messages.SaveMessages(Bot.allMessages);
                                Bot.allMessages.Clear();
                            }

                            if (Bot.allFirstMessages.Count > 0)
                            {
                                Bot.SQL.Channels.SaveFirstMessages(Bot.allFirstMessages);
                                Bot.allFirstMessages.Clear();
                            }

                            stopwatch.Stop();
                            Write($"Saved {messages} messages in {stopwatch.ElapsedMilliseconds} ms", "info");
                        }
                    }
                }
                catch (Exception e)
                {
                    Write(e);
                }
                finally
                {
                    await Task.Delay(1000 - DateTime.UtcNow.Millisecond);
                }
            }
        }

        /// <summary>
        /// Responds to ping requests with status confirmation.
        /// </summary>
        /// <returns>"Pong!" status response</returns>
        public static string Ping()
        {
            return "Pong!";
        }

        /// <summary>
        /// Closes the application with a specific code.
        /// </summary>
        public static void Exit(int exitCode)
        {
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
        private static void ButterBrorFetch()
        {
            
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
  :::::::::::::::::::::::::::::        ButterBror
 ::::::::::::::::::::::::::::::::      Host: {hostName} v.{hostVersion}
 {{~:::::::::::::::::::::::::::::::     Framework: {RuntimeInformation.FrameworkDescription.Pastel("#ff7b42")}
 0000XI::::::::::::::::::::::tC00:     v.{Version.ToString().Pastel("#ff7b42")}.{Patch}
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
        private static void Initialize(string[] args)
        {
            System.Console.Title = $"butterBror | v.{Version}.{Patch}";
            System.Console.Clear();

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

            #if RELEASE
            if (hostName is null || hostVersion is null)
            {
                Write("The bot is running without a host! Please run it from the host, not directly.", "kernel");
                System.Console.ReadLine();
                return;
            }
            #endif

            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                _CPU.NextValue();
            }
        }

        /// <summary>
        /// Starts the core engine of the ButterBror bot, initializing timers and bot instance.
        /// </summary>
        /// <remarks>
        /// This method initializes the main bot instance, sets up file paths for data storage, and starts the tick loop.
        /// Finally, it records the application start time and triggers the bot's startup process, logging progress to the console.
        /// </remarks>
        private static void StartEngine()
        {
            Write($"The engine is currently starting...", "kernel");

            Bot = new InternalBot();
            Bot.Pathes.General = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}/ItzKITb/";
            Bot.Pathes.Main = Bot.Pathes.General + "butterBror/";

            _repeater = Task.Run(() => StartRepeater());
            Write($"TPS counter successfully started.", "kernel");

            StartTime = DateTime.Now;
            Bot.Start();
        }
    }
}