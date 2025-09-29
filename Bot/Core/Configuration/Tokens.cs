using bb.Core.Configuration;
using static bb.Core.Configuration.TwitchToken;

namespace bb.Core.Configuration
{
    /// <summary>
    /// Internal class storing authentication tokens for various external services used by the application.
    /// </summary>
    internal class Tokens
    {
        /// <summary>
        /// Gets or sets the Telegram bot API token used for Telegram integration.
        /// </summary>
        public string? Telegram;

        /// <summary>
        /// Gets or sets Twitch authentication data including access token and refresh token.
        /// </summary>
        public TokenData? Twitch;

        /// <summary>
        /// Gets or sets the Discord bot token used for Discord API authentication.
        /// </summary>
        public string? Discord;

        /// <summary>
        /// Gets or sets the Twitch secret application token used for OAuth authentication flows.
        /// </summary>
        public string? TwitchSecretToken;

        /// <summary>
        /// Gets or sets the Imgur API client token used for image upload operations.
        /// </summary>
        public string? Imgur;

        /// <summary>
        /// Gets or sets the 7TV authentication token used for 7TV API interactions.
        /// </summary>
        public string? SevenTV;

        /// <summary>
        /// Gets or sets Twitch token management object for handling token refresh operations.
        /// </summary>
        public TwitchToken? TwitchGetter;
    }
}
