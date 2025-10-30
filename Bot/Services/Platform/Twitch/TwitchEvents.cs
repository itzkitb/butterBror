using bb.Models.Platform;
using bb.Utils;
using DankDB;
using Newtonsoft.Json.Linq;
using SevenTV.Types.Rest;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Events;
using TwitchLib.Communication.Interfaces;
using static bb.Core.Bot.Logger;
using TelegramLib = Telegram;

namespace bb.Services.Platform.Twitch
{
    /// <summary>
    /// Contains event handlers for Twitch API interactions and bot behavior customization.
    /// </summary>
    public partial class TwitchEvents
    {
        /// <summary>
        /// Handles connection established event for Twitch client.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments containing connection details.</param>
        public static void OnConnected(object? sender, OnConnectedArgs e)
        {
            Write("Twitch: Connected");

            _ = Task.Run(async () =>
            {
                if (Program.BotInstance.PreviousVersion != Program.BotInstance.Version.ToString() && Program.BotInstance.PreviousVersion != string.Empty)
                {
                    Write($"Twitch: {Program.BotInstance.TwitchNewVersionAnnounce.Count()}", LogLevel.Debug);
                    foreach (string channel in Program.BotInstance.TwitchNewVersionAnnounce)
                    {
                        bb.Program.BotInstance.MessageSender.Send(Models.Platform.Platform.Twitch, $"{Program.BotInstance.TwitchName} v.{Program.BotInstance.PreviousVersion} > v.{Program.BotInstance.Version}", UsernameResolver.GetUsername(channel, Models.Platform.Platform.Twitch, true), isSafe: true);
                    }
                }

                foreach (string channel in Program.BotInstance.TwitchConnectAnnounce)
                {
                    Write($"Twitch: {Program.BotInstance.TwitchConnectAnnounce.Count()}", LogLevel.Debug);
                    bb.Program.BotInstance.MessageSender.Send(Models.Platform.Platform.Twitch, $"{Program.BotInstance.TwitchName} Started in {(long)(Program.BotInstance.ConnectedIn).TotalMilliseconds} ms!", UsernameResolver.GetUsername(channel, Models.Platform.Platform.Twitch, true), isSafe: true);
                }
            });
        }

        /// <summary>
        /// Handles message sent event in Twitch chat.
        /// </summary>
        /// <param name="s">The source of the event.</param>
        /// <param name="e">Event arguments containing message details.</param>
        public static void OnMessageSend(object? s, OnMessageSentArgs e)
        {
            Write($"Twitch: #{e.SentMessage.Channel}: \"{e.SentMessage.Message}\"");
        }

        /// <summary>
        /// Handles message throttling event when Twitch rate limits messages.
        /// </summary>
        /// <param name="s">The source of the event.</param>
        /// <param name="e">Event arguments containing throttled message details.</param>
        public static void OnMessageThrottled(object? s, OnMessageThrottledEventArgs e)
        {
            Write($"Twitch: Message throttled! Info: \"{e.Message}\"", LogLevel.Warning);
        }

        /// <summary>
        /// Handles Twitch API logging events.
        /// </summary>
        /// <param name="s">The source of the event.</param>
        /// <param name="e">Event arguments containing log data.</param>
        public static void OnLog(object? s, OnLogArgs e)
        {
            // Tools.LOG($"Twitch LOG: {e.Data}");
        }

        /// <summary>
        /// Handles user permanent ban event in a Twitch channel.
        /// </summary>
        /// <param name="s">The source of the event.</param>
        /// <param name="e">Event arguments containing ban details.</param>
        public static void OnUserBanned(object? s, OnUserBannedArgs e)
        {
            //Write($"Twitch: #{e.UserBan.Channel} User {e.UserBan.Username} has been permanently banned!", "info");
        }

        /// <summary>
        /// Handles Twitch account suspension event.
        /// </summary>
        /// <param name="s">The source of the event.</param>
        /// <param name="e">Event arguments containing suspension details.</param>
        public static void OnSuspended(object? s, OnSuspendedArgs e)
        {
            Write($"Twitch: #{e.Channel} suspended.");
            if (bb.Program.BotInstance.TwitchName == null)
            {
                Write("The bot nickname is null.", LogLevel.Error);
                return;
            }

            bb.Program.BotInstance.MessageSender.Send(Models.Platform.Platform.Twitch, $"What #{e.Channel} suspended.", bb.Program.BotInstance.TwitchName);
        }

        /// <summary>
        /// Handles user timeout event in a Twitch channel.
        /// </summary>
        /// <param name="s">The source of the event.</param>
        /// <param name="e">Event arguments containing timeout details.</param>
        public static void OnUserTimedout(object? s, OnUserTimedoutArgs e)
        {
            //Write($"Twitch: #{e.UserTimeout.Channel} User {e.UserTimeout.Username} has been blocked for {e.UserTimeout.TimeoutDuration} seconds", "info");
        }

        /// <summary>
        /// Handles resubscription event in a Twitch channel.
        /// </summary>
        /// <param name="s">The source of the event.</param>
        /// <param name="e">Event arguments containing resubscription details.</param>
        public static void OnReSubscriber(object? s, OnReSubscriberArgs e)
        {
            //Write($"Twitch: #{e.Channel} {e.ReSubscriber.DisplayName} has renewed his subscription! He has been subscribing for {e.ReSubscriber.MsgParamCumulativeMonths} ​​month(s) \"{e.ReSubscriber.ResubMessage}\"", "info");
        }

        /// <summary>
        /// Handles gifted subscription event in a Twitch channel.
        /// </summary>
        /// <param name="s">The source of the event.</param>
        /// <param name="e">Event arguments containing gift subscription details.</param>
        public static void OnGiftedSubscription(object? s, OnGiftedSubscriptionArgs e)
        {
            //Write($"Twitch: #{e.Channel} {e.GiftedSubscription.DisplayName} has given a subscription to {e.GiftedSubscription.MsgParamRecipientDisplayName} for {e.GiftedSubscription.MsgParamMonths} ​​month(s)!", "info");
        }

        /// <summary>
        /// Handles raid notification event in a Twitch channel.
        /// </summary>
        /// <param name="s">The source of the event.</param>
        /// <param name="e">Event arguments containing raid details.</param>
        public static void OnRaidNotification(object? s, OnRaidNotificationArgs e)
        {
            //Write($"Twitch: #{e.Channel} PagMan RAID from @{e.RaidNotification.DisplayName} with {e.RaidNotification.MsgParamViewerCount} raider(s)", "info");
        }

        /// <summary>
        /// Handles new subscription event in a Twitch channel.
        /// </summary>
        /// <param name="s">The source of the event.</param>
        /// <param name="e">Event arguments containing subscription details.</param>
        public static void OnNewSubscriber(object? s, OnNewSubscriberArgs e)
        {
            //Write($"Twitch: #{e.Channel} {e.Subscriber.DisplayName} subscribed! \"{e.Subscriber.ResubMessage}\"", "info");
        }

        /// <summary>
        /// Handles message deletion event in a Twitch channel.
        /// </summary>
        /// <param name="s">The source of the event.</param>
        /// <param name="e">Event arguments containing message deletion details.</param>
        public static void OnMessageCleared(object? s, OnMessageClearedArgs e)
        {
            //Write($"Twitch: #{e.Channel} The message \"{e.Message}\" has been deleted!", "info");
        }

        /// <summary>
        /// Handles incorrect Twitch login attempt.
        /// </summary>
        /// <param name="s">The source of the event.</param>
        /// <param name="e">Event arguments containing login error details.</param>
        public static void OnIncorrectLogin(object? s, OnIncorrectLoginArgs e)
        {
            Write("Twitch: Incorrect login", LogLevel.Error);
        }

        /// <summary>
        /// Handles chat clearing event in a Twitch channel.
        /// </summary>
        /// <param name="s">The source of the event.</param>
        /// <param name="e">Event arguments containing chat clear details.</param>
        public static void OnChatCleared(object? s, OnChatClearedArgs e)
        {
            //Write($"Twitch: #{e.Channel} The chat was cleared!", "info");
        }

        /// <summary>
        /// Handles Twitch library error events.
        /// </summary>
        /// <param name="s">The source of the event.</param>
        /// <param name="e">Event arguments containing error details.</param>
        public static void OnError(object? s, OnErrorEventArgs e)
        {
            Write($"Twitch: Library error! Info: {e.Exception.Message}", LogLevel.Error);
            if (bb.Program.BotInstance.TwitchName == null)
            {
                Write("The bot nickname is null.", LogLevel.Error);
                return;
            }
            bb.Program.BotInstance.MessageSender.Send(Models.Platform.Platform.Twitch, $"DeadAss TwitchLib error: {e.Exception.Message}", bb.Program.BotInstance.TwitchName);
        }

        /// <summary>
        /// Handles successful channel leave event.
        /// </summary>
        /// <param name="s">The source of the event.</param>
        /// <param name="e">Event arguments containing leave details.</param>
        public static void OnLeftChannel(object? s, OnLeftChannelArgs e)
        {
            Write($"Twitch: Succeful leaved from #{e.Channel}");
        }

        /// <summary>
        /// Handles Twitch reconnection event.
        /// </summary>
        /// <param name="s">The source of the event.</param>
        /// <param name="e">Event arguments containing reconnection details.</param>

        public static void OnReconnected(object? s, OnReconnectedEventArgs e)
        {
            Write("Twitch: Reconnected!");
            bb.Program.BotInstance.JoinTwitchChannels();

            _ = Task.Run(async () =>
            {
                await Task.Delay(1000);

                if (bb.Program.BotInstance.TwitchName == null)
                {
                    Write("The bot nickname is null.", LogLevel.Error);
                }
                else
                {
                    bb.Program.BotInstance.MessageSender.Send(Models.Platform.Platform.Twitch, $"BREAKDANCECAT Reconnected", bb.Program.BotInstance.TwitchName);
                }

                foreach (string channel in bb.Program.BotInstance.TwitchReconnectAnnounce)
                {
                    bb.Program.BotInstance.MessageSender.Send(Models.Platform.Platform.Twitch, $"{bb.Program.BotInstance.TwitchName} Reconnected!", UsernameResolver.GetUsername(channel, Models.Platform.Platform.Twitch, true));
                }
            });
        }

        /// <summary>
        /// Handles Twitch disconnection event.
        /// </summary>
        /// <param name="s">The source of the event.</param>
        /// <param name="e">Event arguments containing disconnection details.</param>

        public static void OnTwitchDisconnected(object? s, OnDisconnectedEventArgs e)
        {
            Write("Twitch: Disconnected!");
            bb.Program.BotInstance.RefreshTwitchTokenAsync().Wait();
        }

        /// <summary>
        /// Handles community gift subscription event.
        /// </summary>
        /// <param name="s">The source of the event.</param>
        /// <param name="e">Event arguments containing community gift details.</param>

        public static void OnCommunitySubscription(object? s, OnCommunitySubscriptionArgs e)
        {
            //Write($"Twitch: #{e.Channel} {e.GiftedSubscription.DisplayName} was gifted {e.GiftedSubscription.MsgParamMassGiftCount} subscription(s) on {e.GiftedSubscription.MsgParamMultiMonthGiftDuration} month(s)", "info");
        }

        /// <summary>
        /// Handles channel announcement event.
        /// </summary>
        /// <param name="s">The source of the event.</param>
        /// <param name="e">Event arguments containing announcement details.</param>

        public static void OnAnnounce(object? s, OnAnnouncementArgs e)
        {
            //Write($"Twitch: #{e.Channel} Announce {e.Announcement.Message}", "info");
        }

        /// <summary>
        /// Handles continuation of gifted subscription event.
        /// </summary>
        /// <param name="s">The source of the event.</param>
        /// <param name="e">Event arguments containing continuation details.</param>

        public static void OnContinuedGiftedSubscription(object? s, OnContinuedGiftedSubscriptionArgs e)
        {
            //Write($"Twitch: #{e.Channel} User @{e.ContinuedGiftedSubscription.DisplayName} extended gift subscription!", "info");
        }

        /// <summary>
        /// Handles bot ban event in a Twitch channel.
        /// </summary>
        /// <param name="s">The source of the event.</param>
        /// <param name="e">Event arguments containing ban details.</param>

        public static void OnBanned(object? s, OnBannedArgs e)
        {
            try
            {
                Write($"Twitch: Bot was banned in #{e.Channel}", LogLevel.Warning);

                if (bb.Program.BotInstance.TwitchName == null)
                {
                    Write("The bot nickname is null.", LogLevel.Error);
                }
                else
                {
                    bb.Program.BotInstance.MessageSender.Send(Models.Platform.Platform.Twitch, $"DeadAss Bot was banned in #{e.Channel}", bb.Program.BotInstance.TwitchName);
                }

                string[] channels = bb.Program.BotInstance.Settings.Get<string[]>("twitch_connect_channels");
                List<string> list = new();
                foreach (var channel in channels)
                {
                    if (!channel.Equals(UsernameResolver.GetUserID(e.Channel, Models.Platform.Platform.Twitch, true), StringComparison.CurrentCultureIgnoreCase))
                    {
                        list.Add(channel);
                    }
                }
                bb.Program.BotInstance.Settings.Set("channels", JToken.FromObject(list));
            }
            catch (Exception ex)
            {
                Write(ex);
            }
        }

        /// <summary>
        /// Handles Twitch connection error events.
        /// </summary>
        /// <param name="s">The source of the event.</param>
        /// <param name="e">Event arguments containing connection error details.</param>

        public static void OnConnectionError(object? s, OnConnectionErrorArgs e)
        {
            Write($"Twitch: Connection error! Info: \"{e.Error.Message}\"", LogLevel.Error);
        }

        /// <summary>
        /// Handles successful channel join event.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments containing join details.</param>

        public static void OnJoin(object? sender, OnJoinedChannelArgs e)
        {
            //Write($"Twitch: Connected to #{e.Channel}", "info");
        }

        /// <summary>
        /// Handles incoming message received event in Twitch chat.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments containing message details.</param>

        public static async void OnMessageReceived(object? sender, OnMessageReceivedArgs e)
        {
            try
            {
                ChatMessage message = e.ChatMessage;

                await bb.Program.BotInstance.MessageProcessor.ProcessMessageAsync(
                    message.UserId,
                    message.RoomId,
                    message.Username,
                    message.Message,
                    message,
                    message.Channel,
                    Models.Platform.Platform.Twitch,
                    new TelegramLib.Bot.Types.Message(),
                    message.Id);
            }
            catch (Exception ex)
            {
                Write(ex);
            }
        }
    }
}
