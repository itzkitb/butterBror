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
using static bb.Core.Bot.Console;
using bb.Data.Repositories;
using bb.Data.Entities;
using bb.Services.Platform.Discord;
using bb.Services.Platform.Twitch;
using bb.Services.Platform.Telegram;
using bb.Models.Platform;
using bb.Core.Commands;


// Task: Add a DI container for all this crap

namespace bb
{
    /// <summary>
    /// Central class.
    /// </summary>
    public class BotInstance
    {
        #region Variables

        private readonly ClientService _clients;
        private readonly Tokens _tokens;
        private readonly SQLService _dataBase;
        private readonly EmoteCacheService _emoteCacheService;
        private readonly SettingsService _settingsService;
        private readonly SevenTvService _sevenTvService;
        private readonly AIService _aiService;
        private readonly ImgurService _imgurService;
        private readonly YouTubeService _youTubeService;
        private readonly CooldownManager _cooldownManager;
        private readonly CurrencyManager _currencyManager;
        private readonly MessageProcessor _messageProcessor;
        private readonly PlatformMessageSender _platformMessageSender;
        private readonly CommandService _commandService;
        private readonly Executor _commandExecutor;
        private readonly Runner _commandRunner;
        private readonly Sender _commandSender;
        private readonly PathService _paths;

        public ClientService Clients => _clients;
        public PathService Paths => _paths;
        public SQLService DataBase => _dataBase;

        #region Core
        public Version Version = new Version("2.18.0.9");
        public string Branch = Environment.GetEnvironmentVariable("BRANCH") ?? "master";
        public string Commit = Environment.GetEnvironmentVariable("COMMIT") ?? "";
        public DateTime StartTime = new();
        public string PreviousVersion = "";
        public bool Initialized = false;
        public bool Connected => _clients.Twitch?.IsConnected == true && _clients.Discord?.ConnectionState == Discord.ConnectionState.Connected;
        public string? TwitchName;
        public string DefaultCommandPrefix = "!";
        private bool SkipFetch = false;
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
        public List<(PlatformsEnum platform, string channelId, long userId, Message message)> allFirstMessages = new();
        #endregion

        #region Twitch
        public string? TwitchClientId;
        public string[] TwitchNewVersionAnnounce = [];
        public string[] TwitchReconnectAnnounce = [];
        public string[] TwitchConnectAnnounce = [];
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
        ImgurService imgurService,
        YouTubeService youTubeService,
        CooldownManager cooldownManager,
        CurrencyManager currencyManager,
        MessageProcessor messageProcessor,
        PlatformMessageSender platformMessageSender,
        CommandService commandService,
        Executor commandExecutor,
        Runner commandRunner,
        Sender commandSender
    )
        {
            _clients = clients;
            _paths = paths;
            _tokens = tokens;
            _dataBase = dataBase;
            _emoteCacheService = emoteCacheService;
            _settingsService = settingsService;
            _sevenTvService = sevenTvService;
            _aiService = aiService;
            _imgurService = imgurService;
            _youTubeService = youTubeService;
            _cooldownManager = cooldownManager;
            _currencyManager = currencyManager;
            _messageProcessor = messageProcessor;
            _platformMessageSender = platformMessageSender;
            _commandService = commandService;
            _commandExecutor = commandExecutor;
            _commandRunner = commandRunner;
            _commandSender = commandSender;
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
        public void Start(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += UnhandledException;

            Initialize(args);

            if (SkipFetch)
            {
                Write("Launched with --skip-fetch parameter, skipping system fetch...");
            }
            else
            {
                SystemDataFetch();
            }

            Setup();
            StartBot();

            Task.Run(async () =>
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

            System.Console.ReadLine();
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
                            _paths.UpdatePaths();
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
                            _ = Task.Run(() =>
                            {
                                _ = Backup.BackupDataAsync();
                                _ = CurrencyManager.GenerateRandomEventAsync();
                                _ = CurrencyManager.CollectTaxesAsync();

                                if (_dataBase == null)
                                {
                                    Write("Reinitialization of currency counters failed: Database is null", LogLevel.Error);
                                    return;
                                }

                                Write("Reinitializing currency counters...");
                                Users = _dataBase.Users.GetTotalUsers();
                                Coins = _dataBase.Users.GetTotalBalance();
                            });
                        }

                        //Write($"({now.Second == 0} && {(now - _lastSave).TotalSeconds > 2}); ({now.Second}; {(now - _lastSave).TotalSeconds}; {now}; {_lastSave})", "debug");
                        if (now.Second == 0 && (now - _lastSave).TotalSeconds > 2)
                        {
                            if (MessagesBuffer == null || UsersBuffer == null || _dataBase == null)
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
                                    _dataBase.Channels.SaveFirstMessages(allFirstMessages);
                                    allFirstMessages.Clear();
                                }
                                #endregion Buffer save 
                                #region Currency save
                                Dictionary<string, dynamic> currencyData = new()
                            {
                                    { "amount", Coins },
                                    { "users", Users },
                                    { "dollars", InBankDollars },
                                    { "cost", InBankDollars / Coins },
                                    { "middleBalance", Coins / Users }
                            };

                                if (_paths.Currency is not null)
                                {
                                    Manager.Save(_paths.Currency, "total", currencyData);
                                    Manager.Save(_paths.Currency, $"{now.Day}.{now.Month}.{now.Year}", currencyData);
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
  :::::::::::::::::::::::::::::        ButterBror
 ::::::::::::::::::::::::::::::::      Host: {HostName} v.{HostVersion}
 {{~:::::::::::::::::::::::::::::::     Framework: {RuntimeInformation.FrameworkDescription.Pastel("#ff7b42")}
 0000XI::::::::::::::::::::::tC00:     v.{Version.ToString().Pastel("#ff7b42")}
 ::c0000nI::::::::::::::::(v1::<l      {OSName.Pastel("#ff7b42")}
 ((((:n0000f-::::::::}}x00(::n000(:     {processor.Pastel("#ff7b42")}
 n0((::::c0000f(:::>}}X(l!00QQ0((::     RAM: {memory.ToString().Pastel("#ff7b42")} GB
  :():::::::C000000000000:::::+l:      Total disks space: {Math.Round(Memory.BytesToGB(totalDiskSpace)).ToString().Pastel("#ff7b42")} GB
     Ix:(((((((:-}}-:((:::100_:         Available disks space: {Math.Round(Memory.BytesToGB(totalFreeDiskSpace)).ToString().Pastel("#ff7b42")} GB
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
            System.Console.Clear();

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
        /// Configures core infrastructure before bot startup.
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        /// <item>Establishes working directories in ApplicationData</item>
        /// <item>Launches background timer task (StartRepeater)</item>
        /// <item>Records application start timestamp</item>
        /// <item>Initializes message and user data buffers</item>
        /// </list>
        /// Executes once during application startup before platform connections.
        /// Configures path structure for all persistent data storage.
        /// </remarks>
        private void Setup()
        {
            _paths.General = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}/ItzKITb/";
            _paths.Main = _paths.General + "butterBror/";

            _repeater = Task.Run(() => StartRepeater());
            Write($"TPS counter successfully started.");

            StartTime = DateTime.Now;
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

            if (FileUtil.FileExists(_paths.Currency))
            {
                var data = Manager.Get<Dictionary<string, dynamic>>(_paths.Currency, "total");
                InBankDollars = data.ContainsKey("dollars") ? data["dollars"].GetInt32() : 0;
            }

            try
            {
                Write("Creating directories...");
                string[] directories = { _paths.General, _paths.Main, _paths.TranslateDefault, _paths.TranslateCustom };

                foreach (var dir in directories)
                {
                    FileUtil.CreateDirectory(dir);
                }

                string[] directories_with_platforms = { _paths.TranslateDefault, _paths.TranslateCustom };

                if (!FileUtil.FileExists(_paths.Settings))
                {
                    SettingsService.InitializeFile(_paths.Settings);
                    Write($"The settings file has been created! ({_paths.Settings})");
                    Thread.Sleep(-1);
                }

                string[] files = {
                            _paths.BlacklistWords, _paths.BlacklistReplacements,
                            _paths.Currency, _paths.Cache, _paths.Logs, _paths.APIUses,
                            Path.Combine(_paths.TranslateDefault, "ru-RU.json"),
                            Path.Combine(_paths.TranslateDefault, "en-US.json"), Path.Combine(_paths.Main, "VERSION.txt")
                    };

                Write("Creating files...");
                foreach (var file in files)
                {
                    FileUtil.CreateFile(file);
                }

                PreviousVersion = File.ReadAllText(Path.Combine(_paths.Main, "VERSION.txt"));
                File.WriteAllText(Path.Combine(_paths.Main, "VERSION.txt"), $"{Version}");

                Write("Loading settings...");
                SettingsService.Load();

                Write("Initializing databases...");
                /*_dataBase = new()
                {
                    Messages = new(Paths.MessagesDatabase),
                    Users = new(Paths.UsersDatabase),
                    Games = new(Paths.GamesDatabase),
                    Channels = new(Paths.ChannelsDatabase),
                    Roles = new(Paths.RolesDatabase)
                };*/
                MessagesBuffer = new(_dataBase.Messages);
                UsersBuffer = new(_dataBase.Users);

                Write("Loading currency counters...");
                Users = _dataBase.Users.GetTotalUsers();
                Coins = _dataBase.Users.GetTotalBalance();

                Write("Getting twitch token...");

                if (TwitchClientId == null)
                {
                    throw new TwitchDataNullException("Twitch client id is null.");
                }

                if (_tokens.TwitchSecretToken == null)
                {
                    throw new TwitchDataNullException("Twitch secret token is null.");
                }

                _tokens.TwitchGetter = new(TwitchClientId, _tokens.TwitchSecretToken, _paths.Main + "TWITCH_AUTH.json");
                var token = await TwitchToken.GetTokenAsync();

                if (token != null)
                {
                    _tokens.Twitch = token;
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

                TimeSpan ConnectedIn = DateTime.UtcNow - _startTime;
                Initialized = true;

                _ = Task.Run(async () =>
                {
                    await Task.Delay(1000);

                    if (PreviousVersion != Version.ToString() && PreviousVersion != string.Empty)
                    {
                        foreach (string channel in TwitchNewVersionAnnounce)
                        {
                            PlatformMessageSender.TwitchSend(UsernameResolver.GetUsername(channel, PlatformsEnum.Twitch, true), $"{TwitchName} v.{PreviousVersion} > v.{Version}", channel, "", "en-US", true);
                        }
                    }

                    foreach (string channel in TwitchConnectAnnounce)
                    {
                        PlatformMessageSender.TwitchSend(UsernameResolver.GetUsername(channel, PlatformsEnum.Twitch, true), $"{TwitchName} Started in {(long)(ConnectedIn).TotalMilliseconds} ms!", channel, "", "en-US", true);
                    }
                });

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
            if (_tokens.Twitch == null)
            {
                throw new TwitchDataNullException("Twitch token cannot be null or empty.");
            }

            if (TwitchName == null)
            {
                throw new TwitchDataNullException("Twitch nickname cannot be null or empty.");
            }

            ConnectionCredentials credentials = new ConnectionCredentials(TwitchName, "oauth:" + _tokens.Twitch.AccessToken);
            ClientOptions client_options = new ClientOptions
            {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(30),
                ReconnectionPolicy = new ReconnectionPolicy(10)
            };
            WebSocketClient webSocket_client = new WebSocketClient(client_options);
            _clients.Twitch = new TwitchClient(webSocket_client);
            _clients.Twitch.Initialize(credentials, TwitchName);
            _clients.TwitchAPI = new TwitchAPI();
            _clients.TwitchAPI.Settings.AccessToken = _tokens.Twitch.AccessToken;
            _clients.TwitchAPI.Settings.ClientId = TwitchClientId;
            _clients.TwitchAPI.Settings.Scopes = [AuthScopes.Chat_Read, AuthScopes.Moderator_Read_Chatters, AuthScopes.Chat_Edit, AuthScopes.Moderator_Manage_Banned_Users];

            #region Events subscription
            _clients.Twitch.OnJoinedChannel += TwitchEvents.OnJoin;
            _clients.Twitch.OnMessageReceived += Core.Commands.Executor.Twitch;
            _clients.Twitch.OnMessageReceived += TwitchEvents.OnMessageReceived;
            _clients.Twitch.OnMessageThrottled += TwitchEvents.OnMessageThrottled;
            _clients.Twitch.OnMessageSent += TwitchEvents.OnMessageSend;
            _clients.Twitch.OnAnnouncement += TwitchEvents.OnAnnounce;
            _clients.Twitch.OnBanned += TwitchEvents.OnBanned;
            _clients.Twitch.OnConnectionError += TwitchEvents.OnConnectionError;
            _clients.Twitch.OnContinuedGiftedSubscription += TwitchEvents.OnContinuedGiftedSubscription;
            _clients.Twitch.OnChatCleared += TwitchEvents.OnChatCleared;
            _clients.Twitch.OnDisconnected += TwitchEvents.OnTwitchDisconnected;
            _clients.Twitch.OnReconnected += TwitchEvents.OnReconnected;
            _clients.Twitch.OnError += TwitchEvents.OnError;
            _clients.Twitch.OnIncorrectLogin += TwitchEvents.OnIncorrectLogin;
            _clients.Twitch.OnLeftChannel += TwitchEvents.OnLeftChannel;
            _clients.Twitch.OnRaidNotification += TwitchEvents.OnRaidNotification;
            _clients.Twitch.OnNewSubscriber += TwitchEvents.OnNewSubscriber;
            _clients.Twitch.OnGiftedSubscription += TwitchEvents.OnGiftedSubscription;
            _clients.Twitch.OnCommunitySubscription += TwitchEvents.OnCommunitySubscription;
            _clients.Twitch.OnReSubscriber += TwitchEvents.OnReSubscriber;
            _clients.Twitch.OnSuspended += TwitchEvents.OnSuspended;
            _clients.Twitch.OnConnected += TwitchEvents.OnConnected;
            _clients.Twitch.OnLog += TwitchEvents.OnLog;
            _clients.Twitch.OnChatCleared += TwitchEvents.OnChatCleared;
            #endregion

            _clients.Twitch.Connect();

            JoinTwitchChannels();

            _clients.Twitch.SendMessage(TwitchName.ToLower(), "truckCrash Connecting to twitch...");

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
            if (_clients.Twitch == null)
            {
                throw new TwitchClientNullException();
            }

            var notFoundedChannels = new List<string>();

            foreach (var channel in Manager.Get<string[]>(_paths.Settings, "twitch_connect_channels"))
            {
                var tempChannel = UsernameResolver.GetUsername(channel, PlatformsEnum.Twitch, true);
                if (tempChannel != null) _clients.Twitch.JoinChannel(tempChannel);
                else notFoundedChannels.Add(channel);
            }

            if (notFoundedChannels.Count > 0)
            {
                Write("Twitch - Can't find ID for " + string.Join(',', notFoundedChannels), LogLevel.Warning);
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
            _clients.Discord = new DiscordSocketClient(discordConfig);
            DiscordServiceProvider = new ServiceCollection()
                .AddSingleton(_clients.Discord)
                .AddSingleton(_commandService)
                .BuildServiceProvider();

            _clients.Discord.Log += DiscordEvents.LogAsync;
            _clients.Discord.JoinedGuild += DiscordEvents.ConnectToGuilt;
            _clients.Discord.Ready += DiscordWorker.ReadyAsync;
            _clients.Discord.MessageReceived += DiscordWorker.MessageReceivedAsync;
            _clients.Discord.SlashCommandExecuted += DiscordEvents.SlashCommandHandler;
            _clients.Discord.ApplicationCommandCreated += DiscordEvents.ApplicationCommandCreated;
            _clients.Discord.ApplicationCommandDeleted += DiscordEvents.ApplicationCommandDeleted;
            _clients.Discord.ApplicationCommandUpdated += DiscordEvents.ApplicationCommandUpdated;
            _clients.Discord.ChannelCreated += DiscordEvents.ChannelCreated;
            _clients.Discord.ChannelDestroyed += DiscordEvents.ChannelDeleted;
            _clients.Discord.ChannelUpdated += DiscordEvents.ChannelUpdated;
            _clients.Discord.Connected += DiscordEvents.Connected;
            _clients.Discord.ButtonExecuted += DiscordEvents.ButtonTouched;

            await DiscordWorker.RegisterCommandsAsync();
            await _clients.Discord.LoginAsync(TokenType.Bot, _tokens.Discord);
            await _clients.Discord.StartAsync();

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
            if (_tokens.Telegram == null)
            {
                throw new TelegramTokenNullException();
            }

            _clients.Telegram = new TelegramBotClient(_tokens.Telegram);
            TelegramReceiverOptions = new ReceiverOptions
            {
                AllowedUpdates = new[] { UpdateType.Message },
                DropPendingUpdates = true,
            };

            _clients.Telegram.StartReceiving(TelegramEvents.UpdateHandler, TelegramEvents.ErrorHandler, TelegramReceiverOptions, _clients.TelegramCancellationToken.Token);
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
                if (_clients.Twitch == null)
                {
                    throw new TwitchClientNullException();
                }

                if (_tokens.Twitch == null)
                {
                    throw new TwitchDataNullException("Twitch token cannot be null or empty.");
                }

                if (TwitchName == null)
                {
                    throw new TwitchDataNullException("Twitch nickname cannot be null or empty.");
                }

                if (_clients.Twitch.IsConnected)
                {
                    _clients.Twitch.Disconnect();
                    await Task.Delay(500);
                }

                _clients.Twitch.SetConnectionCredentials(
                    new ConnectionCredentials(TwitchName, _tokens.Twitch.AccessToken)
                );

                try
                {
                    _clients.Twitch.Connect();
                    JoinTwitchChannels();
                    Write("The token has been updated and the connection has been restored");
                    PlatformMessageSender.TwitchSend(TwitchName, $"sillyCatThinks Token refreshed", "", "", "en-US", true);
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
        public async Task Shutdown(bool force = false)
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

                if (_clients?.TelegramCancellationToken != null)
                {
                    try
                    {
                        Write("Cancelling Telegram operations...");
                        _clients.TelegramCancellationToken.Cancel();
                        Write("Telegram cancellation requested", LogLevel.Info);
                    }
                    catch (Exception ex)
                    {
                        Write($"Telegram cancellation failed: {ex.Message}", LogLevel.Warning);
                    }
                    finally
                    {
                        _clients.TelegramCancellationToken.Dispose();
                        _clients.TelegramCancellationToken = null;
                    }
                }

                if (_clients?.Discord != null)
                {
                    try
                    {
                        Write("Disconnecting from Discord...");
                        await _clients.Discord.LogoutAsync();
                        await _clients.Discord.StopAsync();
                        Write("Discord client disconnected", LogLevel.Info);
                    }
                    catch (Exception ex)
                    {
                        Write($"Discord disconnect failed: {ex.Message}", LogLevel.Warning);
                    }
                    finally
                    {
                        _clients.Discord.Dispose();
                        _clients.Discord = null;
                    }
                }

                if (_dataBase != null)
                {
                    Write("Disposing SQL...");
                    try
                    {
                        _dataBase.Channels.Dispose();
                        _dataBase.Users.Dispose();
                        _dataBase.Games.Dispose();
                        _dataBase.Messages.Dispose();
                        _dataBase.Roles.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Write($"SQL dispose failed: {ex.Message}", LogLevel.Warning);
                    }
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

                    if (_paths.Currency is not null)
                    {
                        Manager.Save(_paths.Currency, "total", currencyData);
                        Manager.Save(_paths.Currency, $"{DateTime.UtcNow.Day}.{DateTime.UtcNow.Month}.{DateTime.UtcNow.Year}", currencyData);
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
                Environment.Exit(force ? 5001 : 0);
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
            var newToken = await TwitchToken.RefreshAccessToken(_tokens.Twitch);
            if (newToken != null)
            {
                _tokens.Twitch = newToken;
            }
        }
        #endregion Other
    }
}