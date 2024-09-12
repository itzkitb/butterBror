using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Text.RegularExpressions;
using TwitchLib.Client.Models;
using static butterBror.BotWorker.FileMng;
using System.Net.NetworkInformation;
using static butterBror.BotWorker.Tools;
using butterBib;

namespace butterBror
{
    public partial class BotWorker
    {
        public class NoBanwords
        {
            private static string FoundedBanWord = "";
            public static bool fullCheck(string message, string channelID)
            {
                NoBanwords.FoundedBanWord = "";
                bool IsCheckFailed = false;
                string FoundSector = "";

                Guid myuuid = Guid.NewGuid();
                string checkUUID = myuuid.ToString();
                var message2 = FilterText(RemoveDuplicateLetters(message.Replace(" ", "").Replace("󠀀", "")));

                LOG($"[check#{checkUUID}] Проверка \"{message}\" ({message2}) (ChlID: " + channelID + ")...");

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
                        chck3 = CheckBannedWords(ChangeLayout(message2), channelID);
                        if (chck3)
                        {
                            chck4 = CheckReplacement(ChangeLayout(message2), channelID);
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
                    LOG($"[check#{checkUUID}] ОБНАРУЖЕНЫ БАНВОРДЫ! Банворд: {FoundedBanWord}, сектор поиска: {FoundSector}.", ConsoleColor.Red);
                }
                else
                {
                    LOG($"[check#{checkUUID}] Успешно! Банворды не найдены.", ConsoleColor.Green);
                }

                return !IsCheckFailed;
            }
            // #NOBAN 0A
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
                    Tools.ErrorOccured(ex.Message, "noban0A");
                    return false;
                }
            }
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
                    Tools.ErrorOccured(ex.Message, "noban1A");
                    return false;
                }
            }
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
                        Tools.Pingator ping = new Tools.Pingator(); // Проверьте, как создаётся экземпляр в вашем коде
                        ping.Ping(domain, 1000); // Пингуем только основной домен

                        if (ping.PingResult.Status == IPStatus.Success)
                        {
                            NoBanwords.FoundedBanWord = domain;
                            return false;
                        }
                    }
                    catch (UriFormatException)
                    {
                        // Обработка неверного формата URL, если это нужно
                    }
                    catch (PingException)
                    {
                        // Обработка ошибок при пинге, если это нужно
                    }
                }
                return true; // Если ни один URL не прошел проверку
            }
        }
        // #TRNSLT
        public class TranslationManager
        {
            private static Dictionary<string, Dictionary<string, string>> translations = new();
            private static Dictionary<string, Dictionary<string, Dictionary<string, string>>> customTranslations = new();

            // #TRNSLT 0A
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
                    ErrorOccured($"Translation not found for key '{key}' ({userLang})", "GetTranslation");
                    return $"Translation not found for key '{key}'";
                }
            }
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
                    FileTools.SaveFile($"{path}{lang}.txt", OutPutString);
                    return true;
                }
                catch (Exception ex) 
                {
                    Tools.ErrorOccured(ex.Message, "setCustomTranslation");
                    return false;
                }
            }
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
                    FileTools.SaveFile($"{path}{lang}.txt", OutPutString);
                    return true;
                }
                catch (Exception ex)
                {
                    ErrorOccured(ex.Message, "setCustomTranslation");
                    return false;
                }
            }
            public static bool TranslateContains(string key)
            {
                if (!translations.ContainsKey("ru"))
                {
                    translations["ru"] = LoadTranslations("ru");
                }

                return translations["ru"].ContainsKey(key);
            }
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

            // #TRNSLT 1A
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
                    Tools.ErrorOccured($"Translation file not found for language '{userLang}'", "LoadTranslations");
                    ConsoleServer.SendConsoleMessage("errors", $"Translation file not found for language '{userLang}'");
                }

                return translations;
            }

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
        // #CMD
        public class FileMng
        {
            // #DATA
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
                        FileTools.CreateDirectory(Bot.UsersDataPath);
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
                        ErrorOccured(ex.Message, "DM0A");
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
                                FileTools.SaveFile(filePath, JsonConvert.SerializeObject(userParams, Formatting.Indented));
                            }
                        }
                        if (autoSave)
                        {
                            SaveParamsToFile(path);
                        }
                    }
                    catch (Exception ex)
                    {
                        ErrorOccured(ex.Message, "DM1A");
                    }
                }
                public static void SaveData(string path)
                {
                    SaveParamsToFile(path);
                }
                private static void SaveParamsToFile(string path)
                {
                    string data = JsonConvert.SerializeObject(jsonsData[path], Formatting.Indented);
                    FileTools.SaveFile(path, data);
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
            public class LogWorker
            {
                private static string logpath = "";
                public static void Ready(string path)
                {
                    logpath = path;
                }
                public static void LogInfo(string message, string sectorName)
                {
                    Log(message, "Информация", sectorName);
                }
                public static void LogWarning(string message, string sectorName)
                {
                    Log(message, "ВНИМАНИЕ", sectorName);
                }
                public static void LogError(string message, string sectorName)
                {
                    Log(message, "ОШИБКА", sectorName);
                }
                public static void LogMessage(string message, string sectorName)
                {
                    Log(message, "Сообщение", sectorName);
                }
                private static void Log(string message, string prefix, string sectorName)
                {
                    var Logs = "";
                    var Date = DateTime.Now;
                    if (File.Exists(logpath))
                    {
                        Logs = File.ReadAllText(logpath);
                    }
                    Logs += $"[{Date.Year}/{Date.Month}/{Date.Day} {Date.Hour}:{Date.Minute}.{Date.Second}.{Date.Millisecond} ({Date.DayOfWeek})] [{prefix} - Сектор: {sectorName}] - {message}\n";
                    FileTools.SaveFile(logpath, Logs, true);
                }
            }
            // #USER
            public class UsersData
            {
                private static Dictionary<string, dynamic> userData = new Dictionary<string, dynamic>();
                private const int MAX_USERS = 50;

                public UsersData()
                {
                    if (!Directory.Exists(Bot.UsersDataPath))
                    {
                        FileTools.CreateDirectory(Bot.UsersDataPath);
                    }
                }
                public static void ClearData()
                {
                    if (userData.Count > MAX_USERS)
                    {
                        userData.Clear();
                        ConsoleServer.SendConsoleMessage("info", "Кэш отчищен!");
                    }
                }
                public static void SaveData(string userID)
                {
                    SaveUserParamsToFile(userID);
                }
                // #USER 0A

                public static T? UserGetData<T>(string userId, string paramName, bool withTry = true)
                {
                    if (withTry)
                    {
                        try
                        {
                            return UserGetData2<T>(userId, paramName);
                        }
                        catch (Exception ex)
                        {
                            Tools.ErrorOccured(ex.Message, "user0A");
                            return default(T);
                        }
                    }
                    else
                    {
                        return UserGetData2<T>(userId, paramName);
                    }
                }

                private static T? UserGetData2<T>(string userId, string paramName)
                {
                    T result = default(T);
                    string filePath = Bot.UsersDataPath + userId + ".json";
                    if (userData.ContainsKey(userId))
                    {
                        if (userData[userId].ContainsKey(paramName))
                        {
                            var data = userData[userId][paramName];
                            if (data is JArray jArray)
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
                            userData[userId][paramName] = default(T);
                            UserSaveData(userId, paramName, default(T));
                            result = default;
                        }
                    }
                    else if (File.Exists(filePath))
                    {
                        string json = File.ReadAllText(filePath);
                        dynamic userParams = JsonConvert.DeserializeObject(json);
                        userData[userId] = new Dictionary<string, dynamic>();
                        userData[userId] = userParams;
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
                        result = default;
                    }
                    return result;
                }



                // #USER 1A

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
                                FileTools.SaveFile(filePath, JsonConvert.SerializeObject(userParams, Formatting.Indented));
                            }
                        }
                        if (autoSave)
                        {
                            SaveUserParamsToFile(userId);
                        }
                    }
                    catch (Exception ex)
                    {
                        Tools.ErrorOccured(ex.Message, "user1A");
                    }
                }

                // #USER 2A

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
                        Tools.ErrorOccured(ex.Message, "user2A");
                    }
                }

                private static void SaveUserParamsToFile(string userId)
                {
                    string filePath = Bot.UsersDataPath + userId + ".json";
                    string json = JsonConvert.SerializeObject(userData[userId], Formatting.Indented);
                    FileTools.SaveFile(filePath, json);
                }

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
            public class Message
            {
                public DateTime messageDate { get; set; }
                public string messageText { get; set; }
                public bool isMe { get; set; }
                public bool isModerator { get; set; }
                public bool isSubscriber { get; set; }
                public bool isPartner { get; set; }
                public bool isStaff { get; set; }
                public bool isTurbo { get; set; }
                public bool isVip { get; set; }
            }
            // #MSG
            public class MessagesWorker
            {
                // #MSG 0A
                public static void SaveMessage(string channelID, string userID, DateTime messageDate, string messageText, bool isMe, bool isModerator, bool isSubscriber, bool isPartner, bool isStaff, bool isTurbo, bool isVip)
                {
                    try
                    {
                        string path = Bot.ChannelsPath + channelID + "/MSGS/";
                        FileTools.CreateDirectory(path);

                        List<Message> messages = new List<Message>();
                        if (File.Exists(path + userID + ".json"))
                        {
                            messages = JsonConvert.DeserializeObject<List<Message>>(File.ReadAllText(path + userID + ".json"));
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
                        };

                        var pathFM = Bot.ChannelsPath + channelID + "/FM/";

                        if (!File.Exists(pathFM + userID + ".txt") && messages.Count > 0)
                        {
                            FileTools.CreateDirectory(pathFM);
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
                            };
                            FileTools.SaveFile(pathFM + userID + ".json", JsonConvert.SerializeObject(FirstMessage));
                        }

                        messages.Insert(0, newMessage); // Добавляем новое сообщение в начало списка
                        if (messages.Count > 3000)
                        {
                            messages = messages.Take(2999).ToList(); // Удаляем последнее сообщения
                        }

                        FileTools.SaveFile(path + userID + ".json", JsonConvert.SerializeObject(messages));
                    }
                    catch (Exception ex)
                    {
                        ErrorOccured(ex.Message, "msg0A");
                    }
                }
                // #MSG 1A
                public static Message? GetMessage(string channelID, string userID, int listMessageNumber)
                {
                    try
                    {
                        string path = Bot.ChannelsPath + channelID + "/MSGS/";
                        if (!File.Exists(path + userID + ".json"))
                        {
                            return default;
                        }

                        List<Message> messages = JsonConvert.DeserializeObject<List<Message>>(File.ReadAllText(path + userID + ".json"));
                        if (listMessageNumber == -1)
                        {
                            return messages.Last();
                        }
                        else if (listMessageNumber >= 0 && listMessageNumber < messages.Count)
                        {
                            return messages[listMessageNumber];
                        }

                        return default;
                    }
                    catch (Exception ex)
                    {
                        ErrorOccured(ex.Message, "msg1A");
                        return default;
                    }
                }
            }
            // #FILE
            public class FileTools
            {
                public static int copiedFiles = 0;

                // #FILE -1A
                public static void CopyDirectory(string sourceDir, string targetDir)
                {
                    DirectoryInfo dirInfo = new DirectoryInfo(sourceDir);
                    var FilesInSource = dirInfo.EnumerateFiles("*.*", SearchOption.AllDirectories).Count();
                    if (!Directory.Exists(targetDir))
                    {
                        Directory.CreateDirectory(targetDir);
                    }
                    foreach (string file in Directory.GetFiles(sourceDir))
                    {
                        string targetFile = Path.Combine(targetDir, Path.GetFileName(file));
                        File.Copy(file, targetFile, true);
                        copiedFiles++;
                    }

                    foreach (string subDir in Directory.GetDirectories(sourceDir))
                    {
                        string targetSubDir = Path.Combine(targetDir, Path.GetFileName(subDir));
                        CopyDirectory(subDir, targetSubDir);
                        Bot.p();
                    }
                }

                // #FILE 0A

                public static void CreateDirectory(string path)
                {
                    try
                    {
                        if (!Directory.Exists(path))
                        {
                            Directory.CreateDirectory(path);
                            ConsoleServer.SendConsoleMessage("files", $"Created directory '{path}'");
                        }
                    }
                    catch (Exception ex)
                    {
                        BotWorker.Tools.ErrorOccured(ex.Message, "file0A");
                    }

                } // Создание директории

                // #FILE 1A

                public static void CreateFile(string path)
                {
                    try
                    {
                        if (!File.Exists(path))
                        {
                            FileStream fs = File.Create(path);
                            fs.Close();
                            ConsoleServer.SendConsoleMessage("files", $"Created file '{path}'");
                        }
                    }
                    catch (Exception ex)
                    {
                        BotWorker.Tools.ErrorOccured(ex.Message, "file1A");
                    }
                } // Создание файла

                // #FILE 2A
                public static void SaveFile(string path, string content)
                {
                    try
                    {
                        CreateFile(path);
                        string destinationFile = Bot.ReserveCopyPath + path.Replace(Bot.MainPath, "");
                        if (!File.Exists(destinationFile))
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(destinationFile));
                            File.Copy(path, destinationFile);
                            ConsoleServer.SendConsoleMessage("files", $"Created reserve copy for '{path}'");
                        }
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
                        Tools.ErrorOccured(ex.Message, "file2A");
                    }
                } // Сохранение файла

                public static void SaveFile(string path, string content, bool notLog)
                {
                    try
                    {
                        CreateFile(path);
                        string destinationFile = Bot.ReserveCopyPath + path.Replace(Bot.MainPath, "");
                        if (!File.Exists(destinationFile))
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(destinationFile));
                            File.Copy(path, destinationFile);
                            ConsoleServer.SendConsoleMessage("files", $"Created reserve copy for '{path}'");
                        }
                        using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            using (StreamWriter sw = new StreamWriter(fs))
                            {
                                sw.Write(content);
                                ConsoleServer.SendConsoleMessage("files", $"Saved data to file '{path}'");
                            }
                        }
                    }
                    catch (Exception)
                    {
                        //
                    }
                } // Сохранение файла без обработки ошибки

                // #FILE 3A
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
                        BotWorker.Tools.ErrorOccured(ex.Message, "file3A");
                    }
                } // Удаление файла

                // #FILE 4A

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
                        ErrorOccured(ex.Message, "file4A");
                    }
                } // Удаление директории
            }
        }
    }
}
