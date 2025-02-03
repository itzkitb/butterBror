using butterBror.Utils;
using butterBror.Utils.DataManagers;
using Newtonsoft.Json.Linq;
using TwitchLib.Client.Events;
using TwitchLib.Communication.Events;

namespace butterBror
{
    public partial class TwitchEventHandler
    {
        public static void OnConnected(object sender, OnConnectedArgs e)
        {

        }
        public static void OnMessageSend(object s, OnMessageSentArgs e)
        {
            ConsoleUtil.LOG($"Message sent to {e.SentMessage.Channel}: \"{e.SentMessage.Message}\"", "info");
        }
        // #EVENT 0A
        public static void OnMessageThrottled(object s, OnMessageThrottledEventArgs e)
        {
            ConsoleUtil.LOG($"Message not sent! \"{e.Message}\" ", "err");
            LogWorker.Log($"Failed to send message! \"{e.Message}\"", LogWorker.LogTypes.Err, $"TwitchEventHandler\\OnMessageThrottled#{e.Message}");
        }
        public static void OnLog(object s, TwitchLib.Client.Events.OnLogArgs e)
        {
            // Tools.LOG($"Twitch LOG: {e.Data}");
        }
        public static void OnUserBanned(object s, OnUserBannedArgs e)
        {
            ConsoleUtil.LOG($"({e.UserBan.Channel}) User {e.UserBan.Username} has been permanently banned!", "info");
        }
        // #EVENT 1A
        public static void OnSuspended(object s, OnSuspendedArgs e)
        {
            string message = $"Can't connect to {e.Channel} because this channel has been banned from Twitch!";
            ConsoleUtil.LOG(message, "err");
            LogWorker.Log(message, LogWorker.LogTypes.Warn, $"TwitchEventHandler\\OnSuspended#{e.Channel}");
        }
        public static void OnUserTimedout(object s, OnUserTimedoutArgs e)
        {
            ConsoleUtil.LOG($"({e.UserTimeout.Channel}) User {e.UserTimeout.Username} has been blocked for {e.UserTimeout.TimeoutDuration} seconds", "info");
        }
        // #EVENT 2A
        public static void OnReSubscriber(object s, OnReSubscriberArgs e)
        {
            string message = $"({e.Channel}) {e.ReSubscriber.DisplayName} has renewed his subscription! He has been subscribing for {e.ReSubscriber.MsgParamCumulativeMonths} ​​month(s) \"{e.ReSubscriber.ResubMessage}\"";
            ConsoleUtil.LOG(message, "info");
            LogWorker.Log(message, LogWorker.LogTypes.Info, $"TwitchEventHandler\\OnReSubscriber");
        }
        // #EVENT 3A
        public static void OnGiftedSubscription(object s, OnGiftedSubscriptionArgs e)
        {
            string message = $"({e.Channel}) {e.GiftedSubscription.DisplayName} has given a subscription to {e.GiftedSubscription.MsgParamRecipientDisplayName} for {e.GiftedSubscription.MsgParamMonths} ​​month(s)!";
            ConsoleUtil.LOG(message, "info");
            LogWorker.Log(message, LogWorker.LogTypes.Info, $"TwitchEventHandler\\OnGiftedSubscription");
        }
        // #EVENT 4A
        public static void OnRaidNotification(object s, OnRaidNotificationArgs e)
        {
            string message = $"[TW] [{e.Channel}] PagMan RAID from @{e.RaidNotification.DisplayName} with {e.RaidNotification.MsgParamViewerCount} raider(s)";
            ConsoleUtil.LOG(message, "info");
            LogWorker.Log(message, LogWorker.LogTypes.Info, $"TwitchEventHandler\\OnRaidNotification");
        }
        // #EVENT 5A
        public static void OnNewSubscriber(object s, OnNewSubscriberArgs e)
        {
            string message = $"[TW] [{e.Channel}] {e.Subscriber.DisplayName} subscribed! \"{e.Subscriber.ResubMessage}\"";
            ConsoleUtil.LOG(message, "info");
            LogWorker.Log(message, LogWorker.LogTypes.Info, $"TwitchEventHandler\\OnNewSubscriber");
        }
        public static void OnMessageCleared(object s, OnMessageClearedArgs e)
        {
            ConsoleUtil.LOG($"[TW] [{e.Channel}] The message \"{e.Message}\" has been deleted!", "info");
            Bot.Client.Reconnect(); // wtf bro
        }
        // #EVENT 6A
        public static void OnIncorrectLogin(object s, OnIncorrectLoginArgs e)
        {
            ConsoleUtil.LOG("Wrong twitch data!", "kernel", ConsoleColor.Red);
            LogWorker.Log("Wrong twitch data!", LogWorker.LogTypes.Err, $"TwitchEventHandler\\OnIncorrectLogin");
            Bot.Restart();
        }
        public static void OnChatCleared(object s, OnChatClearedArgs e)
        {
            ConsoleUtil.LOG($"[TW] [{e.Channel}] The chat was cleared!", "info");
        }
        // #EVENT 7A
        public static void OnError(object s, OnErrorEventArgs e)
        {
            string message = $"[TWITCLIB] Library error! {e.Exception.Message}";
            ConsoleUtil.LOG(message, "kernel", ConsoleColor.Red);
            LogWorker.Log(message, LogWorker.LogTypes.Err, $"TwitchEventHandler\\OnError#{e.Exception.Message}\\{e.Exception.Source}");
        }
        // #EVENT 8A
        public static void OnLeftChannel(object s, OnLeftChannelArgs e)
        {
            string message = $"[TW] Succeful leaved from @{e.Channel}";
            ConsoleUtil.LOG(message, "info");
            LogWorker.Log(message, LogWorker.LogTypes.Err, $"TwitchEventHandler\\OnLeftChannel#{e.Channel}");
        }
        // #EVENT 9A
        public static void OnReconnected(object s, OnReconnectedEventArgs e)
        {
            ConsoleUtil.LOG("Reconnected!", "main");
            Bot.Reconnected = true;
        }
        // #EVENT 0B
        public static void OnTwitchDisconnected(object s, OnDisconnectedEventArgs e)
        {
            if (Thread.CurrentThread.Name == BotEngine.restartedTimes.ToString() && !Bot.Client.IsConnected)
            {
                ConsoleUtil.LOG("Disconnected! Restarting...", "err");
                LogWorker.Log($"Disconnected! Restarting... [Thread №{Thread.CurrentThread.Name}]", LogWorker.LogTypes.Warn, $"TwitchEventHandler\\OnTwitchDisconnected");

                if (!BotEngine.isRestarting)
                    Bot.Restart();
            }
        }
        public static void OnCommunitySubscription(object s, OnCommunitySubscriptionArgs e)
        {
            ConsoleUtil.LOG($"[TW] [{e.Channel}] {e.GiftedSubscription.DisplayName} was gifted {e.GiftedSubscription.MsgParamMassGiftCount} subscription(s) on {e.GiftedSubscription.MsgParamMultiMonthGiftDuration} month(s)", "info");
        }
        public static void OnAnnounce(object s, OnAnnouncementArgs e)
        {
            ConsoleUtil.LOG($"[TW] [{e.Channel}] Announce {e.Announcement.Message} ", "info");
        }
        // #EVENT 1B
        public static void OnContinuedGiftedSubscription(object s, OnContinuedGiftedSubscriptionArgs e)
        {
            string message = $"[TW] [{e.Channel}] User @{e.ContinuedGiftedSubscription.DisplayName} extended gift subscription!";
            ConsoleUtil.LOG(message, "info");
            LogWorker.Log(message, LogWorker.LogTypes.Info, $"TwitchEventHandler\\OnContinuedGiftedSubscription");
        }
        // #EVENT 2B
        public static void OnBanned(object s, OnBannedArgs e)
        {
            try
            {
                DataManager mngr = new();
                var N2IPath = Bot.NicknameToIDPath + e.Channel.ToLower() + ".txt";
                string disconnectedChannel = File.ReadAllText(N2IPath);

                ConsoleUtil.LOG($"[TW] Bot was banned in channel @{e.Channel}!", "info");
                string[] channels = DataManager.GetData<string[]>(Bot.SettingsPath, "channels");
                List<string> list = new();
                foreach (var channel in channels)
                {
                    if (channel.ToLower() != disconnectedChannel.ToLower())
                    {
                        list.Add(channel);
                    }
                }
                DataManager.SaveData(Bot.SettingsPath, "channels", JToken.FromObject(list));
                LogWorker.Log($"[TW] Bot was banned in channel @{e.Channel}!", LogWorker.LogTypes.Warn, "event2B");
                Bot.Client.LeaveChannel(e.Channel);
            }
            catch (Exception ex)
            {
                ConsoleUtil.ErrorOccured(ex, $"Twitch\\OnBannedEvent#{e.Channel}");
            }
        }
        // #EVENT 3B
        public static void OnConnectionError(object s, OnConnectionErrorArgs e)
        {
            string message = $"[TW] Connection error! \"{e.Error.Message}\"";
            ConsoleUtil.LOG(message, "err", ConsoleColor.Red);
            LogWorker.Log(message, LogWorker.LogTypes.Err, $"TwitchEventHandler\\OnConnectionError");
            Bot.Restart();
        }
        // #EVENT 4B
        public static async void OnJoin(object sender, OnJoinedChannelArgs e)
        {
            ConsoleUtil.LOG($"[TW] Connected to {e.Channel}", "main", ConsoleColor.Black, ConsoleColor.Cyan);

            if (Bot.VersionChangeAnnounceChannels.Contains(NamesUtil.GetUserID(e.Channel)) || e.Channel.Equals(Bot.BotNick.ToLower()))
                ChatUtil.TwitchSendMessage(e.Channel, $"butterBror v.{BotEngine.botVersion}{BotEngine.patchID}", e.Channel, "", "ru", true);

            if (Bot.ConnectionAnnounceChannels.Contains(NamesUtil.GetUserID(e.Channel)))
                ChatUtil.TwitchSendMessage(e.Channel, "butterBror Connected!", e.Channel, "", "ru", true);

            if (Bot.Reconnected && Bot.ReconnectionAnnounceChannels.Contains(NamesUtil.GetUserID(e.Channel)) || e.Channel.Equals(Bot.BotNick.ToLower()))
                ChatUtil.TwitchSendMessage(e.Channel, "butterBror Reconnected!", e.Channel, "", "ru", true);
            if (!Bot.Channels.Contains(e.Channel))
                Bot.Channels.Append(e.Channel);
        } // Обработка присоединения к каналу

        public static void OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            // Проверка и создание папки с сообщениями
            try
            {
                CommandUtil.MessageWorker(e.ChatMessage.UserId, e.ChatMessage.RoomId, e.ChatMessage.Username, e.ChatMessage.Message, e, e.ChatMessage.Channel, butterBib.Platforms.Twitch, null);
            }
            catch (Exception ex)
            {
                ConsoleUtil.ErrorOccured(ex, $"Twitch\\OnMessageReceived#User:{e.ChatMessage.UserId}, Message:{e.ChatMessage.Message}");
            }
        }
    }
}
