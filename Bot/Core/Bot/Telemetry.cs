using bb.Core.Commands;
using bb.Services.External;
using bb.Services.Internal;
using bb.Utils;
using bb.Core.Configuration;
using DankDB;
using System.Diagnostics;
using System.Net.NetworkInformation;
using static bb.Core.Bot.Console;
using bb.Core.Configuration;
using bb.Models.Command;
using bb.Models.Platform;
using bb.Models.Users;

namespace bb.Core.Bot
{
    /// <summary>
    /// Collects, processes, and reports system performance metrics for monitoring bot health and performance.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item>Aggregates CPU usage statistics from periodic measurements</item>
    /// <item>Monitors network connectivity to critical services</item>
    /// <item>Tracks command execution performance and system resource usage</item>
    /// <item>Generates comprehensive telemetry reports for Twitch channel</item>
    /// <item>Automatically refreshes Twitch authentication tokens</item>
    /// </list>
    /// Designed to run periodically (typically every 10 minutes) to provide real-time system health monitoring.
    /// All metrics are formatted for human-readable display in Twitch chat while maintaining technical accuracy.
    /// </remarks>
    public class Telemetry
    {
        public static decimal CPU = 0;
        public static long CPUItems = 0;

        /// <summary>
        /// Generates and transmits a comprehensive system health report to Twitch chat.
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        /// <item><term>Cache Management</term><description>Clears stale cache items older than 10 minutes and reports cache statistics</description></item>
        /// <item><term>Network Diagnostics</term><description>Performs latency checks to critical services (Twitch, Discord, Telegram, 7TV) and local network gateway</description></item>
        /// <item><term>Command Performance</term><description>Measures execution time of a test command to gauge system responsiveness</description></item>
        /// <item><term>System Metrics</term><description>Collects memory usage, battery status, and currency statistics</description></item>
        /// <item><term>Platform Statistics</term><description>Reports active connections, message counts, and user metrics across all platforms</description></item>
        /// <item><term>Token Management</term><description>Automatically refreshes Twitch OAuth token if nearing expiration</description></item>
        /// </list>
        /// The report is formatted for Twitch chat readability with appropriate emotes and spacing.
        /// All operations are wrapped in exception handling to prevent telemetry failures from affecting core functionality.
        /// Executes in approximately 500-1000ms depending on network conditions.
        /// </remarks>
        /// <example>
        /// Sample output in Twitch chat:
        /// <code>
        /// glorp 📡 | 🕒 1 h. 20 m. | 50Mbyte | 🔋 -1% | CPU: 18,66%
        /// | Emotes: 0 | 7tv: E:2,USC:2,ES:0
        /// | Messages: 23 | Discord guilds: 1 | Twitch channels: 4
        /// | Completed: 0 | Users: 221 | Coins: 297,29
        /// | Currency: $3,36371900 | Twitch: 155ms | Discord: 39ms
        /// | Telegram: 174ms | 7tv: 24ms | ISP: 0ms | Command: 1ms 
        /// </code>
        /// </example>
        public static async Task Send()
        {
            try
            {
                if (bb.Program.BotInstance.Clients == null || !bb.Program.BotInstance.Clients.Twitch.IsConnected)
                {
                    if (bb.Program.BotInstance.Clients == null)
                    {
                        Write("Clients are not initialized yet.", LogLevel.Warning);
                    }
                    else if (!bb.Program.BotInstance.Clients.Twitch.IsConnected)
                    {
                            Write("Twitch is not connected.", LogLevel.Warning);
                    }

                    return;
                }

                Write("Twitch: Telemetry started!");
                Stopwatch Start = Stopwatch.StartNew();

                int cacheItemsBefore = Worker.cache.count;
                Worker.cache.Clear(TimeSpan.FromMinutes(10));

                #region Ethernet ping
                Ping ping = new();
                PingReply twitch = ping.Send(URLs.twitch, 1000);
                PingReply discord = ping.Send(URLs.discord, 1000);
                long telegram = await TelegramPing.Ping();
                PingReply sevenTV = ping.Send(URLs.seventv, 1000);
                PingReply ISP = ping.Send("192.168.1.1", 1000);

                if (ISP.Status != IPStatus.Success)
                {
                    ISP = ping.Send("192.168.0.1", 1000);
                    if (ISP.Status != IPStatus.Success) Write("Error ISP ping: " + ISP.Status.ToString(), LogLevel.Warning);
                }
                #endregion
                #region Commands ping
                Stopwatch CommandExecute = Stopwatch.StartNew();

                string CommandName = "bot";
                List<string> CommandArguments = [];
                string CommandArgumentsAsString = "";

                UserData user = new()
                {
                    Id = "a123456789",
                    Language = "en",
                    Name = "test",
                    IsModerator = true,
                    IsBroadcaster = true
                };

                CommandData data = new()
                {
                    Name = CommandName.ToLower(),
                    Arguments = CommandArguments,
                    ArgumentsString = CommandArgumentsAsString,
                    Channel = "test",
                    ChannelId = "a123456789",
                    MessageID = "a123456789",
                    Platform = PlatformsEnum.Telegram,
                    User = user,
                    TwitchMessage = new TwitchLib.Client.Events.OnMessageReceivedArgs(),
                    CommandInstanceID = Guid.NewGuid().ToString()
                };

                await bb.Program.BotInstance.CommandRunner.Execute(data, true);
                CommandExecute.Stop();
                #endregion

                decimal cpuPercent = CPUItems == 0 ? 0 : CPU / CPUItems;
                decimal coinCurrency = bb.Program.BotInstance.Coins == 0 ? 0 : bb.Program.BotInstance.InBankDollars / bb.Program.BotInstance.Coins;

                CPU = 0;
                CPUItems = 0;
                Start.Stop();

                long memory = Process.GetCurrentProcess().PrivateMemorySize64 / (1024 * 1024);

                bb.Program.BotInstance.MessageSender.Send(PlatformsEnum.Twitch, $"/me glorp 📡 | " +
                    $"🕒 {TextSanitizer.FormatTimeSpan(DateTime.UtcNow - bb.Program.BotInstance.StartTime, "en-US")} | " +
                    $"{memory}Mbyte | " +
                    $"🔋 {Battery.GetBatteryCharge()}% {(Battery.IsCharging() ? "(Charging) " : "")}| " +
                    $"CPU: {cpuPercent:0.00}% | " +
                    $"Emotes: {bb.Program.BotInstance.EmotesCache.Count} | " +
                    $"7tv: E:{bb.Program.BotInstance.ChannelsSevenTVEmotes.Count},USC:{bb.Program.BotInstance.UsersSearchCache.Count},ES:{bb.Program.BotInstance.EmoteSetsCache.Count} | " +
                    $"Messages: {bb.Program.BotInstance.MessageProcessor.Proccessed} | " +
                    $"Discord guilds: {bb.Program.BotInstance.Clients.Discord.Guilds.Count} | " +
                    $"Twitch channels: {bb.Program.BotInstance.Clients.Twitch.JoinedChannels.Count} | " +
                    $"Completed: {bb.Program.BotInstance.CompletedCommands} | " +
                    $"Users: {bb.Program.BotInstance.Users} | " +
                    $"Coins: {bb.Program.BotInstance.Coins:0.00} | " +
                    $"Currency: ${coinCurrency:0.00000000} | " +
                    $"Twitch: {twitch.RoundtripTime}ms | " +
                    $"Discord: {discord.RoundtripTime}ms | " +
                    $"Telegram: {telegram}ms | " +
                    $"7tv: {sevenTV.RoundtripTime}ms | " +
                    $"ISP: {ISP.RoundtripTime}ms | " +
                    $"Command: {CommandExecute.ElapsedMilliseconds}ms", bb.Program.BotInstance.TwitchName.ToLower());

                Write($"Twitch: Telemetry ended! ({Start.ElapsedMilliseconds}ms)");

                try
                {
                    await bb.Program.BotInstance.RefreshTwitchToken();
                }
                catch (Exception ex)
                {
                    Write(ex);
                }
            }
            catch (Exception ex)
            {
                Write(ex);
            }
        }
    }
}
