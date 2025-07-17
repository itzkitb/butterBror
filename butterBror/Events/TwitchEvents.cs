using butterBror.Data;
using butterBror.Models;
using butterBror.Utils;
using DankDB;
using Newtonsoft.Json.Linq;
using TwitchLib.Client.Events;
using TwitchLib.Communication.Events;
using static butterBror.Core.Bot.Console;

namespace butterBror.Events
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
        [ConsoleSector("butterBror.TwitchEvents", "OnConnected")]
        public static void OnConnected(object sender, OnConnectedArgs e)
        {
            Engine.Statistics.FunctionsUsed.Add();
        }

        /// <summary>
        /// Handles message sent event in Twitch chat.
        /// </summary>
        /// <param name="s">The source of the event.</param>
        /// <param name="e">Event arguments containing message details.</param>
        [ConsoleSector("butterBror.TwitchEvents", "OnMessageSend")]
        public static void OnMessageSend(object s, OnMessageSentArgs e)
        {
            Engine.Statistics.FunctionsUsed.Add();
            Write($"Twitch - Message sent to #{e.SentMessage.Channel}: \"{e.SentMessage.Message}\"", "info");
        }

        /// <summary>
        /// Handles message throttling event when Twitch rate limits messages.
        /// </summary>
        /// <param name="s">The source of the event.</param>
        /// <param name="e">Event arguments containing throttled message details.</param>
        [ConsoleSector("butterBror.TwitchEvents", "OnMessageThrottled")]
        public static void OnMessageThrottled(object s, OnMessageThrottledEventArgs e)
        {
            Engine.Statistics.FunctionsUsed.Add();
            Write($"Twitch - Message not sent! \"{e.Message}\" ", "err");
        }

        /// <summary>
        /// Handles Twitch API logging events.
        /// </summary>
        /// <param name="s">The source of the event.</param>
        /// <param name="e">Event arguments containing log data.</param>
        [ConsoleSector("butterBror.TwitchEvents", "OnLog")]
        public static void OnLog(object s, OnLogArgs e)
        {
            // Tools.LOG($"Twitch LOG: {e.Data}");
        }

        /// <summary>
        /// Handles user permanent ban event in a Twitch channel.
        /// </summary>
        /// <param name="s">The source of the event.</param>
        /// <param name="e">Event arguments containing ban details.</param>
        [ConsoleSector("butterBror.TwitchEvents", "OnUserBanned")]
        public static void OnUserBanned(object s, OnUserBannedArgs e)
        {
            Engine.Statistics.FunctionsUsed.Add();
            Write($"Twitch - #{e.UserBan.Channel} User {e.UserBan.Username} has been permanently banned!", "info");
        }

        /// <summary>
        /// Handles Twitch account suspension event.
        /// </summary>
        /// <param name="s">The source of the event.</param>
        /// <param name="e">Event arguments containing suspension details.</param>
        [ConsoleSector("butterBror.TwitchEvents", "OnSuspended")]
        public static void OnSuspended(object s, OnSuspendedArgs e)
        {
            Engine.Statistics.FunctionsUsed.Add();
            Chat.TwitchSend(Engine.Bot.BotName, $"What #{e.Channel} suspended", "", "", "en", true);
            Write($"Twitch - #{e.Channel} suspended", "err");
        }

        /// <summary>
        /// Handles user timeout event in a Twitch channel.
        /// </summary>
        /// <param name="s">The source of the event.</param>
        /// <param name="e">Event arguments containing timeout details.</param>
        [ConsoleSector("butterBror.TwitchEvents", "OnUserTimedout")]
        public static void OnUserTimedout(object s, OnUserTimedoutArgs e)
        {
            Engine.Statistics.FunctionsUsed.Add();
            Write($"Twitch - #{e.UserTimeout.Channel} User {e.UserTimeout.Username} has been blocked for {e.UserTimeout.TimeoutDuration} seconds", "info");
        }

        /// <summary>
        /// Handles resubscription event in a Twitch channel.
        /// </summary>
        /// <param name="s">The source of the event.</param>
        /// <param name="e">Event arguments containing resubscription details.</param>
        [ConsoleSector("butterBror.TwitchEvents", "OnReSubscriber")]
        public static void OnReSubscriber(object s, OnReSubscriberArgs e)
        {
            Engine.Statistics.FunctionsUsed.Add();
            Write($"Twitch - #{e.Channel} {e.ReSubscriber.DisplayName} has renewed his subscription! He has been subscribing for {e.ReSubscriber.MsgParamCumulativeMonths} ​​month(s) \"{e.ReSubscriber.ResubMessage}\"", "info");
        }

        /// <summary>
        /// Handles gifted subscription event in a Twitch channel.
        /// </summary>
        /// <param name="s">The source of the event.</param>
        /// <param name="e">Event arguments containing gift subscription details.</param>
        [ConsoleSector("butterBror.TwitchEvents", "OnGiftedSubscription")]
        public static void OnGiftedSubscription(object s, OnGiftedSubscriptionArgs e)
        {
            Engine.Statistics.FunctionsUsed.Add();
            Write($"Twitch - #{e.Channel} {e.GiftedSubscription.DisplayName} has given a subscription to {e.GiftedSubscription.MsgParamRecipientDisplayName} for {e.GiftedSubscription.MsgParamMonths} ​​month(s)!", "info");
        }

        /// <summary>
        /// Handles raid notification event in a Twitch channel.
        /// </summary>
        /// <param name="s">The source of the event.</param>
        /// <param name="e">Event arguments containing raid details.</param>
        [ConsoleSector("butterBror.TwitchEvents", "OnRaidNotification")]
        public static void OnRaidNotification(object s, OnRaidNotificationArgs e)
        {
            Engine.Statistics.FunctionsUsed.Add();
            Write($"Twitch - #{e.Channel} PagMan RAID from @{e.RaidNotification.DisplayName} with {e.RaidNotification.MsgParamViewerCount} raider(s)", "info");
        }

        /// <summary>
        /// Handles new subscription event in a Twitch channel.
        /// </summary>
        /// <param name="s">The source of the event.</param>
        /// <param name="e">Event arguments containing subscription details.</param>
        [ConsoleSector("butterBror.TwitchEvents", "OnNewSubscriber")]
        public static void OnNewSubscriber(object s, OnNewSubscriberArgs e)
        {
            Engine.Statistics.FunctionsUsed.Add();
            Write($"Twitch - #{e.Channel} {e.Subscriber.DisplayName} subscribed! \"{e.Subscriber.ResubMessage}\"", "info");
        }

        /// <summary>
        /// Handles message deletion event in a Twitch channel.
        /// </summary>
        /// <param name="s">The source of the event.</param>
        /// <param name="e">Event arguments containing message deletion details.</param>
        [ConsoleSector("butterBror.TwitchEvents", "OnMessageCleared")]
        public static void OnMessageCleared(object s, OnMessageClearedArgs e)
        {
            Engine.Statistics.FunctionsUsed.Add();
            Write($"Twitch - #{e.Channel} The message \"{e.Message}\" has been deleted!", "info");
            Engine.Bot.Restart(); // wtf bro
        }

        /// <summary>
        /// Handles incorrect Twitch login attempt.
        /// </summary>
        /// <param name="s">The source of the event.</param>
        /// <param name="e">Event arguments containing login error details.</param>
        [ConsoleSector("butterBror.TwitchEvents", "OnIncorrectLogin")]
        public static void OnIncorrectLogin(object s, OnIncorrectLoginArgs e)
        {
            Engine.Statistics.FunctionsUsed.Add();
            Write("Twitch - Incorrect login!", "info", LogLevel.Error);
            Engine.Bot.Restart();
        }

        /// <summary>
        /// Handles chat clearing event in a Twitch channel.
        /// </summary>
        /// <param name="s">The source of the event.</param>
        /// <param name="e">Event arguments containing chat clear details.</param>
        [ConsoleSector("butterBror.TwitchEvents", "OnChatCleared")]
        public static void OnChatCleared(object s, OnChatClearedArgs e)
        {
            Engine.Statistics.FunctionsUsed.Add();
            Write($"Twitch - #{e.Channel} The chat was cleared!", "info");
        }

        /// <summary>
        /// Handles Twitch library error events.
        /// </summary>
        /// <param name="s">The source of the event.</param>
        /// <param name="e">Event arguments containing error details.</param>
        [ConsoleSector("butterBror.TwitchEvents", "OnError")]
        public static void OnError(object s, OnErrorEventArgs e)
        {
            Engine.Statistics.FunctionsUsed.Add();
            Chat.TwitchSend(Engine.Bot.BotName, $"DeadAss TwitchLib error: {e.Exception.Message}", "", "", "en", true);
            Write($"Twitch - Library error! {e.Exception.Message}", "info", LogLevel.Error);
        }

        /// <summary>
        /// Handles successful channel leave event.
        /// </summary>
        /// <param name="s">The source of the event.</param>
        /// <param name="e">Event arguments containing leave details.</param>
        [ConsoleSector("butterBror.TwitchEvents", "OnLeftChannel")]
        public static void OnLeftChannel(object s, OnLeftChannelArgs e)
        {
            Engine.Statistics.FunctionsUsed.Add();
            Write($"Twitch - Succeful leaved from #{e.Channel}", "info");
        }

        /// <summary>
        /// Handles Twitch reconnection event.
        /// </summary>
        /// <param name="s">The source of the event.</param>
        /// <param name="e">Event arguments containing reconnection details.</param>
        [ConsoleSector("butterBror.TwitchEvents", "OnReconnected")]
        public static void OnReconnected(object s, OnReconnectedEventArgs e)
        {
            Engine.Statistics.FunctionsUsed.Add();
            Write("Twitch - Reconnected!", "info");
            Chat.TwitchSend(Engine.Bot.BotName, $"BREAKDANCECAT Reconnected", "", "", "en", true);
            Engine.Bot.TwitchReconnected = true;
        }

        /// <summary>
        /// Handles Twitch disconnection event.
        /// </summary>
        /// <param name="s">The source of the event.</param>
        /// <param name="e">Event arguments containing disconnection details.</param>
        [ConsoleSector("butterBror.TwitchEvents", "OnTwitchDisconnected")]
        public static void OnTwitchDisconnected(object s, OnDisconnectedEventArgs e)
        {
            Engine.Statistics.FunctionsUsed.Add();
            Write("Twitch - Disconnected! Restarting...", "info");
            Engine.Bot.Restart();
        }

        /// <summary>
        /// Handles community gift subscription event.
        /// </summary>
        /// <param name="s">The source of the event.</param>
        /// <param name="e">Event arguments containing community gift details.</param>
        [ConsoleSector("butterBror.TwitchEvents", "OnCommunitySubscription")]
        public static void OnCommunitySubscription(object s, OnCommunitySubscriptionArgs e)
        {
            Engine.Statistics.FunctionsUsed.Add();
            Write($"Twitch - #{e.Channel} {e.GiftedSubscription.DisplayName} was gifted {e.GiftedSubscription.MsgParamMassGiftCount} subscription(s) on {e.GiftedSubscription.MsgParamMultiMonthGiftDuration} month(s)", "info");
        }

        /// <summary>
        /// Handles channel announcement event.
        /// </summary>
        /// <param name="s">The source of the event.</param>
        /// <param name="e">Event arguments containing announcement details.</param>
        [ConsoleSector("butterBror.TwitchEvents", "OnAnnounce")]
        public static void OnAnnounce(object s, OnAnnouncementArgs e)
        {
            Engine.Statistics.FunctionsUsed.Add();
            Write($"Twitch - #{e.Channel} Announce {e.Announcement.Message}", "info");
        }

        /// <summary>
        /// Handles continuation of gifted subscription event.
        /// </summary>
        /// <param name="s">The source of the event.</param>
        /// <param name="e">Event arguments containing continuation details.</param>
        [ConsoleSector("butterBror.TwitchEvents", "OnContinuedGiftedSubscription")]
        public static void OnContinuedGiftedSubscription(object s, OnContinuedGiftedSubscriptionArgs e)
        {
            Engine.Statistics.FunctionsUsed.Add();
            Write($"Twitch - #{e.Channel} User @{e.ContinuedGiftedSubscription.DisplayName} extended gift subscription!", "info");
        }

        /// <summary>
        /// Handles bot ban event in a Twitch channel.
        /// </summary>
        /// <param name="s">The source of the event.</param>
        /// <param name="e">Event arguments containing ban details.</param>
        [ConsoleSector("butterBror.TwitchEvents", "OnBanned")]
        public static async void OnBanned(object s, OnBannedArgs e)
        {
            Engine.Statistics.FunctionsUsed.Add();
            try
            {
                var N2IPath = Engine.Bot.Pathes.Nick2ID + PlatformsPathName.strings[(int)PlatformsEnum.Twitch] + "/" + e.Channel.ToLower() + ".txt";
                string disconnectedChannel = FileUtil.GetFileContent(N2IPath);

                Write($"Twitch - Bot was banned in channel #{e.Channel}!", "info");
                Chat.TwitchSend(Engine.Bot.BotName, $"DeadAss Bot was banned in channel #{e.Channel}!", "", "", "en", true);

                string[] channels = Manager.Get<string[]>(Engine.Bot.Pathes.Settings, "channels");
                List<string> list = new();
                foreach (var channel in channels)
                {
                    if (channel.ToLower() != disconnectedChannel.ToLower())
                    {
                        list.Add(channel);
                    }
                }
                SafeManager.Save(Engine.Bot.Pathes.Settings, "channels", JToken.FromObject(list));
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
        [ConsoleSector("butterBror.TwitchEvents", "OnConnectionError")]
        public static void OnConnectionError(object s, OnConnectionErrorArgs e)
        {
            Engine.Statistics.FunctionsUsed.Add();
            Write($"Twitch - Connection error! \"{e.Error.Message}\"", "info", LogLevel.Error);
            Engine.Bot.Restart();
        }

        /// <summary>
        /// Handles successful channel join event.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments containing join details.</param>
        [ConsoleSector("butterBror.TwitchEvents", "OnJoin")]
        public static async void OnJoin(object sender, OnJoinedChannelArgs e)
        {
            Engine.Statistics.FunctionsUsed.Add();
            //Write($"Twitch - Connected to #{e.Channel}", "info");

            if (Engine.PreviousVersion != $"{Engine.Version}.{Engine.Patch}" && Engine.PreviousVersion != string.Empty && (Engine.Bot.TwitchNewVersionAnnounce.Contains(Names.GetUserID(e.Channel, PlatformsEnum.Twitch)) || e.Channel.Equals(Engine.Bot.BotName.ToLower())))
                Chat.TwitchSend(e.Channel, $"butterBror v.{Engine.PreviousVersion} > v.{Engine.Version}.{Engine.Patch}", e.Channel, "", "ru", true);

            if (Engine.Bot.TwitchConnectAnnounce.Contains(Names.GetUserID(e.Channel, PlatformsEnum.Twitch)))
                Chat.TwitchSend(e.Channel, "butterBror Connected!", e.Channel, "", "ru", true);

            if (Engine.Bot.TwitchReconnected && Engine.Bot.TwitchReconnectAnnounce.Contains(Names.GetUserID(e.Channel, PlatformsEnum.Twitch)) || e.Channel.Equals(Engine.Bot.BotName.ToLower()))
                Chat.TwitchSend(e.Channel, "butterBror Reconnected!", e.Channel, "", "ru", true);

            if (!Engine.Bot.TwitchChannels.Contains(e.Channel))
                Engine.Bot.TwitchChannels.Append(e.Channel);
        }

        /// <summary>
        /// Handles incoming message received event in Twitch chat.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments containing message details.</param>
        [ConsoleSector("butterBror.TwitchEvents", "OnMessageReceived")]
        public static async void OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            Engine.Statistics.FunctionsUsed.Add();

            try
            {
                await Command.ProcessMessageAsync(e.ChatMessage.UserId, e.ChatMessage.RoomId, e.ChatMessage.Username, e.ChatMessage.Message, e, e.ChatMessage.Channel, PlatformsEnum.Twitch, null);
            }
            catch (Exception ex)
            {
                Write(ex);
            }
        }
    }
}
