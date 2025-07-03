using butterBror.Utils;
using butterBror.Utils.DataManagers;
using butterBror.Utils.Tools;
using butterBror.Utils.Types;
using Discord;
using Discord.WebSocket;
using Telegram.Bot;
using Telegram.Bot.Types;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using static butterBror.Utils.Bot.Console;

namespace butterBror
{
    /// <summary>
    /// Provides platform-specific command reply handlers for Twitch, Telegram, and Discord platforms.
    /// </summary>
    public partial class Commands
    {
        /// <summary>
        /// Sends a reply message to Twitch chat with message splitting and banword check support.
        /// </summary>
        /// <param name="data">Twitch-specific message data including channel, message, and reply context.</param>
        /// <remarks>
        /// - Automatically splits messages over 500 characters (with ASCII cleaning)
        /// - Handles Twitch message length limits (1500 chars total)
        /// - Includes safety checks for banned words
        /// - Maintains message threading through message ID
        /// - Applies nickname color formatting
        /// </remarks>
        [ConsoleSector("butterBror.Commands", "SendCommandReply#1")]
        public static async void SendCommandReply(TwitchMessageSendData data)
        {
            Core.Statistics.FunctionsUsed.Add();
            try
            {
                string message = data.Message;
                TwitchMessageSendData messageToSendPart2 = null;
                //Write($"Twitch - A response to message {data.message_id} was sent to channel {data.channel}: {data.message}", "info");
                message = Text.CleanAscii(data.Message);

                if (message.Length > 1500)
                    message = TranslationManager.GetTranslation(data.Language, "error:too_large_text", data.ChannelID, Platforms.Twitch);
                else if (message.Length > 500)
                {
                    int splitIndex = message.LastIndexOf(' ', 450);
                    string part2 = string.Concat("... ", message.AsSpan(splitIndex));

                    message = string.Concat(message.AsSpan(0, splitIndex), "...");

                    Task task = Task.Run(() =>
                    {
                        Thread.Sleep(1000);
                        Utils.Tools.Chat.TwitchReply(data.Channel, data.ChannelID, part2, data.MessageID, data.Language, data.SafeExecute);
                    });
                }

                if (!Core.Bot.Clients.Twitch.JoinedChannels.Contains(new JoinedChannel(data.Channel)))
                    Core.Bot.Clients.Twitch.JoinChannel(data.Channel);

                if (data.SafeExecute || new NoBanwords().Check(message, data.ChannelID, Platforms.Twitch))
                    Core.Bot.Clients.Twitch.SendReply(data.Channel, data.MessageID, message);
                else
                    Core.Bot.Clients.Twitch.SendReply(data.Channel, data.MessageID, TranslationManager.GetTranslation(data.Language, "error:message_could_not_be_sent", data.ChannelID, Platforms.Twitch));
            }
            catch (Exception ex)
            {
                Write(ex);
            }
        }

        /// <summary>
        /// Sends a reply message to Telegram chat with message splitting and banword check support.
        /// </summary>
        /// <param name="data">Telegram-specific message data including channel info and reply context.</param>
        /// <remarks>
        /// - Handles Telegram message length limit (12288 chars total)
        /// - Automatically splits messages over 4096 characters
        /// - Maintains message threading through message ID
        /// - Includes safety checks for banned words
        /// - Supports reply context preservation
        /// </remarks>
        [ConsoleSector("butterBror.Commands", "SendCommandReply#2")]
        public static async void SendCommandReply(TelegramMessageSendData data)
        {
            Core.Statistics.FunctionsUsed.Add();
            try
            {
                string messageToSend = data.Message;
                TelegramMessageSendData messageToSendPart2 = null;
                Write($"Telegram - A message response was sent to the {data.Channel} channel: {data.Message}", "info");
                messageToSend = Text.CleanAscii(data.Message);

                if (messageToSend.Length > 12288)
                {
                    messageToSend = TranslationManager.GetTranslation(data.Language, "error:too_large_text", data.ChannelID, Platforms.Telegram);
                }
                else if (messageToSend.Length > 4096)
                {
                    int splitIndex = messageToSend.LastIndexOf(' ', 4000);

                    string part1 = messageToSend.Substring(0, splitIndex) + "...";
                    string part2 = "..." + messageToSend.Substring(splitIndex);

                    messageToSend = part1;
                    messageToSendPart2 = data;
                    messageToSendPart2.Message = part2;
                }

                if (data.SafeExecute || new NoBanwords().Check(messageToSend, data.ChannelID, Platforms.Telegram))
                    await Core.Bot.Clients.Telegram.SendMessage(long.Parse(data.ChannelID), data.Message, replyParameters: int.Parse(data.MessageID));
                else
                    await Core.Bot.Clients.Telegram.SendMessage(long.Parse(data.ChannelID), TranslationManager.GetTranslation(data.Language, "error:message_could_not_be_sent", data.ChannelID, Platforms.Telegram), replyParameters: int.Parse(data.MessageID));
                

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
        /// Sends a reply message to Discord chat with embed support and permission handling.
        /// </summary>
        /// <param name="data">Discord-specific message data including embed formatting and permissions.</param>
        /// <remarks>
        /// - Handles ephemeral (private) message responses
        /// - Supports rich embed formatting with title, description, images
        /// - Handles message length limits through splitting
        /// - Applies banword filtering to both message and description
        /// - Falls back to channel-based sending if command context unavailable
        /// - Uses Discord's embed builder for rich message formatting
        /// </remarks>
        [ConsoleSector("butterBror.Commands", "SendCommandReply#3")]
        public static async void SendCommandReply(DiscordCommandSendData data)
        {
            Core.Statistics.FunctionsUsed.Add();
            try
            {
                Write($"Discord - A message response was sent to the {data.Server}: {data.Message}", "info");
                data.Message = Text.CleanAscii(data.Message);

                if (data.SocketCommandBase != null)
                {
                    if ((data.SafeExecute | data.IsEphemeral) || new NoBanwords().Check(data.Message, data.ServerID, Platforms.Discord) && new NoBanwords().Check(data.Description, data.ServerID, Platforms.Discord))
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
                            .WithTitle(TranslationManager.GetTranslation(data.Language, "error:message_could_not_be_sent", "", Platforms.Discord))
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
                    {
                        messageToSend = TranslationManager.GetTranslation(data.Language, "error:too_large_text", data.ChannelID, Platforms.Telegram);
                    }
                    else if (messageToSend.Length > 4096)
                    {
                        int splitIndex = messageToSend.LastIndexOf(' ', 4000);

                        string part1 = messageToSend.Substring(0, splitIndex) + "...";
                        string part2 = "..." + messageToSend.Substring(splitIndex);

                        messageToSend = part1;
                        messageToSendPart2 = data;
                        messageToSendPart2.Message = part2;
                    }

                    ITextChannel sender = await Core.Bot.Clients.Discord.GetChannelAsync(ulong.Parse(data.ChannelID)) as ITextChannel;

                    if ((data.SafeExecute | data.IsEphemeral) || new NoBanwords().Check(data.Message, data.ServerID, Platforms.Discord) && new NoBanwords().Check(data.Description, data.ServerID, Platforms.Discord))
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
                            .WithTitle(TranslationManager.GetTranslation(data.Language, "error:message_could_not_be_sent", "", Platforms.Discord))
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
