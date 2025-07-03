using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace butterBror.Utils.Bot
{
    /// <summary>
    /// Provides centralized storage for base URLs of various platform services used in the application.
    /// </summary>
    public class URLs
    {
        /// <summary>
        /// The official Telegram domain used for Telegram-related API operations and links.
        /// </summary>
        public static string telegram = "telegram.org";

        /// <summary>
        /// The official Twitch domain used for Twitch API integration and streaming services.
        /// </summary>
        public static string twitch = "twitch.tv";

        /// <summary>
        /// The official Discord domain used for Discord bot and API operations.
        /// </summary>
        public static string discord = "discord.com";

        /// <summary>
        /// The official 7TV domain used for 7TV API integration and emote services.
        /// </summary>
        public static string seventvAPI = "7tv.io";

        /// <summary>
        /// The official 7TV domain.
        /// </summary>
        public static string seventv = "7tv.app";
    }
}
