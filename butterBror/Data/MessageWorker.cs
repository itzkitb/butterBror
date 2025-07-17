using butterBror.Models;
using butterBror.Models.DataBase;
using DankDB;
using Newtonsoft.Json;
using static butterBror.Core.Bot.Console;

namespace butterBror.Data
{
    /// <summary>
    /// Manages chat message storage and retrieval operations for Twitch/Discord platforms.
    /// </summary>
    public class MessagesWorker
    {
        private static int _maxMessages = 1000;

        /// <summary>
        /// Saves a chat message to persistent storage with caching and backup management.
        /// </summary>
        /// <param name="channelID">The channel identifier.</param>
        /// <param name="userID">The user identifier.</param>
        /// <param name="newMessage">The message data to save.</param>
        /// <param name="platform">The platform (Twitch/Discord) where the message originated.</param>
        [ConsoleSector("butterBror.Utils.DataManagers", "SaveMessage")]
        public static void SaveMessage(string channelID, string userID, Message newMessage, PlatformsEnum platform)
        {
            Engine.Statistics.FunctionsUsed.Add();
            try
            {
                string path = $"{Engine.Bot.Pathes.Channels}{PlatformsPathName.strings[(int)platform]}/{channelID}/MSGS/";
                string user_messages_path = $"{path}{userID}.json";
                if (FileUtil.FileExists(user_messages_path)) FileUtil.CreateBackup(user_messages_path);
                string first_message_path = $"{Engine.Bot.Pathes.Channels}{PlatformsPathName.strings[(int)platform]}/{channelID}/FM/";
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
                if (messages.Count > _maxMessages) messages = messages.Take(_maxMessages - 1).ToList();

                SafeManager.Save(user_messages_path, "messages", messages);

                return;
            }
            catch (Exception ex)
            {
                Write(ex);
            }
        }

        /// <summary>
        /// Retrieves chat messages from cache or persistent storage.
        /// </summary>
        /// <param name="channelID">The channel identifier.</param>
        /// <param name="userID">The user identifier.</param>
        /// <param name="platform">The platform (Twitch/Discord) to retrieve messages from.</param>
        /// <param name="isGetCustomNumber">Indicates whether to retrieve a specific message index.</param>
        /// <param name="customNumber">The message index to retrieve (-1 for last message).</param>
        /// <returns>The requested message or null if not found.</returns>
        [ConsoleSector("butterBror.Utils.DataManagers", "GetMessage")]
        public static Message GetMessage(string channelID, string userID, PlatformsEnum platform, bool isGetCustomNumber = false, int customNumber = 0)
        {
            Engine.Statistics.FunctionsUsed.Add();
            try
            {
                string path = $"{Engine.Bot.Pathes.Channels}{PlatformsPathName.strings[(int)platform]}/{channelID}/MSGS/";
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
