using butterBror.Utils;
using butterBror.Utils.DataManagers;
using Newtonsoft.Json.Linq;
using TwitchLib.Client.Events;
using TwitchLib.Communication.Events;
using TwitchLib.PubSub.Events;

namespace butterBror
{
    public partial class TwitchEventHandler
    {
        public static void OnConnected(object sender, OnConnectedArgs e)
        {
            if (!Bot.botAlreadyConnected)
            {

            }
        }
        public static void OnMessageSend(object s, OnMessageSentArgs e)
        {
            ConsoleServer.SendConsoleMessage("info", $"Отправленно сообщение в {e.SentMessage.Channel}! \"{e.SentMessage.Message}\"");
        }
        // #EVENT 0A
        public static void OnMessageThrottled(object s, OnMessageThrottledEventArgs e)
        {
            ConsoleServer.SendConsoleMessage("errors", $"Сообщение не отправленно! \"{e.Message}\" ");
            LogWorker.Log($"Не удалось отправить сообщение! \"{e.Message}\"", LogWorker.LogTypes.Err, $"TwitchEventHandler\\OnMessageThrottled#{e.Message}");
        }
        public static void OnLog(object s, TwitchLib.Client.Events.OnLogArgs e)
        {
            // Tools.LOG($"Twitch LOG: {e.Data}");
        }
        public static void OnUserBanned(object s, OnUserBannedArgs e)
        {
            ConsoleServer.SendConsoleMessage("info", $"({e.UserBan.Channel}) Пользователь {e.UserBan.Username} был пермаментно забанен!");
            Console.ResetColor();
        }
        // #EVENT 1A
        public static void OnSuspended(object s, OnSuspendedArgs e)
        {
            ConsoleServer.SendConsoleMessage("errors", $"Не удаётся подключится к {e.Channel}, так как этот канал был забанен на Twitch!");
            LogWorker.Log($"Не удаётся подключится к {e.Channel}, так как этот канал был забанен на Twitch!", LogWorker.LogTypes.Warn, $"TwitchEventHandler\\OnSuspended#{e.Channel}");
            Console.ResetColor();
        }
        public static void OnUserTimedout(object s, OnUserTimedoutArgs e)
        {
            ConsoleServer.SendConsoleMessage("info", $"({e.UserTimeout.Channel}) Пользователь {e.UserTimeout.Username} был заблокирован на {e.UserTimeout.TimeoutDuration} секунд");
            Console.ResetColor();
        }
        // #EVENT 2A
        public static void OnReSubscriber(object s, OnReSubscriberArgs e)
        {
            ConsoleServer.SendConsoleMessage("info", $"({e.Channel}) {e.ReSubscriber.DisplayName} продлил подписку! Он подписывается уже {e.ReSubscriber.MsgParamCumulativeMonths} месяц(ев/а) \"{e.ReSubscriber.ResubMessage}\"");
            LogWorker.Log($"{e.Channel} | {e.ReSubscriber.DisplayName} продлил подписку! Он подписывается уже {e.ReSubscriber.MsgParamCumulativeMonths} месяц(ев/а) \"{e.ReSubscriber.ResubMessage}\"", LogWorker.LogTypes.Info, $"TwitchEventHandler\\OnReSubscriber");
            Console.ResetColor();
        }
        // #EVENT 3A
        public static void OnGiftedSubscription(object s, OnGiftedSubscriptionArgs e)
        {
            ConsoleServer.SendConsoleMessage("info", $"({e.Channel}) {e.GiftedSubscription.DisplayName} подарил подписку {e.GiftedSubscription.MsgParamRecipientDisplayName} на {e.GiftedSubscription.MsgParamMonths} месяц(а/ев)!");
            LogWorker.Log($"{e.Channel} | {e.GiftedSubscription.DisplayName} подарил подписку {e.GiftedSubscription.MsgParamRecipientDisplayName} на {e.GiftedSubscription.MsgParamMonths} месяц(а/ев)!", LogWorker.LogTypes.Info, $"TwitchEventHandler\\OnGiftedSubscription");
            Console.ResetColor();
        }
        // #EVENT 4A
        public static void OnRaidNotification(object s, OnRaidNotificationArgs e)
        {
            ConsoleServer.SendConsoleMessage("info", $"({e.Channel}) Рейд от {e.RaidNotification.DisplayName} с {e.RaidNotification.MsgParamViewerCount} зрител(ем/ями)");
            LogWorker.Log($"{e.Channel} | Рейд от {e.RaidNotification.DisplayName} с {e.RaidNotification.MsgParamViewerCount} зрител(ем/ями)", LogWorker.LogTypes.Info, $"TwitchEventHandler\\OnRaidNotification");
            Console.ResetColor();
        }
        // #EVENT 5A
        public static void OnNewSubscriber(object s, OnNewSubscriberArgs e)
        {
            ConsoleServer.SendConsoleMessage("info", $"({e.Channel}) {e.Subscriber.DisplayName} купил подписку! \"{e.Subscriber.ResubMessage}\"");
            LogWorker.Log($"{e.Channel} | {e.Subscriber.DisplayName} купил подписку! \"{e.Subscriber.ResubMessage}\"", LogWorker.LogTypes.Info, $"TwitchEventHandler\\OnNewSubscriber");
            Console.ResetColor();
        }
        public static void OnMessageCleared(object s, OnMessageClearedArgs e)
        {
            ConsoleServer.SendConsoleMessage("info", $"({e.Channel}) Было удалено сообщение! \"{e.Message}\"");
            Console.ResetColor();
            Bot.client.Reconnect();
        }
        // #EVENT 6A
        public static void OnIncorrectLogin(object s, OnIncorrectLoginArgs e)
        {
            ConsoleUtil.LOG("Неверные twitch данные!", ConsoleColor.Red);
            LogWorker.Log("Неверные данные входа!", LogWorker.LogTypes.Err, $"TwitchEventHandler\\OnIncorrectLogin");
            Console.ResetColor();
            Bot.RestartPlease();
        }
        public static void OnChatCleared(object s, OnChatClearedArgs e)
        {
            ConsoleServer.SendConsoleMessage("info", $"({e.Channel}) Чат был отчищен!");
            Console.ResetColor();
        }
        // #EVENT 7A
        public static void OnError(object s, OnErrorEventArgs e)
        {
            ConsoleUtil.LOG($"Ошибка библиотеки! {e.Exception.Message}", ConsoleColor.Red);
            LogWorker.Log($"Ошибка библиотеки! {e.Exception.Message}", LogWorker.LogTypes.Err, $"TwitchEventHandler\\OnError#{e.Exception.Message}\\{e.Exception.Source}");
            Console.ResetColor();
        }
        // #EVENT 8A
        public static void OnLeftChannel(object s, OnLeftChannelArgs e)
        {
            ConsoleServer.SendConsoleMessage("info", $"Успешный выход с канала {e.Channel}");
            LogWorker.Log($"Успешный выход с канала {e.Channel}", LogWorker.LogTypes.Err, $"TwitchEventHandler\\OnLeftChannel#{e.Channel}");
            Console.ResetColor();
        }
        // #EVENT 9A
        public static void OnReconnected(object s, OnReconnectedEventArgs e)
        {
            ConsoleUtil.LOG("Переподключен!");
            foreach (string channel in Bot.reconnectionAnnounceChannels)
            {
                string name = NamesUtil.GetUsername(channel, "NONE\n");
                if (name != "NONE\n")
                {
                    ChatUtil.SendMessage(name, "butterBror Переподключен!", channel, "", "ru", true);
                }
            }
        }
        // #EVENT 0B
        public static void OnTwitchDisconnected(object s, OnDisconnectedEventArgs e)
        {
            if (Thread.CurrentThread.Name == BotEngine.restartedTimes.ToString())
            {
                if (!Bot.client.IsConnected)
                {
                    ConsoleServer.SendConsoleMessage("errors", "Был отключён от Twitch! Рестарт...");
                    LogWorker.Log($"Был отключён от Twitch! Рестарт... ({Thread.CurrentThread.Name})", LogWorker.LogTypes.Warn, $"TwitchEventHandler\\OnTwitchDisconnected");
                    Console.ResetColor();

                    if (!BotEngine.isNeedRestart)
                    {
                        Bot.RestartPlease();
                    }
                }
            }
        }
        public static void OnCommunitySubscription(object s, OnCommunitySubscriptionArgs e)
        {
            ConsoleServer.SendConsoleMessage("info", $"({e.Channel}) {e.GiftedSubscription.DisplayName} подарил {e.GiftedSubscription.MsgParamMassGiftCount} чатер(ам/у) подписку на {e.GiftedSubscription.MsgParamMultiMonthGiftDuration} месяцев");
            Console.ResetColor();
        }
        public static void OnAnnounce(object s, OnAnnouncementArgs e)
        {
            ConsoleServer.SendConsoleMessage("info", $"({e.Channel}) Аноунс {e.Announcement.Message} ");
            Console.ResetColor();
        }
        // #EVENT 1B
        public static void OnContinuedGiftedSubscription(object s, OnContinuedGiftedSubscriptionArgs e)
        {
            ConsoleServer.SendConsoleMessage("info", $"({e.Channel}) Пользователь {e.ContinuedGiftedSubscription.DisplayName} продлил подарочную подписку!");
            LogWorker.Log($"Пользователь {e.ContinuedGiftedSubscription.DisplayName} продлил подарочную подписку!", LogWorker.LogTypes.Info, $"TwitchEventHandler\\OnContinuedGiftedSubscription");
            Console.ResetColor();
        }
        // #EVENT 2B
        public static void OnBanned(object s, OnBannedArgs e)
        {
            try
            {
                DataManager mngr = new();
                var N2IPath = Bot.NicknameToIDPath + e.Channel.ToLower() + ".txt";
                string disconnectedChannel = File.ReadAllText(N2IPath);

                ConsoleServer.SendConsoleMessage("info", $"Бот был пермаментно забанен в канале {e.Channel}!");
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
                LogWorker.Log($"Бот был забанен на канале {e.Channel}!", LogWorker.LogTypes.Warn, "event2B");
                Console.ResetColor();
                Bot.client.LeaveChannel(e.Channel);
            }
            catch (Exception ex)
            {
                ConsoleUtil.ErrorOccured(ex, $"Twitch\\OnBannedEvent#{e.Channel}");
            }
        }
        // #EVENT 3B
        public static void OnConnectionError(object s, OnConnectionErrorArgs e)
        {
            ConsoleUtil.LOG($"Ошибка соединения! \"{e.Error.Message}\"", ConsoleColor.Red);
            LogWorker.Log($"Ошибка соединения к Twitch! \"{e.Error.Message}\"", LogWorker.LogTypes.Err, $"TwitchEventHandler\\OnConnectionError");
            Bot.RestartPlease();
        }
        // #EVENT 4B
        public static async void OnJoin(object sender, OnJoinedChannelArgs e)
        {
            ConsoleUtil.LOG($"Присоединился к {e.Channel}", ConsoleColor.Black, ConsoleColor.Cyan);
            Console.ResetColor();
            if (e.Channel.ToLower() == Bot.BotNick.ToLower())
            {
                if (!Bot.is_connected)
                {
                    await CommandUtil.ChangeNicknameColorAsync(TwitchLib.Client.Enums.ChatColorPresets.DodgerBlue);
                    ChatUtil.SendMessage(Bot.BotNick, "truckCrash Подключение к Twitch...", "", "", "ru", true);
                    LogWorker.Log("Подключение к Twitch...", LogWorker.LogTypes.Info, $"TwitchEventHandler\\OnJoin");
                }
            }
            else if (e.Channel == Bot.last_channel_connected.ToLower() & !Bot.is_connected)
            {
                await CommandUtil.ChangeNicknameColorAsync(TwitchLib.Client.Enums.ChatColorPresets.DodgerBlue);
                ChatUtil.SendMessage(Bot.BotNick, "butterBror Подключён!", "", "", "ru", true);
                foreach (string channel in Bot.connectionAnnounceChannels)
                {
                    string name = NamesUtil.GetUsername(channel, "NONE\n");
                    if (name != "NONE\n")
                    {
                        ChatUtil.SendMessage(name, "butterBror Подключён!", channel, "", "ru", true);
                    }
                }
                LogWorker.Log("Подключён к Twitch!", LogWorker.LogTypes.Info, $"TwitchEventHandler\\OnJoin");
                Bot.is_connected = true;
            }
            Bot.Channels.Append(e.Channel);
        } // Обработка присоединения к каналу

        public static void OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            // Проверка и создание папки с сообщениями
            try
            {
                CommandUtil.MessageWorker(e.ChatMessage.UserId, e.ChatMessage.RoomId, e.ChatMessage.Username, e.ChatMessage.Message, e, e.ChatMessage.Channel, "tw");
            }
            catch (Exception ex)
            {
                ConsoleUtil.ErrorOccured(ex, $"Twitch\\OnMessageReceived#User:{e.ChatMessage.UserId}, Message:{e.ChatMessage.Message}");
            }
        }
    }
}
