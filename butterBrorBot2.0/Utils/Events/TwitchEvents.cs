using butterBror.Utils.DataManagers;
using butterBror.Utils.Tools;
using DankDB;
using Newtonsoft.Json.Linq;
using TwitchLib.Client.Events;
using TwitchLib.Communication.Events;
using static butterBror.Utils.Things.Console;

namespace butterBror
{
    public partial class TwitchEvents
    {
        [ConsoleSector("butterBror.TwitchEvents", "OnConnected")]
        public static void OnConnected(object sender, OnConnectedArgs e)
        {
            Core.Statistics.FunctionsUsed.Add();
        }

        [ConsoleSector("butterBror.TwitchEvents", "OnMessageSend")]
        public static void OnMessageSend(object s, OnMessageSentArgs e)
        {
            Core.Statistics.FunctionsUsed.Add();
            Write($"Twitch - Message sent to #{e.SentMessage.Channel}: \"{e.SentMessage.Message}\"", "info");
        }

        [ConsoleSector("butterBror.TwitchEvents", "OnMessageThrottled")]
        public static void OnMessageThrottled(object s, OnMessageThrottledEventArgs e)
        {
            Core.Statistics.FunctionsUsed.Add();
            Write($"Twitch - Message not sent! \"{e.Message}\" ", "err");
        }

        [ConsoleSector("butterBror.TwitchEvents", "OnLog")]
        public static void OnLog(object s, OnLogArgs e)
        {
            // Tools.LOG($"Twitch LOG: {e.Data}");
        }

        [ConsoleSector("butterBror.TwitchEvents", "OnUserBanned")]
        public static void OnUserBanned(object s, OnUserBannedArgs e)
        {
            Core.Statistics.FunctionsUsed.Add();
            Write($"Twitch - #{e.UserBan.Channel} User {e.UserBan.Username} has been permanently banned!", "info");
        }

        [ConsoleSector("butterBror.TwitchEvents", "OnSuspended")]
        public static void OnSuspended(object s, OnSuspendedArgs e)
        {
            Core.Statistics.FunctionsUsed.Add();
            Chat.TwitchSend(Core.Bot.BotName, $"What #{e.Channel} suspended", "", "", "en", true);
            Write($"Twitch - #{e.Channel} suspended", "err");
        }

        [ConsoleSector("butterBror.TwitchEvents", "OnUserTimedout")]
        public static void OnUserTimedout(object s, OnUserTimedoutArgs e)
        {
            Core.Statistics.FunctionsUsed.Add();
            Write($"Twitch - #{e.UserTimeout.Channel} User {e.UserTimeout.Username} has been blocked for {e.UserTimeout.TimeoutDuration} seconds", "info");
        }

        [ConsoleSector("butterBror.TwitchEvents", "OnReSubscriber")]
        public static void OnReSubscriber(object s, OnReSubscriberArgs e)
        {
            Core.Statistics.FunctionsUsed.Add();
            Write($"Twitch - #{e.Channel} {e.ReSubscriber.DisplayName} has renewed his subscription! He has been subscribing for {e.ReSubscriber.MsgParamCumulativeMonths} ​​month(s) \"{e.ReSubscriber.ResubMessage}\"", "info");
        }

        [ConsoleSector("butterBror.TwitchEvents", "OnGiftedSubscription")]
        public static void OnGiftedSubscription(object s, OnGiftedSubscriptionArgs e)
        {
            Core.Statistics.FunctionsUsed.Add();
            Write($"Twitch - #{e.Channel} {e.GiftedSubscription.DisplayName} has given a subscription to {e.GiftedSubscription.MsgParamRecipientDisplayName} for {e.GiftedSubscription.MsgParamMonths} ​​month(s)!", "info");
        }

        [ConsoleSector("butterBror.TwitchEvents", "OnRaidNotification")]
        public static void OnRaidNotification(object s, OnRaidNotificationArgs e)
        {
            Core.Statistics.FunctionsUsed.Add();
            Write($"Twitch - #{e.Channel} PagMan RAID from @{e.RaidNotification.DisplayName} with {e.RaidNotification.MsgParamViewerCount} raider(s)", "info");
        }

        [ConsoleSector("butterBror.TwitchEvents", "OnNewSubscriber")]
        public static void OnNewSubscriber(object s, OnNewSubscriberArgs e)
        {
            Core.Statistics.FunctionsUsed.Add();
            Write($"Twitch - #{e.Channel} {e.Subscriber.DisplayName} subscribed! \"{e.Subscriber.ResubMessage}\"", "info");
        }

        [ConsoleSector("butterBror.TwitchEvents", "OnMessageCleared")]
        public static void OnMessageCleared(object s, OnMessageClearedArgs e)
        {
            Core.Statistics.FunctionsUsed.Add();
            Write($"Twitch - #{e.Channel} The message \"{e.Message}\" has been deleted!", "info");
            Core.Bot.Restart(); // wtf bro
        }

        [ConsoleSector("butterBror.TwitchEvents", "OnIncorrectLogin")]
        public static void OnIncorrectLogin(object s, OnIncorrectLoginArgs e)
        {
            Core.Statistics.FunctionsUsed.Add();
            Write("Twitch - Incorrect login!", "info", Utils.Things.Console.LogLevel.Error);
            Core.Bot.Restart();
        }

        [ConsoleSector("butterBror.TwitchEvents", "OnChatCleared")]
        public static void OnChatCleared(object s, OnChatClearedArgs e)
        {
            Core.Statistics.FunctionsUsed.Add();
            Write($"Twitch - #{e.Channel} The chat was cleared!", "info");
        }

        [ConsoleSector("butterBror.TwitchEvents", "OnError")]
        public static void OnError(object s, OnErrorEventArgs e)
        {
            Core.Statistics.FunctionsUsed.Add();
            Chat.TwitchSend(Core.Bot.BotName, $"DeadAss TwitchLib error: {e.Exception.Message}", "", "", "en", true);
            Write($"Twitch - Library error! {e.Exception.Message}", "info", Utils.Things.Console.LogLevel.Error);
        }

        [ConsoleSector("butterBror.TwitchEvents", "OnLeftChannel")]
        public static void OnLeftChannel(object s, OnLeftChannelArgs e)
        {
            Core.Statistics.FunctionsUsed.Add();
            Write($"Twitch - Succeful leaved from #{e.Channel}", "info");
        }

        [ConsoleSector("butterBror.TwitchEvents", "OnReconnected")]
        public static void OnReconnected(object s, OnReconnectedEventArgs e)
        {
            Core.Statistics.FunctionsUsed.Add();
            Write("Twitch - Reconnected!", "info");
            Chat.TwitchSend(Core.Bot.BotName, $"BREAKDANCECAT Reconnected", "", "", "en", true);
            Core.Bot.TwitchReconnected = true;
        }

        [ConsoleSector("butterBror.TwitchEvents", "OnTwitchDisconnected")]
        public static void OnTwitchDisconnected(object s, OnDisconnectedEventArgs e)
        {
            Core.Statistics.FunctionsUsed.Add();
            if (Thread.CurrentThread.Name == Core.RestartedTimes.ToString() && !Core.Bot.Clients.Twitch.IsConnected)
            {
                Write("Twitch - Disconnected! Restarting...", "info");

                if (!Core.Bot.NeedRestart)
                    Core.Bot.Restart();
            }
        }

        [ConsoleSector("butterBror.TwitchEvents", "OnCommunitySubscription")]
        public static void OnCommunitySubscription(object s, OnCommunitySubscriptionArgs e)
        {
            Core.Statistics.FunctionsUsed.Add();
            Write($"Twitch - #{e.Channel} {e.GiftedSubscription.DisplayName} was gifted {e.GiftedSubscription.MsgParamMassGiftCount} subscription(s) on {e.GiftedSubscription.MsgParamMultiMonthGiftDuration} month(s)", "info");
        }

        [ConsoleSector("butterBror.TwitchEvents", "OnAnnounce")]
        public static void OnAnnounce(object s, OnAnnouncementArgs e)
        {
            Core.Statistics.FunctionsUsed.Add();
            Write($"Twitch - #{e.Channel} Announce {e.Announcement.Message}", "info");
        }

        [ConsoleSector("butterBror.TwitchEvents", "OnContinuedGiftedSubscription")]
        public static void OnContinuedGiftedSubscription(object s, OnContinuedGiftedSubscriptionArgs e)
        {
            Core.Statistics.FunctionsUsed.Add();
            Write($"Twitch - #{e.Channel} User @{e.ContinuedGiftedSubscription.DisplayName} extended gift subscription!", "info");
        }

        [ConsoleSector("butterBror.TwitchEvents", "OnBanned")]
        public static async void OnBanned(object s, OnBannedArgs e)
        {
            Core.Statistics.FunctionsUsed.Add();
            try
            {
                var N2IPath = Core.Bot.Pathes.Nick2ID + Platform.strings[(int)Platforms.Twitch] + "/" + e.Channel.ToLower() + ".txt";
                string disconnectedChannel = FileUtil.GetFileContent(N2IPath);

                Write($"Twitch - Bot was banned in channel #{e.Channel}!", "info");
                Chat.TwitchSend(Core.Bot.BotName, $"DeadAss Bot was banned in channel #{e.Channel}!", "", "", "en", true);

                string[] channels = Manager.Get<string[]>(Core.Bot.Pathes.Settings, "channels");
                List<string> list = new();
                foreach (var channel in channels)
                {
                    if (channel.ToLower() != disconnectedChannel.ToLower())
                    {
                        list.Add(channel);
                    }
                }
                SafeManager.Save(Core.Bot.Pathes.Settings, "channels", JToken.FromObject(list));
            }
            catch (Exception ex)
            {
                Write(ex);
            }
        }

        [ConsoleSector("butterBror.TwitchEvents", "OnConnectionError")]
        public static void OnConnectionError(object s, OnConnectionErrorArgs e)
        {
            Core.Statistics.FunctionsUsed.Add();
            Write($"Twitch - Connection error! \"{e.Error.Message}\"", "info", Utils.Things.Console.LogLevel.Error);
            Core.Bot.Restart();
        }

        [ConsoleSector("butterBror.TwitchEvents", "OnJoin")]
        public static async void OnJoin(object sender, OnJoinedChannelArgs e)
        {
            Core.Statistics.FunctionsUsed.Add();
            //Write($"Twitch - Connected to #{e.Channel}", "info");

            if (Core.PreviousVersion != $"{Core.Version}.{Core.Patch}" && Core.PreviousVersion != string.Empty && (Core.Bot.TwitchNewVersionAnnounce.Contains(Names.GetUserID(e.Channel, Platforms.Twitch)) || e.Channel.Equals(Core.Bot.BotName.ToLower())))
                Chat.TwitchSend(e.Channel, $"butterBror v.{Core.PreviousVersion} > v.{Core.Version}.{Core.Patch}", e.Channel, "", "ru", true);

            if (Core.Bot.TwitchConnectAnnounce.Contains(Names.GetUserID(e.Channel, Platforms.Twitch)))
                Chat.TwitchSend(e.Channel, "butterBror Connected!", e.Channel, "", "ru", true);

            if (Core.Bot.TwitchReconnected && Core.Bot.TwitchReconnectAnnounce.Contains(Names.GetUserID(e.Channel, Platforms.Twitch)) || e.Channel.Equals(Core.Bot.BotName.ToLower()))
                Chat.TwitchSend(e.Channel, "butterBror Reconnected!", e.Channel, "", "ru", true);

            if (!Core.Bot.TwitchChannels.Contains(e.Channel))
                Core.Bot.TwitchChannels.Append(e.Channel);
        }

        [ConsoleSector("butterBror.TwitchEvents", "OnMessageReceived")]
        public static async void OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            Core.Statistics.FunctionsUsed.Add();

            try
            {
                await Command.ProcessMessageAsync(e.ChatMessage.UserId, e.ChatMessage.RoomId, e.ChatMessage.Username, e.ChatMessage.Message, e, e.ChatMessage.Channel, butterBror.Platforms.Twitch, null);
            }
            catch (Exception ex)
            {
                Write(ex);
            }
        }
    }
}
