using butterBror.Utils.DataManagers;
using DankDB;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static butterBror.Utils.Things.Console;

namespace butterBror.Utils.DataManagers
{
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

        [ConsoleSector("butterBror.Utils.DataManagers", "SaveMessage")]
        public static void SaveMessage(string channelID, string userID, Message newMessage, Platforms platform)
        {
            Core.Statistics.FunctionsUsed.Add();
            try
            {
                string path = $"{Core.Bot.Pathes.Channels}{Platform.strings[(int)platform]}/{channelID}/MSGS/";
                string user_messages_path = $"{path}{userID}.json";
                if (FileUtil.FileExists(user_messages_path)) FileUtil.CreateBackup(user_messages_path);
                string first_message_path = $"{Core.Bot.Pathes.Channels}{Platform.strings[(int)platform]}/{channelID}/FM/";
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

                if (!File.Exists(first_message_path + userID + ".txt") && messages is not null && messages.Count > 0)
                {
                    Message FirstMessage = messages.Last();
                    FileUtil.SaveFileContent(first_message_path + userID + ".json", JsonConvert.SerializeObject(FirstMessage));
                    FileUtil.CreateBackup(first_message_path + userID + ".json");
                }

                if (messages is null)
                {
                    messages = [];
                }

                messages.Insert(0, newMessage);
                if (messages.Count > max_messages) messages = messages.Take(max_messages - 1).ToList();

                SafeManager.Save(user_messages_path, "messages", messages);

                return;
            }
            catch (Exception ex)
            {
                Write(ex);
            }
        }

        [ConsoleSector("butterBror.Utils.DataManagers", "GetMessage")]
        public static Message GetMessage(string channelID, string userID, Platforms platform, bool isGetCustomNumber = false, int customNumber = 0)
        {
            Core.Statistics.FunctionsUsed.Add();
            try
            {
                string path = $"{Core.Bot.Pathes.Channels}{Platform.strings[(int)platform]}/{channelID}/MSGS/";
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
                Write(ex);
                return null;
            }
        }
    }
}
