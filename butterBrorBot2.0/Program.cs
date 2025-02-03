using butterBib;
using butterBror.Utils;
using butterBror.Utils.DataManagers;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Reflection;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;
using static System.Net.Mime.MediaTypeNames;

namespace butterBror
{
    public class BotEngine
    {
        public static int restartedTimes = 0;
        public static int completedCommands = 0;
        public static int users = 0;
        public static bool isRestarting = false;
        public static bool isTwitchReady = false;
        public static float buttersAmount = 0;
        public static string botVersion = "2.10.1";
        public static string patchID = "J";
        public static int buttersDollars = 0;
        public static DateTime botStartTime = new();
        public static DataManager currencyWorker = new();
        private static float previousButters = 0;
        public static int ticks = 20;
        public static long tickCounter = 0;
        public static double tickDelay = 0;
        private static bool tickEnded = true;
        public static long tiksSkipped = 0;
        private static long oldTickCounterStatus = 0;
        public static long tps = 0;

        private static Timer ticksTimer;
        private static Timer secondTimer;

        public static async void Start(string? mainPath = null, int customTickSpeed = 20)
        {
            ConsoleUtil.LOG($"[butterBror] Hiii!!!", "main");
            ConsoleUtil.LOG($"Engine starting...", "kernel");
            ticks = customTickSpeed;

            if (customTickSpeed > 1000)
                throw new Exception("Ticks cannot exceed 1000 per second!");

            if (customTickSpeed < 1)
                throw new Exception("Ticks cannot be less than 1 per second!");

            int ticks_time = 1000 / ticks;

            if (mainPath != null)
            {
                Bot.ProgramPath = mainPath;
                Bot.MainPath = Bot.ProgramPath + "butterBror/";
                Bot.ChannelsPath = Bot.MainPath + "CHNLS/";
                Bot.UsersDataPath = Bot.MainPath + "USERSDB/";
                Bot.UsersBankDataPath = Bot.MainPath + "BANKDB/";
                Bot.ConvertorsPath = Bot.MainPath + "CONVRT/";
                Bot.NicknameToIDPath = Bot.ConvertorsPath + "N2I/";
                Bot.IDToNicknamePath = Bot.ConvertorsPath + "I2N/";
                Bot.SettingsPath = Bot.MainPath + "SETTINGS.json";
                Bot.CookiesPath = Bot.MainPath + "COOKIES.MDS";
                Bot.TranslatePath = Bot.MainPath + "TRNSLT/";
                Bot.TranslateDefualtPath = Bot.MainPath + "TRNSLT/DEFAULT/";
                Bot.TranslateCustomPath = Bot.MainPath + "TRNSLT/CUSTOM/";
                Bot.BanWordsPath = Bot.MainPath + "BNWORDS.txt";
                Bot.BanWordsReplacementPath = Bot.MainPath + "BNWORDSREP.txt";
                Bot.APIUseDataPath = Bot.MainPath + "API.json";
                Bot.LogsPath = Bot.MainPath + "LOGS.log";
                Bot.ErrorsPath = Bot.MainPath + "ERRORS.log";
                Bot.LocationsCachePath = Bot.MainPath + "LOC.cache";
                Bot.CurrencyPath = Bot.MainPath + "CURR.json";
                Bot.ReserveCopyPath = Bot.ProgramPath + "bbRESERVE/" + DateTime.UtcNow.Year + DateTime.UtcNow.Month + DateTime.UtcNow.Day + "/";
                ConsoleUtil.LOG($"Main path: {Bot.ChannelsPath}", "kernel");
                ConsoleUtil.LOG($"Pathes setted!", "kernel");
            }

            ticksTimer = new(OnTick, null, 0, ticks_time);
            secondTimer = new((object? timer) =>
            {
                tps = tickCounter - oldTickCounterStatus;
                oldTickCounterStatus = tickCounter;
            }, null, 0, 1000);
            ConsoleUtil.LOG($"Timer started!", "kernel");

            try
            {
                botStartTime = DateTime.Now;
                Task task = Task.Run(Restart);

                while (true)
                {
                    if (isRestarting)
                    {
                        restartedTimes++;
                        isRestarting = false;
                        ConsoleUtil.LOG("Restarting...", "kernel");
                        try
                        {
                            task.Dispose();
                        }
                        catch (Exception e)
                        {
                            ConsoleUtil.LOG($"Thread task dispose error: {e.Message}", "kernel");
                        }
                        task = Task.Run(Restart);
                    }
                }
            }
            catch (Exception e)
            {
                string ErrorText = $"{e.Message} : {e.StackTrace}";
                LogWorker.Log(ErrorText, LogWorker.LogTypes.Err, "BotEngine/Main");
                ConsoleUtil.LOG(ErrorText, "err", ConsoleColor.Black, ConsoleColor.Red);
                ConsoleUtil.LOG("Restarting...", "kernel");
                Start(mainPath, customTickSpeed);
            }
        }
        public static void OnTick(object? timer)
        {
            if (!tickEnded)
            {
                tiksSkipped++;
                return;
            }

            Task.Run(() =>
            {
                try
                {
                    tickEnded = false;
                    DateTime startTime = DateTime.Now;
                    tickCounter++;
                    if ((int)tickCounter % ticks == 0)
                    {
                        var workTime = DateTime.Now - botStartTime;
                        long workingAppSet = Process.GetCurrentProcess().WorkingSet64 / (1024 * 1024);
                        ConsoleUtil.LOG($"{workTime.Days}d {workTime.Hours}h {workTime.Minutes}m, {workingAppSet} мб, 🥪 {buttersAmount}, 👥 {users}, tps: {tps}/{ticks} ({tickDelay} ms)", "status");

                        if (!isRestarting && buttersAmount != 0 && users != 0 && previousButters != buttersAmount)
                        {
                            var date = DateTime.UtcNow;
                            Dictionary<string, dynamic> currencyData = new()
                            {
                                    { "amount", buttersAmount },
                                    { "users", users },
                                    { "dollars", buttersDollars },
                                    { "cost", buttersDollars / buttersAmount },
                                    { "middleBalance", buttersAmount / users }
                            };

                            DataManager.SaveData(Bot.CurrencyPath, "totalAmount", buttersAmount, false);
                            DataManager.SaveData(Bot.CurrencyPath, "totalUsers", users, false);
                            DataManager.SaveData(Bot.CurrencyPath, "totalDollarsInTheBank", buttersDollars, false);

                            DataManager.SaveData(Bot.CurrencyPath, $"[{date.Day}.{date.Month}.{date.Year}]", "", false);
                            DataManager.SaveData(Bot.CurrencyPath, $"[{date.Day}.{date.Month}.{date.Year}] cost", float.Parse((buttersDollars / buttersAmount).ToString("0.00")), false);
                            DataManager.SaveData(Bot.CurrencyPath, $"[{date.Day}.{date.Month}.{date.Year}] amount", buttersAmount, false);
                            DataManager.SaveData(Bot.CurrencyPath, $"[{date.Day}.{date.Month}.{date.Year}] users", users, false);
                            DataManager.SaveData(Bot.CurrencyPath, $"[{date.Day}.{date.Month}.{date.Year}] dollars", buttersDollars, false);

                            DataManager.SaveData(Bot.CurrencyPath);
                            previousButters = buttersAmount;
                        }

                        if (DateTime.UtcNow.Minute % 10 == 0 && DateTime.UtcNow.Second == 0)
                        {
                            Bot.ReserveCopyPath = Bot.ProgramPath + "bbRESERVE/" + DateTime.UtcNow.Year + DateTime.UtcNow.Month + DateTime.UtcNow.Day + "/";
                            Task.Run(Bot.StatusSender);
                        }
                    }
                    //ConsoleUtil.LOG($"Tick execution time: {(DateTime.Now - startTime).TotalMilliseconds}ms", "debug");
                    tickDelay = (DateTime.Now - startTime).TotalMilliseconds;
                    tickEnded = true;
                }
                catch (Exception e)
                {
                    string ErrorText = $"[ TICK ERROR ] {e.Message} : {e.StackTrace}";
                    LogWorker.Log(ErrorText, LogWorker.LogTypes.Err, "BotEngine/Main");
                    ConsoleUtil.LOG(ErrorText, "kernel", ConsoleColor.Black, ConsoleColor.Red);
                }
            });
        }
        private static void Restart()
        {
            try
            {
                Bot.Start(restartedTimes);
            }
            catch (Exception e)
            {
                ConsoleUtil.LOG($"[ RESTART ERROR ] {e.Message} : {e.StackTrace}", "kernel");
                isRestarting = true;
            }
        }
    }
    // #BOT
    public class Bot
    {
        public static string ProgramPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}/ItzKITb/";
        public static string MainPath = ProgramPath + "butterBror/";
        public static string ChannelsPath = MainPath + "CHNLS/";
        public static string UsersDataPath = MainPath + "USERSDB/";
        public static string UsersBankDataPath = MainPath + "BANKDB/";
        public static string ConvertorsPath = MainPath + "CONVRT/";
        public static string NicknameToIDPath = ConvertorsPath + "N2I/";
        public static string IDToNicknamePath = ConvertorsPath + "I2N/";
        public static string SettingsPath = MainPath + "SETTINGS.json";
        public static string CookiesPath = MainPath + "COOKIES.MDS";
        public static string TranslatePath = MainPath + "TRNSLT/";
        public static string TranslateDefualtPath = MainPath + "TRNSLT/DEFAULT/";
        public static string TranslateCustomPath = MainPath + "TRNSLT/CUSTOM/";
        public static string BanWordsPath = MainPath + "BNWORDS.txt";
        public static string BanWordsReplacementPath = MainPath + "BNWORDSREP.txt";
        public static string APIUseDataPath = MainPath + "API.json";
        public static string LogsPath = MainPath + "LOGS.log";
        public static string ErrorsPath = MainPath + "ERRORS.log";
        public static string LocationsCachePath = MainPath + "LOC.cache";
        public static string CurrencyPath = MainPath + "CURR.json";
        public static string TelegramToken = "";
        public static string ReserveCopyPath = ProgramPath + "bbRESERVE/" + DateTime.UtcNow.Year + DateTime.UtcNow.Month + DateTime.UtcNow.Day + "/";
        
        public static string BotNick = "";
        public static string BotToken = "";
        private static string BotDiscordToken = "";
        public static string UID = "";
        public static string ClientID = "";
        public static string Secret = "";
        public static string NowColor = "";
        public static string ImgurAPIkey = "";
        public static string LastChannelConnected = "";
        public static string[] ConnectionAnnounceChannels = [];
        public static string[] ReconnectionAnnounceChannels = [];
        public static string[] VersionChangeAnnounceChannels = [];
        public static string[] Channels = [];
        public static int CommandsActive = 24;
        public static int ServersConnected = 0;
        public static int PreviousCheckTimersMinute = DateTime.UtcNow.Minute;
        public static int ReadedMessages = 0;
        public static bool Connected = false;
        public static bool TimerStarted = false;
        public static bool BotAlreadyConnected = false;
        public static bool Reconnected = false;
        public static string CoinSymbol = "🥪";
        public static int AddingCoinsToTheMentionedUser = 8;
        public static int AddingCoinsToTheMentioningUser = 2;

        public static TwitchTokenUtil TokenGetter = new(ClientID, Secret, "tw_auth_keys.db");
        public static Dictionary<string, string[]> EmotesByChannel = [];
        public static TwitchClient Client = new();
        public static DiscordSocketClient? DiscordClient;
        public static CommandService? DiscordCommands;
        public static ReceiverOptions? TGReceiverOptions;
        public static IServiceProvider? DiscordServices;
        public static ITelegramBotClient? TelegramClient;
        private static DateTime StartTime = DateTime.UtcNow;

        // #BOT 0A
        public static async void Start(int ThreadID)
        {
            StartTime = DateTime.UtcNow;
            Thread.CurrentThread.Name = ThreadID.ToString();
            BotEngine.isTwitchReady = false;
            ConsoleUtil.LOG("Bot starting...", "main", FG: ConsoleColor.Yellow);
            ConsoleUtil.LOG($"v. {BotEngine.botVersion}#{BotEngine.patchID} is loading...", "main");

            if (System.IO.File.Exists(CurrencyPath))
            {
                BotEngine.buttersAmount = DataManager.GetData<float>(CurrencyPath, "totalAmount");
                BotEngine.buttersDollars = DataManager.GetData<int>(CurrencyPath, "totalDollarsInTheBank");
                BotEngine.users = DataManager.GetData<int>(CurrencyPath, "totalUsers");
            }

            try
            {
                ConsoleUtil.LOG("Bot", "main");
                ConsoleUtil.LOG("Directories check...", "main");
                FileUtil.CreateDirectory(ProgramPath);
                FileUtil.CreateDirectory(MainPath);
                FileUtil.CreateDirectory(ChannelsPath);
                FileUtil.CreateDirectory(UsersDataPath);
                FileUtil.CreateDirectory(ConvertorsPath);
                FileUtil.CreateDirectory(NicknameToIDPath);
                FileUtil.CreateDirectory(IDToNicknamePath);
                FileUtil.CreateDirectory(TranslateDefualtPath);
                FileUtil.CreateDirectory(TranslateCustomPath);
                FileUtil.CreateDirectory(UsersBankDataPath);

                ConsoleUtil.LOG("Files check...", "main");
                if (!System.IO.File.Exists(SettingsPath))
                {
                    FileUtil.CreateFile(SettingsPath);
                    DataManager.SaveData(SettingsPath, "nickname", "");
                    DataManager.SaveData(SettingsPath, "token", "");
                    DataManager.SaveData(SettingsPath, "discordToken", "");
                    DataManager.SaveData(SettingsPath, "imgurAPI", "");
                    DataManager.SaveData(SettingsPath, "UID", "");
                    DataManager.SaveData(SettingsPath, "ClientID", "");
                    DataManager.SaveData(SettingsPath, "Secret", "");
                    string[] channels = ["First channel", "Second channel"];
                    DataManager.SaveData(SettingsPath, "connectionInfoChannels", (string[])[]);
                    DataManager.SaveData(SettingsPath, "reconnectionInfoChannels", (string[])[]);
                    DataManager.SaveData(SettingsPath, "channels", channels);
                    string[] apis = ["First api", "Second api"];
                    DataManager.SaveData(SettingsPath, "weatherApis", apis);
                    DataManager.SaveData(SettingsPath, "gptApis", apis);
                    DataManager.SaveData(SettingsPath, "telegramToken", "");
                    DataManager.SaveData(SettingsPath, "versionChangeAnnounceChannels", (string[])[]);
                    ConsoleUtil.LOG($"Settings file created! ({SettingsPath})", "main", ConsoleColor.Black, ConsoleColor.Cyan);
                    Thread.Sleep(-1);
                }
                else
                {
                    FileUtil.CreateFile(CookiesPath);
                    FileUtil.CreateFile(BanWordsPath);
                    FileUtil.CreateFile(BanWordsReplacementPath);
                    FileUtil.CreateFile(CurrencyPath);
                    FileUtil.CreateFile(LocationsCachePath);
                    FileUtil.CreateFile(LogsPath);
                    FileUtil.CreateFile(APIUseDataPath);
                    FileUtil.CreateFile(TranslateDefualtPath + "ru.txt");
                    FileUtil.CreateFile(TranslateDefualtPath + "en.txt");
                    Commands.IndexCommands();

                    ConsoleUtil.LOG("Loading files...", "main");
                    BotNick = DataManager.GetData<string>(SettingsPath, "nickname");
                    Channels = DataManager.GetData<string[]>(SettingsPath, "channels");
                    ReconnectionAnnounceChannels = DataManager.GetData<string[]>(SettingsPath, "reconnectionInfoChannels");
                    ConnectionAnnounceChannels = DataManager.GetData<string[]>(SettingsPath, "connectionInfoChannels");
                    BotDiscordToken = DataManager.GetData<string>(SettingsPath, "discordToken");
                    ImgurAPIkey = DataManager.GetData<string>(SettingsPath, "imgurAPI");
                    UID = DataManager.GetData<string>(SettingsPath, "UID");
                    ClientID = DataManager.GetData<string>(SettingsPath, "ClientID");
                    Secret = DataManager.GetData<string>(SettingsPath, "Secret");
                    TelegramToken = DataManager.GetData<string>(SettingsPath, "telegramToken");
                    VersionChangeAnnounceChannels = DataManager.GetData<string[]>(SettingsPath, "versionChangeAnnounceChannels");
                    ConsoleUtil.LOG("Generating twitch token...", "main");
                    TokenGetter = new(ClientID, Secret, "keys.db");
                    var token = await TokenGetter.GetTokenAsync();
                    if (token != null)
                    {
                        BotToken = token;
                        Connect();
                    }
                    else
                        Restart();
                }
            }
            catch (Exception ex)
            {
                LogWorker.Log(ex.Message, LogWorker.LogTypes.Err, "bot_maintrance");
                ConsoleUtil.LOG("[ BOT ] ERROR! Restarting...", "err", ConsoleColor.Red);
                LogWorker.Log($"[ BOT ] ERROR! Restarting...", LogWorker.LogTypes.Err, "bot_maintrance");
                Restart();
            }
        }
        public static async Task Connect()
        {
            try
            {
                ConsoleUtil.LOG("Connecting to twitch...", "main");
                ConnectionCredentials credentials = new(BotNick, "oauth:" + BotToken);
                var clientOptions = new ClientOptions
                {
                    MessagesAllowedInPeriod = 750,
                    ThrottlingPeriod = TimeSpan.FromSeconds(30)
                };
                var webSocketClient = new WebSocketClient(clientOptions);
                Client = new TwitchClient(webSocketClient);
                Client.Initialize(credentials, BotNick, '#');
                Client.OnJoinedChannel += TwitchEventHandler.OnJoin;
                Client.OnChatCommandReceived += Commands.TwitchCommand;
                Client.OnMessageReceived += TwitchEventHandler.OnMessageReceived;
                Client.OnMessageThrottled += TwitchEventHandler.OnMessageThrottled;
                Client.OnMessageSent += TwitchEventHandler.OnMessageSend;
                Client.OnAnnouncement += TwitchEventHandler.OnAnnounce;
                Client.OnBanned += TwitchEventHandler.OnBanned;
                Client.OnConnectionError += TwitchEventHandler.OnConnectionError;
                Client.OnContinuedGiftedSubscription += TwitchEventHandler.OnContinuedGiftedSubscription;
                Client.OnChatCleared += TwitchEventHandler.OnChatCleared;
                Client.OnDisconnected += TwitchEventHandler.OnTwitchDisconnected;
                Client.OnReconnected += TwitchEventHandler.OnReconnected;
                Client.OnError += TwitchEventHandler.OnError;
                Client.OnIncorrectLogin += TwitchEventHandler.OnIncorrectLogin;
                Client.OnLeftChannel += TwitchEventHandler.OnLeftChannel;
                Client.OnRaidNotification += TwitchEventHandler.OnRaidNotification;
                Client.OnNewSubscriber += TwitchEventHandler.OnNewSubscriber;
                Client.OnGiftedSubscription += TwitchEventHandler.OnGiftedSubscription;
                Client.OnCommunitySubscription += TwitchEventHandler.OnCommunitySubscription;
                Client.OnReSubscriber += TwitchEventHandler.OnReSubscriber;
                Client.OnSuspended += TwitchEventHandler.OnSuspended;
                Client.OnConnected += TwitchEventHandler.OnConnected;
                Client.OnLog += TwitchEventHandler.OnLog;
                Client.Connect();
                BotAlreadyConnected = true;

                var lastChannel = NamesUtil.GetUsername(Channels.LastOrDefault(), Channels.LastOrDefault());
                var notFoundedChannels = new List<string>();
                LastChannelConnected = lastChannel;
                string sendChannelsMsg = "";
                foreach (var channel in Channels)
                {
                    var channel2 = NamesUtil.GetUsername(channel, "NONE\n");
                    if (channel2 != "NONE\n") sendChannelsMsg += $"{channel2}, ";
                    else notFoundedChannels.Add(channel);
                }
                ConsoleUtil.LOG($"Connecting to: @twitch/[{sendChannelsMsg.TrimEnd(',', ' ')}]", "main");
                foreach (var channel in Channels)
                {
                    var channel2 = NamesUtil.GetUsername(channel, "NONE\n");
                    if (channel2 != "NONE\n") Client.JoinChannel(channel2);
                }
                foreach (var channel in notFoundedChannels) ConsoleUtil.LOG("Can't find ID: " + channel, "err", ConsoleColor.Red);
                BotEngine.isTwitchReady = true;
                Client.JoinChannel(BotNick.ToLower());
                Client.SendMessage(BotNick.ToLower(), "truckCrash Connecting to twitch...");

                ConsoleUtil.LOG("Connecting to Discord...", "main");
                var discordConfig = new DiscordSocketConfig
                {
                    MessageCacheSize = 1000,
                    GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.DirectMessages | GatewayIntents.MessageContent
                };
                DiscordClient = new DiscordSocketClient(discordConfig);
                DiscordCommands = new CommandService();
                DiscordServices = new ServiceCollection()
                    .AddSingleton(DiscordClient)
                    .AddSingleton(DiscordCommands)
                    .BuildServiceProvider();

                DiscordClient.Log += DiscordEventHandler.LogAsync;
                DiscordClient.JoinedGuild += DiscordEventHandler.ConnectToGuilt;
                DiscordClient.Ready += DiscordWorker.ReadyAsync;
                DiscordClient.MessageReceived += DiscordWorker.MessageReceivedAsync;
                DiscordClient.SlashCommandExecuted += DiscordEventHandler.SlashCommandHandler;
                DiscordClient.ApplicationCommandCreated += DiscordEventHandler.ApplicationCommandCreated;
                DiscordClient.ApplicationCommandDeleted += DiscordEventHandler.ApplicationCommandDeleted;
                DiscordClient.ApplicationCommandUpdated += DiscordEventHandler.ApplicationCommandUpdated;
                DiscordClient.ChannelCreated += DiscordEventHandler.ChannelCreated;
                DiscordClient.ChannelDestroyed += DiscordEventHandler.ChannelDeleted;
                DiscordClient.ChannelUpdated += DiscordEventHandler.ChannelUpdated;
                DiscordClient.Connected += DiscordEventHandler.Connected;
                DiscordClient.ButtonExecuted += DiscordEventHandler.ButtonTouched;
                await DiscordWorker.RegisterCommandsAsync();
                await DiscordClient.LoginAsync(TokenType.Bot, BotDiscordToken);
                await DiscordClient.StartAsync();

                ConsoleUtil.LOG("Connecting to Telegram...", "main");
                TelegramClient = new TelegramBotClient(TelegramToken);
                TGReceiverOptions = new ReceiverOptions
                {
                    AllowedUpdates = new[]
                    {
                        UpdateType.Message,
                    },
                    DropPendingUpdates = true,
                };

                using var cts = new CancellationTokenSource();
                TelegramClient.StartReceiving(TelegramWorker.UpdateHandler, TelegramWorker.ErrorHandler, TGReceiverOptions, cts.Token);

                DateTime endTime = DateTime.UtcNow;
                ConsoleUtil.LOG($"Well done! (Started in {(endTime - StartTime).TotalMilliseconds} ms)", "main");
            }
            catch (Exception ex)
            {
                LogWorker.Log(ex.Message + " : " + ex.StackTrace, LogWorker.LogTypes.Err, "bot_connect");
                ConsoleUtil.LOG("[ BOT ] ERROR! Restarting...", "err", ConsoleColor.Red);
                LogWorker.Log($"[ BOT ] ERROR! Restarting...", LogWorker.LogTypes.Err, "bot_connect");
                Restart();
            }
        }
        public static async Task StatusSender()
        {
            try
            {
                DateTime start = DateTime.UtcNow;
                ConsoleUtil.LOG("Status sender started!", "kernel", ConsoleColor.Red);

                Ping ping = new();
                PingReply twitch = ping.Send("twitch.tv", 1000);
                PingReply discord = ping.Send("discord.com", 1000);
                PingReply telegram = ping.Send("t.me", 1000);
                PingReply ISP = ping.Send("192.168.1.1", 1000);

                if (ISP.Status != IPStatus.Success)
                {
                    ISP = ping.Send("192.168.0.1", 1000);
                    if (ISP.Status != IPStatus.Success) ConsoleUtil.LOG("Error ISP ping: " + ISP.Status.ToString(), "err");
                }
                long memory = Process.GetCurrentProcess().WorkingSet64 / (1024 * 1024);
                ChatUtil.TwitchSendMessage(BotNick.ToLower(), $"/me glorp 📡 Twitch: {twitch.RoundtripTime}ms | Discord: {discord.RoundtripTime}ms | Telegram: {telegram.RoundtripTime}ms | ISP: {ISP.RoundtripTime}ms | {DateTime.Now - BotEngine.botStartTime:dd\\:hh\\:mm\\.ss} | {memory}mb", "", "", "", true);

                try
                {
                    string newToken = await TokenGetter.RefreshAccessToken();
                    if (newToken != null) BotToken = newToken;
                }
                catch (Exception ex)
                {
                    ConsoleUtil.ErrorOccured(ex, "StatusSender/TokenRefresher");
                }
                ConsoleUtil.LOG($"Status sender ended! (In {(DateTime.UtcNow - start).TotalMilliseconds}ms)", "kernel", ConsoleColor.Red);
            }
            catch (Exception ex)
            {
                string text = $"Error while sending status message! ({ex.Message + " : " + ex.StackTrace})";
                ConsoleUtil.LOG(text, "err", ConsoleColor.Red);
                LogWorker.Log(text, LogWorker.LogTypes.Err, "Bot/StatusSender");
            }
        }
        public static void Restart()
        {
            ConsoleUtil.LOG("Restarting...", "main");
            try
            {
                foreach (var channel in Client.JoinedChannels)
                {
                    try
                    {
                        Client.LeaveChannel(channel);
                    }
                    catch (Exception ex)
                    {
                        ConsoleUtil.LOG($"[TW] Leave channel error: {ex.Message} : {ex.StackTrace}", "main");
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleUtil.LOG($"[TW] Leave channels error: {ex.Message} : {ex.StackTrace}", "main");
            }
            try
            {
                Client.OnJoinedChannel -= TwitchEventHandler.OnJoin;
                Client.OnChatCommandReceived -= Commands.TwitchCommand;
                Client.OnMessageReceived -= TwitchEventHandler.OnMessageReceived;
                Client.OnMessageThrottled -= TwitchEventHandler.OnMessageThrottled;
                Client.OnMessageSent -= TwitchEventHandler.OnMessageSend;
                Client.OnAnnouncement -= TwitchEventHandler.OnAnnounce;
                Client.OnBanned -= TwitchEventHandler.OnBanned;
                Client.OnConnectionError -= TwitchEventHandler.OnConnectionError;
                Client.OnContinuedGiftedSubscription -= TwitchEventHandler.OnContinuedGiftedSubscription;
                Client.OnChatCleared -= TwitchEventHandler.OnChatCleared;
                Client.OnDisconnected -= TwitchEventHandler.OnTwitchDisconnected;
                Client.OnReconnected -= TwitchEventHandler.OnReconnected;
                Client.OnError -= TwitchEventHandler.OnError;
                Client.OnIncorrectLogin -= TwitchEventHandler.OnIncorrectLogin;
                Client.OnLeftChannel -= TwitchEventHandler.OnLeftChannel;
                Client.OnRaidNotification -= TwitchEventHandler.OnRaidNotification;
                Client.OnNewSubscriber -= TwitchEventHandler.OnNewSubscriber;
                Client.OnGiftedSubscription -= TwitchEventHandler.OnGiftedSubscription;
                Client.OnCommunitySubscription -= TwitchEventHandler.OnCommunitySubscription;
                Client.OnReSubscriber -= TwitchEventHandler.OnReSubscriber;
                Client.OnSuspended -= TwitchEventHandler.OnSuspended;
                Client.OnConnected -= TwitchEventHandler.OnConnected;
            }
            catch (Exception ex)
            {
                ConsoleUtil.LOG($"[TW] Disconnect error: {ex.Message} : {ex.StackTrace}", "main");
            }
            try
            {
                Client.Disconnect();
            }
            catch (Exception ex)
            {
                ConsoleUtil.LOG($"[TW] Client disconnect error: {ex.Message} : {ex.StackTrace}", "main");
            }
            Client = null;
            BotEngine.isTwitchReady = false;
            BotEngine.isRestarting = true;
        }
        public static void TurnOff()
        {
            try
            {
                Client.SendMessage(BotNick, "Zevlo Turning off...");
            }
            catch { }
            foreach (var channel in ConnectionAnnounceChannels)
            {
                try
                {
                    Client.SendMessage(NamesUtil.GetUsername(channel, channel), "Zevlo Turning off...");
                }
                catch { }
            }

            BotEngine.isTwitchReady = false;
            
            UsersData.ClearData();
            ConsoleUtil.LOG($"Bot disabled!", "main", ConsoleColor.Black, ConsoleColor.Cyan);
            Thread.Sleep(-1);
        }
    }
    public class DiscordWorker
    {
        public static async Task ReadyAsync()
        {
            try
            {
                ConsoleUtil.LOG($"[DS] Connected as @{Bot.DiscordClient.CurrentUser}!", "discord");

                foreach (var guild in Bot.DiscordClient.Guilds)
                {
                    ConsoleUtil.LOG($"[DS] Connected to server: {guild.Name}", "discord");
                    Bot.ServersConnected++;
                }
            }
            catch (Exception ex)
            {
                ConsoleUtil.ErrorOccured(ex, "DiscordWorker\\ReadyAsync");
            }
        }
        public static async Task MessageReceivedAsync(SocketMessage message)
        {
            try
            {
                if (!(message is SocketUserMessage msg) || message.Author.IsBot) return;
                OnMessageReceivedArgs e = default;
                CommandUtil.MessageWorker("ds" + message.Author.Id.ToString(), ((SocketGuildChannel)message.Channel).Guild.Id.ToString(), message.Author.Username.ToLower(), message.Content, e, ((SocketGuildChannel)message.Channel).Guild.Name, Platforms.Discord, null, message.Channel.ToString());
            }
            catch (Exception ex)
            {
                ConsoleUtil.ErrorOccured(ex, $"DiscordWorker\\MessageReceivedAsync#{message.Content}");
            }
        }
        public static async Task RegisterCommandsAsync()
        {
            try
            {
                Bot.DiscordClient.Ready += RegisterSlashCommands;
                Bot.DiscordClient.MessageReceived += DiscordEventHandler.HandleCommandAsync;

                await Bot.DiscordCommands.AddModulesAsync(assembly: Assembly.GetEntryAssembly(), services: Bot.DiscordServices);
            }
            catch (Exception ex)
            {
                ConsoleUtil.ErrorOccured(ex, "DiscordWorker\\RegisterCommandsAsync");
            }
        }
        private static async Task RegisterSlashCommands()
        {
            // Удаление старых команд
            ConsoleUtil.LOG("[DS] Updating commands...", "discord");
            await Bot.DiscordClient.Rest.DeleteAllGlobalCommandsAsync();
            ConsoleUtil.LOG("[DS] Registering commands...", "discord");
            await Bot.DiscordClient.Rest.CreateGlobalCommand(new SlashCommandBuilder()
                .WithName("ping")
                .WithDescription("Check bot status")
                .Build());
            await Bot.DiscordClient.Rest.CreateGlobalCommand(new SlashCommandBuilder()
                .WithName("status")
                .WithDescription("View the bot's status. (Bot administrators only)")
                .Build());
            await Bot.DiscordClient.Rest.CreateGlobalCommand(new SlashCommandBuilder()
                .WithName("weather")
                .WithDescription("Check the weather")
                .AddOption("location", ApplicationCommandOptionType.String, "weather check location", isRequired: false)
                .AddOption("showpage", ApplicationCommandOptionType.Integer, "show weather on page", isRequired: false)
                .AddOption("page", ApplicationCommandOptionType.Integer, "show the result page of the received weather", isRequired: false)
                .Build());
            ConsoleUtil.LOG("[DS] All commands are registered!", "discord");
        }
    }
    public class DiscordEventHandler
    {
        public static Task LogAsync(LogMessage log)
        {
            try
            {
                ConsoleUtil.LOG(log.ToString().Replace("\n", " ").Replace("\r", ""), "discord");
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                ConsoleUtil.ErrorOccured(ex, $"DiscordEventHandler\\LogAsync#{log.Message}");
                return Task.CompletedTask;
            }
        }
        public static async Task ConnectToGuilt(SocketGuild g)
        {
            ConsoleUtil.LOG($"[DS] Connected to a server: {g.Name}", "discord");
            Bot.ServersConnected++;
        }
        public static async Task HandleCommandAsync(SocketMessage arg)
        {
            try
            {
                var message = arg as SocketUserMessage;
                if (message == null || message.Author.IsBot) return;

                int argPos = 0;
                if (message.HasCharPrefix('#', ref argPos))
                {
                    var context = new SocketCommandContext(Bot.DiscordClient, message);
                    var result = await Bot.DiscordCommands.ExecuteAsync(context, argPos, Bot.DiscordServices);
                    if (!result.IsSuccess)
                    {
                        ConsoleUtil.LOG(result.ErrorReason, "discord", ConsoleColor.Red);
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleUtil.ErrorOccured(ex, $"DiscordEventHandler\\HandleCommandAsync");
            }
        }
        public static async Task SlashCommandHandler(SocketSlashCommand command)
        {
            Commands.DiscordCommand(command);
        }
        public static async Task ApplicationCommandCreated(SocketApplicationCommand e)
        {
            ConsoleUtil.LOG("[DS] The command has been created: /" + e.Name + " (" + e.Description + ")", "info");
        }
        public static async Task ApplicationCommandDeleted(SocketApplicationCommand e)
        {
            ConsoleUtil.LOG("[DS] Command deleted: /" + e.Name + " (" + e.Description + ")", "info");
        }
        public static async Task ApplicationCommandUpdated(SocketApplicationCommand e)
        {
            ConsoleUtil.LOG("[DS] Command updated: /" + e.Name + " (" + e.Description + ")", "info");
        }
        public static async Task ChannelCreated(SocketChannel e)
        {
            ConsoleUtil.LOG("[DS] New channel created: " + e.Id, "discord");
        }
        public static async Task ChannelDeleted(SocketChannel e)
        {
            ConsoleUtil.LOG("[DS] The channel has been deleted: " + e.Id, "discord");
        }
        public static async Task ChannelUpdated(SocketChannel e, SocketChannel a)
        {
            ConsoleUtil.LOG("[DS] Channel updated: " + e.Id + "/" + a.Id, "discord");
        }
        public static async Task Connected()
        {
            ConsoleUtil.LOG("[DS] Connected!", "discord");
        }
        public static async Task ButtonTouched(SocketMessageComponent e)
        {
            ConsoleUtil.LOG($"[DS] A button was pressed. User: {e.User}, Button ID: {e.Id}, Server: {((SocketGuildChannel)e.Channel).Guild.Name}", "info");
        }
    }
    public class TelegramWorker
    {
        public static async Task UpdateHandler(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
#pragma warning disable CS4014
            _ = Task.Run(async () =>
            {
                try
                {
                    switch (update.Type)
                    {
                        case UpdateType.Message:
                            {
                                Message message = update.Message;
                                User user = message.From;
                                Chat chat = message.Chat;
                                User meData = Bot.TelegramClient.GetMe().Result;

                                ConsoleUtil.LOG($"{chat.Title}/{user.Username}: {message.Text}", "tg_chat");
                                CommandUtil.MessageWorker("tg" + user.Id, "tg" + chat.Id.ToString(), user.Username.ToLower(), (message.Text == null ? "[ Not text ]" : message.Text), new OnMessageReceivedArgs(), (chat.Title == null ? meData.Username : chat.Title), Platforms.Telegram, message);

                                string lang = UsersData.UserGetData<string>("tg" + user.Id, "language");
                                if (lang == null) lang = "ru";

                                if (message.Text == "/start" || message.Text == "/start@" + meData.Username)
                                {
                                    await botClient.SendMessage(
                                        chat.Id,
                                        TranslationManager.GetTranslation(lang, "tgWelcome", "tg" + chat.Id)
                                        .Replace("%ID%", user.Id.ToString())
                                        .Replace("%WorkTime%", TextUtil.FormatTimeSpan(DateTime.Now - BotEngine.botStartTime, lang))
                                        .Replace("%Version%", BotEngine.botVersion)
                                        .Replace("%Ping%", new Ping().Send("t.me", 1000).RoundtripTime.ToString()),
                                        replyParameters: message.MessageId
                                    );
                                }
                                else if (message.Text == "/ping" || message.Text == "/ping@" + meData.Username)
                                {
                                    var workTime = DateTime.Now - BotEngine.botStartTime;
                                    PingReply reply = new Ping().Send("t.me", 1000);
                                    string returnMessage = TranslationManager.GetTranslation(lang, "pingText", "tg" + chat.Id)
                                                .Replace("%version%", BotEngine.botVersion)
                                                .Replace("%workTime%", TextUtil.FormatTimeSpan(workTime, lang))
                                                .Replace("%tabs%", Bot.Channels.Length.ToString())
                                                .Replace("%loadedCMDs%", Bot.CommandsActive.ToString())
                                                .Replace("%completedCMDs%", BotEngine.completedCommands.ToString())
                                                .Replace("%ping%", reply.RoundtripTime.ToString());
                                    await botClient.SendMessage(
                                        chat.Id,
                                        returnMessage,
                                        replyParameters: message.MessageId
                                    );
                                }
                                else if (message.Text == "/help" || message.Text == "/help@" + meData.Username)
                                {
                                    string returnMessage = TranslationManager.GetTranslation(lang, "botInfo", "tg" + chat.Id);
                                    await botClient.SendMessage(
                                        chat.Id,
                                        returnMessage,
                                        replyParameters: message.MessageId
                                    );
                                }
                                else if (message.Text == "/commands" || message.Text == "/commands@" + meData.Username)
                                {
                                    string returnMessage = TranslationManager.GetTranslation(lang, "help", "tg" + chat.Id);
                                    await botClient.SendMessage(
                                        chat.Id,
                                        returnMessage,
                                        replyParameters: message.MessageId
                                    );
                                }
                                else if (message.Text.StartsWith("#"))
                                {
                                    message.Text = message.Text[1..];
                                    Commands.TelegramCommand(message);
                                }

                                return;
                            }
                    }
                }
                catch (Exception ex)
                {
                    ConsoleUtil.ErrorOccured(ex, "TelegramWorker\\UpdateHandler");
                }
            }, cancellationToken);
            #pragma warning restore CS4014
        }
        public static Task ErrorHandler(ITelegramBotClient botClient, Exception error, CancellationToken cancellationToken)
        {
            var ErrorMessage = error switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => error.ToString()
            };

            ConsoleUtil.ErrorOccured(error, "TelegramWorker\\ErrorHandler");
            return Task.CompletedTask;
        }
    }
}