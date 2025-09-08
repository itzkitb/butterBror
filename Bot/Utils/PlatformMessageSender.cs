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
    /// </list>
    /// </para>
    /// All methods are designed to be thread-safe and handle high-frequency chat operations.
    /// </remarks>
    public class PlatformMessageSender
    {
        /// <summary>
        /// Sends a message to a Twitch channel with automatic handling of Twitch's message constraints.
        /// </summary>
        /// <param name="channel">Target Twitch channel name (without # prefix)</param>
        /// <param name="message">Content to send to the channel</param>
        /// <param name="channelID">Channel identifier for banword filtering</param>
        /// <param name="messageID">Message ID for reply context (when applicable)</param>
        /// <param name="lang">Language code for error messages</param>
        /// <param name="isSafeEx">Bypass banword checks when <see langword="true"/></param>
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
        public static void TwitchSend(string channel, string message, string channelID, string messageID, string lang, bool isSafeEx = false, bool asciiClean = true)
        {
            try
            {
                //Write($"Twitch - A message was sent to the {channel} channel: {message}", "info");
                if (asciiClean)
                {
                    message = TextSanitizer.CleanAscii(message);
                }

                if (message.Length > 1500)
                    message = LocalizationService.GetString(lang, "error:too_large_text", channelID, PlatformsEnum.Twitch);
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

                if (!Bot.Clients.Twitch.JoinedChannels.Contains(new JoinedChannel(channel)))
                    Bot.Clients.Twitch.JoinChannel(channel);

                if (isSafeEx || new BlockedWordDetector().Check(message, channelID, PlatformsEnum.Twitch))
                    Bot.Clients.Twitch.SendMessage(channel, message);
                else
                    Bot.Clients.Twitch.SendReply(
                        channel,
                        messageID,
                        LocalizationService.GetString(lang, "error:message_could_not_be_sent", channelID, PlatformsEnum.Twitch));
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
                //Write($"Twitch - A response to a message was sent to the {channel} channel: {message}", "info");
                message = TextSanitizer.CleanAscii(message);

                if (message.Length > 1500)
                    message = LocalizationService.GetString(lang, "error:too_large_text", channelID, PlatformsEnum.Twitch);
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

                if (!Bot.Clients.Twitch.JoinedChannels.Contains(new JoinedChannel(channel)))
                    Bot.Clients.Twitch.JoinChannel(channel);

                if (isSafeEx || new BlockedWordDetector().Check(message, channelID, PlatformsEnum.Twitch))
                    Bot.Clients.Twitch.SendReply(channel, messageID, message);
                else
                    Bot.Clients.Twitch.SendReply(
                        channel,
                        messageID,
                        LocalizationService.GetString(lang, "error:message_could_not_be_sent", channelID, PlatformsEnum.Twitch));
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
    }
}
