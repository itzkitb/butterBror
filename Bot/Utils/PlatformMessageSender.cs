using bb.Core.Commands;
using bb.Core.Commands.List;
using bb.Models.Platform;
using bb.Models.SevenTVLib;
using bb.Services.Platform.Discord;
using bb.Services.Platform.Telegram;
using bb.Services.Platform.Twitch;
using Discord;
using Jint.Runtime;
using System.Globalization;
using Telegram.Bot;
using Telegram.Bot.Types;
using TwitchLib.Client;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Models;
using static bb.Core.Bot.Console;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
        /// Maximum allowed message length before splitting on twitch
        /// </summary>
        private const int MaxTwitchMessageLength = 500;

        /// <summary>
        /// Maximum allowed message length before splitting on twitch
        /// </summary>
        private const int MaxDiscordMessageLength = 4096;

        /// <summary>
        /// Maximum allowed message length before splitting on twitch
        /// </summary>
        private const int MaxTelegramMessageLength = 4096;

        /// <summary>
        /// Platform-agnostic method for sending reply messages across all supported platforms.
        /// </summary>
        /// <param name="platform">Target platform (Twitch, Discord, or Telegram)</param>
        /// <param name="channel">Display name of the target channel</param>
        /// <param name="channelId">Platform-specific channel identifier</param>
        /// <param name="message">Content to send as a reply</param>
        /// <param name="language">Language code for message localization</param>
        /// <param name="username">Username to mention in the response</param>
        /// <param name="userID">User identifier for data operations</param>
        /// <param name="server">Server/guild name (Discord-specific)</param>
        /// <param name="serverId">Server/guild identifier (Discord-specific)</param>
        /// <param name="messageId">Platform-specific message ID for reply context</param>
        /// <param name="messageReply">Telegram message object for reply context</param>
        /// <param name="isSafe">Bypass banword checks when <see langword="true"/></param>
        /// <param name="isReply">Treat as reply rather than new message when <see langword="true"/></param>
        /// <remarks>
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
        public void Send(PlatformsEnum platform, string message, string channel,
            string channelId = null, string language = "en-US", string username = null,
            string userID = null, string server = null, string serverId = null,
            string messageId = null, Telegram.Bot.Types.Message messageReply = null,
            bool isSafe = false, bool isReply = false, bool addUsername = false)
        {
            try
            {
                switch (platform)
                {
                    case PlatformsEnum.Twitch:
                        TwitchSend(message, language, channel, channelId, username, messageId, isSafe, isReply, addUsername);
                        break;
                    case PlatformsEnum.Discord:
                        DiscordSend(message, language, channelId, server, serverId, isSafe, userID);
                        break;
                    case PlatformsEnum.Telegram:
                        TelegramSend(message, language, channel, channelId, username, messageId, isSafe, isReply, addUsername);
                        break;
                }
            }
            catch (Exception ex)
            {
                Write(ex);
            }
        }

        private void TwitchSend(string message, string language, string channel, string channelId, string username, string messageId, bool safeExec, bool reply, bool addUsername)
        {
            if (message == null || bb.Program.BotInstance.Clients.Twitch == null) return;

            _ = Task.Run(async() =>
            {
                try
                {
                    string send = message;
                    send = TextSanitizer.CleanAscii(message);

                    bool check = safeExec || bb.Program.BotInstance.MessageFilter.Check(send, channelId, PlatformsEnum.Twitch).Item1;

                    if (send.Length > MaxTwitchMessageLength * 3)
                    {
                        send = LocalizationService.GetString(language, "error:too_large_text", channelId, PlatformsEnum.Twitch);
                    }
                    else if (send.Length > MaxTwitchMessageLength)
                    {
                        int splitIndex = send.LastIndexOf(' ', 450);
                        string part2 = string.Concat("... ", send.AsSpan(splitIndex));

                        send = string.Concat(send.AsSpan(0, splitIndex), "...");

                        _ = Task.Run(async () =>
                        {
                            await Task.Delay(1000);
                            bb.Program.BotInstance.MessageSender.Send(PlatformsEnum.Twitch, part2, channel, channelId, language, username, messageId: messageId, isSafe: safeExec);
                        });
                    }

                    if (!bb.Program.BotInstance.Clients.Twitch.JoinedChannels.Any(jc => string.Equals(jc.Channel, channel, StringComparison.OrdinalIgnoreCase)))
                    {
                        bb.Program.BotInstance.Clients.Twitch.JoinChannel(channel);
                        Write("Join channel.", LogLevel.Debug);
                        await Task.Delay(1100);
                    }

                    if (safeExec || check)
                    {
                        if (reply)
                        {
                            bb.Program.BotInstance.Clients.Twitch.SendReply(channel, messageId, send);
                        }
                        else
                        {
                            string ping = $"@{username}, ";
                            bb.Program.BotInstance.Clients.Twitch.SendMessage(new JoinedChannel(channel), (addUsername ? ping : string.Empty) + send);
                        }
                    }
                    else
                    {
                        bb.Program.BotInstance.Clients.Twitch.SendReply(
                            channel,
                            messageId,
                            LocalizationService.GetString(language, "error:message_could_not_be_sent", channelId, PlatformsEnum.Twitch));
                    }
                }
                catch (Exception ex)
                {
                    Write(ex);
                }
            });
        }

        private void DiscordSend(string message, string language, string channelId, string server, string serverId, bool safeExec, string userId)
        {
            if (message == null || bb.Program.BotInstance.Clients.Discord == null) return;

            _ = Task.Run(async () =>
            {
                try
                {
                    Write($"Discord: A message response was sent to the #{server}: {message}");
                    string send = message;

                    send = TextSanitizer.CleanAscii(send);

                    if (message.Length > MaxDiscordMessageLength)
                    {
                        send = LocalizationService.GetString(language, "error:too_large_text", channelId, PlatformsEnum.Telegram);
                    }

                    ITextChannel sender = await bb.Program.BotInstance.Clients.Discord.GetChannelAsync(ulong.Parse(channelId)) as ITextChannel;

                    if (safeExec || bb.Program.BotInstance.MessageFilter.Check(send, serverId, PlatformsEnum.Discord).Item1)
                    {
                        sender.SendMessageAsync($"<@{userId}> {send}");
                    }
                    else
                    {
                        var embed = new EmbedBuilder()
                            .WithTitle(LocalizationService.GetString(language, "error:message_could_not_be_sent", "", PlatformsEnum.Discord))
                            .WithColor(global::Discord.Color.Red)
                            .Build();
                        sender.SendMessageAsync($"<@{userId}> {send}", embed: embed);
                    }
                }
                catch (Exception ex)
                {
                    Write(ex);
                }
            });
        }

        private void TelegramSend(string message, string language, string channel, string channelId, string username, string messageId, bool safeExec, bool reply, bool addUsername)
        {
            if (message == null || bb.Program.BotInstance.Clients.Telegram == null) return;

            _ = Task.Run(async () =>
            {
                try
                {
                    string send = message;
                    send = TextSanitizer.CleanAscii(message);

                    bool check = safeExec || bb.Program.BotInstance.MessageFilter.Check(send, channelId, PlatformsEnum.Telegram).Item1;

                    if (send.Length > MaxTelegramMessageLength * 4)
                    {
                        send = LocalizationService.GetString(language, "error:too_large_text", channelId, PlatformsEnum.Telegram);
                    }
                    else if (send.Length > MaxTelegramMessageLength)
                    {
                        int splitIndex = send.LastIndexOf(' ', 450);
                        string part2 = string.Concat("... ", send.AsSpan(splitIndex));

                        send = string.Concat(send.AsSpan(0, splitIndex), "...");

                        _ = Task.Run(async () =>
                        {
                            await Task.Delay(1100);
                            bb.Program.BotInstance.MessageSender.Send(PlatformsEnum.Twitch, part2, channel, channelId, language, username, messageId: messageId, isSafe: safeExec);
                        });
                    }

                    if (safeExec || check)
                    {
                        if (reply)
                        {
                            await bb.Program.BotInstance.Clients.Telegram.SendMessage(long.Parse(channelId), send, replyParameters: int.Parse(messageId));
                        }
                        else
                        {
                            string ping = $"@{username}, ";
                            await bb.Program.BotInstance.Clients.Telegram.SendMessage(long.Parse(channelId), (addUsername ? ping : string.Empty) + send, replyParameters: int.Parse(messageId));
                        }
                    }
                    else
                    {
                        await bb.Program.BotInstance.Clients.Telegram.SendMessage(long.Parse(channelId), LocalizationService.GetString(language, "error:message_could_not_be_sent", channelId, PlatformsEnum.Telegram), replyParameters: int.Parse(messageId));
                    }
                }
                catch (Exception ex)
                {
                    Write(ex);
                }
            });
        }
    }
}
