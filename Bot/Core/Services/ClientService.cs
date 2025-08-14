using Discord.WebSocket;
using SevenTV;
using Telegram.Bot;
using TwitchLib.Api;
using TwitchLib.Client;

namespace butterBror.Core.Services
{
    /// <summary>
    /// Represents client instances for various platforms used in the application.
    /// </summary>
    public class ClientService
    {
        /// <summary>
        /// Gets or sets the Twitch client instance used for Twitch API interactions.
        /// </summary>
        public TwitchClient? Twitch;

        /// <summary>
        /// Gets or sets the Twitch API client instance used for Twitch API interactions.
        /// </summary>
        public TwitchAPI? TwitchAPI;

        /// <summary>
        /// Gets or sets the Discord client instance used for Discord API interactions.
        /// </summary>
        public DiscordSocketClient? Discord;

        /// <summary>
        /// Gets or sets the Telegram bot client instance used for Telegram bot interactions.
        /// </summary>
        public ITelegramBotClient? Telegram;

        /// <summary>
        /// Gets or sets the cancellation token source for Telegram operations.
        /// </summary>
        public CancellationTokenSource TelegramCancellationToken = new CancellationTokenSource();

        /// <summary>
        /// Gets or sets the 7TV client instance used for 7TV API interactions.
        /// </summary>
        public SevenTVClient SevenTV = new SevenTVClient();
    }
}
