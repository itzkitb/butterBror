using butterBror;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using butterBror.Utils;
using butterBror.Utils.DataManagers;

namespace butterBrorBot2._0.BotUtils
{
    public class PayAccountData
    {
        public required string UserName { set; get; }
        public required string CardUUID { get; set; }
        public required string UserID { get; set; }
    }
    public class BalanceAccountData
    {
        public required string UserName { set; get; }
        public required string UserID { get; set; }
        public required string CardUUID { get; set; }
        public required ulong Butters { get; set; }
        public required int Cutlet { get; set; }
    }
    public partial class Management
    {
        public void AddCoinsToAccount(PayAccountData From, PayAccountData To, int Butters, int Cutlets)
        {

        }

        private class Worker
        {
            private static Dictionary<string, dynamic> userData = new();
            private const int MAX_USERS = 50;
            public static BalanceAccountData GetUserData(string UserID)
            {
                BalanceAccountData data = new()
                {
                    Butters = UserGetData<ulong>(UserID, "Butters"),
                    CardUUID = UserGetData<string>(UserID, "CardUUID"),
                    Cutlet = UserGetData<int>(UserID, "Cutlet"),
                    UserID = UserID,
                    UserName = NamesUtil.GetUsername(UserID, UserID)
                };
                return data;
            }
            private void UsersData()
            {
                if (!Directory.Exists(Bot.UsersDataPath))
                {
                    FileUtil.CreateDirectory(Bot.UsersDataPath);
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
                        ConsoleUtil.ErrorOccured(ex.Message, "bankGetData");
                        return default;
                    }
                }
                else
                {
                    return UserGetData2<T>(userId, paramName);
                }
            }

            private static T? UserGetData2<T>(string userId, string paramName)
            {
                T result = default;
                string filePath = Bot.UsersBankDataPath + userId + ".json";
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
                        string filePath = Bot.UsersBankDataPath + userId + ".json";
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
                    ConsoleUtil.ErrorOccured(ex.Message, "user1A");
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
                    ConsoleUtil.ErrorOccured(ex.Message, "user2A");
                }
            }

            private static void SaveUserParamsToFile(string userId)
            {
                string filePath = Bot.UsersBankDataPath + userId + ".json";
                string json = JsonConvert.SerializeObject(userData[userId], Formatting.Indented);
                FileUtil.SaveFile(filePath, json);
            }

            public static bool IsContainsKey(string key, string userId)
            {
                if (userData.ContainsKey(userId))
                {
                    return userData[userId].ContainsKey(key);
                }
                else
                {
                    string filePath = Bot.UsersBankDataPath + userId + ".json";
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
    }
}
