using butterBror.Utils.DataManagers;
using butterBror.Utils.Tools;
using butterBror.Utils.Tools.Device;
using Microsoft.TeamFoundation.Common;
using Pastel;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using static butterBror.Utils.Things.Console;

namespace butterBror
{
    public class Core
    {
        public static int RestartedTimes = 0;
        public static int CompletedCommands = 0;
        public static int Users = 0;
        public static int BankDollars = 0;
        public static int Ticks = 20;

        public static bool Ready = false;

        public static string Version = "2.16";
        public static string Patch = "3";
        public static string PreviousVersion = "";

        public static long TPSCounter = 0;
        public static long CPUCounterItems = 0;
        public static long TPSCounterItems = 0;
        public static long TicksCounter = 0;
        public static long SkippedTicks = 0;
        public static long TicksPerSecond = 0;
        public static float CPUPercentage = 0;

        public static float Coins = 0;
        public static double TickDelay = 0;
        public static decimal CPUCounter = 0;
        public static DateTime StartTime = new();
        public static InternalBot Bot = new InternalBot();

        private static float LastCoinAmount = 0;
        private static bool IsTickEnded = true;
        private static long LastTickCount = 0;
        private static long LastSendTick = 0;
        private static PerformanceCounter CPU = new PerformanceCounter("Processor", "% Processor Time", "_Total");


        private class DankDB_previous_statistics
        {
            public static long FileReads = 0;
            public static long CacheReads = 0;
            public static long CacheWrites = 0;
            public static long FileWrites = 0;
            public static long Checks = 0;
        }

        private static Timer TicksTimer;
        private static Timer SecondTimer;
        private static Task BotTask;

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

        public class StatisticItem
        {
            private int PerSecond = 0;
            private int Total = 0;

            private DateTime LastUpdate = DateTime.UtcNow;

            public int Get()
            {
                if ((DateTime.UtcNow - LastUpdate).TotalSeconds >= 1)
                {
                    LastUpdate = DateTime.UtcNow;
                    PerSecond = Total;
                    Total = 0;
                }

                return PerSecond;
            }

            public void Add(int count = 1)
            {
                Total += count;

                if ((DateTime.UtcNow - LastUpdate).TotalSeconds >= 1)
                {
                    LastUpdate = DateTime.UtcNow;
                    PerSecond = Total;
                    Total = 0;
                }
            }
        }

        [ConsoleSector("butterBror.Core", "Start")]
        public static async void Start(string? mainPath = null, int customTickSpeed = 20, bool runsInConsole = true)
        {
            Statistics.FunctionsUsed.Add();
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                CPU.NextValue();
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

            TicksTimer = new(OnTick, null, 0, ticks_time);
            SecondTimer = new((object? timer) =>
            {
                TicksPerSecond = TicksCounter - LastTickCount;
                LastTickCount = TicksCounter;

                TPSCounter += TicksPerSecond;
                TPSCounterItems++;

                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    CPUPercentage = CPU.NextValue();
                    CPUCounter += (decimal)CPUPercentage;
                    CPUCounterItems++;
                }
            }, null, 0, 1000);
            Write($"TPS counter successfully started.", "kernel");

            try
            {
                StartTime = DateTime.Now;
                BotTask = Task.Run(() => { Bot.Start(0); });
            }
            catch (Exception e)
            {
                Write(e);
            }
        }

        [ConsoleSector("butterBror.Core", "OnTick")]
        public static async void OnTick(object? timer)
        {
            Statistics.FunctionsUsed.Add();

            if (!IsTickEnded)
            {
                SkippedTicks++;
                return;
            }

            try
            {
                if (Bot.Initialized)
                {
                    int cache_reads = (int)((long)DankDB.Statistics.cache_reads - DankDB_previous_statistics.CacheReads);
                    int cache_writes = (int)((long)DankDB.Statistics.cache_writes - DankDB_previous_statistics.CacheWrites);
                    int writes = (int)((long)DankDB.Statistics.writes - DankDB_previous_statistics.FileWrites);
                    int reads = (int)((long)DankDB.Statistics.reads - DankDB_previous_statistics.FileReads);
                    int checks = (int)((long)DankDB.Statistics.checks - DankDB_previous_statistics.Checks);

                    Statistics.DataBase.CacheReads.Add(cache_reads);
                    Statistics.DataBase.CacheWrites.Add(cache_writes);
                    Statistics.DataBase.FileWrites.Add(writes);
                    Statistics.DataBase.FileReads.Add(reads);
                    Statistics.DataBase.Checks.Add(checks);
                    Statistics.DataBase.Operations.Add(cache_reads + cache_writes + writes + reads + checks);

                    DankDB_previous_statistics.CacheReads = (long)DankDB.Statistics.cache_reads;
                    DankDB_previous_statistics.CacheWrites = (long)DankDB.Statistics.cache_writes;
                    DankDB_previous_statistics.FileReads = (long)DankDB.Statistics.reads;
                    DankDB_previous_statistics.FileWrites = (long)DankDB.Statistics.writes;
                    DankDB_previous_statistics.Checks = (long)DankDB.Statistics.checks;

                    DateTime startTime = DateTime.Now;

                    if (!Bot.NeedRestart && Bot.Initialized && Coins != 0 && Users != 0 && LastCoinAmount != Coins)
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

                        LastCoinAmount = Coins;
                    }

                    if (DateTime.UtcNow.Minute % 10 == 0 && DateTime.UtcNow.Second == 0 && TicksCounter - LastSendTick > Ticks)
                    {
                        LastSendTick = TicksCounter;
                        Bot.Pathes.UpdatePaths();
                        Bot.SaveEmoteCache();
                        await Bot.SendTelemetry();

                        // Clearing semaphore
                        var now = DateTime.UtcNow;
                        var timeout = TimeSpan.FromMinutes(10);

                        foreach (var (userId, (semaphore, lastUsed)) in Command.messages_semaphores.ToList())
                        {
                            if (now - lastUsed > timeout)
                            {
                                if (Command.messages_semaphores.TryRemove(userId, out var entry))
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

                    IsTickEnded = false;
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
                IsTickEnded = true;
            }
        }

        [ConsoleSector("butterBror.Core", "Restart")]
        private static void Restart()
        {
            Statistics.FunctionsUsed.Add();

            RestartedTimes++;
            TicksTimer?.Dispose();
            SecondTimer?.Dispose();
            BotTask.Dispose();

            BotTask = Task.Run(() => { Bot.Start(RestartedTimes); });

            int ticks_time = 1000 / Ticks;
            TicksTimer = new Timer(OnTick, null, 0, ticks_time);
            SecondTimer = new((object? timer) =>
            {
                TicksPerSecond = TicksCounter - LastTickCount;
                LastTickCount = TicksCounter;

                TPSCounter += TicksPerSecond;
                TPSCounterItems++;

                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    CPUCounter += (decimal)CPU.NextValue();
                    CPUCounterItems++;
                }
            }, null, 0, 1000);
        }

        [ConsoleSector("butterBror.Core", "Ping")]
        public static string Ping()
        {
            return "Pong!";
        }
    }
}