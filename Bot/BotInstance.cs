using bb.Core.Bot;
using bb.Core.Services;
using bb.Data;
using bb.Services.External;
using bb.Services.Internal;
using bb.Utils;
using bb.Core.Configuration;
using bb.Models.Exceptions;
using DankDB;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Pastel;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using TwitchLib.Api;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Client;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;
using static bb.Core.Bot.Logger;
using bb.Data.Repositories;
using bb.Data.Entities;
using bb.Services.Platform.Discord;
using bb.Services.Platform.Twitch;
using bb.Services.Platform.Telegram;
using bb.Models.Platform;
using bb.Core.Commands;
using System.Threading.Tasks;


// Task: Add a DI container for all this crap

namespace bb
{
    /// <summary>
    /// Central class.
    /// </summary>
    public class BotInstance
    {
        #region Variables

        public readonly ClientService Clients;
        public readonly PathService Paths;
        public readonly SQLService DataBase;
        public readonly CommandService DiscordCommandService;
        public readonly Tokens Tokens;
        public readonly SevenTvService SevenTv;

        public readonly EmoteCacheService EmoteCache;
        public readonly SettingsService Settings;
        public readonly AIService AiService;
        public readonly YouTubeService YouTube;
        public readonly CooldownManager Cooldown;
        public readonly CurrencyManager Currency;
        public readonly MessageProcessor MessageProcessor;
        public readonly PlatformMessageSender MessageSender;
        public readonly Executor CommandExecutor;
        public readonly Runner CommandRunner;
        public readonly BlockedWordDetector MessageFilter;
        public readonly GitHubActionsNotifier GitHubActions;

        public CancellationTokenSource GithubCT;

        #region Core
        public Version Version = new Version("2.18.1.0");
        public string Branch = "master";
        public string Commit = "";
        public DateTime StartTime = new();
        public string PreviousVersion = "";
        public bool Initialized = false;
        public bool Connected => Clients.Twitch?.IsConnected == true && Clients.Discord?.ConnectionState == Discord.ConnectionState.Connected;
        public string? TwitchName;
        public string DefaultCommandPrefix = "!";
        private bool SkipFetch = false;
        public TimeSpan ConnectedIn;
        #endregion Core

        #region Currency
        public int Users = 0;
        public int InBankDollars = 0;
        public int CompletedCommands = 0;
        public decimal Coins = 0;
        public int CurrencyMentioned = 8;
        public int CurrencyMentioner = 2;
        public double TaxesCost = 0.0069d;
        #endregion Currency

        #region Hosting
        public string? HostName = null;
        public string? HostVersion = null;
        #endregion Hosting

        #region Buffers
        public MessagesBuffer? MessagesBuffer;
        public UsersBuffer? UsersBuffer;
        public List<(Platform platform, string channelId, long userId, Message message)> allFirstMessages = new();
        #endregion

        #region Twitch
        public string? TwitchClientId;
        public List<string> TwitchNewVersionAnnounce = [];
        public List<string> TwitchReconnectAnnounce = [];
        public List<string> TwitchConnectAnnounce = [];
        public List<string> TwitchDevAnnounce = [];
        public List<string> TwitchCurrencyRandomEvent = [];
        public List<string> TwitchTaxesEvent = [];
        #endregion Twitch

        #region Cache
        public ConcurrentDictionary<string, (SevenTV.Types.Rest.Emote emote, DateTime expiration)> EmotesCache = new();
        public ConcurrentDictionary<string, (List<string> emotes, DateTime expiration)> ChannelsSevenTVEmotes = new();
        public ConcurrentDictionary<string, (string userId, DateTime expiration)> UsersSearchCache = new();
        public ConcurrentDictionary<string, (string setId, DateTime expiration)> EmoteSetsCache = new();
        public Dictionary<string, string> UsersSevenTVIDs = new Dictionary<string, string>();
        public readonly TimeSpan CacheTTL = TimeSpan.FromMinutes(30);
        #endregion Cache

        public IServiceProvider? DiscordServiceProvider;
        public ReceiverOptions? TelegramReceiverOptions;

        private DateTime _startTime = DateTime.UtcNow;
        private CpuUsage _CpuUsage = new CpuUsage();
        private Task? _repeater;

        private DateTime _lastTelemtry = DateTime.UtcNow.AddMinutes(-1);
        private DateTime _lastSave = DateTime.UtcNow.AddMinutes(-1);
        #endregion

        public BotInstance(
        ClientService clients,
        PathService paths,
        Tokens tokens,
        SQLService dataBase,
        EmoteCacheService emoteCacheService,
        SettingsService settingsService,
        SevenTvService sevenTvService,
        AIService aiService,
        YouTubeService youTubeService,
        CooldownManager cooldownManager,
        CurrencyManager currencyManager,
        MessageProcessor messageProcessor,
        PlatformMessageSender platformMessageSender,
        CommandService commandService,
        Executor commandExecutor,
        Runner commandRunner,
        BlockedWordDetector blockedWordDetector,
        IGitHubActionsNotifier gitHubActions
    )
        {
            Clients = clients;
            Paths = paths;
            Tokens = tokens;
            DataBase = dataBase;
            EmoteCache = emoteCacheService;
            Settings = settingsService;
            SevenTv = sevenTvService;
            AiService = aiService;
            YouTube = youTubeService;
            Cooldown = cooldownManager;
            Currency = currencyManager;
            MessageProcessor = messageProcessor;
            MessageSender = platformMessageSender;
            DiscordCommandService = commandService;
            CommandExecutor = commandExecutor;
            CommandRunner = commandRunner;
            MessageFilter = blockedWordDetector;
            GitHubActions = (GitHubActionsNotifier?)gitHubActions;
        }

        #region Core
        /// <summary>
        /// Entry point of the application. Initializes and starts the bot core.
        /// </summary>
        /// <param name="args">Command-line arguments. Supported parameters:
        /// <list type="table">
        /// <item><term>--core-name [host_name]</term><description> Specifies the host name for execution</description></item>
        /// <item><term>--core-version [version]</term><description> Specifies the host version</description></item>
        /// </list>
        /// </param>
        public async Task Start(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += UnhandledException;
            StartTime = DateTime.UtcNow;

            Initialize(args);

            if (SkipFetch)
            {
                Write("Launched with --skip-fetch parameter, skipping system fetch...");
            }
            else
            {
                SystemDataFetch();
            }
            
            StartBot();

            _= Task.Run(async () =>
            {
                try
                {
                    await DashboardServer.StartAsync();
                }
                catch (Exception ex)
                {
                    Write($"Error starting dashboard: {ex.Message}", LogLevel.Error);
                }
            });

            while (true)
            {
                System.Console.ReadLine();
            }
        }

        /// <summary>
        /// Background task for periodic system metrics updates and data persistence.
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        /// <item>Updates database statistics every second</item>
        /// <item>Tracks currency metrics (coins, dollars, users) with file persistence</item>
        /// <item>Performs semaphore cleanup for inactive command handlers every 10 minutes</item>
        /// <item>Sends telemetry data at the start of each hour (00 seconds)</item>
        /// <item>Executes daily database backups at 00:00 UTC</item>
        /// <item>Persists buffered messages and user data at the start of each minute</item>
        /// </list>
        /// The task automatically adjusts delay to synchronize with system clock (triggers exactly at second boundaries).
        /// Handles all exceptions to prevent task termination while logging errors.
        /// </remarks>
        private async Task StartRepeater()
        {
            await Task.Delay(1010 - DateTime.UtcNow.Millisecond);

            while (true)
            {
                try
                {
                    if (Initialized)
                    {
                        DateTime now = DateTime.UtcNow;
                        Telemetry.CPUItems++;
                        Telemetry.CPU += (decimal)_CpuUsage.GetUsage();

                        if (now.Minute % 10 == 0 && (now - _lastTelemtry).Minutes > 0)
                        {
                            _lastTelemtry = now;
                            Paths.UpdatePaths();
                            EmoteCacheService.Save();
                            _ = Telemetry.Send();

                            var timeout = TimeSpan.FromMinutes(10);
                            foreach (var (userId, (semaphore, lastUsed)) in MessageProcessor.messagesSemaphores.ToList())
                            {
                                if (now - lastUsed > timeout)
                                {
                                    if (MessageProcessor.messagesSemaphores.TryRemove(userId, out var entry))
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

                        if (now.Hour == 0 && now.Minute == 0 && now.Second == 0)
                        {
                            _ = Task.Run((Action)(() =>
                            {
                                _ = Backup.BackupDataAsync();
                                _ = Currency.GenerateRandomEventAsync();
                                _ = Currency.CollectTaxesAsync();

                                if (this.DataBase == null)
                                {
                                    Write("Reinitialization of currency counters failed: Database is null", LogLevel.Error);
                                    return;
                                }

                                Write("Reinitializing currency counters...");
                                Users = this.DataBase.Users.GetTotalUsers();
                                Coins = this.DataBase.Users.GetTotalBalance();
                            }));
                        }

                        //Write($"({now.Second == 0} && {(now - _lastSave).TotalSeconds > 2}); ({now.Second}; {(now - _lastSave).TotalSeconds}; {now}; {_lastSave})", "debug");
                        if (now.Second == 0 && (now - _lastSave).TotalSeconds > 2)
                        {
                            if (MessagesBuffer == null || UsersBuffer == null || DataBase == null)
                            {
                                Write("Failed to save buffers and currency: MessagesBuffer, UsersBuffer, or DataBase are null", LogLevel.Error);
                            }
                            else
                            {
                                _lastSave = now;
                                Stopwatch stopwatch = Stopwatch.StartNew();

                                #region Buffer save 
                                int messages = MessagesBuffer.Count() + allFirstMessages.Count;
                                int users = UsersBuffer.Count();

                                if (MessagesBuffer.Count() > 0)
                                {
                                    MessagesBuffer.Flush();
                                }

                                if (UsersBuffer.Count() > 0)
                                {
                                    UsersBuffer.Flush();
                                }

                                if (allFirstMessages.Count > 0)
                                {
                                    DataBase.Channels.SaveFirstMessages(allFirstMessages);
                                    allFirstMessages.Clear();
                                }
                                #endregion Buffer save 
                                #region Currency save
                                Dictionary<string, dynamic> currencyData = new()
                            {
                                    { "amount", Coins },
                                    { "users", Users },
                                    { "dollars", InBankDollars },
                                    { "cost", Coins == 0 ? 0 : InBankDollars / Coins },
                                    { "middleBalance", Users == 0 ? 0 : Coins / Users }
                            };

                                if (Paths.Currency is not null)
                                {
                                    Manager.Save(Paths.Currency, "total", currencyData);
                                    Manager.Save(Paths.Currency, $"{now.Day}.{now.Month}.{now.Year}", currencyData);
                                }
                                #endregion Currency save

                                stopwatch.Stop();
                                Write($"Saved {messages} messages, {users} users and currency in {stopwatch.ElapsedMilliseconds} ms");
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Write(e);
                }
                finally
                {
                    await Task.Delay(1010 - DateTime.UtcNow.Millisecond);
                }
            }
        }

        /// <summary>
        /// Collects and displays hardware/software environment information.
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        /// <item>Processor: Retrieved via WMI (Windows only)</item>
        /// <item>Operating System: Displays OS caption/name</item>
        /// <item>Memory: Total RAM and available space in GB</item>
        /// <item>Disk Space: Aggregate storage and free space across all drives</item>
        /// </list>
        /// Outputs stylized ASCII-art banner with color-highlighted system metrics.
        /// Logs warnings but continues execution when data collection fails.
        /// Uses Pastel library for colored console output where applicable.
        /// </remarks>
        private void SystemDataFetch()
        {
            Write("Please wait...");
            string processor = "Unnamed processor";
            string OSName = "Unknown";
            double memory = 0;
            long totalDiskSpace = 0;
            long totalFreeDiskSpace = 0;

            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor"))
                    {
                        foreach (ManagementObject cpu in searcher.Get())
                        {
                            processor = cpu["Name"]?.ToString() ?? "Unnamed processor";
                            break;
                        }
                    }
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    try
                    {
                        string cpuInfo = File.ReadAllText("/proc/cpuinfo");
                        string[] lines = cpuInfo.Split('\n');
                        foreach (string line in lines)
                        {
                            if (line.StartsWith("model name"))
                            {
                                processor = line.Split(':')[1].Trim();
                                break;
                            }
                        }
                        if (processor == "Unnamed processor")
                        {
                            processor = "Linux CPU";
                        }
                    }
                    catch
                    {
                        processor = "Linux CPU";
                    }
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    try
                    {
                        using (Process process = new Process())
                        {
                            process.StartInfo.FileName = "sysctl";
                            process.StartInfo.Arguments = "-n machdep.cpu.brand_string";
                            process.StartInfo.RedirectStandardOutput = true;
                            process.StartInfo.UseShellExecute = false;
                            process.StartInfo.CreateNoWindow = true;
                            process.Start();

                            string output = process.StandardOutput.ReadToEnd();
                            process.WaitForExit();

                            if (!string.IsNullOrEmpty(output))
                            {
                                processor = output.Trim();
                            }
                            else
                            {
                                processor = "Apple CPU";
                            }
                        }
                    }
                    catch
                    {
                        processor = "Apple CPU";
                    }
                }
            }
            catch (Exception ex)
            {
                Write($"Unable to get processor info: {ex.Message}");
            } // CPU

            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    try
                    {
                        using (var searcher = new ManagementObjectSearcher("SELECT Caption FROM Win32_OperatingSystem"))
                        {
                            var os = searcher.Get().Cast<ManagementObject>().FirstOrDefault();
                            OSName = os?.GetPropertyValue("Caption")?.ToString() ?? Environment.OSVersion.ToString();
                        }
                    }
                    catch
                    {
                        OSName = Environment.OSVersion.ToString();
                    }
                }
                else
                {
                    OSName = RuntimeInformation.OSDescription;

                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && File.Exists("/etc/os-release"))
                    {
                        try
                        {
                            string osInfo = File.ReadAllText("/etc/os-release");
                            foreach (string line in osInfo.Split('\n'))
                            {
                                if (line.StartsWith("PRETTY_NAME"))
                                {
                                    OSName = line.Split('=')[1].Trim('"');
                                    break;
                                }
                            }
                        }
                        catch
                        {

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Write($"Unable to get OS name: {ex.Message}");
                OSName = Environment.OSVersion.ToString();
            } // OS name

            try
            {
                memory = Memory.BytesToGB(Memory.GetTotalMemoryBytes());
            }
            catch (Exception ex)
            {
                Write($"Unable to get RAM size: {ex.Message}");
            } // RAM

            try
            {
                bool drivesFound = false;
                foreach (DriveInfo drive in DriveInfo.GetDrives())
                {
                    if (drive.IsReady)
                    {
                        totalDiskSpace += drive.TotalSize;
                        totalFreeDiskSpace += drive.AvailableFreeSpace;
                        drivesFound = true;
                    }
                }

                if (!drivesFound && (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
                                   RuntimeInformation.IsOSPlatform(OSPlatform.OSX)))
                {
                    try
                    {
                        using (Process process = new Process())
                        {
                            process.StartInfo.FileName = "df";
                            process.StartInfo.Arguments = "-B1";
                            process.StartInfo.RedirectStandardOutput = true;
                            process.StartInfo.UseShellExecute = false;
                            process.StartInfo.CreateNoWindow = true;
                            process.Start();

                            string output = process.StandardOutput.ReadToEnd();
                            process.WaitForExit();

                            string[] lines = output.Split('\n');
                            foreach (string line in lines)
                            {
                                string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                if (parts.Length >= 4 && long.TryParse(parts[1], out long total) &&
                                    long.TryParse(parts[3], out long free))
                                {
                                    if (!parts[0].StartsWith("tmpfs") && !parts[0].StartsWith("dev") &&
                                        !parts[0].StartsWith("sys") && !parts[0].StartsWith("run") &&
                                        !parts[0].StartsWith("none"))
                                    {
                                        totalDiskSpace += total;
                                        totalFreeDiskSpace += free;
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {

                    }
                }
            }
            catch (Exception ex)
            {
                Write($"Unable to get disks sizes: {ex.Message}");
            } // Drives

            Write($@"

            :::::::::                  
        :::::::::::::::::              
      ::i:::::::::::::::::::           
  :::::::::::::::::::::::::::::        Butter{"Bror".Pastel(Program.MainColor)}
 ::::::::::::::::::::::::::::::::      Host: {HostName.Pastel(Program.MainColor)} v.{HostVersion}
 {{~:::::::::::::::::::::::::::::::     Framework: {RuntimeInformation.FrameworkDescription.Pastel(Program.MainColor)}
 0000XI::::::::::::::::::::::tC00:     v.{Version.ToString().Pastel(Program.MainColor)}
 ::c0000nI::::::::::::::::(v1::<l      {OSName.Pastel(Program.MainColor)}
 ((((:n0000f-::::::::}}x00(::n000(:     {processor.Pastel(Program.MainColor)}
 n0((::::c0000f(:::>}}X(l!00QQ0((::     RAM: {memory.ToString().Pastel(Program.MainColor)} GB
  :():::::::C000000000000:::::+l:      Total disks space: {Math.Round(Memory.BytesToGB(totalDiskSpace)).ToString().Pastel(Program.MainColor)} GB
     Ix:(((((((:-}}-:((:::100_:         Available disks space: {Math.Round(Memory.BytesToGB(totalFreeDiskSpace)).ToString().Pastel(Program.MainColor)} GB
        :X00:((:::::]000x;:            
            :x0000000n:                
              :::::::
");
        }

        /// <summary>
        /// Initializes core bot parameters and environment.
        /// </summary>
        /// <param name="args">Command-line configuration parameters:
        /// <list type="table">
        /// <item><term>--core-name</term><description> Host name (mandatory in RELEASE builds)</description></item>
        /// <item><term>--core-version</term><description> Host version (mandatory in RELEASE builds)</description></item>
        /// </list>
        /// </param>
        /// <remarks>
        /// <list type="bullet">
        /// <item>Sets console title with bot version information</item>
        /// <item>Validates critical host parameters in RELEASE configuration</item>
        /// <item>Initializes CPU performance counter for Windows systems</item>
        /// <item>Performs TPS (ticks per second) validation (1-1000 range)</item>
        /// </list>
        /// Terminates execution with error message if required parameters are missing in RELEASE mode.
        /// </remarks>
        private void Initialize(string[] args)
        {
            System.Console.Title = $"butterBror | v.{Version}";
            Clear();

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--core-version" && i + 1 < args.Length)
                {
                    HostVersion = args[i + 1];
                }

                if (args[i] == "--core-name" && i + 1 < args.Length)
                {
                    HostName = args[i + 1];
                }

                if (args[i] == "--skip-fetch")
                {
                    SkipFetch = true;
                }
            }

#if RELEASE
            if (HostName is null || HostVersion is null)
            {
                Write("The bot is running without a host! Please run it from the host, not directly.");
                System.Console.ReadLine();
                return;
            }
#endif
        }

        /// <summary>
        /// Launches the main bot loop and initializes components.
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        /// <item>Loads currency statistics from persistent storage</item>
        /// <item>Creates required configuration directories and files</item>
        /// <item>Initializes SQL database connections through SQLService</item>
        /// <item>Authenticates with Twitch using OAuth token</item>
        /// <item>Triggers platform connection sequence</item>
        /// </list>
        /// Records current version in VERSION.txt for update tracking.
        /// Initiates shutdown sequence via Shutdown() on critical failures.
        /// Handles both new installations and version upgrades.
        /// </remarks>
        public async void StartBot()
        {
            _startTime = DateTime.UtcNow;
            GitHubActions.RunStatusChanged += GitHubActionsStatusChanged;

            Write("Reading release data...");
            var releaseData = ReleaseManager.GetReleaseInfo();
            if (releaseData == null)
            {
                Write("Try deleting the release folder (Versions) and restarting the host. Press enter to exit.");
                System.Console.ReadLine();
                return;
            }

            Commit = releaseData.Commit;
            Branch = releaseData.Branch;

            GithubCT = new CancellationTokenSource();
            _ = GitHubActions.StartAsync(GithubCT.Token);

            //

            if (FileUtil.FileExists(Paths.Currency))
            {
                if (FileUtil.GetFileContent(Paths.Currency) == string.Empty)
                {
                    FileUtil.SaveFileContent(Paths.Currency, "{\"total\":{\"amount\":0.0,\"users\":0,\"dollars\":0,\"cost\":0,\"middleBalance\":0}}");
                }

                var data = Manager.Get<Dictionary<string, dynamic>>(Paths.Currency, "total");
                InBankDollars = data.ContainsKey("dollars") ? data["dollars"].GetInt32() : 0;
            }

            try
            {
                Write("Creating directories...");
                string[] directories = { Paths.Root, Paths.General, Paths.TranslateDefault, Paths.TranslateCustom };

                foreach (var dir in directories)
                {
                    FileUtil.CreateDirectory(dir);
                }

                string[] directories_with_platforms = { Paths.TranslateDefault, Paths.TranslateCustom };

                if (!FileUtil.FileExists(Paths.Settings))
                {
                    Settings.Initialize();
                    Write($"The settings file has been created! ({Paths.Settings})");
                    Thread.Sleep(-1);
                }

                string[] files = {
                        Paths.Currency,
                        Paths.Cache, Paths.Logs, Paths.APIUses,
                        Path.Combine(Paths.TranslateDefault, "ru-RU.json"),
                        Path.Combine(Paths.TranslateDefault, "en-US.json"),
                        Path.Combine(Paths.General, "Version")
                    };

                Write("Creating files...");
                foreach (var file in files)
                {
                    FileUtil.CreateFile(file);
                }

                PreviousVersion = File.ReadAllText(Path.Combine(Paths.General, "Version"));
                File.WriteAllText(Path.Combine(Paths.General, "Version"), $"{Version}");

                Write("Loading settings...");
                LoadSettings();

                Write("Initializing blocked words...");
                if (!FileUtil.FileExists(Paths.BlacklistWords))
                {
                    Manager.Save(Paths.BlacklistWords, "single_word", new List<string>());
                    Manager.Save(Paths.BlacklistWords, "replacement_list", new Dictionary<string, string>());
                    Manager.Save(Paths.BlacklistWords, "list", new List<string>());

                    Write("The file with blocked words has been created!");
                }


                Write("Initializing databases...");

                DataBase.Messages = new(Paths.MessagesDatabase);
                DataBase.Users = new(Paths.UsersDatabase);
                DataBase.Games = new(Paths.GamesDatabase);
                DataBase.Channels = new(Paths.ChannelsDatabase);

                Write("Initializing buffers...");

                MessagesBuffer = new(DataBase.Messages);
                UsersBuffer = new(DataBase.Users);

                Write("Loading currency counters...");

                Users = DataBase.Users.GetTotalUsers();
                Coins = DataBase.Users.GetTotalBalance();

                _repeater = Task.Run(() => StartRepeater());
                Write($"TPS counter successfully started");

                Write("Getting twitch token...");

                if (TwitchClientId == null)
                {
                    throw new TwitchDataNullException("Twitch client id is null.");
                }

                if (Tokens.TwitchSecretToken == null)
                {
                    throw new TwitchDataNullException("Twitch secret token is null.");
                }

                Tokens.TwitchGetter = new(TwitchClientId, Tokens.TwitchSecretToken, Paths.General + "TWITCH_AUTH.json");
                var token = await TwitchToken.GetTokenAsync();

                if (token != null)
                {
                    Tokens.Twitch = token;
                    await Connect();
                }
                else
                {
                    Write("Twitch token is null! Something went wrong...");
                    await Shutdown();
                }
            }
            catch (Exception ex)
            {
                Write(ex);
                await Shutdown();
            }
        }

        private void GitHubActionsStatusChanged(object? sender, RunStatusChangedEventArgs e)
        {
            if (e.Status == null || e.Status != "completed") return;
            string notify = $"forsenPls | Github: {e.Event} in {e.Repository}#{e.Branch} by {e.Actor}: {e.Conclusion}";

            Write(notify);

            foreach (string channel in Program.BotInstance.TwitchDevAnnounce)
            {
                bb.Program.BotInstance.MessageSender.Send(Platform.Twitch, notify, UsernameResolver.GetUsername(channel, Platform.Twitch, true), isSafe: true);
            }
        }

        /// <summary>
        /// Loads and applies bot configuration from persistent storage to runtime memory.
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        /// <item>Reads all configuration parameters from the settings file</item>
        /// <item>Maps stored values to corresponding bot properties and services</item>
        /// <item>Handles multiple data types including strings, arrays, and dictionaries</item>
        /// <item>Converts prefix character from string representation</item>
        /// <item>Initializes token managers with loaded credentials</item>
        /// </list>
        /// Critical initialization step that must complete successfully before platform connections.
        /// Throws exceptions on missing critical parameters (bot_name, tokens, etc.).
        /// Automatically decodes Unicode escape sequences for special characters.
        /// Maintains separation between sensitive credentials and public configuration.
        /// </remarks>
        public void LoadSettings()
        {
            LoadTokens();
            LoadOtherData();
            bb.Program.BotInstance.UsersSevenTVIDs = Manager.Get<Dictionary<string, string>>(Paths.SevenTVCache, "ids");
        }

        private void LoadTokens()
        {
            bb.Program.BotInstance.Tokens.Discord = Settings.Get<string>("discord_token");
            bb.Program.BotInstance.Tokens.TwitchSecretToken = Settings.Get<string>("twitch_secret_token");
            bb.Program.BotInstance.Tokens.Telegram = Settings.Get<string>("telegram_token");
            bb.Program.BotInstance.Tokens.SevenTV = Settings.Get<string>("seventv_token");
        }

        private void LoadOtherData()
        {
            bb.Program.BotInstance.TwitchName = Settings.Get<string>("bot_name");
            bb.Program.BotInstance.TwitchReconnectAnnounce = Settings.Get<List<string>>("twitch_reconnect_message_channels");
            bb.Program.BotInstance.TwitchConnectAnnounce = Settings.Get<List<string>>("twitch_connect_message_channels");
            bb.Program.BotInstance.TwitchClientId = Settings.Get<string>("twitch_client_id");
            bb.Program.BotInstance.TwitchNewVersionAnnounce = Settings.Get<List<string>>("twitch_version_message_channels");
            bb.Program.BotInstance.TwitchCurrencyRandomEvent = Settings.Get<List<string>>("twitch_currency_random_event");
            bb.Program.BotInstance.TwitchTaxesEvent = Settings.Get<List<string>>("twitch_taxes_event");
            bb.Program.BotInstance.TwitchDevAnnounce = Settings.Get<List<string>>("twitch_dev_channels");
            bb.Program.BotInstance.CurrencyMentioned = Settings.Get<int>("currency_mentioned_payment");
            bb.Program.BotInstance.CurrencyMentioner = Settings.Get<int>("currency_mentioner_payment");
            bb.Program.BotInstance.DefaultCommandPrefix = Settings.Get<string>("prefix");
            bb.Program.BotInstance.TaxesCost = Settings.Get<double>("taxes_cost");
        }
        #endregion Core

        #region Connects
        /// <summary>
        /// Establishes connections to all supported platforms.
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        /// <item>Twitch: Initializes client with 20+ event handlers</item>
        /// <item>Discord: Registers command services and event subscriptions</item>
        /// <item>Telegram: Starts message polling with error handling</item>
        /// <item>Loads 7TV emote cache from persistent storage</item>
        /// </list>
        /// Sets Ready and Connected flags upon successful initialization.
        /// Measures and logs total initialization time in milliseconds.
        /// Handles all connection exceptions through centralized error handling.
        /// </remarks>
        public async Task Connect()
        {
            try
            {
                Write("Connecting...");

                var tasks = new List<Task>
                {
                    Task.Run(ConnectToTwitch),
                    ConnectToDiscord(),
                    Task.Run(ConnectToTelegram),
                    Task.Run(EmoteCacheService.Load)
                };

                await Task.WhenAll(tasks);

                ConnectedIn = DateTime.UtcNow - _startTime;
                Initialized = true;

                Write($"Well done! ({(long)(ConnectedIn).TotalMilliseconds} ms)");
            }
            catch (Exception ex)
            {
                Write(ex);
                await Shutdown();
            }
        }

        /// <summary>
        /// Connects to Twitch API and configures event handling.
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        /// <item>Authenticates using OAuth token with required scopes</item>
        /// <item>Registers handlers for chat, subscription, and channel events</item>
        /// <item>Joins configured channels and bot's own channel</item>
        /// <item>Sends connection notification message with "truckCrash" emote</item>
        /// </list>
        /// Message rate limits: 750 messages per 30 seconds.
        /// Handles channel ID resolution through Names.GetUsername().
        /// Automatically reconnects on disconnection events.
        /// </remarks>
        /// <exception cref="TwitchDataNullException">Twitch token cannot be null or empty.</exception>
        /// <exception cref="TwitchDataNullException">Twitch nickname cannot be null or empty.</exception>
        private void ConnectToTwitch()
        {
            if (Tokens.Twitch == null)
            {
                throw new TwitchDataNullException("Twitch token cannot be null or empty.");
            }

            if (TwitchName == null)
            {
                throw new TwitchDataNullException("Twitch nickname cannot be null or empty.");
            }

            ConnectionCredentials credentials = new ConnectionCredentials(TwitchName, "oauth:" + Tokens.Twitch.AccessToken);
            ClientOptions client_options = new ClientOptions
            {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(30),
                ReconnectionPolicy = new ReconnectionPolicy(10)
            };
            WebSocketClient webSocket_client = new WebSocketClient(client_options);
            Clients.Twitch = new TwitchClient(webSocket_client);
            Clients.Twitch.Initialize(credentials, TwitchName);
            Clients.TwitchAPI = new TwitchAPI();
            Clients.TwitchAPI.Settings.AccessToken = Tokens.Twitch.AccessToken;
            Clients.TwitchAPI.Settings.ClientId = TwitchClientId;
            Clients.TwitchAPI.Settings.Scopes = [AuthScopes.Chat_Read, AuthScopes.Moderator_Read_Chatters, AuthScopes.Chat_Edit, AuthScopes.Moderator_Manage_Banned_Users];

            #region Events subscription
            Clients.Twitch.OnJoinedChannel += TwitchEvents.OnJoin;
            Clients.Twitch.OnMessageReceived += bb.Program.BotInstance.CommandExecutor.Twitch;
            Clients.Twitch.OnMessageReceived += TwitchEvents.OnMessageReceived;
            Clients.Twitch.OnMessageThrottled += TwitchEvents.OnMessageThrottled;
            Clients.Twitch.OnMessageSent += TwitchEvents.OnMessageSend;
            Clients.Twitch.OnAnnouncement += TwitchEvents.OnAnnounce;
            Clients.Twitch.OnBanned += TwitchEvents.OnBanned;
            Clients.Twitch.OnConnectionError += TwitchEvents.OnConnectionError;
            Clients.Twitch.OnContinuedGiftedSubscription += TwitchEvents.OnContinuedGiftedSubscription;
            Clients.Twitch.OnChatCleared += TwitchEvents.OnChatCleared;
            Clients.Twitch.OnDisconnected += TwitchEvents.OnTwitchDisconnected;
            Clients.Twitch.OnReconnected += TwitchEvents.OnReconnected;
            Clients.Twitch.OnError += TwitchEvents.OnError;
            Clients.Twitch.OnIncorrectLogin += TwitchEvents.OnIncorrectLogin;
            Clients.Twitch.OnLeftChannel += TwitchEvents.OnLeftChannel;
            Clients.Twitch.OnRaidNotification += TwitchEvents.OnRaidNotification;
            Clients.Twitch.OnNewSubscriber += TwitchEvents.OnNewSubscriber;
            Clients.Twitch.OnGiftedSubscription += TwitchEvents.OnGiftedSubscription;
            Clients.Twitch.OnCommunitySubscription += TwitchEvents.OnCommunitySubscription;
            Clients.Twitch.OnReSubscriber += TwitchEvents.OnReSubscriber;
            Clients.Twitch.OnSuspended += TwitchEvents.OnSuspended;
            Clients.Twitch.OnConnected += TwitchEvents.OnConnected;
            Clients.Twitch.OnLog += TwitchEvents.OnLog;
            Clients.Twitch.OnChatCleared += TwitchEvents.OnChatCleared;
            #endregion

            Clients.Twitch.Connect();

            JoinTwitchChannels();

            Clients.Twitch.SendMessage(TwitchName.ToLower(), "truckCrash Connecting to twitch...");

            Write("Twitch is ready.");
        }

        /// <summary>
        /// Connects to Twitch channels specified in the configuration.
        /// Resolves channel usernames to valid channel names and joins each channel.
        /// Logs connection attempts and reports any channels that couldn't be found.
        /// </summary>
        /// <remarks>
        /// The method first attempts to resolve all channel names using UsernameResolver.
        /// It then connects to each valid channel and logs warnings for any channels that couldn't be resolved.
        /// This is typically called during application initialization to establish connections to all configured Twitch channels.
        /// </remarks>
        /// <exception cref="TwitchClientNullException">Twitch client is null.</exception>
        public void JoinTwitchChannels()
        {
            if (Clients.Twitch == null)
            {
                throw new TwitchClientNullException();
            }

            var notFoundedChannels = new List<string>();

            foreach (var channel in bb.Program.BotInstance.Settings.Get<string[]>("twitch_connect_channels"))
            {
                var tempChannel = UsernameResolver.GetUsername(channel, Platform.Twitch, true);
                if (tempChannel != null) Clients.Twitch.JoinChannel(tempChannel);
                else notFoundedChannels.Add(channel);
            }

            if (notFoundedChannels.Count > 0)
            {
                Write("Twitch: Can't find ID for " + string.Join(',', notFoundedChannels), LogLevel.Warning);
            }
        }

        /// <summary>
        /// Initializes Discord connection with command registration.
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        /// <item>Configures client with required GatewayIntents</item>
        /// <item>Registers 10+ event handlers for messages, commands, and guilds</item>
        /// <item>Loads text and slash commands through CommandService</item>
        /// <item>Authenticates and establishes WebSocket connection</item>
        /// </list>
        /// Implements dependency injection through ServiceCollection.
        /// Maintains 1000-message cache for efficient command processing.
        /// Handles both guild and direct message contexts.
        /// </remarks>
        private async Task ConnectToDiscord()
        {
            var discordConfig = new DiscordSocketConfig
            {
                MessageCacheSize = 1000,
                GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.DirectMessages | GatewayIntents.MessageContent
            };
            Clients.Discord = new DiscordSocketClient(discordConfig);
            DiscordServiceProvider = new ServiceCollection()
                .AddSingleton(Clients.Discord)
                .AddSingleton(DiscordCommandService)
                .BuildServiceProvider();

            Clients.Discord.Log += DiscordEvents.LogAsync;
            Clients.Discord.JoinedGuild += DiscordEvents.ConnectToGuilt;
            Clients.Discord.Ready += DiscordWorker.ReadyAsync;
            Clients.Discord.MessageReceived += DiscordWorker.MessageReceivedAsync;
            Clients.Discord.SlashCommandExecuted += DiscordEvents.SlashCommandHandler;
            Clients.Discord.ApplicationCommandCreated += DiscordEvents.ApplicationCommandCreated;
            Clients.Discord.ApplicationCommandDeleted += DiscordEvents.ApplicationCommandDeleted;
            Clients.Discord.ApplicationCommandUpdated += DiscordEvents.ApplicationCommandUpdated;
            Clients.Discord.ChannelCreated += DiscordEvents.ChannelCreated;
            Clients.Discord.ChannelDestroyed += DiscordEvents.ChannelDeleted;
            Clients.Discord.ChannelUpdated += DiscordEvents.ChannelUpdated;
            Clients.Discord.Connected += DiscordEvents.Connected;
            Clients.Discord.ButtonExecuted += DiscordEvents.ButtonTouched;

            await DiscordWorker.RegisterCommandsAsync();
            await Clients.Discord.LoginAsync(TokenType.Bot, Tokens.Discord);
            await Clients.Discord.StartAsync();

            Write("Discord is ready.");
        }

        /// <summary>
        /// Starts message reception from Telegram platform.
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        /// <item>Initializes client with bot token authentication</item>
        /// <item>Configures update options (text messages only)</item>
        /// <item>Starts asynchronous polling with error handling</item>
        /// <item>Uses CancellationToken for graceful shutdown</item>
        /// </list>
        /// Drops pending updates on startup (DropPendingUpdates=true).
        /// Processes only Message-type updates (UpdateType.Message).
        /// Integrates with TelegramEvents error handling system.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Telegram token is null.</exception>
        private void ConnectToTelegram()
        {
            if (Tokens.Telegram == null)
            {
                throw new TelegramTokenNullException();
            }

            Clients.Telegram = new TelegramBotClient(Tokens.Telegram);
            TelegramReceiverOptions = new ReceiverOptions
            {
                AllowedUpdates = new[] { UpdateType.Message },
                DropPendingUpdates = true,
            };

            Clients.Telegram.StartReceiving(TelegramEvents.UpdateHandler, TelegramEvents.ErrorHandler, TelegramReceiverOptions, Clients.TelegramCancellationToken.Token);
            Write("Telegram is ready.");
        }
        #endregion Connects

        #region Other
        /// <summary>
        /// Refreshes the Twitch authentication token and reestablishes connection to the Twitch platform.
        /// This method handles the complete process of disconnecting, updating credentials, and reconnecting.
        /// </summary>
        /// <remarks>
        /// The method first disconnects from Twitch if currently connected, then sets new connection credentials
        /// using the updated access token. It then attempts to reconnect and join all configured channels.
        /// Upon successful refresh, it sends notification messages to indicate the token was updated.
        /// If any errors occur during the process, they are logged but not rethrown to prevent application crashes.
        /// This is essential for maintaining uninterrupted connection to Twitch when access tokens expire.
        /// </remarks>
        /// <exception cref="TwitchClientNullException"></exception>
        /// <exception cref="TwitchDataNullException"></exception>
        public async Task RefreshTwitchTokenAsync()
        {
            try
            {
                if (Clients.Twitch == null)
                {
                    throw new TwitchClientNullException();
                }

                if (Tokens.Twitch == null)
                {
                    throw new TwitchDataNullException("Twitch token cannot be null or empty.");
                }

                if (TwitchName == null)
                {
                    throw new TwitchDataNullException("Twitch nickname cannot be null or empty.");
                }

                if (Clients.Twitch.IsConnected)
                {
                    Clients.Twitch.Disconnect();
                    await Task.Delay(500);
                }

                Clients.Twitch.SetConnectionCredentials(
                    new ConnectionCredentials(TwitchName, Tokens.Twitch.AccessToken)
                );

                try
                {
                    Clients.Twitch.Connect();
                    JoinTwitchChannels();
                    Write("The token has been updated and the connection has been restored");
                    bb.Program.BotInstance.MessageSender.Send(Platform.Twitch, $"sillyCatThinks Token refreshed", TwitchName, isSafe: true);
                }
                catch (Exception ex)
                {
                    Write("Twitch connection error!");
                    Write(ex);
                }
            }
            catch (Exception ex)
            {
                Write(ex);
            }
        }

        /// <summary>
        /// Executes a graceful shutdown sequence with platform-specific notifications and resource cleanup.
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        /// <item>Clears initialization flags (<c>Initialized</c>, <c>Connected</c>) immediately</item>
        /// <item>Disposes all buffer systems with null-safety checks</item>
        /// <item>Properly cancels and disposes Telegram polling operations</item>
        /// <item>Gracefully disconnects Discord client with cleanup</item>
        /// <item>Logs each stage of the shutdown sequence for diagnostics</item>
        /// <item>Ensures all pending messages are processed before termination</item>
        /// </list>
        /// <para>
        /// This method follows a structured shutdown sequence:
        /// <list type="number">
        /// <item>Flag reset and resource disposal (immediate)</item>
        /// <item>Buffered data flush with timeout handling</item>
        /// <item>Network connection termination</item>
        /// <item>Final process termination with diagnostic exit code</item>
        /// </list>
        /// </para>
        /// <para>
        /// Critical considerations:
        /// <list type="bullet">
        /// <item>Uses proper async/await pattern without blocking calls</item>
        /// <item>Implements comprehensive error handling for each resource</item>
        /// <item>Ensures all disposables are properly released</item>
        /// <item>Waits for critical operations to complete before exit</item>
        /// </list>
        /// </para>
        /// </remarks>
        /// <returns>Task representing the asynchronous shutdown operation</returns>
        public async Task Shutdown(bool force = false, bool update = false)
        {
            Write("Initiating shutdown sequence...");

            Initialized = false;

            Write($"Shutdown process started (PID: {Environment.ProcessId})", LogLevel.Info);

            try
            {
                if (UsersBuffer != null)
                {
                    try
                    {
                        Write("Flushing user data buffer...");
                        UsersBuffer.Flush();
                        Write("User buffer disposed successfully", LogLevel.Info);
                    }
                    catch (Exception ex)
                    {
                        Write($"User buffer flush failed: {ex.Message}", LogLevel.Warning);
                    }
                    finally
                    {
                        UsersBuffer.Dispose();
                        UsersBuffer = null;
                    }
                }

                if (MessagesBuffer != null)
                {
                    try
                    {
                        Write("Flushing message data buffer...");
                        MessagesBuffer.Flush();
                        Write("Message buffer disposed successfully", LogLevel.Info);
                    }
                    catch (Exception ex)
                    {
                        Write($"Message buffer flush failed: {ex.Message}", LogLevel.Warning);
                    }
                    finally
                    {
                        MessagesBuffer.Dispose();
                        MessagesBuffer = null;
                    }
                }

                if (Clients?.TelegramCancellationToken != null)
                {
                    try
                    {
                        Write("Cancelling Telegram operations...");
                        Clients.TelegramCancellationToken.Cancel();
                        Write("Telegram cancellation requested", LogLevel.Info);
                    }
                    catch (Exception ex)
                    {
                        Write($"Telegram cancellation failed: {ex.Message}", LogLevel.Warning);
                    }
                    finally
                    {
                        Clients.TelegramCancellationToken.Dispose();
                        Clients.TelegramCancellationToken = null;
                    }
                }

                if (Clients?.Discord != null)
                {
                    try
                    {
                        Write("Disconnecting from Discord...");
                        await Clients.Discord.LogoutAsync();
                        await Clients.Discord.StopAsync();
                        Write("Discord client disconnected", LogLevel.Info);
                    }
                    catch (Exception ex)
                    {
                        Write($"Discord disconnect failed: {ex.Message}", LogLevel.Warning);
                    }
                    finally
                    {
                        Clients.Discord.Dispose();
                        Clients.Discord = null;
                    }
                }

                if (DataBase != null)
                {
                    Write("Disposing SQL...");
                    try
                    {
                        DataBase.Channels.Dispose();
                        DataBase.Users.Dispose();
                        DataBase.Games.Dispose();
                        DataBase.Messages.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Write($"SQL dispose failed: {ex.Message}", LogLevel.Warning);
                    }
                }

                if (GithubCT != null)
                {
                    GithubCT.Dispose();
                }

                try
                {
                    Dictionary<string, dynamic> currencyData = new()
                            {
                                    { "amount", Coins },
                                    { "users", Users },
                                    { "dollars", InBankDollars },
                                    { "cost", InBankDollars / Coins },
                                    { "middleBalance", Coins / Users }
                            };

                    if (Paths.Currency is not null)
                    {
                        Manager.Save(Paths.Currency, "total", currencyData);
                        Manager.Save(Paths.Currency, $"{DateTime.UtcNow.Day}.{DateTime.UtcNow.Month}.{DateTime.UtcNow.Year}", currencyData);
                    }
                }
                catch (Exception ex)
                {
                    Write($"Currency dispose failed: {ex.Message}", LogLevel.Warning);
                }

                Write("Waiting for pending operations to complete...");
                await Task.Delay(2000);
            }
            catch (Exception ex)
            {
                Write($"Critical error during shutdown sequence: {ex}", LogLevel.Error);
            }
            finally
            {
                Write("Restart sequence completed - terminating process");
                Environment.Exit(update ? 5051 : force ? 5001 : 0);
            }
        }

        private void UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = (Exception)e.ExceptionObject;
            try
            {
                Write("Critical error.", LogLevel.Error);
                Write(ex);
                Shutdown().RunSynchronously();
            }
            catch { }
            finally { Environment.Exit(ex.HResult); }
        }

        public async Task RefreshTwitchToken()
        {
            var newToken = await TwitchToken.RefreshAccessToken(Tokens.Twitch);
            if (newToken != null)
            {
                Tokens.Twitch = newToken;
            }
        }
        #endregion Other
    }
}