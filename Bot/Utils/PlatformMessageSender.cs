using bb.Core.Bot.SQLColumnNames;
using bb.Core.Commands;
using bb.Models;
using System.Globalization;
using Telegram.Bot;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Models;
using static bb.Core.Bot.Console;

namespace bb.Utils
{
    /// <summary>
    /// Centralized utility class for handling chat operations across multiple streaming platforms.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class provides platform-agnostic messaging capabilities with:
    /// <list type="bullet">
    /// <item>AFK status management and return notifications</item>
    /// <item>Localized message generation with translation support</item>
    /// <item>Platform-specific message formatting and constraints handling</item>
    /// <item>Content safety checks through banned word filtering</item>
    /// <item>Automatic message splitting for length compliance</item>
    /// <item>Full async support for non-blocking operations</item>
    /// </list>
    /// </para>
    /// <para>
    /// Key features:
    /// <list type="bullet">
    /// <item>Supports Twitch, Discord, and Telegram platforms</item>
    /// <item>Integrates with localization system for multilingual support</item>
    /// <item>Handles message threading and reply context preservation</item>
    /// <item>Manages ASCII character normalization for consistent rendering</item>
    /// <item>Provides safety mechanisms against banned content</item>
    /// <item>Optimized for high-frequency chat operations with async/await</item>
    /// </list>
    /// </para>
    /// All methods are designed to be thread-safe and handle high-frequency chat operations.
    /// </remarks>
    public class PlatformMessageSender
    {
        /// <summary>
        /// Maximum allowed message length before splitting
        /// </summary>
        private const int MaxMessageLength = 500;

        /// <summary>
        /// Maximum allowed message length before truncation
        /// </summary>
        private const int MaxTruncateLength = 1500;

        /// <summary>
        /// Maximum length for first part of split message
        /// </summary>
        private const int FirstPartMaxLength = 450;

        /// <summary>
        /// Delay between split message parts (milliseconds)
        /// </summary>
        private const int SplitMessageDelay = 1000;

        /// <summary>
        /// Sends a message to a Twitch channel with automatic handling of Twitch's message constraints.
        /// </summary>
        /// <param name="channel">Target Twitch channel name (without # prefix)</param>
        /// <param name="message">Content to send to the channel</param>
        /// <param name="channelID">Channel identifier for banword filtering</param>
        /// <param name="messageID">Message ID for reply context (when applicable)</param>
        /// <param name="lang">Language code for error messages</param>
        /// <param name="isSafe">Bypass banword checks when <see langword="true"/></param>
        /// <param name="asciiClean">Apply ASCII normalization when <see langword="true"/></param>
        /// <remarks>
        /// <para>
        /// Message handling workflow:
        /// <list type="number">
        /// <item>Normalizes ASCII characters when requested</item>
        /// <item>Verifies message length compliance (Twitch limit: 500 characters)</item>
        /// <item>Automatically splits messages exceeding 450 characters</item>
        /// <item>Performs banned word filtering before sending</item>
        /// <item>Joins channel if not already connected</item>
        /// <item>Sends message through Twitch client</item>
        /// </list>
        /// </para>
        /// <para>
        /// Length handling specifics:
        /// <list type="bullet">
        /// <item>Messages >1500 characters: Replaced with "text too large" error</item>
        /// <item>Messages >500 characters: Automatically split at word boundary</item>
        /// <item>Split messages include continuation ellipses (...)</item>
        /// <item>Second part delayed by 1 second to avoid rate limiting</item>
        /// </list>
        /// </para>
        /// <para>
        /// Error handling:
        /// <list type="bullet">
        /// <item>Failed banword checks trigger "message could not be sent" response</item>
        /// <item>All exceptions are logged but don't interrupt program flow</item>
        /// <item>Channel auto-joining ensures message delivery even if disconnected</item>
        /// </list>
        /// </para>
        /// This method is optimized for high-frequency command responses in Twitch chat environments.
        /// </remarks>
        public static void TwitchSend(string channel, string message, string channelID, string messageID, string lang, bool isSafe = false, bool asciiClean = true)
        {
            try
            {
                if (asciiClean)
                {
                    message = TextSanitizer.CleanAscii(message);
                }

                var (cleanedMessage, secondPart) = SplitMessage(message, lang, PlatformsEnum.Twitch, channelID);

                if (!string.IsNullOrEmpty(secondPart))
                {
                    SendTwitchMessage(channel, cleanedMessage, channelID, messageID, lang, isSafe);
                    Task.Delay(SplitMessageDelay).Wait();
                    SendTwitchMessage(channel, secondPart, channelID, messageID, lang, isSafe);
                }
                else
                {
                    SendTwitchMessage(channel, cleanedMessage, channelID, messageID, lang, isSafe);
                }
            }
            catch (Exception ex)
            {
                Write(ex);
            }
        }

        /// <summary>
        /// Sends a threaded reply to a specific Twitch message while maintaining conversation context.
        /// </summary>
        /// <param name="channel">Target Twitch channel name (without # prefix)</param>
        /// <param name="channelID">Channel identifier for banword filtering</param>
        /// <param name="message">Reply content to send</param>
        /// <param name="messageID">ID of the message to reply to</param>
        /// <param name="lang">Language code for error messages</param>
        /// <param name="isSafeEx">Bypass banword checks when <see langword="true"/></param>
        /// <remarks>
        /// <para>
        /// Key differences from standard message sending:
        /// <list type="bullet">
        /// <item>Maintains message threading through parent message ID</item>
        /// <item>Uses Twitch's reply protocol for conversation context</item>
        /// <item>Preserves original message context in chat UI</item>
        /// <item>Includes @mention of original sender automatically</item>
        /// </list>
        /// </para>
        /// <para>
        /// Implementation details:
        /// <list type="bullet">
        /// <item>Applies ASCII normalization to ensure clean rendering</item>
        /// <item>Handles message splitting identical to <see cref="TwitchSend"/></item>
        /// <item>Verifies channel connection before sending</item>
        /// <item>Provides appropriate error feedback for banword violations</item>
        /// </list>
        /// </para>
        /// <para>
        /// Usage considerations:
        /// <list type="bullet">
        /// <item>Requires Twitch message ID from original message</item>
        /// <item>Best practice for command responses and user interactions</item>
        /// <item>Maintains better conversation flow than standard messages</item>
        /// <item>Respects Twitch's conversation threading UI features</item>
        /// </list>
        /// </para>
        /// This method should be preferred over <see cref="TwitchSend"/> when responding to specific messages.
        /// </remarks>
        public static void TwitchReply(string channel, string channelID, string message, string messageID, string lang, bool isSafeEx = false)
        {
            try
            {
                message = TextSanitizer.CleanAscii(message);

                var (cleanedMessage, secondPart) = SplitMessage(message, lang, PlatformsEnum.Twitch, channelID);

                if (!string.IsNullOrEmpty(secondPart))
                {
                    SendTwitchReply(channel, channelID, cleanedMessage, messageID, lang, isSafeEx);
                    Task.Delay(SplitMessageDelay).Wait();
                    SendTwitchReply(channel, channelID, secondPart, messageID, lang, isSafeEx);
                }
                else
                {
                    SendTwitchReply(channel, channelID, cleanedMessage, messageID, lang, isSafeEx);
                }
            }
            catch (Exception ex)
            {
                Write(ex);
            }
        }

        /// <summary>
        /// Platform-agnostic method for sending reply messages across all supported platforms.
        /// </summary>
        /// <param name="platform">Target platform (Twitch, Discord, or Telegram)</param>
        /// <param name="channel">Display name of the target channel</param>
        /// <param name="channelID">Platform-specific channel identifier</param>
        /// <param name="message">Content to send as a reply</param>
        /// <param name="language">Language code for message localization</param>
        /// <param name="username">Username to mention in the response</param>
        /// <param name="userID">User identifier for data operations</param>
        /// <param name="server">Server/guild name (Discord-specific)</param>
        /// <param name="serverID">Server/guild identifier (Discord-specific)</param>
        /// <param name="messageID">Platform-specific message ID for reply context</param>
        /// <param name="messageReply">Telegram message object for reply context</param>
        /// <param name="isSafe">Bypass banword checks when <see langword="true"/></param>
        /// <param name="usernameColor">Color preference for Twitch username mentions</param>
        /// <param name="isReply">Treat as reply rather than new message when <see langword="true"/></param>
        /// <remarks>
        /// <para>
        /// Routing behavior:
        /// <list type="table">
        /// <item><term>Twitch</term><description>Delegates to <see cref="Sender.SendCommandReply(TwitchMessageSendData, bool)"/></description></item>
        /// <item><term>Discord</term><description>Delegates to <see cref="Sender.SendCommandReply(DiscordCommandSendData)"/></description></item>
        /// <item><term>Telegram</term><description>Delegates to <see cref="Sender.SendCommandReply(TelegramMessageSendData, bool)"/></description></item>
        /// </list>
        /// </para>
        /// <para>
        /// Platform-specific features:
        /// <list type="bullet">
        /// <item><b>Twitch:</b> Supports colored username mentions and message threading</item>
        /// <item><b>Discord:</b> Supports embeds, ephemeral messages, and rich formatting</item>
        /// <item><b>Telegram:</b> Maintains message threading through reply context</item>
        /// </list>
        /// </para>
        /// <para>
        /// Key advantages:
        /// <list type="bullet">
        /// <item>Abstracts platform-specific implementation details</item>
        /// <item>Ensures consistent message formatting across platforms</item>
        /// <item>Centralizes banword filtering and safety checks</item>
        /// <item>Provides unified interface for command response handling</item>
        /// </list>
        /// </para>
        /// This method serves as the primary interface for command responses and user interactions across all platforms.
        /// </remarks>
        public static void SendReply(PlatformsEnum platform, string channel, string channelID, string message, string language, string username, string userID, string server, string serverID, string messageID, Telegram.Bot.Types.Message messageReply, bool isSafe = false, ChatColorPresets usernameColor = ChatColorPresets.YellowGreen, bool isReply = true, bool addUsername = true)
        {
            try
            {
                switch (platform)
                {
                    case PlatformsEnum.Twitch:
                        Sender.SendCommandReply(new TwitchMessageSendData
                        {
                            Message = message,
                            Channel = channel,
                            ChannelID = channelID,
                            MessageID = messageID,
                            Language = language,
                            Username = username,
                            SafeExecute = isSafe,
                            UsernameColor = usernameColor
                        }, isReply, addUsername);
                        break;
                    case PlatformsEnum.Discord:
                        Sender.SendCommandReply(new DiscordCommandSendData
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
                    case PlatformsEnum.Telegram:
                        Sender.SendCommandReply(new TelegramMessageSendData
                        {
                            Message = message,
                            Language = language,
                            SafeExecute = isSafe,
                            Channel = channel,
                            ChannelID = channelID,
                            MessageID = messageID,
                            Username = username
                        }, isReply, addUsername);
                        break;
                }
            }
            catch (Exception ex)
            {
                Write(ex);
            }
        }

        /// <summary>
        /// Asynchronous version of TwitchSend that handles message sending without blocking the calling thread.
        /// </summary>
        /// <param name="channel">Target Twitch channel name (without # prefix)</param>
        /// <param name="message">Content to send to the channel</param>
        /// <param name="channelID">Channel identifier for banword filtering</param>
        /// <param name="messageID">Message ID for reply context (when applicable)</param>
        /// <param name="lang">Language code for error messages</param>
        /// <param name="isSafe">Bypass banword checks when <see langword="true"/></param>
        /// <param name="asciiClean">Apply ASCII normalization when <see langword="true"/></param>
        /// <returns>Task representing the asynchronous operation</returns>
        public static async Task TwitchSendAsync(string channel, string message, string channelID, string messageID, string lang, bool isSafe = false, bool asciiClean = true)
        {
            try
            {
                if (asciiClean)
                {
                    message = TextSanitizer.CleanAscii(message);
                }

                var (cleanedMessage, secondPart) = SplitMessage(message, lang, PlatformsEnum.Twitch, channelID);

                if (!string.IsNullOrEmpty(secondPart))
                {
                    await SendTwitchMessageAsync(channel, cleanedMessage, channelID, messageID, lang, isSafe);
                    await Task.Delay(SplitMessageDelay);
                    await SendTwitchMessageAsync(channel, secondPart, channelID, messageID, lang, isSafe);
                }
                else
                {
                    await SendTwitchMessageAsync(channel, cleanedMessage, channelID, messageID, lang, isSafe);
                }
            }
            catch (Exception ex)
            {
                Write(ex);
            }
        }

        /// <summary>
        /// Asynchronous version of TwitchReply that handles message sending without blocking the calling thread.
        /// </summary>
        /// <param name="channel">Target Twitch channel name (without # prefix)</param>
        /// <param name="channelID">Channel identifier for banword filtering</param>
        /// <param name="message">Reply content to send</param>
        /// <param name="messageID">ID of the message to reply to</param>
        /// <param name="lang">Language code for error messages</param>
        /// <param name="isSafeEx">Bypass banword checks when <see langword="true"/></param>
        /// <returns>Task representing the asynchronous operation</returns>
        public static async Task TwitchReplyAsync(string channel, string channelID, string message, string messageID, string lang, bool isSafeEx = false)
        {
            try
            {
                message = TextSanitizer.CleanAscii(message);

                var (cleanedMessage, secondPart) = SplitMessage(message, lang, PlatformsEnum.Twitch, channelID);

                if (!string.IsNullOrEmpty(secondPart))
                {
                    await SendTwitchReplyAsync(channel, channelID, cleanedMessage, messageID, lang, isSafeEx);
                    await Task.Delay(SplitMessageDelay);
                    await SendTwitchReplyAsync(channel, channelID, secondPart, messageID, lang, isSafeEx);
                }
                else
                {
                    await SendTwitchReplyAsync(channel, channelID, cleanedMessage, messageID, lang, isSafeEx);
                }
            }
            catch (Exception ex)
            {
                Write(ex);
            }
        }

        /// <summary>
        /// Asynchronous platform-agnostic method for sending reply messages across all supported platforms.
        /// </summary>
        /// <param name="platform">Target platform (Twitch, Discord, or Telegram)</param>
        /// <param name="channel">Display name of the target channel</param>
        /// <param name="channelID">Platform-specific channel identifier</param>
        /// <param name="message">Content to send as a reply</param>
        /// <param name="language">Language code for message localization</param>
        /// <param name="username">Username to mention in the response</param>
        /// <param name="userID">User identifier for data operations</param>
        /// <param name="server">Server/guild name (Discord-specific)</param>
        /// <param name="serverID">Server/guild identifier (Discord-specific)</param>
        /// <param name="messageID">Platform-specific message ID for reply context</param>
        /// <param name="messageReply">Telegram message object for reply context</param>
        /// <param name="isSafe">Bypass banword checks when <see langword="true"/></param>
        /// <param name="usernameColor">Color preference for Twitch username mentions</param>
        /// <param name="isReply">Treat as reply rather than new message when <see langword="true"/></param>
        /// <returns>Task representing the asynchronous operation</returns>
        public static async Task SendReplyAsync(PlatformsEnum platform, string channel, string channelID, string message, string language, string username, string userID, string server, string serverID, string messageID, Telegram.Bot.Types.Message messageReply, bool isSafe = false, ChatColorPresets usernameColor = ChatColorPresets.YellowGreen, bool isReply = true, bool addUsername = true)
        {
            try
            {
                switch (platform)
                {
                    case PlatformsEnum.Twitch:
                        Sender.SendCommandReply(new TwitchMessageSendData
                        {
                            Message = message,
                            Channel = channel,
                            ChannelID = channelID,
                            MessageID = messageID,
                            Language = language,
                            Username = username,
                            SafeExecute = isSafe,
                            UsernameColor = usernameColor
                        }, isReply, addUsername);
                        break;
                    case PlatformsEnum.Discord:
                        Sender.SendCommandReply(new DiscordCommandSendData
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
                    case PlatformsEnum.Telegram:
                        Sender.SendCommandReply(new TelegramMessageSendData
                        {
                            Message = message,
                            Language = language,
                            SafeExecute = isSafe,
                            Channel = channel,
                            ChannelID = channelID,
                            MessageID = messageID,
                            Username = username
                        }, isReply, addUsername);
                        break;
                }
            }
            catch (Exception ex)
            {
                Write(ex);
            }
        }

        /// <summary>
        /// Splits a message into two parts if it exceeds the maximum length.
        /// </summary>
        /// <param name="message">Message to split</param>
        /// <returns>Tuple containing first part and second part (or null if no split needed)</returns>
        private static (string, string?) SplitMessage(string message, string language, PlatformsEnum platform, string channelId)
        {
            if (message.Length <= MaxMessageLength)
                return (message, null);

            if (message.Length > MaxTruncateLength)
                return (LocalizationService.GetString(language, "error:too_large_text", channelId, platform), null);

            int splitIndex = message.LastIndexOf(' ', FirstPartMaxLength);
            if (splitIndex <= 0)
                splitIndex = FirstPartMaxLength;

            string part1 = message.Substring(0, splitIndex) + "...";
            string part2 = "... " + message.Substring(splitIndex);

            return (part1, part2);
        }

        /// <summary>
        /// Sends a Twitch message with proper channel joining and banword checking.
        /// </summary>
        /// <param name="channel">Target channel name</param>
        /// <param name="message">Message content</param>
        /// <param name="channelID">Channel identifier</param>
        /// <param name="messageID">Message ID for reply context</param>
        /// <param name="lang">Language code</param>
        /// <param name="isSafe">Bypass banword checks</param>
        private static void SendTwitchMessage(string channel, string message, string channelID, string messageID, string lang, bool isSafe)
        {
            if (!Bot.Clients.Twitch.JoinedChannels.Contains(new JoinedChannel(channel)))
                Bot.Clients.Twitch.JoinChannel(channel);

            if (isSafe || new BlockedWordDetector().Check(message, channelID, PlatformsEnum.Twitch))
            {
                if (!string.IsNullOrEmpty(messageID))
                    Bot.Clients.Twitch.SendReply(channel, messageID, message);
                else
                    Bot.Clients.Twitch.SendMessage(channel, message);
            }
            else
            {
                string errorMessage = LocalizationService.GetString(lang, "error:message_could_not_be_sent", channelID, PlatformsEnum.Twitch);
                if (!string.IsNullOrEmpty(messageID))
                    Bot.Clients.Twitch.SendReply(channel, messageID, errorMessage);
                else
                    Bot.Clients.Twitch.SendMessage(channel, errorMessage);
            }
        }

        /// <summary>
        /// Sends a Twitch reply with proper channel joining and banword checking.
        /// </summary>
        /// <param name="channel">Target channel name</param>
        /// <param name="channelID">Channel identifier</param>
        /// <param name="message">Message content</param>
        /// <param name="messageID">Message ID to reply to</param>
        /// <param name="lang">Language code</param>
        /// <param name="isSafeEx">Bypass banword checks</param>
        private static void SendTwitchReply(string channel, string channelID, string message, string messageID, string lang, bool isSafeEx)
        {
            if (!Bot.Clients.Twitch.JoinedChannels.Contains(new JoinedChannel(channel)))
                Bot.Clients.Twitch.JoinChannel(channel);

            if (isSafeEx || new BlockedWordDetector().Check(message, channelID, PlatformsEnum.Twitch))
                Bot.Clients.Twitch.SendReply(channel, messageID, message);
            else
                Bot.Clients.Twitch.SendReply(channel, messageID, LocalizationService.GetString(lang, "error:message_could_not_be_sent", channelID, PlatformsEnum.Twitch));
        }

        /// <summary>
        /// Asynchronously sends a Twitch message with proper channel joining and banword checking.
        /// </summary>
        /// <param name="channel">Target channel name</param>
        /// <param name="message">Message content</param>
        /// <param name="channelID">Channel identifier</param>
        /// <param name="messageID">Message ID for reply context</param>
        /// <param name="lang">Language code</param>
        /// <param name="isSafe">Bypass banword checks</param>
        /// <returns>Task representing the asynchronous operation</returns>
        private static async Task SendTwitchMessageAsync(string channel, string message, string channelID, string messageID, string lang, bool isSafe)
        {
            if (!Bot.Clients.Twitch.JoinedChannels.Contains(new JoinedChannel(channel)))
                Bot.Clients.Twitch.JoinChannel(channel);

            if (isSafe || new BlockedWordDetector().Check(message, channelID, PlatformsEnum.Twitch))
            {
                if (!string.IsNullOrEmpty(messageID))
                    Bot.Clients.Twitch.SendReply(channel, messageID, message);
                else
                    Bot.Clients.Twitch.SendMessage(channel, message);
            }
            else
            {
                string errorMessage = LocalizationService.GetString(lang, "error:message_could_not_be_sent", channelID, PlatformsEnum.Twitch);
                if (!string.IsNullOrEmpty(messageID))
                    Bot.Clients.Twitch.SendReply(channel, messageID, errorMessage);
                else
                    Bot.Clients.Twitch.SendMessage(channel, errorMessage);
            }
        }

        /// <summary>
        /// Asynchronously sends a Twitch reply with proper channel joining and banword checking.
        /// </summary>
        /// <param name="channel">Target channel name</param>
        /// <param name="channelID">Channel identifier</param>
        /// <param name="message">Message content</param>
        /// <param name="messageID">Message ID to reply to</param>
        /// <param name="lang">Language code</param>
        /// <param name="isSafeEx">Bypass banword checks</param>
        /// <returns>Task representing the asynchronous operation</returns>
        private static async Task SendTwitchReplyAsync(string channel, string channelID, string message, string messageID, string lang, bool isSafeEx)
        {
            if (!Bot.Clients.Twitch.JoinedChannels.Contains(new JoinedChannel(channel)))
                Bot.Clients.Twitch.JoinChannel(channel);

            if (isSafeEx || new BlockedWordDetector().Check(message, channelID, PlatformsEnum.Twitch))
                Bot.Clients.Twitch.SendReply(channel, messageID, message);
            else
                Bot.Clients.Twitch.SendReply(channel, messageID, LocalizationService.GetString(lang, "error:message_could_not_be_sent", channelID, PlatformsEnum.Twitch));
        }
    }
}
