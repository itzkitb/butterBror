using butterBror.Utils.DataManagers;
using butterBror.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Models;
using Telegram.Bot.Types;
using static butterBror.Utils.Bot.Console;
using Telegram.Bot;
using butterBror.Utils.Types;

namespace butterBror.Utils.Tools
{
    /// <summary>
    /// Handles AFK return operations and sends platform-specific chat messages with translation support.
    /// </summary>
    public class Chat
    {
        /// <summary>
        /// Processes a user returning from AFK status with appropriate message generation based on AFK duration and type.
        /// </summary>
        /// <param name="UserID">User identifier</param>
        /// <param name="RoomID">Channel/room identifier</param>
        /// <param name="channel">Target channel name</param>
        /// <param name="username">Username to mention</param>
        /// <param name="message_id">Message ID for reply context</param>
        /// <param name="message_reply">Telegram message context for replies</param>
        /// <param name="platform">Target platform (Twitch/Telegram)</param>
        /// <remarks>
        /// - Determines appropriate AFK response based on duration and type
        /// - Updates user AFK status in storage
        /// - Sends formatted message with time elapsed since AFK
        /// - Handles multiple AFK types (draw, sleep, study, etc.)
        /// </remarks>
        [ConsoleSector("butterBror.Utils.Tools.Chat", "ReturnFromAFK")]
        public static void ReturnFromAFK(string UserID, string RoomID, string channel, string username, string message_id, Telegram.Bot.Types.Message message_reply, Platforms platform)
        {
            Engine.Statistics.FunctionsUsed.Add();
            var language = "ru";

            try
            {
                if (UsersData.Get<string>(UserID, "language", platform) == default)
                    UsersData.Save(UserID, "language", "ru", platform);
                else
                    language = UsersData.Get<string>(UserID, "language", platform);
            }
            catch (Exception ex)
            {
                Write(ex);
            }

            string? message = UsersData.Get<string>(UserID, "afkText", platform);
            if (!new NoBanwords().Check(message, RoomID, platform))
                return;

            string send = (Text.CleanAsciiWithoutSpaces(message) == "" ? "" : ": " + message);

            TimeSpan timeElapsed = DateTime.UtcNow - UsersData.Get<DateTime>(UserID, "afkTime", platform);
            var afkType = UsersData.Get<string>(UserID, "afkType", platform);
            string translateKey = "";

            if (afkType == "draw")
            {
                if (timeElapsed.TotalHours >= 2 && timeElapsed.TotalHours < 8) translateKey = "draw:2h";
                else if (timeElapsed.TotalHours >= 8 && timeElapsed.TotalHours < 24) translateKey = "draw:8h";
                else if (timeElapsed.TotalDays >= 1 && timeElapsed.TotalDays < 7) translateKey = "draw:1d";
                else if (timeElapsed.TotalDays >= 7 && timeElapsed.TotalDays < 31) translateKey = "draw:7d";
                else if (timeElapsed.TotalDays >= 31 && timeElapsed.TotalDays < 364) translateKey = "draw:1mn";
                else if (timeElapsed.TotalDays >= 364) translateKey = "draw:1y";
                else translateKey = "draw:default";
            }
            else if (afkType == "afk")
            {
                if (timeElapsed.TotalHours >= 8 && timeElapsed.TotalHours < 14) translateKey = "default:8h";
                else if (timeElapsed.TotalHours >= 14 && timeElapsed.TotalDays < 1) translateKey = "default:14h";
                else if (timeElapsed.TotalDays >= 1 && timeElapsed.TotalDays < 3) translateKey = "default:1d";
                else if (timeElapsed.TotalDays >= 3 && timeElapsed.TotalDays < 7) translateKey = "default:3d";
                else if (timeElapsed.TotalDays >= 7 && timeElapsed.TotalDays < 9) translateKey = "default:7d";
                else if (timeElapsed.TotalDays >= 9 && timeElapsed.TotalDays < 31) translateKey = "default:9d";
                else if (timeElapsed.TotalDays >= 31 && timeElapsed.TotalDays < 364) translateKey = "default:1mn";
                else if (timeElapsed.TotalDays >= 364) translateKey = "default:1y";
                else translateKey = "default";
            }
            else if (afkType == "sleep")
            {
                if (timeElapsed.TotalHours >= 2 && timeElapsed.TotalHours < 5) translateKey = "sleep:2h";
                else if (timeElapsed.TotalHours >= 5 && timeElapsed.TotalHours < 8) translateKey = "sleep:5h";
                else if (timeElapsed.TotalHours >= 8 && timeElapsed.TotalHours < 12) translateKey = "sleep:8h";
                else if (timeElapsed.TotalHours >= 12 && timeElapsed.TotalDays < 1) translateKey = "sleep:12h";
                else if (timeElapsed.TotalDays >= 1 && timeElapsed.TotalDays < 3) translateKey = "sleep:1d";
                else if (timeElapsed.TotalDays >= 3 && timeElapsed.TotalDays < 7) translateKey = "sleep:3d";
                else if (timeElapsed.TotalDays >= 7 && timeElapsed.TotalDays < 31) translateKey = "sleep:7d";
                else if (timeElapsed.TotalDays >= 31 && timeElapsed.TotalDays < 364) translateKey = "sleep:1mn";
                else if (timeElapsed.TotalDays >= 364) translateKey = "sleep:1y";
                else translateKey = "sleep:default";
            }
            else if (afkType == "rest")
            {
                if (timeElapsed.TotalHours >= 8 && timeElapsed.TotalHours < 24) translateKey = "rest:8h";
                else if (timeElapsed.TotalDays >= 1 && timeElapsed.TotalDays < 7) translateKey = "rest:1d";
                else if (timeElapsed.TotalDays >= 7 && timeElapsed.TotalDays < 31) translateKey = "rest:7d";
                else if (timeElapsed.TotalDays >= 31 && timeElapsed.TotalDays < 364) translateKey = "rest:1mn";
                else if (timeElapsed.TotalDays >= 364) translateKey = "rest:1y";
                else translateKey = "rest:default";
            }
            else if (afkType == "lurk") translateKey = "lurk:default";
            else if (afkType == "study")
            {
                if (timeElapsed.TotalHours >= 2 && timeElapsed.TotalHours < 5) translateKey = "study:2h";
                else if (timeElapsed.TotalHours >= 5 && timeElapsed.TotalHours < 8) translateKey = "study:5h";
                else if (timeElapsed.TotalHours >= 8 && timeElapsed.TotalHours < 24) translateKey = "study:8h";
                else if (timeElapsed.TotalDays >= 1 && timeElapsed.TotalDays < 7) translateKey = "study:1d";
                else if (timeElapsed.TotalDays >= 7 && timeElapsed.TotalDays < 31) translateKey = "study:7d";
                else if (timeElapsed.TotalDays >= 31 && timeElapsed.TotalDays < 364) translateKey = "study:1mn";
                else if (timeElapsed.TotalDays >= 364) translateKey = "study:1y";
                else translateKey = "study:default";
            }
            else if (afkType == "poop")
            {
                if (timeElapsed.TotalMinutes >= 1 && timeElapsed.TotalHours < 1) translateKey = "poop:1m";
                else if (timeElapsed.TotalHours >= 1 && timeElapsed.TotalHours < 8) translateKey = "poop:1h";
                else if (timeElapsed.TotalHours >= 8) translateKey = "poop:8h";
                else translateKey = "poop:default";
            }
            else if (afkType == "shower")
            {
                if (timeElapsed.TotalMinutes >= 1 && timeElapsed.TotalMinutes < 10) translateKey = "shower:1m";
                else if (timeElapsed.TotalMinutes >= 10 && timeElapsed.TotalHours < 1) translateKey = "shower:10m";
                else if (timeElapsed.TotalHours >= 1 && timeElapsed.TotalHours < 8) translateKey = "shower:1h";
                else if (timeElapsed.TotalHours >= 8) translateKey = "shower:8h";
                else translateKey = "shower:default";
            }
            string text = TranslationManager.GetTranslation(language, "afk:" + translateKey, RoomID, platform); // FIX AA0
            UsersData.Save(UserID, "lastFromAfkResume", DateTime.UtcNow, platform);
            UsersData.Save(UserID, "isAfk", false, platform);

            if (platform.Equals(Platforms.Twitch))
                TwitchReply(channel, RoomID, text.Replace("%user%", username) + send + " (" + Text.FormatTimeSpan(timeElapsed, language) + ")", message_id, language, true);
            if (platform.Equals(Platforms.Telegram))
                TelegramReply(channel, message_reply.Chat.Id, text.Replace("%user%", username) + send + " (" + Text.FormatTimeSpan(timeElapsed, language) + ")", message_reply, language);
        }

        /// <summary>
        /// Sends a message to Twitch chat with automatic message splitting for long content.
        /// </summary>
        /// <param name="channel">Target Twitch channel</param>
        /// <param name="message">Message content to send</param>
        /// <param name="channelID">Channel identifier for banword checks</param>
        /// <param name="messageID">Message ID for reply context</param>
        /// <param name="lang">Language for error messages</param>
        /// <param name="isSafeEx">Bypass banword check if true</param>
        /// <param name="asciiClean">Clean ASCII characters if true</param>
        /// <remarks>
        /// - Automatically splits messages over 500 characters
        /// - Handles Twitch message length limits (1500 chars)
        /// - Includes safety checks for banned words
        /// - Automatically joins channel if not already joined
        /// </remarks>
        [ConsoleSector("butterBror.Utils.Tools.Chat", "TwitchSend")]
        public static void TwitchSend(string channel, string message, string channelID, string messageID, string lang, bool isSafeEx = false, bool asciiClean = true)
        {
            Engine.Statistics.FunctionsUsed.Add();
            try
            {
                //Write($"Twitch - A message was sent to the {channel} channel: {message}", "info");
                if (asciiClean)
                {
                    message = Text.CleanAscii(message);
                }

                if (message.Length > 1500)
                    message = TranslationManager.GetTranslation(lang, "error:too_large_text", channelID, Platforms.Twitch);
                else if (message.Length > 500)
                {
                    int splitIndex = message.LastIndexOf(' ', 450);
                    string part2 = string.Concat("... ", message.AsSpan(splitIndex));

                    message = string.Concat(message.AsSpan(0, splitIndex), "...");

                    Task task = Task.Run(() =>
                    {
                        Thread.Sleep(1000);
                        TwitchSend(channel, channelID, part2, messageID, lang, isSafeEx);
                    });
                }

                if (!Engine.Bot.Clients.Twitch.JoinedChannels.Contains(new JoinedChannel(channel)))
                    Engine.Bot.Clients.Twitch.JoinChannel(channel);

                if (isSafeEx || new NoBanwords().Check(message, channelID, Platforms.Twitch))
                    Engine.Bot.Clients.Twitch.SendMessage(channel, message);
                else
                    Engine.Bot.Clients.Twitch.SendReply(channel, messageID, TranslationManager.GetTranslation(lang, "error:message_could_not_be_sent", channelID, Platforms.Twitch));
            }
            catch (Exception ex)
            {
                Write(ex);
            }
        }

        /// <summary>
        /// Sends a reply message to Twitch chat with message threading support.
        /// </summary>
        /// <param name="channel">Target Twitch channel</param>
        /// <param name="channelID">Channel identifier for banword checks</param>
        /// <param name="message">Reply message content</param>
        /// <param name="messageID">Message ID to reply to</param>
        /// <param name="lang">Language for error messages</param>
        /// <param name="isSafeEx">Bypass banword check if true</param>
        /// <remarks>
        /// - Automatically splits long messages
        /// - Maintains message threading through message ID
        /// - Handles Twitch reply limitations
        /// - Includes ASCII character cleaning
        /// </remarks>
        [ConsoleSector("butterBror.Utils.Tools.Chat", "TwitchReply")]
        public static void TwitchReply(string channel, string channelID, string message, string messageID, string lang, bool isSafeEx = false)
        {
            Engine.Statistics.FunctionsUsed.Add();
            try
            {
                //Write($"Twitch - A response to a message was sent to the {channel} channel: {message}", "info");
                message = Text.CleanAscii(message);

                if (message.Length > 1500)
                    message = TranslationManager.GetTranslation(lang, "error:too_large_text", channelID, Platforms.Twitch);
                else if (message.Length > 500)
                {
                    int splitIndex = message.LastIndexOf(' ', 450);
                    string part2 = string.Concat("... ", message.AsSpan(splitIndex));

                    message = string.Concat(message.AsSpan(0, splitIndex), "...");

                    Task task = Task.Run(() =>
                    {
                        Thread.Sleep(1000);
                        TwitchReply(channel, channelID, part2, messageID, lang, isSafeEx);
                    });
                }

                if (!Engine.Bot.Clients.Twitch.JoinedChannels.Contains(new JoinedChannel(channel)))
                    Engine.Bot.Clients.Twitch.JoinChannel(channel);

                if (isSafeEx || new NoBanwords().Check(message, channelID, Platforms.Twitch))
                    Engine.Bot.Clients.Twitch.SendReply(channel, messageID, message);
                else
                    Engine.Bot.Clients.Twitch.SendReply(channel, messageID, TranslationManager.GetTranslation(lang, "error:message_could_not_be_sent", channelID, Platforms.Twitch));
            }
            catch (Exception ex)
            {
                Write(ex);
            }
        }

        /// <summary>
        /// Sends a reply message to Telegram chat with message threading support.
        /// </summary>
        /// <param name="channel">Target Telegram chat name</param>
        /// <param name="channelID">Telegram chat ID</param>
        /// <param name="message">Message content to send</param>
        /// <param name="messageReply">Original message for reply context</param>
        /// <param name="lang">Language for error messages</param>
        /// <param name="isSafeEx">Bypass banword check if true</param>
        /// <remarks>
        /// - Handles Telegram message length limit (4096 chars)
        /// - Maintains message threading through message ID
        /// - Includes safety checks for banned words
        /// - Supports message replies in Telegram
        /// </remarks>
        [ConsoleSector("butterBror.Utils.Tools.Chat", "TelegramReply")]
        public static void TelegramReply(string channel, long channelID, string message, Telegram.Bot.Types.Message messageReply, string lang, bool isSafeEx = false)
        {
            Engine.Statistics.FunctionsUsed.Add();
            try
            {
                Write($"Telegram - A message was sent to {channel}: {message}", "info");

                if (message.Length > 4096)
                    message = TranslationManager.GetTranslation(lang, "error:too_large_text", channelID.ToString(), Platforms.Telegram);

                if (isSafeEx || new NoBanwords().Check(message, channelID.ToString(), Platforms.Telegram))
                    Engine.Bot.Clients.Telegram.SendMessage(
                        channelID,
                        message,
                        replyParameters: messageReply.Id
                    );
                else
                    Engine.Bot.Clients.Telegram.SendMessage(
                        channelID,
                        TranslationManager.GetTranslation(lang, "error:message_could_not_be_sent", channelID.ToString(), Platforms.Telegram),
                        replyParameters: messageReply.Id
                    );
            }
            catch (Exception ex)
            {
                Write(ex);
            }
        }

        /// <summary>
        /// Sends a platform-agnostic reply message across supported platforms.
        /// </summary>
        /// <param name="platform">Target platform (Twitch/Discord/Telegram)</param>
        /// <param name="channel">Target channel name</param>
        /// <param name="channelID">Channel identifier for banword checks</param>
        /// <param name="message">Message content to send</param>
        /// <param name="language">Language for content</param>
        /// <param name="username">Username to mention</param>
        /// <param name="userID">User identifier</param>
        /// <param name="server">Server name (for Discord)</param>
        /// <param name="serverID">Server identifier (for Discord)</param>
        /// <param name="messageID">Message ID for reply context</param>
        /// <param name="messageReply">Telegram message context for replies</param>
        /// <param name="isSafe">Bypass banword check if true</param>
        /// <param name="usernameColor">Color preference for Twitch messages</param>
        /// <remarks>
        /// - Delegates to platform-specific implementation
        /// - Handles message routing across multiple platforms
        /// - Maintains consistent message formatting across platforms
        /// - Supports ephemeral messages and embeds for Discord
        /// </remarks>
        [ConsoleSector("butterBror.Utils.Tools.Chat", "SendReply")]
        public static void SendReply(Platforms platform, string channel, string channelID, string message, string language, string username, string userID, string server, string serverID, string messageID, Telegram.Bot.Types.Message messageReply, bool isSafe = false, ChatColorPresets usernameColor = ChatColorPresets.YellowGreen)
        {
            switch (platform)
            {
                case Platforms.Twitch:
                    Commands.SendCommandReply(new TwitchMessageSendData
                    {
                        Message = message,
                        Channel = channel,
                        ChannelID = channelID,
                        MessageID = messageID,
                        Language = language,
                        Username = username,
                        SafeExecute = isSafe,
                        UsernameColor = usernameColor
                    });
                    break;
                case Platforms.Discord:
                    Commands.SendCommandReply(new DiscordCommandSendData
                    {
                        Message = message,
                        Title = "",
                        Description = "",
                        EmbedColor = Discord.Color.Green,
                        IsEmbed = false,
                        IsEphemeral = false,
                        Server = server,
                        ServerID = serverID,
                        Language = language,
                        SafeExecute = isSafe,
                        SocketCommandBase = null,
                        Author = "",
                        ImageLink = "",
                        ThumbnailLink = "",
                        Footer = "",
                        ChannelID = channelID,
                        UserID = userID
                    });
                    break;
                case Platforms.Telegram:
                    Commands.SendCommandReply(new TelegramMessageSendData
                    {
                        Message = message,
                        Language = language,
                        SafeExecute = isSafe,
                        Channel = channel,
                        ChannelID = channelID,
                        MessageID = messageID,
                        Username = username
                    });
                    break;
            }
        }
    }
}
