using Discord.WebSocket;
using SevenTV;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using TwitchLib.Client;

namespace butterBror.Utils.Things
{
    public class ClientWorker
    {
        public TwitchClient? Twitch;
        public DiscordSocketClient? Discord;
        public ITelegramBotClient? Telegram;
        public CancellationTokenSource TelegramCancellationToken = new CancellationTokenSource();
        public SevenTVClient SevenTV = new SevenTVClient();
    }
}
