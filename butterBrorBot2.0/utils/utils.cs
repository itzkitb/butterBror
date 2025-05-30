using butterBror.Utils.DataManagers;
using DankDB;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;

namespace butterBror
{
    namespace Utils
    {
        /// <summary>
        /// БЕЗ БАНВОРДОВ!
        /// </summary>
        public class NoBanwords
        {
            private readonly ConcurrentDictionary<string, string> FoundedBanWords = new();

            /// <summary>
            /// Полная проверка
            /// </summary>
            public bool Check(string message, string channelID, Platforms platform)
            {
                Engine.Statistics.functions_used.Add();
                try
                {
                    bool failed = false;
                    string sector = "";
                    DateTime start_time = DateTime.UtcNow;

                    string check_UUID = Guid.NewGuid().ToString();

                    string cleared_message = TextUtil.CleanAsciiWithoutSpaces(message.ToLower());
                    string cleared_message_without_repeats = TextUtil.RemoveDuplicates(cleared_message);
                    string cleared_message_without_repeats_changed_layout = TextUtil.ChangeLayout(cleared_message_without_repeats);
                    string cleared_message_changed_layout = TextUtil.ChangeLayout(cleared_message);

                    string banned_words_path = Maintenance.path_blacklist_words;
                    string channel_banned_words_path = Maintenance.path_channels + Platform.strings[(int)platform] + "/" + channelID + "/BANWORDS.json";
                    string replacement_path = Maintenance.path_blacklist_replacements;

                    List<string> single_banwords = Manager.Get<List<string>>(banned_words_path, "single_word");
                    Dictionary<string, string> replacements = Manager.Get<Dictionary<string, string>>(replacement_path, "list") ?? new Dictionary<string, string>();
                    List<string> banned_words = Manager.Get<List<string>>(banned_words_path, "list");
                    if (FileUtil.FileExists(channel_banned_words_path))
                        banned_words.AddRange(Manager.Get<List<string>>(channel_banned_words_path, "list"));


                    (bool, string) check_result = RunCheck(channelID,
                        check_UUID,
                        banned_words,
                        single_banwords,
                        replacements,
                        cleared_message_without_repeats,
                        cleared_message_without_repeats,
                        cleared_message,
                        cleared_message_changed_layout);

                    failed = !check_result.Item1;
                    sector = check_result.Item2;

                    if (failed) Console.WriteLine($"[{check_UUID}] BANWORDS WAS FOUNDED! Banword: {FoundedBanWords[check_UUID]}, sector: {sector} (Runned in {(DateTime.UtcNow - start_time).TotalMilliseconds}ms)", "nbw", ConsoleColor.Red);
                    else Console.WriteLine($"[{check_UUID}] Succeful! Banwords was not found! (Runned in {(DateTime.UtcNow - start_time).TotalMilliseconds}ms)", "nbw", ConsoleColor.Green);

                    return !failed;
                }
                catch (Exception ex)
                {
                    Console.WriteError(ex, $"NOBANWORDS\\FullCheck");
                    return false;
                }
            }

            private (bool, string) RunCheck(string channelID, string check_UUID, List<string> banned_words,
                List<string> single_banwords, Dictionary<string, string> replacements, string cleared_message_without_repeats,
                string cleared_message_without_repeats_changed_layout, string cleared_message, string cleared_message_changed_layout)
            {
                bool processed = false;

                // Without repeats
                processed = CheckBanWords(cleared_message_without_repeats, channelID, check_UUID, banned_words, single_banwords);
                if (!processed) return (false, "LightCheckWR");

                processed = CheckReplacements(cleared_message_without_repeats, channelID, check_UUID, banned_words, single_banwords, replacements);
                if (!processed) return (false, "LightReplacemetCheckWR");

                processed = CheckBanWords(cleared_message_without_repeats_changed_layout, channelID, check_UUID, banned_words, single_banwords);
                if (!processed) return (false, "LayoutChangeCheckWR");

                processed = CheckReplacements(cleared_message_without_repeats_changed_layout, channelID, check_UUID, banned_words, single_banwords, replacements);
                if (!processed) return (false, "LayoutChangeReplacementCheckWR");

                // With repeats
                processed = CheckBanWords(cleared_message, channelID, check_UUID, banned_words, single_banwords);
                if (!processed) return (false, "LightCheck");

                processed = CheckReplacements(cleared_message, channelID, check_UUID, banned_words, single_banwords, replacements);
                if (!processed) return (false, "LightReplacemetCheck");

                processed = CheckBanWords(cleared_message_changed_layout, channelID, check_UUID, banned_words, single_banwords);
                if (!processed) return (false, "LayoutChangeCheck");

                processed = CheckReplacements(cleared_message_changed_layout, channelID, check_UUID, banned_words, single_banwords, replacements);

                return (processed, "LayoutChangeReplacementCheck");
            }

            /// <summary>
            /// Проверка текста
            /// </summary>
            private bool CheckBanWords(string message, string channelID, string check_UUID, List<string> banned_words, List<string> single_banwords)
            {
                Engine.Statistics.functions_used.Add();
                try
                {
                    var banword_found = banned_words.AsParallel().Any(word =>
                    {
                        if (message.Contains(word, StringComparison.OrdinalIgnoreCase))
                        {
                            FoundedBanWords.TryAdd(check_UUID, word);
                            return true;
                        }
                        return false;
                    });

                    if (banword_found) return false;

                    var single_banword_found = banned_words.AsParallel().Any(word =>
                    {
                        if (message.Equals(word, StringComparison.OrdinalIgnoreCase))
                        {
                            FoundedBanWords.TryAdd(check_UUID, word);
                            return true;
                        }
                        return false;
                    });

                    if (single_banword_found) return false;

                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteError(ex, $"NOBANWORDS\\CheckBannedWords#{channelID}\\{message}");
                    FoundedBanWords.TryAdd(check_UUID, "CHECK ERROR");
                    return false;
                }
            }
            /// <summary>
            /// Включить в текст замены и перепроверить
            /// </summary>
            private bool CheckReplacements(string message, string ChannelID, string check_UUID, List<string> banned_words, List<string> single_banwords, Dictionary<string, string> replacements)
            {
                Engine.Statistics.functions_used.Add();
                try
                {
                    string maskedWord = message;
                    foreach (var pair in replacements)
                    {
                        maskedWord = maskedWord.Replace(
                            pair.Key.ToLower(),
                            pair.Value.ToLower()
                        );
                    }

                    return CheckBanWords(maskedWord, ChannelID, check_UUID, banned_words, single_banwords);
                }
                catch (Exception ex)
                {
                    Console.WriteError(ex, $"NOBANWORDS\\CheckReplacement#{ChannelID}\\{message}");
                    return false;
                }
            }
        }
        /// <summary>
        /// Мэнеджер перевода
        /// </summary>
        public class TranslationManager
        {
            private static Dictionary<string, Dictionary<string, string>> translations = new();
            private static Dictionary<string, Dictionary<string, Dictionary<string, string>>> customTranslations = new();

            /// <summary>
            /// Получить перевод
            /// </summary>
            public static string GetTranslation(string userLang, string key, string channel_id, Platforms platform, Dictionary<string, string> replacements = null)
            {
                Engine.Statistics.functions_used.Add();
                try
                {
                    if (!translations.ContainsKey(userLang))
                        translations[userLang] = LoadTranslations(userLang);

                    if (!customTranslations.ContainsKey(channel_id))
                        customTranslations[channel_id] = new();

                    if (!customTranslations[channel_id].ContainsKey(userLang))
                        customTranslations[channel_id][userLang] = LoadCustomTranslations(userLang, channel_id, platform);

                    var custom = customTranslations[channel_id][userLang];
                    if (custom.TryGetValue(key, out var customValue))
                    {
                        if (replacements is not null) customValue = TextUtil.ArgumentsReplacement(customValue, replacements);
                        return customValue;
                    }

                    if (translations[userLang].TryGetValue(key, out var defaultVal))
                    {
                        if (replacements is not null) defaultVal = TextUtil.ArgumentsReplacement(defaultVal, replacements);
                        return defaultVal;
                    }

                    LogWorker.Log($"Translate \"{key}\" in lang \"{userLang}\" was not found!",
                        LogWorker.LogTypes.Warn, "TranslationManager\\GetTranslation");
                    return key;
                }
                catch (Exception ex)
                {
                    Console.WriteError(ex, $"TranslationManager\\GetTranslation#{channel_id},{key},{userLang}");
                    return null;
                }
            }
            /// <summary>
            /// Установить кастомный перевод
            /// </summary>
            public static bool SetCustomTranslation(string key, string value, string channel, string lang, Platforms platform)
            {
                Engine.Statistics.functions_used.Add();
                try
                {
                    string path = $"{Maintenance.path_translate_custom}{Platform.strings[(int)platform]}/{channel}/";
                    Directory.CreateDirectory(path);

                    var content = FileUtil.FileExists($"{path}{lang}.json")
                        ? Manager.Get<Dictionary<string, string>>($"{path}{lang}.json", "translations")
                        : new Dictionary<string, string>();

                    content[key] = value;
                    customTranslations[channel][lang] = content;

                    FileUtil.SaveFileContent(
                        $"{path}{lang}.json",
                        JsonConvert.SerializeObject(new { translations = content }, Formatting.Indented)
                    );
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteError(ex, $"TranslationManager\\SetCustomTranslation#CHNL:{channel}, LANG:{lang}, KEY:{key}");
                    return false;
                }
            }
            /// <summary>
            /// Удалить кастомный перевод
            /// </summary>
            public static bool DeleteCustomTranslation(string key, string channel, string lang, Platforms platform)
            {
                Engine.Statistics.functions_used.Add();
                try
                {
                    string path = $"{Maintenance.path_translate_custom}{Platform.strings[(int)platform]}/{channel}/";
                    if (!Directory.Exists(path)) return false;

                    var content = Manager.Get<Dictionary<string, string>>($"{path}{lang}.json", "translations");
                    if (content == null || !content.ContainsKey(key)) return false;

                    content.Remove(key);
                    FileUtil.SaveFileContent(
                        $"{path}{lang}.json",
                        JsonConvert.SerializeObject(new { translations = content }, Formatting.Indented)
                    );
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteError(ex, $"TranslationManager\\DeleteCustomTranslation#CHNL:{channel}, LANG:{lang}, KEY:{key}");
                    return false;
                }
            }
            /// <summary>
            /// Загрузить перевод
            /// </summary>
            private static Dictionary<string, string> LoadTranslations(string userLang)
            {
                Engine.Statistics.functions_used.Add();
                return Manager.Get<Dictionary<string, string>>(
                    $"{Maintenance.path_translate_default}{userLang}.json",
                    "translations"
                ) ?? new Dictionary<string, string>();
            }
            /// <summary>
            /// Загрузить кастомный перевод
            /// </summary>
            private static Dictionary<string, string> LoadCustomTranslations(string userLang, string channel, Platforms platform)
            {
                Engine.Statistics.functions_used.Add();
                return Manager.Get<Dictionary<string, string>>(
                    $"{Maintenance.path_translate_custom}{channel}/{Platform.strings[(int)platform]}/{userLang}.json",
                    "translations"
                ) ?? new Dictionary<string, string>();
            }
            /// <summary>
            /// Проверить наличие перевода
            /// </summary>
            public static bool TranslateContains(string key)
            {
                Engine.Statistics.functions_used.Add();
                if (!translations.ContainsKey("ru"))
                {
                    translations["ru"] = LoadTranslations("ru");
                }

                return translations["ru"].ContainsKey(key);
            }
            /// <summary>
            /// Обновить перевод
            /// </summary>
            public static bool UpdateTranslation(string userLang, string channel, Platforms platform)
            {
                Engine.Statistics.functions_used.Add();
                try
                {
                    if (translations.ContainsKey(userLang))
                    {
                        translations[userLang].Clear();
                    }
                    if (customTranslations.ContainsKey(channel))
                    {
                        if (customTranslations[channel].ContainsKey(userLang))
                        {
                            customTranslations[channel][userLang].Clear();
                        }
                    }

                    translations[userLang] = LoadTranslations(userLang);
                    customTranslations[channel][userLang] = LoadCustomTranslations(userLang, channel, platform);

                    if (translations[userLang].Count > 0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteError(ex, $"TranslationManager\\UpdateTranslation#{channel}\\{userLang}");
                    return false;
                }
            }
        }

        namespace DataManagers
        {
            /// <summary>
            /// Работа с логами
            /// </summary>
            public class LogWorker
            {
                /// <summary>
                /// Типы логов
                /// </summary>
                public class LogTypes
                {
                    public static readonly LogType Info = new LogType
                    {
                        Name = "Information",
                        Prefix = "ℹ",
                        Text = "инфо"
                    };
                    public static readonly LogType Warn = new LogType
                    {
                        Name = "Warning",
                        Prefix = "❗",
                        Text = "ВНИМ"
                    };
                    public static readonly LogType Err = new LogType
                    {
                        Name = "Error",
                        Prefix = "⚠",
                        Text = "ОШИБ"
                    };
                    public static readonly LogType Msg = new LogType
                    {
                        Name = "Message",
                        Prefix = "💬",
                        Text = "сооб"
                    };
                }
                /// <summary>
                /// Тип логов
                /// </summary>
                public class LogType
                {
                    public string Name { get; set; }
                    public string Prefix { get; set; }
                    public string Text { get; set; }
                }
                /// <summary>
                /// Данные в базе данных логов
                /// </summary>
                public class LogData
                {
                    public string Text { get; set; }
                    public string SectorName { get; set; }
                    public LogType LogType { get; set; }
                    public DateTime LogTime { get; set; }
                }
                static List<LogData> log_cache = [];
                static List<LogData> errors_log_cache = [];
                static string start_text = "";
                static string errors_start_text = "";
                /// <summary>
                /// Сохранение в файл логов
                /// </summary>
                public static async void Log(string message, LogType type, string sector)
                {
                    Engine.Statistics.functions_used.Add();
                    try
                    {
                        if (type == LogTypes.Err)
                            LogError(message, sector);
                        else
                        {
                            FileUtil.CreateFile(Maintenance.path_logs);
                            if (start_text == "")
                                start_text = FileUtil.GetFileContent(Maintenance.path_logs);

                            string Logs = start_text;
                            var D = DateTime.Now;
                            LogData newLog = new()
                            {
                                Text = message,
                                SectorName = sector,
                                LogType = type,
                                LogTime = DateTime.Now
                            };

                            log_cache.Add(newLog);

                            foreach (var e in log_cache)
                                Logs += $"[{e.LogTime.Year}/{e.LogTime.Month}/{e.LogTime.Day} {e.LogTime.Hour}:{e.LogTime.Minute}.{e.LogTime.Second}.{e.LogTime.Millisecond} ({e.LogTime.DayOfWeek})] [{e.LogType.Text} - Сектор: {e.SectorName}] - {e.Text}\n";

                            FileUtil.SaveFileContent(Maintenance.path_logs, Logs);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteError(ex, $"LogWorker\\LOG");
                    }
                }

                private static void LogError(string message, string sector)
                {
                    Engine.Statistics.functions_used.Add();
                    try
                    {
                        FileUtil.CreateFile(Maintenance.path_errors);
                        if (errors_start_text == "")
                        {
                            errors_start_text = FileUtil.GetFileContent(Maintenance.path_errors);
                        }
                        string Logs = errors_start_text;
                        var D = DateTime.Now;
                        LogData newLog = new LogData
                        {
                            Text = message,
                            SectorName = sector,
                            LogType = LogTypes.Err,
                            LogTime = DateTime.Now
                        };
                        errors_log_cache.Add(newLog);
                        foreach (var e in errors_log_cache)
                        {
                            Logs += $"[{e.LogTime.Year}/{e.LogTime.Month}/{e.LogTime.Day} {e.LogTime.Hour}:{e.LogTime.Minute}.{e.LogTime.Second}.{e.LogTime.Millisecond} ({e.LogTime.DayOfWeek})] [Сектор: {e.SectorName}] - {e.Text}\n";
                        }
                        FileUtil.SaveFileContent(Maintenance.path_errors, Logs);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteError(ex, $"LogWorker\\LogError");
                    }
                }
            }
            /// <summary>
            /// Работа с данными пользователей
            /// </summary>
            public class UsersData
            {
                private static readonly string directory = Maintenance.path_users;

                public static T Get<T>(string userId, string paramName, Platforms platform)
                {
                    Engine.Statistics.functions_used.Add();
                    try
                    {
                        return Manager.Get<T>(GetUserFilePath(userId, platform), paramName);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteError(ex, $"{nameof(Get)}:{userId}/{paramName}");
                        return default;
                    }
                }

                public static void Save(string userId, string paramName, object value, Platforms platform)
                {
                    Engine.Statistics.functions_used.Add();
                    try
                    {
                        FileUtil.CreateBackup(GetUserFilePath(userId, platform));
                        Manager.Save(GetUserFilePath(userId, platform), paramName, value);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteError(ex, $"{nameof(Save)}:{userId}/{paramName}");
                    }
                }

                public static bool Contains(string userId, string paramName, Platforms platform)
                {
                    Engine.Statistics.functions_used.Add();
                    try
                    {
                        return Manager.Get<dynamic>(GetUserFilePath(userId, platform), paramName) is not null;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteError(ex, $"{nameof(Get)}:{userId}/{paramName}");
                        return false;
                    }
                }

                public static void Register(string userId, string firstMessage, Platforms platform)
                {
                    Engine.Statistics.functions_used.Add();
                    string path = GetUserFilePath(userId, platform);
                    Manager.Save(path, "firstSeen", DateTime.UtcNow);
                    Manager.Save(path, "firstMessage", firstMessage);
                    Manager.Save(path, "lastSeenMessage", firstMessage);
                    Manager.Save(path, "lastSeen", DateTime.UtcNow);
                    Manager.Save(path, "floatBalance", 0);
                    Manager.Save(path, "balance", 0);
                    Manager.Save(path, "totalMessages", 0);
                    Manager.Save(path, "miningVideocards", new JArray());
                    Manager.Save(path, "miningProcessors", new JArray());
                    Manager.Save(path, "lastMiningClear", DateTime.UtcNow);
                    Manager.Save(path, "isBotModerator", false);
                    Manager.Save(path, "isBanned", false);
                    Manager.Save(path, "isIgnored", false);
                    Manager.Save(path, "rating", 500);
                    Manager.Save(path, "inventory", new JArray());
                    Manager.Save(path, "warningLvl", 3);
                    Manager.Save(path, "isVip", false);
                    Manager.Save(path, "isAfk", false);
                    Manager.Save(path, "afkText", string.Empty);
                    Manager.Save(path, "afkType", string.Empty);
                    Manager.Save(path, "reminders", new JObject());
                    Manager.Save(path, "lastCookieEat", DateTime.UtcNow.AddDays(-1));
                    Manager.Save(path, "giftedCookies", 0);
                    Manager.Save(path, "eatedCookies", 0);
                    Manager.Save(path, "buyedCookies", 0);
                    Manager.Save(path, "userPlace", string.Empty);
                    Manager.Save(path, "userLon", "0");
                    Manager.Save(path, "userLat", "0");
                    Manager.Save(path, "language", "ru");
                    Manager.Save(path, "afkTime", DateTime.UtcNow);
                }

                private static string GetUserFilePath(string userId, Platforms platform)
                {
                    Engine.Statistics.functions_used.Add();
                    return Path.Combine(directory, $"{Platform.strings[(int)platform]}/{userId}.json");
                }
            }
            /// <summary>
            /// Работа с сообщениями пользователей
            /// </summary>
            public class MessagesWorker
            {
                private static int max_messages = 1000;

                /// <summary>
                /// Класс данных о сообщении из Twitch/Discord чата
                /// </summary>
                public class Message
                {
                    public required DateTime messageDate { get; set; }
                    public required string messageText { get; set; }
                    public required bool isMe { get; set; }
                    public required bool isModerator { get; set; }
                    public required bool isSubscriber { get; set; }
                    public required bool isPartner { get; set; }
                    public required bool isStaff { get; set; }
                    public required bool isTurbo { get; set; }
                    public required bool isVip { get; set; }
                }
                /// <summary>
                /// Сохранение нового сообщения в базу данных бота
                /// </summary>
                public static async Task SaveMessage(string channelID, string userID, Message newMessage, Platforms platform)
                {
                    Engine.Statistics.functions_used.Add();
                    try
                    {
                        string path = $"{Maintenance.path_channels}{Platform.strings[(int)platform]}/{channelID}/MSGS/";
                        string user_messages_path = $"{path}{userID}.json";
                        string first_message_path = $"{Maintenance.path_channels}{Platform.strings[(int)platform]}/{channelID}/FM/";
                        FileUtil.CreateDirectory(first_message_path);
                        FileUtil.CreateDirectory(path);
                        List<Message> messages = [];

                        if (Worker.cache.TryGet(user_messages_path, out var value)) messages = Manager.Get<List<Message>>(user_messages_path, "messages");
                        else
                        {
                            if (File.Exists(user_messages_path))
                            {
                                string content = FileUtil.GetFileContent(user_messages_path);
                                if (content.StartsWith("[{\""))
                                {
                                    messages = JsonConvert.DeserializeObject<List<Message>>(content);
                                    FileUtil.DeleteFile(user_messages_path);
                                    Manager.CreateDatabase(user_messages_path);
                                }
                                else messages = Manager.Get<List<Message>>(user_messages_path, "messages");
                            }
                        }

                        if (!File.Exists(first_message_path + userID + ".txt") && messages.Count > 0)
                        {
                            Message FirstMessage = messages.Last();
                            FileUtil.SaveFileContent(first_message_path + userID + ".json", JsonConvert.SerializeObject(FirstMessage));
                        }

                        messages.Insert(0, newMessage);
                        if (messages.Count > max_messages) messages = messages.Take(max_messages - 1).ToList();

                        Manager.Save(user_messages_path, "messages", messages);

                        return;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteError(ex, $"MessagesWorker\\SaveMessage#{channelID}\\{userID}");
                    }
                }
                /// <summary>
                /// Получение определенного сообщения из базы данных бота
                /// </summary>
                public static async Task<Message> GetMessage(string channelID, string userID, Platforms platform, bool isGetCustomNumber = false, int customNumber = 0)
                {
                    Engine.Statistics.functions_used.Add();
                    try
                    {
                        string path = $"{Maintenance.path_channels}{Platform.strings[(int)platform]}/{channelID}/MSGS/";
                        string user_messages_path = $"{path}{userID}.json";
                        if (!File.Exists(path + userID + ".json")) return null;

                        List<Message> messages = [];

                        if (Worker.cache.TryGet(user_messages_path, out var value)) messages = Manager.Get<List<Message>>(user_messages_path, "messages");
                        else
                        {
                            string content = File.ReadAllText(user_messages_path);
                            if (content.StartsWith("[{\""))
                            {
                                messages = JsonConvert.DeserializeObject<List<Message>>(content);
                                FileUtil.DeleteFile(user_messages_path);
                                Manager.CreateDatabase(user_messages_path);
                            }
                            else messages = Manager.Get<List<Message>>(user_messages_path, "messages");
                        }

                        if (!isGetCustomNumber) return messages[0];
                        else if (customNumber >= -1 && customNumber < messages.Count)
                        {
                            if (customNumber == -1) return messages.Last();
                            else return messages[customNumber];
                        }

                        return null;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteError(ex, $"MessagesWorker\\GetMessage#{channelID}\\{userID}\\{customNumber}");
                        return null;
                    }
                }
            }
            /// <summary>
            /// Работа с файлами
            /// </summary>
            public static class FileUtil
            {
                private static readonly LruCache<string, string> _fileCache = new(100);

                public static void CreateDirectory(string directoryPath)
                {
                    Directory.CreateDirectory(directoryPath);
                }

                public static bool DirectoryExists(string directoryPath)
                {
                    return Directory.Exists(directoryPath);
                }

                public static bool FileExists(string filePath)
                {
                    return File.Exists(filePath);
                }

                public static void CreateFile(string filePath)
                {
                    var directory = Path.GetDirectoryName(filePath);
                    if (!string.IsNullOrEmpty(directory) && !DirectoryExists(directory))
                    {
                        CreateDirectory(directory);
                    }
                    if (!FileExists(filePath))
                    {
                        using (File.Create(filePath)) { }
                        _fileCache.AddOrUpdate(filePath, "");
                    }
                }

                public static void DeleteFile(string filePath)
                {
                    if (FileExists(filePath))
                    {
                        File.Delete(filePath);
                        _fileCache.Invalidate(filePath);
                    }
                }

                public static string GetFileContent(string filePath)
                {
                    return _fileCache.GetOrAdd(filePath, key =>
                    {
                        if (FileExists(key))
                            return File.ReadAllText(key);

                        throw new FileNotFoundException($"File {key} not found");
                    });
                }

                public static void SaveFileContent(string filePath, string content)
                {
                    CreateBackup(filePath);
                    CreateFile(filePath);

                    var directory = Path.GetDirectoryName(filePath);
                    if (!string.IsNullOrEmpty(directory) && !DirectoryExists(directory))
                    {
                        CreateDirectory(directory);
                    }

                    RetryIOAction(() =>
                    {
                        File.WriteAllText(filePath, content);
                        _fileCache.AddOrUpdate(filePath, content);
                    });
                }

                public static void ClearCache()
                {
                    _fileCache.Clear();
                }

                private static bool IsPathInDirectory(string filePath, string directoryPath)
                {
                    var directory = Path.GetDirectoryName(filePath);
                    return !string.IsNullOrEmpty(directory) &&
                           directory.StartsWith(directoryPath, StringComparison.OrdinalIgnoreCase);
                }

                public static void CreateBackup(string filePath)
                {
                    var backupPath = GetBackupPath(filePath);
                    var backupDir = Path.GetDirectoryName(backupPath);

                    if (!FileExists(backupPath) && FileExists(filePath))
                    {
                        if (!string.IsNullOrEmpty(backupDir))
                        {
                            Directory.CreateDirectory(backupDir);
                            RetryIOAction(() =>
                            {
                                File.Copy(filePath, backupPath, overwrite: true);
                            });
                        }
                    }
                }

                private static string GetBackupPath(string originalPath)
                {
                    Engine.Statistics.functions_used.Add();
                    var fileName = $"{Maintenance.path_reserve_copies}{DateTime.UtcNow.Hour}/{originalPath.Replace(Maintenance.path_main, "")}";

                    return fileName;
                }

                private static void RetryIOAction(Action action, int retries = 3, int delay = 100)
                {
                    for (int i = 0; i < retries; i++)
                    {
                        try
                        {
                            action();
                            return;
                        }
                        catch (IOException) when (i < retries - 1)
                        {
                            Thread.Sleep(delay);
                        }
                    }
                }
            }
        }
    }
}
