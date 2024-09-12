using static butterBror.BotWorker.FileMng;
using static butterBror.BotWorker;
using TwitchLib.Client.Events;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace butterBror
{
    public partial class Commands
    {
        static void fishing(OnChatCommandReceivedArgs e, string lang)
        {
            string[] moveAlias = ["move", "двигаться", "place", "место", "m", "д", "p", "м"];
            if (Tools.IsNotOnCooldown(10, 1, "Fishing", e.Command.ChatMessage.UserId, e.Command.ChatMessage.RoomId))
            {
                if (e.Command.ArgumentsAsList.Count > 0)
                {
                    if (e.Command.ArgumentsAsList.ElementAt(0) == "move")
                    {
                        int nowLocation = UsersData.UserGetData<int>(e.Command.ChatMessage.UserId, "fishLocation");
                        if (e.Command.ArgumentsAsList.Count > 1)
                        {
                            int point = Tools.ToNumber(e.Command.ArgumentsAsList.ElementAt(1));
                            if (point < 11 && point > 0)
                            {
                                int distanceToLocation = 0;
                                if (point > nowLocation)
                                {
                                    distanceToLocation = point - nowLocation;
                                }
                                else
                                {
                                    distanceToLocation = nowLocation - point;
                                }
                                if (distanceToLocation != 0)
                                {
                                    Random rand = new();
                                    int randomMultiplier = rand.Next(600000, 900000);
                                    UsersData.UserSaveData(e.Command.ChatMessage.UserId, "fishIsMovingNow", true);
                                    int endTime = randomMultiplier * distanceToLocation;
                                    DateTime ArrivalTime = DateTime.UtcNow.AddMilliseconds(endTime);
                                    Tools.SendMsgReply(e.Command.ChatMessage.Channel, e.Command.ChatMessage.RoomId, TranslationManager.GetTranslation(lang, "fishMoving", "").Replace("%point%", point.ToString()).Replace("%time%", Tools.FormatTimeSpan(Tools.GetTimeTo(ArrivalTime, DateTime.UtcNow), lang)), e.Command.ChatMessage.Id, lang, true);
                                    Task task = Task.Run(() =>
                                    {
                                        Thread.Sleep(endTime);
                                        UsersData.UserSaveData(e.Command.ChatMessage.UserId, "fishIsMovingNow", true);
                                        Tools.SendMessage(UsersData.UserGetData<string>(e.Command.ChatMessage.UserId, "lastSeenChannel"), TranslationManager.GetTranslation(lang, "fishMoveEnd", "").Replace("%point%", point.ToString()).Replace("%user%", e.Command.ChatMessage.Username), Tools.GetUserID(UsersData.UserGetData<string>(e.Command.ChatMessage.UserId, "lastSeenChannel")), "", lang, true);
                                    });
                                }
                                else
                                {
                                    Tools.SendMsgReply(e.Command.ChatMessage.Channel, e.Command.ChatMessage.RoomId, TranslationManager.GetTranslation(lang, "fishWrong", ""), e.Command.ChatMessage.Id, lang, true);
                                }
                            }
                            else
                            {
                                Tools.SendMsgReply(e.Command.ChatMessage.Channel, e.Command.ChatMessage.RoomId, TranslationManager.GetTranslation(lang, "fishWrongNum", ""), e.Command.ChatMessage.Id, lang, true);
                            }
                        }
                        else
                        {
                            Tools.SendMsgReply(e.Command.ChatMessage.Channel, e.Command.ChatMessage.RoomId, TranslationManager.GetTranslation(lang, "fishCurrentPlace", "").Replace("%point%", nowLocation.ToString()), e.Command.ChatMessage.Id, lang, true);
                        }
                    }
                }
                else
                {

                }
            }
        }
    }
}
