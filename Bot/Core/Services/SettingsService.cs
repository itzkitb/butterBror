using bb.Data;
using DankDB;
using System.IO;

namespace bb.Core.Services
{
    public class SettingsService(string path)
    {
        private readonly string _path = path;

        /// <summary>
        /// Initializes a new settings file with default configuration values.
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        /// <item>Creates an empty settings file if none exists at the specified location</item>
        /// <item>Populates configuration with default values for all critical parameters</item>
        /// <item>Overwrites existing settings values (does not preserve previous configuration)</item>
        /// <item>Uses secure hashing for default dashboard password (SHA3-512)</item>
        /// </list>
        /// Intended for first-time setup when no configuration exists. 
        /// Does not validate parameter values - assumes default values are valid for initialization.
        /// Special characters like coin symbol are stored in Unicode escape sequence format.
        /// </remarks>
        public void Initialize()
        {
            FileUtil.CreateFile(_path);
            FileUtil.SaveFileContent(_path, "{}");
            string[] apis = { "first_api", "second_api" };
            string[] channels = { "first_twitch_channel_id", "second_twitch_channel_id" };

            Set("open_router_tokens", apis);
            Set("discord_token", "Your bot's Discord token. You can get it here: https://discord.com/developers/applications");
            Set("7tv_token", "Your bot's Twitch nickname");
            Set("prefix", "!");
            Set("taxes_cost", 0.0069d);
            Set("currency_mentioner_payment", 2);
            Set("currency_mentioned_payment", 8);
            Set("dashboard_password", "6FF8E2CF58249F757ECEE669C6CB015A1C1F44552442B364C8A388B0BDB1322A7AF6B67678D9206378D8969FFEC48263C9AB3167D222C80486FC848099535568"); // Pass: bbAdmin

            Set("bot_name", "Your bot's Twitch nickname");
            Set("twitch_user_id", "Your Twitch account ID. You can find it here: https://twitch.tv/butterbror. Enter _id <bot nickname> in the chat.");
            Set("twitch_client_id", "ClientId of your twitch app (https://dev.twitch.tv/console/apps)");
            Set("twitch_secret_token", "Secret token of your twitch app (https://dev.twitch.tv/console/apps)");

            Set("twitch_connect_message_channels", channels);
            Set("twitch_reconnect_message_channels", channels);
            Set("twitch_version_message_channels", channels);
            Set("twitch_currency_random_event", channels);
            Set("twitch_taxes_event", channels);
            Set("twitch_connect_channels", channels);
        }

        public void Set(string key, object obj)
        {
            Manager.Save(_path, key, obj);
        }

        public T Get<T>(string key)
        {
            return Manager.Get<T>(_path, key);
        }
    }
}
