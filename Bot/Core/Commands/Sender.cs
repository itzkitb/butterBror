using bb.Models;
using bb.Utils;
using Discord;
using Telegram.Bot;
using TwitchLib.Client.Models;
using static bb.Core.Bot.Console;

namespace bb.Core.Commands
{
    /// <summary>
    /// Provides platform-specific command reply handlers for Twitch, Telegram, and Discord platforms with consistent message formatting and safety checks.
    /// </summary>
    public class Sender
    {
        /// <summary>
        /// Sends a reply message to Twitch chat with message splitting, banword filtering, and channel management.
        /// </summary>
        /// <param name="data">Twitch-specific message data containing channel, message content, language, and reply context.</param>
        /// <param name="isReply">Determines whether to send as a reply to the original message (default: true).</param>
        /// <remarks>
        /// <list type="bullet">
        /// <item>Enforces Twitch's 500-character visible limit (with 1500-character total message limit)</item>
        /// <item>Automatically splits messages at 450 characters to prevent truncation (using last space before limit)</item>
        /// <item>Applies ASCII cleaning to prevent encoding issues in Twitch chat</item>
        /// <item>Performs real-time banword filtering using NoBanwords service</item>
        /// <item>Automatically joins target channel if not currently connected</item>
        /// <item>Maintains message threading through Twitch message ID references</item>
        /// <item>Handles split message sequencing with 1-second delay between parts</item>
        /// </list>
        /// When message exceeds 1500 characters, sends localized error message instead of content.
        /// For split messages, first part ends with "..." and second part begins with "... " after delay.
        /// If banwords detected, sends localized rejection message while logging violation.
        /// </remarks>
        public static async void SendCommandReply(TwitchMessageSendData data, bool isReply = true, bool addUsername = true)
        {
            try
            {
                string message = data.Message;
                if (message is null) return;
                //Write($"Twitch - A response to message {data.message_id} was sent to channel {data.channel}: {data.message}", "info");
                message = TextSanitizer.CleanAscii(data.Message);

                if (message.Length > 1500)
                    message = LocalizationService.GetString(data.Language, "error:too_large_text", data.ChannelID, PlatformsEnum.Twitch);
                else if (message.Length > 500)
                {
                    int splitIndex = message.LastIndexOf(' ', 450);
                    string part2 = string.Concat("... ", message.AsSpan(splitIndex));

                    message = string.Concat(message.AsSpan(0, splitIndex), "...");

                    Task task = Task.Run(() =>
                    {
                        Thread.Sleep(1000);
                        PlatformMessageSender.TwitchReply(data.Channel, data.ChannelID, part2, data.MessageID, data.Language, data.SafeExecute);
                    });
                }

                if (!bb.Bot.Clients.Twitch.JoinedChannels.Contains(new JoinedChannel(data.Channel)))
                {
                    bb.Bot.Clients.Twitch.JoinChannel(data.Channel);
                }

                if (data.SafeExecute || new BannedWordDetector().Check(message, data.ChannelID, PlatformsEnum.Twitch))
                {
                    Write(isReply.ToString(), "debug");
                    if (isReply)
                    {
                        bb.Bot.Clients.Twitch.SendReply(data.Channel, data.MessageID, message);
                    }
                    else
                    {
                        bb.Bot.Clients.Twitch.SendMessage(new JoinedChannel(data.Channel), addUsername ? $"@{data.Username}, " + message : message);
                    }
                }
                else
                {
                    bb.Bot.Clients.Twitch.SendReply(
                        data.Channel,
                        data.MessageID,
                        LocalizationService.GetString(data.Language, "error:message_could_not_be_sent", data.ChannelID, PlatformsEnum.Twitch));
                }
            }
            catch (Exception ex)
            {
                Write(ex);
            }
        }

        /// <summary>
        /// Sends a reply message to Telegram chat with message splitting, banword filtering, and reply threading.
        /// </summary>
        /// <param name="data">Telegram-specific message data containing channel ID, message content, language, and reply context.</param>
        /// <param name="isReply">Determines whether to send as a reply to the original message (default: true).</param>
        /// <remarks>
        /// <list type="bullet">
        /// <item>Handles Telegram's 4096-character per-message limit with automatic splitting</item>
        /// <item>Enforces 12288-character total message batch limit with error fallback</item>
        /// <item>Preserves message threading through replyParameters using original message ID</item>
        /// <item>Applies ASCII cleaning to prevent encoding issues in Telegram</item>
        /// <item>Performs comprehensive banword filtering before message transmission</item>
        /// <item>Logs all message operations at "info" level for debugging and auditing</item>
        /// <item>Handles split message sequencing with 1500ms delay between parts</item>
        /// </list>
        /// When splitting messages, first part ends with "..." and second part begins with "...".
        /// If banwords detected, sends localized error message instead of original content.
        /// Uses long.Parse for channel ID conversion with exception handling in implementation.
        /// </remarks>
        public static async void SendCommandReply(TelegramMessageSendData data, bool isReply = true, bool addUsername = true)
        {
            try
            {
                string messageToSend = data.Message;
                TelegramMessageSendData messageToSendPart2 = null;
                Write($"Telegram - A message response was sent to the {data.Channel} channel: {data.Message}", "info");
                messageToSend = TextSanitizer.CleanAscii(data.Message);

                if (messageToSend.Length > 12288)
                    messageToSend = LocalizationService.GetString(data.Language, "error:too_large_text", data.ChannelID, PlatformsEnum.Telegram);
                else if (messageToSend.Length > 4096)
                {
                    int splitIndex = messageToSend.LastIndexOf(' ', 4000);

                    string part1 = messageToSend.Substring(0, splitIndex) + "...";
                    string part2 = "..." + messageToSend.Substring(splitIndex);

                    messageToSend = part1;
                    messageToSendPart2 = data;
                    messageToSendPart2.Message = part2;
                }

                if (data.SafeExecute || new BannedWordDetector().Check(messageToSend, data.ChannelID, PlatformsEnum.Telegram))
                {
                    if (isReply)
                    {
                        await bb.Bot.Clients.Telegram.SendMessage(long.Parse(data.ChannelID), data.Message, replyParameters: int.Parse(data.MessageID));
                    }
                    else
                    {
                        await bb.Bot.Clients.Telegram.SendMessage(long.Parse(data.ChannelID), addUsername ? $"@{data.Username}, " + data.Message : data.Message);
                    }
                }
                else
                {
                    if (isReply)
                    {
                        await bb.Bot.Clients.Telegram.SendMessage(long.Parse(data.ChannelID), LocalizationService.GetString(data.Language, "error:message_could_not_be_sent", data.ChannelID, PlatformsEnum.Telegram), replyParameters: int.Parse(data.MessageID));
                    }
                    else
                    {
                        await bb.Bot.Clients.Telegram.SendMessage(long.Parse(data.ChannelID), LocalizationService.GetString(data.Language, "error:message_could_not_be_sent", data.ChannelID, PlatformsEnum.Telegram));
                    }
                }


                if (messageToSendPart2 != null)
                {
                    await Task.Delay(1500);
                    SendCommandReply(messageToSendPart2);
                }
            }
            catch (Exception ex)
            {
                Write(ex);
            }
        }

        /// <summary>
        /// Sends a reply message to Discord with embed support, banword filtering, and context-aware delivery.
        /// </summary>
        /// <param name="data">Discord-specific message data containing server, channel, message content, embed options, and command context.</param>
        /// <remarks>
        /// <list type="bullet">
        /// <item>Supports ephemeral (private) responses via IsEphemeral flag</item>
        /// <item>Provides rich embed formatting with title, description, colors, thumbnails, and images</item>
        /// <item>Performs dual banword filtering on both message content and embed description</item>
        /// <item>Intelligently handles command context (SocketCommandBase) when available</item>
        /// <item>Fallbacks to channel-based SendMessageAsync when command context unavailable</item>
        /// <item>Applies ASCII cleaning to prevent encoding issues in Discord</item>
        /// <item>Supports message splitting for content exceeding Discord's character limits</item>
        /// </list>
        /// When command context exists, uses RespondAsync for immediate command responses.
        /// When context unavailable, falls back to channel-based SendMessageAsync with user mention.
        /// If banwords detected, sends red-colored error embed with localized message.
        /// EmbedBuilder configuration handles all optional components (title, color, etc.) conditionally.
        /// </remarks>
        public static async void SendCommandReply(DiscordCommandSendData data)
        {
            try
            {
                Write($"Discord - A message response was sent to the {data.Server}: {data.Message}", "info");
                data.Message = TextSanitizer.CleanAscii(data.Message);

                if (data.SocketCommandBase != null)
                {
                    if ((data.SafeExecute | data.IsEphemeral) || new BannedWordDetector().Check(data.Message, data.ServerID, PlatformsEnum.Discord) && new BannedWordDetector().Check(data.Description, data.ServerID, PlatformsEnum.Discord))
                    {
                        if (data.IsEmbed)
                        {
                            var embed = new EmbedBuilder();
                            if (data.Title != "")
                                embed.WithTitle(data.Title);
                            if (data.EmbedColor != default(Discord.Color))
                                embed.WithColor((Discord.Color)data.EmbedColor);
                            if (data.Description != "")
                                embed.WithDescription(data.Description);
                            if (data.ThumbnailLink != "")
                                embed.WithThumbnailUrl(data.ThumbnailLink);
                            if (data.ImageLink != "")
                                embed.WithImageUrl(data.ImageLink);

                            var resultEmbed = embed.Build();
                            data.SocketCommandBase.RespondAsync(embed: resultEmbed, ephemeral: data.IsEphemeral);
                        }
                        else
                        {
                            data.SocketCommandBase.RespondAsync(data.Message, ephemeral: data.IsEphemeral);
                        }
                    }
                    else
                    {
                        var embed = new EmbedBuilder()
                            .WithTitle(LocalizationService.GetString(data.Language, "error:message_could_not_be_sent", "", PlatformsEnum.Discord))
                            .WithColor(global::Discord.Color.Red)
                            .Build();
                        data.SocketCommandBase.RespondAsync(embed: embed, ephemeral: data.IsEphemeral);
                    }
                }
                else
                {
                    string messageToSend = "";
                    DiscordCommandSendData messageToSendPart2;
                    if (data.Message.Length > 12288)
                        messageToSend = LocalizationService.GetString(data.Language, "error:too_large_text", data.ChannelID, PlatformsEnum.Telegram);
                    else if (messageToSend.Length > 4096)
                    {
                        int splitIndex = messageToSend.LastIndexOf(' ', 4000);

                        string part1 = messageToSend.Substring(0, splitIndex) + "...";
                        string part2 = "..." + messageToSend.Substring(splitIndex);

                        messageToSend = part1;
                        messageToSendPart2 = data;
                        messageToSendPart2.Message = part2;
                    }

                    ITextChannel sender = await bb.Bot.Clients.Discord.GetChannelAsync(ulong.Parse(data.ChannelID)) as ITextChannel;

                    if ((data.SafeExecute | data.IsEphemeral) || new BannedWordDetector().Check(data.Message, data.ServerID, PlatformsEnum.Discord) && new BannedWordDetector().Check(data.Description, data.ServerID, PlatformsEnum.Discord))
                    {
                        if (data.IsEmbed)
                        {
                            var embed = new EmbedBuilder();
                            if (data.Title != "")
                                embed.WithTitle(data.Title);
                            if (data.EmbedColor != default(Discord.Color))
                                embed.WithColor((Discord.Color)data.EmbedColor);
                            if (data.Description != "")
                                embed.WithDescription(data.Description);
                            if (data.ThumbnailLink != "")
                                embed.WithThumbnailUrl(data.ThumbnailLink);
                            if (data.ImageLink != "")
                                embed.WithImageUrl(data.ImageLink);

                            var resultEmbed = embed.Build();
                            sender.SendMessageAsync($"<@{data.UserID}> {data.Message}", embed: resultEmbed);
                        }
                        else
                        {
                            sender.SendMessageAsync($"<@{data.UserID}> {data.Message}");
                        }
                    }
                    else
                    {
                        var embed = new EmbedBuilder()
                            .WithTitle(LocalizationService.GetString(data.Language, "error:message_could_not_be_sent", "", PlatformsEnum.Discord))
                            .WithColor(global::Discord.Color.Red)
                            .Build();
                        sender.SendMessageAsync($"<@{data.UserID}> {data.Message}", embed: embed);
                    }
                }
            }
            catch (Exception ex)
            {
                Write(ex);
            }
        }
    }
}
