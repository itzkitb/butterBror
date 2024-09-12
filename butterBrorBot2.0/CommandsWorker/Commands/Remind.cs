using Discord.WebSocket;
using static butterBror.BotWorker;
using TwitchLib.Client.Events;

namespace butterBror
{
    public partial class Commands
    {
        public static void Remind(OnChatCommandReceivedArgs e, SocketSlashCommand d, string lang, string platform)
        {
            string[] lookAliases = ["look", "see", "посмотреть", "l", "s", "п"];
            string[] timeAliases = ["in", "at", "в"];

            string UserID = "";
            string? RoomID = "";
            string? location = "";
            bool? isShow = false;
            bool? isPage = false;
            long? Page = 0;
            long? ShowID = 0;
            if (platform == "tw")
            {
                UserID = e.Command.ChatMessage.UserId;
                RoomID = e.Command.ChatMessage.RoomId;
                location = Tools.FilterText(e.Command.ArgumentsAsString);
            }
            else if (platform == "ds")
            {
                UserID = d.User.Id.ToString();
                RoomID = d.GuildId.ToString();
                location = d.Data.Options.FirstOrDefault(x => x.Name == "location")?.Value as string;
                if (d.Data.Options.FirstOrDefault(x => x.Name == "showpage")?.Value is long)
                {
                    Page = (long)d.Data.Options.FirstOrDefault(x => x.Name == "showpage")?.Value;
                    isPage = true;
                }
                else if (d.Data.Options.FirstOrDefault(x => x.Name == "page")?.Value is long)
                {
                    ShowID = (long)d.Data.Options.FirstOrDefault(x => x.Name == "page")?.Value;
                    isShow = true;
                }
            }
        }
    }
}
