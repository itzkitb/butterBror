using butterBror.Utils;
using butterBror.Utils.DataManagers;
using DankDB;
using Newtonsoft.Json.Linq;
using TwitchLib.Client.Events;
using TwitchLib.Communication.Events;

namespace butterBror
{
    public partial class twitch_events
    {
        public static void OnConnected(object sender, OnConnectedArgs e)
        {
            Engine.Statistics.functions_used.Add();
        }
        public static void OnMessageSend(object s, OnMessageSentArgs e)
        {
            Engine.Statistics.functions_used.Add();
            Utils.Console.WriteLine($"Message sent to {e.SentMessage.Channel}: \"{e.SentMessage.Message}\"", "info");
        }
        // #EVENT 0A
        public static void OnMessageThrottled(object s, OnMessageThrottledEventArgs e)
        {
            Engine.Statistics.functions_used.Add();
            Utils.Console.WriteLine($"Message not sent! \"{e.Message}\" ", "err");
            LogWorker.Log($"Failed to send message! \"{e.Message}\"", LogWorker.LogTypes.Err, $"TwitchEventHandler\\OnMessageThrottled#{e.Message}");
        }
        public static void OnLog(object s, TwitchLib.Client.Events.OnLogArgs e)
        {
            // Tools.LOG($"Twitch LOG: {e.Data}");
        }
        public static void OnUserBanned(object s, OnUserBannedArgs e)
        {
            Engine.Statistics.functions_used.Add();
            Utils.Console.WriteLine($"({e.UserBan.Channel}) User {e.UserBan.Username} has been permanently banned!", "info");
        }
        // #EVENT 1A
        public static void OnSuspended(object s, OnSuspendedArgs e)
        {
            Engine.Statistics.functions_used.Add();
            string message = $"Can't connect to {e.Channel} because this channel has been banned from Twitch!";
            Utils.Console.WriteLine(message, "err");
            LogWorker.Log(message, LogWorker.LogTypes.Warn, $"TwitchEventHandler\\OnSuspended#{e.Channel}");
        }
        public static void OnUserTimedout(object s, OnUserTimedoutArgs e)
        {
            Engine.Statistics.functions_used.Add();
            Utils.Console.WriteLine($"({e.UserTimeout.Channel}) User {e.UserTimeout.Username} has been blocked for {e.UserTimeout.TimeoutDuration} seconds", "info");
        }
        // #EVENT 2A
        public static void OnReSubscriber(object s, OnReSubscriberArgs e)
        {
            Engine.Statistics.functions_used.Add();
            string message = $"({e.Channel}) {e.ReSubscriber.DisplayName} has renewed his subscription! He has been subscribing for {e.ReSubscriber.MsgParamCumulativeMonths} ​​month(s) \"{e.ReSubscriber.ResubMessage}\"";
            Utils.Console.WriteLine(message, "info");
            LogWorker.Log(message, LogWorker.LogTypes.Info, $"TwitchEventHandler\\OnReSubscriber");
        }
        // #EVENT 3A
        public static void OnGiftedSubscription(object s, OnGiftedSubscriptionArgs e)
        {
            Engine.Statistics.functions_used.Add();
            string message = $"({e.Channel}) {e.GiftedSubscription.DisplayName} has given a subscription to {e.GiftedSubscription.MsgParamRecipientDisplayName} for {e.GiftedSubscription.MsgParamMonths} ​​month(s)!";
            Utils.Console.WriteLine(message, "info");
            LogWorker.Log(message, LogWorker.LogTypes.Info, $"TwitchEventHandler\\OnGiftedSubscription");
        }
        // #EVENT 4A
        public static void OnRaidNotification(object s, OnRaidNotificationArgs e)
        {
            Engine.Statistics.functions_used.Add();
            string message = $"[TW] [{e.Channel}] PagMan RAID from @{e.RaidNotification.DisplayName} with {e.RaidNotification.MsgParamViewerCount} raider(s)";
            Utils.Console.WriteLine(message, "info");
            LogWorker.Log(message, LogWorker.LogTypes.Info, $"TwitchEventHandler\\OnRaidNotification");
        }
        // #EVENT 5A
        public static void OnNewSubscriber(object s, OnNewSubscriberArgs e)
        {
            Engine.Statistics.functions_used.Add();
            string message = $"[TW] [{e.Channel}] {e.Subscriber.DisplayName} subscribed! \"{e.Subscriber.ResubMessage}\"";
            Utils.Console.WriteLine(message, "info");
            LogWorker.Log(message, LogWorker.LogTypes.Info, $"TwitchEventHandler\\OnNewSubscriber");
        }
        public static void OnMessageCleared(object s, OnMessageClearedArgs e)
        {
            Engine.Statistics.functions_used.Add();
            Utils.Console.WriteLine($"[TW] [{e.Channel}] The message \"{e.Message}\" has been deleted!", "info");
            Maintenance.twitch_client.Reconnect(); // wtf bro
        }
        // #EVENT 6A
        public static void OnIncorrectLogin(object s, OnIncorrectLoginArgs e)
        {
            Engine.Statistics.functions_used.Add();
            Utils.Console.WriteLine("Wrong twitch data!", "kernel", ConsoleColor.Red);
            LogWorker.Log("Wrong twitch data!", LogWorker.LogTypes.Err, $"TwitchEventHandler\\OnIncorrectLogin");
            Maintenance.Restart();
        }
        public static void OnChatCleared(object s, OnChatClearedArgs e)
        {
            Engine.Statistics.functions_used.Add();
            Utils.Console.WriteLine($"[TW] [{e.Channel}] The chat was cleared!", "info");
        }
        // #EVENT 7A
        public static void OnError(object s, OnErrorEventArgs e)
        {
            Engine.Statistics.functions_used.Add();
            string message = $"[TWITCLIB] Library error! {e.Exception.Message}";
            Utils.Console.WriteLine(message, "kernel", ConsoleColor.Red);
            LogWorker.Log(message, LogWorker.LogTypes.Err, $"TwitchEventHandler\\OnError#{e.Exception.Message}\\{e.Exception.Source}");
        }
        // #EVENT 8A
        public static void OnLeftChannel(object s, OnLeftChannelArgs e)
        {
            Engine.Statistics.functions_used.Add();
            string message = $"[TW] Succeful leaved from @{e.Channel}";
            Utils.Console.WriteLine(message, "info");
            LogWorker.Log(message, LogWorker.LogTypes.Err, $"TwitchEventHandler\\OnLeftChannel#{e.Channel}");
        }
        // #EVENT 9A
        public static void OnReconnected(object s, OnReconnectedEventArgs e)
        {
            Engine.Statistics.functions_used.Add();
            Utils.Console.WriteLine("Reconnected!", "main");
            Maintenance.twitch_reconnected = true;
        }
        // #EVENT 0B
        public static void OnTwitchDisconnected(object s, OnDisconnectedEventArgs e)
        {
            Engine.Statistics.functions_used.Add();
            if (Thread.CurrentThread.Name == Engine.restarted_times.ToString() && !Maintenance.twitch_client.IsConnected)
            {
                Utils.Console.WriteLine("Disconnected! Restarting...", "err");
                LogWorker.Log($"Disconnected! Restarting... [Thread №{Thread.CurrentThread.Name}]", LogWorker.LogTypes.Warn, $"TwitchEventHandler\\OnTwitchDisconnected");

                if (!Engine.restarting)
                    Maintenance.Restart();
            }
        }
        public static void OnCommunitySubscription(object s, OnCommunitySubscriptionArgs e)
        {
            Engine.Statistics.functions_used.Add();
            Utils.Console.WriteLine($"[TW] [{e.Channel}] {e.GiftedSubscription.DisplayName} was gifted {e.GiftedSubscription.MsgParamMassGiftCount} subscription(s) on {e.GiftedSubscription.MsgParamMultiMonthGiftDuration} month(s)", "info");
        }
        public static void OnAnnounce(object s, OnAnnouncementArgs e)
        {
            Engine.Statistics.functions_used.Add();
            Utils.Console.WriteLine($"[TW] [{e.Channel}] Announce {e.Announcement.Message} ", "info");
        }
        // #EVENT 1B
        public static void OnContinuedGiftedSubscription(object s, OnContinuedGiftedSubscriptionArgs e)
        {
            Engine.Statistics.functions_used.Add();
            string message = $"[TW] [{e.Channel}] User @{e.ContinuedGiftedSubscription.DisplayName} extended gift subscription!";
            Utils.Console.WriteLine(message, "info");
            LogWorker.Log(message, LogWorker.LogTypes.Info, $"TwitchEventHandler\\OnContinuedGiftedSubscription");
        }
        // #EVENT 2B
        public static async void OnBanned(object s, OnBannedArgs e)
        {
            Engine.Statistics.functions_used.Add();
            try
            {
                var N2IPath = Maintenance.path_n2id + Platform.strings[(int)Platforms.Twitch] + "/" + e.Channel.ToLower() + ".txt";
                string disconnectedChannel = FileUtil.GetFileContent(N2IPath);

                Utils.Console.WriteLine($"[TW] Bot was banned in channel @{e.Channel}!", "info");
                string[] channels = Manager.Get<string[]>(Maintenance.path_settings, "channels");
                List<string> list = new();
                foreach (var channel in channels)
                {
                    if (channel.ToLower() != disconnectedChannel.ToLower())
                    {
                        list.Add(channel);
                    }
                }
                Manager.Save(Maintenance.path_settings, "channels", JToken.FromObject(list));
                LogWorker.Log($"[TW] Bot was banned in channel @{e.Channel}!", LogWorker.LogTypes.Warn, "event2B");
                Maintenance.twitch_client.LeaveChannel(e.Channel);
            }
            catch (Exception ex)
            {
                Utils.Console.WriteError(ex, $"Twitch\\OnBannedEvent#{e.Channel}");
            }
        }
        // #EVENT 3B
        public static void OnConnectionError(object s, OnConnectionErrorArgs e)
        {
            Engine.Statistics.functions_used.Add();
            string message = $"[TW] Connection error! \"{e.Error.Message}\"";
            Utils.Console.WriteLine(message, "err", ConsoleColor.Red);
            LogWorker.Log(message, LogWorker.LogTypes.Err, $"TwitchEventHandler\\OnConnectionError");
            Maintenance.Restart();
        }
        // #EVENT 4B
        public static async void OnJoin(object sender, OnJoinedChannelArgs e)
        {
            Engine.Statistics.functions_used.Add();
            Utils.Console.WriteLine($"[TW] Connected to {e.Channel}", "main", ConsoleColor.Black, ConsoleColor.Cyan);

            if (Engine.previous_version != $"{Engine.version}#{Engine.patch}" && Engine.previous_version != string.Empty && (Maintenance.channels_version_change_announcement.Contains(Names.GetUserID(e.Channel, Platforms.Twitch)) || e.Channel.Equals(Maintenance.bot_name.ToLower())))
                Chat.TwitchSend(e.Channel, $"butterBror v.{Engine.previous_version} > v.{Engine.version}#{Engine.patch}", e.Channel, "", "ru", true);

            if (Maintenance.channels_connect_announcement.Contains(Names.GetUserID(e.Channel, Platforms.Twitch)))
                Chat.TwitchSend(e.Channel, "butterBror Connected!", e.Channel, "", "ru", true);

            if (Maintenance.twitch_reconnected && Maintenance.channels_reconnect_announcement.Contains(Names.GetUserID(e.Channel, Platforms.Twitch)) || e.Channel.Equals(Maintenance.bot_name.ToLower()))
                Chat.TwitchSend(e.Channel, "butterBror Reconnected!", e.Channel, "", "ru", true);

            if (!Maintenance.channels_list.Contains(e.Channel))
                Maintenance.channels_list.Append(e.Channel);
        } // Обработка присоединения к каналу

        public static async void OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            Engine.Statistics.functions_used.Add();
            // Проверка и создание папки с сообщениями
            try
            {
                await Command.ProcessMessageAsync(e.ChatMessage.UserId, e.ChatMessage.RoomId, e.ChatMessage.Username, e.ChatMessage.Message, e, e.ChatMessage.Channel, butterBror.Platforms.Twitch, null);
            }
            catch (Exception ex)
            {
                Utils.Console.WriteError(ex, $"Twitch\\OnMessageReceived#User:{e.ChatMessage.UserId}, Message:{e.ChatMessage.Message}");
            }
        }
    }
}
