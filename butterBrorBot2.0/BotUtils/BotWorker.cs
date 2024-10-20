using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using System.Net.NetworkInformation;
using butterBib;
using butterBror.Utils;
using butterBror.Utils.DataManagers;
using Jint.Runtime;
using TwitchLib.Client.Models;

namespace butterBror
{
    namespace Utils
    {
        /// <summary>
        /// БЕЗ БАНВОРДОВ!
        /// </summary>
        public class NoBanwords
        {
            private static string FoundedBanWord = "";
            /// <summary>
            /// Полная проверка
            /// </summary>
            public static bool fullCheck(string message, string channelID)
            {
                FoundedBanWord = "";
                bool IsCheckFailed = false;
                string FoundSector = "";

                Guid myuuid = Guid.NewGuid();
                string checkUUID = myuuid.ToString();
                var message2 = TextUtil.FilterText(TextUtil.RemoveDuplicateLetters(message.Replace(" ", "").Replace("󠀀", "")));

                ConsoleUtil.LOG($"[check#{checkUUID}] Проверка \"{message}\" ({message2}) (ChlID: " + channelID + ")...");

                bool chck1 = false;
                bool chck2 = false;
                bool chck3 = false;
                bool chck4 = false;
                bool chck5 = false;

                // Проверка
                chck1 = CheckBannedWords(message2, channelID);
                if (chck1)
                {
                    chck2 = CheckReplacement(message2, channelID);
                    if (chck2)
                    {
                        chck3 = CheckBannedWords(TextUtil.ChangeLayout(message2), channelID);
                        if (chck3)
                        {
                            chck4 = CheckReplacement(TextUtil.ChangeLayout(message2), channelID);
                            if (chck4)
                            {
                                chck5 = LinkCheck(message);
                                if (!chck5)
                                {
                                    IsCheckFailed = true;
                                    FoundSector = "LinksCheck";
                                }
                            }
                            else
                            {
                                IsCheckFailed = true;
                                FoundSector = "LayoutChangeReplacementCheck";
                            }
                        }
                        else
                        {
                            IsCheckFailed = true;
                            FoundSector = "LayoutChangeCheck";
                        }
                    }
                    else
                    {
                        IsCheckFailed = true;
                        FoundSector = "LightReplacemetCheck";
                    }
                }
                else
                {
                    IsCheckFailed = true;
                    FoundSector = "LightCheck";
                }
                if (IsCheckFailed)
                {
                    ConsoleUtil.LOG($"[check#{checkUUID}] ОБНАРУЖЕНЫ БАНВОРДЫ! Банворд: {FoundedBanWord}, сектор поиска: {FoundSector}.", ConsoleColor.Red);
                }
                else
                {
                    ConsoleUtil.LOG($"[check#{checkUUID}] Успешно! Банворды не найдены.", ConsoleColor.Green);
                }

                return !IsCheckFailed;
            }
            /// <summary>
            /// Проверка текста
            /// </summary>
            public static bool CheckBannedWords(string message, string channelID)
            {
                try
                {
                    string bannedWordsPath = Bot.BanWordsPath;
                    string channelBannedWordsPath = Bot.ChannelsPath + channelID + "/BANWORDS.txt";
                    // Загрузка списка запрещенных слов из файлов
                    List<string> bannedWords = File.ReadAllLines(bannedWordsPath).ToList();
                    if (File.Exists(channelBannedWordsPath))
                    {
                        bannedWords.AddRange(File.ReadAllLines(channelBannedWordsPath));
                    }
                    // Проверка наличия запрещенных слов в сообщении
                    foreach (string word in bannedWords)
                    {
                        if (message.ToLower().Contains(word.ToLower()))
                        {
                            FoundedBanWord = word;
                            return false;
                        }
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    ConsoleUtil.ErrorOccured(ex.Message, $"noBanWords\\CheckBannedWords#{channelID}\\{message}");
                    return false;
                }
            }
            /// <summary>
            /// Включить в текст замены и перепроверить
            /// </summary>
            public static bool CheckReplacement(string message, string ChannelID)
            {
                try
                {
                    string banWordsReplacementPath = Bot.BanWordsReplacementPath;
                    Dictionary<string, string> replacementWords = new Dictionary<string, string>();
                    string[] lines = File.ReadAllLines(banWordsReplacementPath);
                    foreach (string line in lines)
                    {
                        string[] parts = line.Split("::");
                        replacementWords[parts[0].Trim().Replace("\"", "")] = parts[1].Trim().Replace("\"", "");
                    }
                    string maskedWord = "";
                    foreach (var pair in replacementWords)
                    {
                        maskedWord = message.ToLower().Replace(pair.Key, pair.Value);
                    }
                    return CheckBannedWords(maskedWord, ChannelID);
                }
                catch (Exception ex)
                {
                    ConsoleUtil.ErrorOccured(ex.Message, $"noBanWords\\CheckReplacement#{ChannelID}\\{message}");
                    return false;
                }
            }
            /// <summary>
            /// Проверка ссылок в тексте
            /// </summary>
            public static bool LinksCheck(string message)
            {
                string[] words = message.Split(' ');

                foreach (string word in words)
                {
                    bool result = LinkCheck(word);
                    if (result)
                    {
                        return false;
                    }
                }
                return true;
            }
            /// <summary>
            /// Проверка ссылки
            /// </summary>
            public static bool LinkCheck(string link)
            {
                // Регулярное выражение для поиска URL в тексте
                string pattern = @"(?:[a-zA-Z]+://)?[a-zA-ZА-Яа-я0-9-]+(?:\.[a-z0-9-]+)+\b";
                Regex regex = new Regex(pattern);

                foreach (Match match in regex.Matches(link))
                {
                    string url = match.Value.ToLower(); // Получаем URL из текста

                    // Используем Uri для разбора URL и получения основного домена
                    try
                    {
                        Uri uri = new Uri(url);
                        string domain = uri.Host; // Получаем только доменное имя без префиксов

                        // Делаем проверку пингом (пример вашего кода)
                        PingUtil ping = new(); // Проверьте, как создаётся экземпляр в вашем коде
                        ping.Ping(domain, 1000); // Пингуем только основной домен

                        if (ping.PingResult.Status == IPStatus.Success)
                        {
                            NoBanwords.FoundedBanWord = domain;
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        // Обработка неверного формата URL, если это нужно (Не нужно)
                    }
                }
                return true; // Если ни один URL не прошел проверку
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
            public static string GetTranslation(string userLang, string key, string channel)
            {
                if (!translations.ContainsKey(userLang))
                {
                    translations[userLang] = LoadTranslations(userLang);
                }
                if (!customTranslations.ContainsKey(channel))
                {
                    customTranslations[channel] = new();
                    customTranslations[channel][userLang] = LoadCustomTranslations(userLang, channel);
                }
                else if (!customTranslations[channel].ContainsKey(userLang))
                {
                    customTranslations[channel][userLang] = LoadCustomTranslations(userLang, channel);
                }

                if (customTranslations[channel][userLang].ContainsKey(key))
                {
                    return customTranslations[channel][userLang][key];
                }
                else if (translations[userLang].ContainsKey(key))
                {
                    return translations[userLang][key];
                }
                else
                {
                    ConsoleUtil.ErrorOccured($"Перевод для ключа '{key}' не найден! ({userLang})", $"TranslationManager\\GetTranslation#{channel}\\{userLang}\\{key}");
                    return $"¯\\_(ツ)_/¯";
                }
            }
            /// <summary>
            /// Установить кастомный перевод
            /// </summary>
            public static bool SetCustomTranslation(string key, string value, string channel, string lang)
            {
                try
                {
                    string path = $"{Bot.TranslateCustomPath}{channel}\\";
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }
                    Dictionary<string, string> content = [];
                    if (File.Exists($"{path}{lang}.txt"))
                    {
                        content = LoadCustomTranslations(lang, channel);
                        if (content.ContainsKey(key))
                        {
                            customTranslations[channel][lang][key] = value;
                            content[key] = value;
                        }
                        else
                        {
                            content.Add(key, value);
                        }
                    }
                    else
                    {
                        content.Add(key, value);
                    }
                    string OutPutString = "";
                    foreach (var item in content)
                    {
                        string result = $"\"{item.Key}\":::\"{item.Value}\"";
                        if (OutPutString == "")
                        {
                            OutPutString = result;
                        }
                        else
                        {
                            OutPutString += "\n" + result;
                        }
                    }
                    FileUtil.SaveFile($"{path}{lang}.txt", OutPutString);
                    return true;
                }
                catch (Exception ex)
                {
                    ConsoleUtil.ErrorOccured(ex.Message, $"TranslationManager\\SetCustomTranslation#{channel}\\{lang}\\{key}");
                    return false;
                }
            }
            /// <summary>
            /// Удалить кастомный перевод
            /// </summary>
            public static bool DeleteCustomTranslation(string key, string channel, string lang, Platforms platform)
            {
                try
                {
                    string path = $"{Bot.TranslateCustomPath}{channel}\\";
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }
                    Dictionary<string, string> content = [];
                    if (File.Exists($"{path}{lang}.txt"))
                    {
                        content = LoadCustomTranslations(lang, channel);
                        if (content.ContainsKey(key))
                        {
                            customTranslations[channel][lang] = content;
                            content.Remove(key);
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                    string OutPutString = "";
                    foreach (var item in content)
                    {
                        string result = $"\"{item.Key}\":::\"{item.Value}\"";
                        if (OutPutString == "")
                        {
                            OutPutString = result;
                        }
                        else
                        {
                            OutPutString += "\n" + result;
                        }
                    }
                    FileUtil.SaveFile($"{path}{lang}.txt", OutPutString);
                    return true;
                }
                catch (Exception ex)
                {
                    ConsoleUtil.ErrorOccured(ex.Message, $"TranslationManager\\DeleteCustomTranslation#{channel}\\{lang}\\{key}");
                    return false;
                }
            }
            /// <summary>
            /// Проверить наличие перевода
            /// </summary>
            public static bool TranslateContains(string key)
            {
                if (!translations.ContainsKey("ru"))
                {
                    translations["ru"] = LoadTranslations("ru");
                }

                return translations["ru"].ContainsKey(key);
            }
            /// <summary>
            /// Обновить перевод
            /// </summary>
            public static bool UpdateTranslation(string userLang, string channel)
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
                customTranslations[channel][userLang] = LoadCustomTranslations(userLang, channel);

                if (translations[userLang].Count > 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            /// <summary>
            /// Загрузить перевод
            /// </summary>
            private static Dictionary<string, string> LoadTranslations(string userLang)
            {
                string filePath = $"{Bot.TranslateDefualtPath}{userLang}.txt";
                var translations = new Dictionary<string, string>();

                if (File.Exists(filePath))
                {
                    string[] lines = File.ReadAllLines(filePath);

                    foreach (string line in lines)
                    {
                        string[] parts = line.Split(":::");
                        if (parts.Length == 2)
                        {
                            string key = parts[0].Trim().Trim('"');
                            string translation = parts[1].Trim().Trim('"');
                            translations[key] = translation;
                        }
                    }
                }
                else
                {
                    ConsoleUtil.ErrorOccured($"Translation file not found for language '{userLang}'", $"TranslationManager\\LoadTranslations#{userLang}");
                    ConsoleServer.SendConsoleMessage("errors", $"Translation file not found for language '{userLang}'");
                }

                return translations;
            }
            /// <summary>
            /// Загрузить кастомный перевод
            /// </summary>
            private static Dictionary<string, string> LoadCustomTranslations(string userLang, string channel)
            {
                string filePath = $"{Bot.TranslateCustomPath}{channel}\\{userLang}.txt";
                var translations = new Dictionary<string, string>();

                if (File.Exists(filePath))
                {
                    string[] lines = File.ReadAllLines(filePath);

                    foreach (string line in lines)
                    {
                        string[] parts = line.Split(":::");
                        if (parts.Length == 2)
                        {
                            string key = parts[0].Trim().Trim('"');
                            string translation = parts[1].Trim().Trim('"');
                            translations[key] = translation;
                        }
                    }
                }

                return translations;
            }
        }
        namespace DataManagers
        {
            /// <summary>
            /// Работа с различными данными
            /// </summary>
            public class DataManager
            {
                private static string? _filePath;
                private static dynamic _Data;
                private static Dictionary<string, dynamic> jsonsData = new Dictionary<string, dynamic>();
                private const int JSONS_MAX = 50;

                public DataManager()
                {
                    if (!Directory.Exists(Bot.UsersDataPath))
                    {
                        FileUtil.CreateDirectory(Bot.UsersDataPath);
                    }
                }
                public static void ClearData()
                {
                    if (jsonsData.Count > JSONS_MAX)
                    {
                        jsonsData.Clear();
                        ConsoleServer.SendConsoleMessage("info", "Кэш отчищен!");
                    }
                }

                // #USER 0A

                public static T? GetData<T>(string path, string paramName)
                {
                    try
                    {
                        if (jsonsData.ContainsKey(path))
                        {
                            if (jsonsData[path].ContainsKey(paramName))
                            {
                                var data = jsonsData[path][paramName];
                                return data.ToObject<T>();
                            }
                            else
                            {
                                jsonsData[path][paramName] = default(T);
                                SaveData(path, paramName, default(T));
                                return default(T);
                            }
                        }
                        else if (File.Exists(path))
                        {
                            string json = File.ReadAllText(path);
                            var userParams = JObject.Parse(json);
                            jsonsData[path] = new Dictionary<string, JToken>();
                            jsonsData[path] = userParams;
                            return userParams.GetValue(paramName).ToObject<T>();
                        }
                        else
                        {
                            return default(T);
                        }
                    }
                    catch (Exception ex)
                    {
                        ConsoleUtil.ErrorOccured(ex.Message, $"DataManager\\GetData#{path}\\{paramName}");
                        return default(T);
                    }
                }

                public static void SaveData(string path, string paramName, object value, bool autoSave = true)
                {
                    try
                    {
                        if (jsonsData.ContainsKey(path))
                        {
                            jsonsData[path][paramName] = JToken.FromObject(value);
                        }
                        else
                        {
                            string filePath = Bot.UsersDataPath + path + ".json";
                            if (!File.Exists(filePath))
                            {
                                jsonsData[path] = new Dictionary<string, JToken>();
                                jsonsData[path][paramName] = JToken.FromObject(value);
                            }
                            else
                            {
                                string json = File.ReadAllText(filePath);
                                dynamic userParams = JsonConvert.DeserializeObject(json);
                                userParams[paramName] = JToken.FromObject(value);
                                FileUtil.SaveFile(filePath, JsonConvert.SerializeObject(userParams, Formatting.Indented));
                            }
                        }
                        if (autoSave)
                        {
                            SaveParamsToFile(path);
                        }
                    }
                    catch (Exception ex)
                    {
                        ConsoleUtil.ErrorOccured(ex.Message, $"DataManager\\SaveData#{path}\\{paramName}");
                    }
                }
                public static void SaveData(string path)
                {
                    SaveParamsToFile(path);
                }
                private static void SaveParamsToFile(string path)
                {
                    string data = JsonConvert.SerializeObject(jsonsData[path], Formatting.Indented);
                    FileUtil.SaveFile(path, data);
                }
                public static bool IsContainsKey(string key, string path)
                {
                    if (jsonsData.ContainsKey(path))
                    {
                        return jsonsData[path].ContainsKey(key);
                    }
                    else
                    {
                        string filePath = Bot.UsersDataPath + path + ".json";
                        if (!File.Exists(filePath))
                        {
                            return false;
                        }
                        else
                        {
                            string json = File.ReadAllText(filePath);
                            dynamic userParams = JsonConvert.DeserializeObject(json);
                            jsonsData[path] = new Dictionary<string, dynamic>();
                            jsonsData[path] = userParams;
                            return jsonsData[path].ContainsKey(key);
                        }
                    }
                }
            }
            /// <summary>
            /// Работа с различными данными
            /// </summary>
            public class JsonManager
            {
                private string? _filePath;
                private JObject _Data;
                private const int JSONS_MAX = 50;

                public JsonManager(string path)
                {
                    _filePath = path;
                    if (!Directory.Exists(Path.GetDirectoryName(path)))
                    {
                        FileUtil.CreateDirectory(Path.GetDirectoryName(path));
                    }
                    if (!File.Exists(path))
                    {
                        FileUtil.CreateFile(path);
                    }
                    string json = File.ReadAllText(_filePath);
                    _Data = new JObject();
                    if (json != "")
                    {
                        var userParams = JObject.Parse(json);
                        _Data = userParams;
                    }
                }
                public T? GetData<T>(string paramName)
                {
                    DebugUtil.LOG("GD1");
                    if (_Data.ContainsKey(paramName))
                    {
                        DebugUtil.LOG("GD2");
                        var data = _Data[paramName];
                        return data.ToObject<T>();
                    }
                    else
                    {
                        DebugUtil.LOG("GD3");
                        SaveData(paramName, default(T));
                        return default(T);
                    }
                }

                public void SaveData(string paramName, object value, bool autoSave = true)
                {
                    DebugUtil.LOG("SD1");
                    if (_Data.ContainsKey(paramName))
                    {
                        DebugUtil.LOG("SD2");
                        _Data[paramName] = JToken.FromObject(value);
                    }
                    else
                    {
                        DebugUtil.LOG("SD3");
                        _Data = new JObject();
                        DebugUtil.LOG("SD4");
                        _Data[paramName] = JToken.FromObject(value);
                    }
                    if (autoSave)
                    {
                        DebugUtil.LOG("SD5");
                        SaveParamsToFile();
                    }
                }
                public void SaveData()
                {
                    SaveParamsToFile();
                }
                private void SaveParamsToFile()
                {
                    string data = JsonConvert.SerializeObject(_Data, Formatting.Indented);
                    FileUtil.SaveFile(_filePath, data);
                }
                public bool IsContainsKey(string key, string path)
                {
                    if (_Data.ContainsKey(path))
                    {
                        return _Data.ContainsKey(key);
                    }
                    else
                    {
                        string filePath = Bot.UsersDataPath + path + ".json";
                        if (!File.Exists(filePath))
                        {
                            return false;
                        }
                        else
                        {
                            string json = File.ReadAllText(filePath);
                            dynamic userParams = JsonConvert.DeserializeObject(json);
                            _Data = new JObject();
                            _Data = userParams;
                            return _Data.ContainsKey(key);
                        }
                    }
                }
            }
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
                public static void Log(string message, LogType type, string sector)
                {
                    if (type == LogTypes.Err)
                    {
                        LogError(message, sector);
                    }
                    else
                    {
                        FileUtil.CreateFile(Bot.LogsPath);
                        if (start_text == "")
                        {
                            start_text = File.ReadAllText(Bot.LogsPath);
                        }

                        string Logs = start_text;
                        var D = DateTime.Now;
                        LogData newLog = new LogData
                        {
                            Text = message,
                            SectorName = sector,
                            LogType = type,
                            LogTime = DateTime.Now
                        };

                        log_cache.Add(newLog);

                        foreach (var e in log_cache)
                        {
                            Logs += $"[{e.LogTime.Year}/{e.LogTime.Month}/{e.LogTime.Day} {e.LogTime.Hour}:{e.LogTime.Minute}.{e.LogTime.Second}.{e.LogTime.Millisecond} ({e.LogTime.DayOfWeek})] [{e.LogType.Text} - Сектор: {e.SectorName}] - {e.Text}\n";
                        }
                        FileUtil.SaveFile(Bot.LogsPath, Logs, false);
                    }
                }

                private static void LogError(string message, string sector)
                {
                    FileUtil.CreateFile(Bot.ErrorsPath);
                    if (errors_start_text == "")
                    {
                        errors_start_text = File.ReadAllText(Bot.ErrorsPath);
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
                    FileUtil.SaveFile(Bot.ErrorsPath, Logs, false);
                }
            }
            /// <summary>
            /// Работа с данными пользователей
            /// </summary>
            public class UsersData
            {
                private static Dictionary<string, dynamic> userData = new Dictionary<string, dynamic>();
                private const int MAX_USERS = 500;
                /// <summary>
                /// Работа с данными пользователей
                /// </summary>
                public UsersData()
                {
                    if (!Directory.Exists(Bot.UsersDataPath))
                    {
                        FileUtil.CreateDirectory(Bot.UsersDataPath);
                    }
                }
                /// <summary>
                /// Отчистка кэша
                /// </summary>
                public static void ClearData()
                {
                    if (userData.Count > MAX_USERS)
                    {
                        userData.Clear();
                        ConsoleServer.SendConsoleMessage("info", "Кэш отчищен!");
                    }
                }
                /// <summary>
                /// Сохранение данных
                /// </summary>
                public static void SaveData(string userID)
                {
                    SaveUserParamsToFile(userID);
                }
                /// <summary>
                /// Получение данных пользователя
                /// </summary>
                public static T? UserGetData<T>(string userId, string paramName, bool withTry = true)
                {
                    if (withTry) // Надо ли проверять ошибки?
                    {
                        try
                        {
                            return UserGetData2<T>(userId, paramName);
                        }
                        catch (Exception ex)
                        {
                            ConsoleUtil.ErrorOccured(ex.Message, $"UsersData\\UserGetData#{userId}\\{paramName}");
                            return default;
                        }
                    }
                    else
                    {
                        return UserGetData2<T>(userId, paramName);
                    }
                }
                /// <summary>
                /// Работа с данными пользователя 2 - Возвращение
                /// </summary>
                private static T? UserGetData2<T>(string userId, string paramName)
                {
                    T result = default(T); // Переменная с результатом
                    string filePath = Bot.UsersDataPath + userId + ".json"; // Путь к файлу
                    if (userData.ContainsKey(userId)) // Проверка наличия пользователя в кэше
                    {
                        if (userData[userId].ContainsKey(paramName)) // Проверка наличия параметра в кэше пользователя
                        {
                            var data = userData[userId][paramName]; // Получение данных
                            if (data is JArray jArray) // Проверка на наличие списков в данных
                            {
                                result = jArray.ToObject<T>();
                            }
                            else
                            {
                                result = (T)data;
                            }
                        }
                        else
                        {
                            // Возврат null и сохранение параметра в файле
                            userData[userId][paramName] = default(T);
                            UserSaveData(userId, paramName, default(T));
                            result = default;
                        }
                    }
                    else if (File.Exists(filePath)) // Проверка наличия в базе данных
                    {
                        string json = File.ReadAllText(filePath); // Чтение из базы данных
                        dynamic userParams = JsonConvert.DeserializeObject(json); // Превратить данные в волшебный список
                        userData[userId] = userParams; // Сохранение в кэш
                        var paramData = userParams[paramName];
                        if (paramData is JArray jArray)
                        {
                            result = jArray.ToObject<T>();
                        }
                        else
                        {
                            result = (T)paramData;
                        }
                    }
                    else
                    {
                        // ¯\_(ツ)_/¯
                        // Ты кто?
                        result = default;
                    }
                    return result;
                }
                /// <summary>
                /// Сохранить данные пользователя
                /// </summary>
                public static void UserSaveData(string userId, string paramName, dynamic value, bool autoSave = true)
                {
                    try
                    {
                        if (userData.ContainsKey(userId))
                        {
                            userData[userId][paramName] = JToken.FromObject(value);
                        }
                        else
                        {
                            string filePath = Bot.UsersDataPath + userId + ".json";
                            if (!File.Exists(filePath))
                            {
                                userData[userId] = new Dictionary<string, JToken>();
                                userData[userId][paramName] = JToken.FromObject(value);
                            }
                            else
                            {
                                string json = File.ReadAllText(filePath);
                                dynamic userParams = JsonConvert.DeserializeObject(json);
                                userParams[paramName] = JToken.FromObject(value);
                                FileUtil.SaveFile(filePath, JsonConvert.SerializeObject(userParams, Formatting.Indented));
                            }
                        }
                        if (autoSave)
                        {
                            SaveUserParamsToFile(userId);
                        }
                    }
                    catch (Exception ex)
                    {
                        ConsoleUtil.ErrorOccured(ex.Message, $"UsersData\\UserSaveData#{userId}\\{paramName}");
                    }
                }

                /// <summary>
                /// Регистрация пользователя
                /// </summary>
                public static void UserRegister(string userId, string firstMessage)
                {
                    try
                    {
                        string[] empty = [];
                        Dictionary<string, dynamic> EmptyRemind = new Dictionary<string, dynamic>();
                        DateTime minusDay = DateTime.UtcNow.AddDays(-1);
                        userData[userId] = new Dictionary<string, dynamic>();
                        userData[userId]["firstSeen"] = DateTime.UtcNow;
                        userData[userId]["firstMessage"] = firstMessage;
                        userData[userId]["lastSeenMessage"] = firstMessage;
                        userData[userId]["lastSeen"] = DateTime.UtcNow;
                        userData[userId]["floatBalance"] = 0;
                        userData[userId]["balance"] = 0;
                        userData[userId]["totalMessages"] = 0;
                        userData[userId]["miningVideocards"] = empty;
                        userData[userId]["miningProccessors"] = empty;
                        userData[userId]["lastMiningClear"] = DateTime.UtcNow;
                        userData[userId]["isBotModerator"] = false;
                        userData[userId]["isBanned"] = false;
                        userData[userId]["isIgnored"] = false;
                        userData[userId]["rating"] = 500;
                        userData[userId]["invertory"] = empty;
                        userData[userId]["warningLvl"] = 3;
                        userData[userId]["isVip"] = false;
                        userData[userId]["isAfk"] = false;
                        userData[userId]["afkText"] = "";
                        userData[userId]["afkType"] = "";
                        userData[userId]["reminders"] = EmptyRemind;
                        userData[userId]["lastCookieEat"] = minusDay;
                        userData[userId]["giftedCookies"] = 0;
                        userData[userId]["eatedCookies"] = 0;
                        userData[userId]["buyedCookies"] = 0;
                        userData[userId]["userPlace"] = "";
                        userData[userId]["userLon"] = "0";
                        userData[userId]["userLat"] = "0";
                        userData[userId]["language"] = "ru";
                        userData[userId]["afkTime"] = DateTime.UtcNow;
                    }
                    catch (Exception ex)
                    {
                        ConsoleUtil.ErrorOccured(ex.Message, $"UsersData\\UserRegister#{userId}");
                    }
                }
                /// <summary>
                /// Сохранение данных в базу данных lol
                /// </summary>
                private static void SaveUserParamsToFile(string userId)
                {
                    string filePath = Bot.UsersDataPath + userId + ".json";
                    string json = JsonConvert.SerializeObject(userData[userId], Formatting.Indented);
                    FileUtil.SaveFile(filePath, json);
                }
                /// <summary>
                /// Проверка наличия определенного ключа в базе данных пользователя
                /// </summary>
                public static bool IsContainsKey(string key, string userId)
                {
                    if (userData.ContainsKey(userId))
                    {
                        return userData[userId].ContainsKey(key);
                    }
                    else
                    {
                        string filePath = Bot.UsersDataPath + userId + ".json";
                        if (!File.Exists(filePath))
                        {
                            return false;
                        }
                        else
                        {
                            string json = File.ReadAllText(filePath);
                            dynamic userParams = JsonConvert.DeserializeObject(json);
                            userData[userId] = new Dictionary<string, dynamic>();
                            userData[userId] = userParams;
                            return userData[userId].ContainsKey(key);
                        }
                    }
                }
            }
            /// <summary>
            /// Работа с сообщениями пользователей
            /// </summary>
            public class MessagesWorker
            {
                /// <summary>
                /// Класс данных о сообщении из Twitch/Discord чата
                /// </summary>
                public class Message
                {
                    public required DateTime messageDate { get; set; } // Дата выкладывания сообщения
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
                public static void SaveMessage(string channelID, string userID, DateTime messageDate, string messageText, bool isMe, bool isModerator, bool isSubscriber, bool isPartner, bool isStaff, bool isTurbo, bool isVip)
                {
                    // Инициализация функции
                    try
                    {
                        string path = Bot.ChannelsPath + channelID + "/MSGS/"; // Путь директории с сообщениями 
                        FileUtil.CreateDirectory(path); // Проверка и создание директории
                        List<Message> messages = []; // Список сообщений

                        if (File.Exists(path + userID + ".json")) // Проверка наличия файла с сообщениями
                        {
                            messages = JsonConvert.DeserializeObject<List<Message>>(File.ReadAllText(path + userID + ".json")); // Загрузка и конвертация сообщений в список
                        }
                        Message newMessage = new Message
                        {
                            messageDate = messageDate,
                            messageText = messageText,
                            isMe = isMe,
                            isModerator = isModerator,
                            isSubscriber = isSubscriber,
                            isPartner = isPartner,
                            isStaff = isStaff,
                            isTurbo = isTurbo,
                            isVip = isVip
                        }; // Новый экземпляр сообщения на сохранение

                        var pathFM = Bot.ChannelsPath + channelID + "/FM/"; // Путь к директории с первыми сообщениями пользователей
                        if (!File.Exists(pathFM + userID + ".txt") && messages.Count > 0) // Проверка наличия файла с первым сообщением пользователя
                        {
                            FileUtil.CreateDirectory(pathFM); // Проверка и создание директории с первыми сообщениями
                            Message FirstMessage = new Message
                            {
                                messageDate = messages.Last().messageDate,
                                messageText = messages.Last().messageText,
                                isMe = messages.Last().isMe,
                                isModerator = messages.Last().isModerator,
                                isSubscriber = messages.Last().isSubscriber,
                                isPartner = messages.Last().isPartner,
                                isStaff = messages.Last().isStaff,
                                isTurbo = messages.Last().isTurbo,
                                isVip = messages.Last().isVip
                            }; // Создание экземпляра с последним сообщением
                            FileUtil.SaveFile(pathFM + userID + ".json", JsonConvert.SerializeObject(FirstMessage)); // Сохранение файла с первым сообщением
                        }

                        messages.Insert(0, newMessage); // Добавление нового экземпляра в список сообщений
                        if (messages.Count > 3000) 
                        {
                            messages = messages.Take(2999).ToList(); // Удаление последнего сообщения, если сообщений больше 3000
                        }

                        FileUtil.SaveFile(path + userID + ".json", JsonConvert.SerializeObject(messages)); // Сохранение файла с сообщением
                    }
                    catch (Exception ex)
                    {
                        // Срабатывает, если произошла непридвиденная ошибка
                        ConsoleUtil.ErrorOccured(ex.Message, $"MessagesWorker\\SaveMessage#{channelID}\\{userID}");
                    }
                    // Конец
                }
                /// <summary>
                /// Получение определенного сообщения из базы данных бота
                /// </summary>
                public static Message? GetMessage(string channelID, string userID, bool isGetCustomNumber = false, int customNumber = 0)
                {
                    try
                    {
                        string path = Bot.ChannelsPath + channelID + "/MSGS/"; // Путь к директории сообщений
                        if (!File.Exists(path + userID + ".json")) // Если файл сообщений пользователя не существует, то возвращаем null
                        {
                            return null;
                        }

                        List<Message> messages = JsonConvert.DeserializeObject<List<Message>>(File.ReadAllText(path + userID + ".json")); // Создаем список сообщений пользователя
                        if (!isGetCustomNumber) // Проверка, не нужно ли вернуть определенный элемент списка
                        {
                            // Да
                            return messages[0];
                        }
                        else if (customNumber >= -1 && customNumber < messages.Count) // Проверяем, что число больше-равно 0 и меньше максимального числа в списке
                        {
                            // Нет
                            if (customNumber == -1)
                            {
                                return messages.Last();
                            }
                            else
                            {
                                return messages[customNumber];
                            }
                        }

                        return null; // Возвращаем null, если ничего не подошло
                    }
                    catch (Exception ex)
                    {
                        // Я хз когда это должно сработать
                        ConsoleUtil.ErrorOccured(ex.Message, $"MessagesWorker\\GetMessage#{channelID}\\{userID}\\{customNumber}");
                        return null;
                    }
                }
            }
            /// <summary>
            /// Работа с файлами
            /// </summary>
            public class FileUtil
            {
                /// <summary>
                /// Проверка наличия и создание директории
                /// </summary>
                public static void CreateDirectory(string path)
                {
                    try
                    {
                        if (!Directory.Exists(path))
                        {
                            Directory.CreateDirectory(path);
                            ConsoleServer.SendConsoleMessage("files", $"Создана директория '{path}'");
                        }
                    }
                    catch (Exception ex)
                    {
                        ConsoleUtil.ErrorOccured(ex.Message, $"FileUtil\\CreateDirectory#{path}");
                    }

                }
                /// <summary>
                /// Проверка наличия и создание файла
                /// </summary>
                public static void CreateFile(string path)
                {
                    try
                    {
                        if (!File.Exists(path))
                        {
                            FileStream fs = File.Create(path);
                            fs.Close();
                            ConsoleServer.SendConsoleMessage("files", $"Создан файл '{path}'");
                        }
                    }
                    catch (Exception ex)
                    {
                        ConsoleUtil.ErrorOccured(ex.Message, $"FileUtil\\CreateFile#{path}");
                    }
                } // Создание файла
                /// <summary>
                /// Сохранение данных в файл
                /// </summary>
                public static void SaveFile(string path, string content, bool isSavingErrorLogs = true)
                {
                    try
                    {
                        CreateFile(path); // Создание файла
                        string destinationFile = Bot.ReserveCopyPath + path.Replace(Bot.MainPath, ""); // Путь к резервной копии файла
                        if (!File.Exists(destinationFile))
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(destinationFile));
                            File.Copy(path, destinationFile);
                            ConsoleServer.SendConsoleMessage("files", $"Создана резервная копия файла '{path}'");
                        } // Проверка и создание резервной копии
                        using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            using (StreamWriter sw = new StreamWriter(fs))
                            {
                                sw.Write(content);
                                ConsoleServer.SendConsoleMessage("files", $"Saved data to file '{path}'");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (isSavingErrorLogs)
                        {
                            ConsoleUtil.ErrorOccured(ex.Message, $"FileUtil\\SaveFile#{path}");
                        }
                    }
                }
                /// <summary>
                /// Удаление файла
                /// </summary>
                public static void DeleteFile(string path)
                {
                    try
                    {
                        if (!File.Exists(path))
                        {
                            File.Delete(path);
                        }
                    }
                    catch (Exception ex)
                    {
                        ConsoleUtil.ErrorOccured(ex.Message, $"FileUtil\\DeleteFile#{path}");
                    }
                }
                /// <summary>
                /// Удаление директории
                /// </summary>
                public static void DeleteDirectory(string path)
                {
                    try
                    {
                        if (Directory.Exists(path))
                        {
                            Directory.Delete(path);
                        }
                    }
                    catch (Exception ex)
                    {
                        ConsoleUtil.ErrorOccured(ex.Message, $"FileUtil\\DeleteDirectory#{path}");
                    }
                }
                /// <summary>
                /// Получение файлов из директорий
                /// </summary>
                public static string[] GetFilesInDirectory(string directory)
                {
                    if (Directory.Exists(directory))
                    {
                        return Directory.GetFiles(directory);
                    }
                    return [];
                }
                /// <summary>
                /// Узнать вес файла
                /// </summary>
                public static byte[] GetFileBytes(string imagePath)
                {
                    if (File.Exists(imagePath))
                    {
                        return File.ReadAllBytes(imagePath);
                    }
                    return null;
                }
            }
        }
    }
}
