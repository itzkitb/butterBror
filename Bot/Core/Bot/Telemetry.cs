﻿using butterBror.Core.Commands;
using butterBror.Models;
using butterBror.Services.External;
using butterBror.Services.System;
using butterBror.Utils;
using DankDB;
using System.Diagnostics;
using System.Net.NetworkInformation;
using static butterBror.Core.Bot.Console;

namespace butterBror.Core.Bot
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
                if (butterBror.Bot.Clients == null || !butterBror.Bot.Clients.Twitch.IsConnected)
                {
                    if (butterBror.Bot.Clients == null)
                    {
                        Write("Clients are not initialized yet", "telemetry", LogLevel.Warning);
                    }
                    else if (!butterBror.Bot.Clients.Twitch.IsConnected)
                    {
                        Write("Twitch is not connected", "telemetry", LogLevel.Warning);
                    }

                    return;
                }

                Write("Twitch - Telemetry started!", "telemetry");
                Stopwatch Start = Stopwatch.StartNew();

                int cacheItemsBefore = Worker.cache.count;
                Worker.cache.Clear(TimeSpan.FromMinutes(10));

                #region Ethernet ping
                Ping ping = new();
                PingReply twitch = ping.Send(URLs.twitch, 1000);
                PingReply discord = ping.Send(URLs.discord, 1000);
                long telegram = await TelegramService.Ping();
                PingReply sevenTV = ping.Send(URLs.seventv, 1000);
                PingReply ISP = ping.Send("192.168.1.1", 1000);

                if (ISP.Status != IPStatus.Success)
                {
                    ISP = ping.Send("192.168.0.1", 1000);
                    if (ISP.Status != IPStatus.Success) Write("Twitch - Error ISP ping: " + ISP.Status.ToString(), "info", LogLevel.Warning);
                }
                #endregion
                #region Commands ping
                Stopwatch CommandExecute = Stopwatch.StartNew();

                string CommandName = "bot";
                List<string> CommandArguments = [];
                string CommandArgumentsAsString = "";

                UserData user = new()
                {
                    ID = "a123456789",
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
                    TwitchArguments = new TwitchLib.Client.Events.OnChatCommandReceivedArgs(),
                    CommandInstanceID = Guid.NewGuid().ToString()
                };

                await Runner.Run(data, true);
                CommandExecute.Stop();
                #endregion

                decimal cpuPercent = CPUItems == 0 ? 0 : CPU / CPUItems;
                decimal coinCurrency = butterBror.Bot.Coins == 0 ? 0 : butterBror.Bot.InBankDollars / butterBror.Bot.Coins;

                CPU = 0;
                CPUItems = 0;
                Start.Stop();

                long memory = Process.GetCurrentProcess().PrivateMemorySize64 / (1024 * 1024);

                PlatformMessageSender.TwitchSend(butterBror.Bot.BotName.ToLower(), $"/me glorp 📡 | " +
                    $"🕒 {TextSanitizer.FormatTimeSpan(DateTime.Now - butterBror.Bot.StartTime, "en-US")} | " +
                    $"{memory}Mbyte | " +
                    $"🔋 {Battery.GetBatteryCharge()}% {(Battery.IsCharging() ? "(Charging) " : "")}| " +
                    $"CPU: {cpuPercent:0.00}% | " +
                    $"Emotes: {butterBror.Bot.EmotesCache.Count} | " +
                    $"7tv: E:{butterBror.Bot.ChannelsSevenTVEmotes.Count},USC:{butterBror.Bot.UsersSearchCache.Count},ES:{butterBror.Bot.EmoteSetsCache.Count} | " +
                    $"Messages: {butterBror.Bot.MessagesProccessed} | " +
                    $"Discord guilds: {butterBror.Bot.Clients.Discord.Guilds.Count} | " +
                    $"Twitch channels: {butterBror.Bot.Clients.Twitch.JoinedChannels.Count} | " +
                    $"Completed: {butterBror.Bot.CompletedCommands} | " +
                    $"Users: {butterBror.Bot.Users} | " +
                    $"Coins: {butterBror.Bot.Coins:0.00} | " +
                    $"Currency: ${coinCurrency:0.00000000} | " +
                    $"Twitch: {twitch.RoundtripTime}ms | " +
                    $"Discord: {discord.RoundtripTime}ms | " +
                    $"Telegram: {telegram}ms | " +
                    $"7tv: {sevenTV.RoundtripTime}ms | " +
                    $"ISP: {ISP.RoundtripTime}ms | " +
                    $"Command: {CommandExecute.ElapsedMilliseconds}ms", "", "", "", true, false);

                Write($"Twitch - Telemetry ended! ({Start.ElapsedMilliseconds}ms)", "telemetry");

                try
                {
                    var newToken = await TwitchToken.RefreshAccessToken(butterBror.Bot.Tokens.Twitch);
                    if (newToken != null)
                    {
                        butterBror.Bot.Tokens.Twitch = newToken;
                    }
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
