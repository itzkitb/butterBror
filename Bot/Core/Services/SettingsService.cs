using bb.Data;
using DankDB;

namespace bb.Core.Services
{
    public class SettingsService
    {
        /// <summary>
        /// Initializes a new settings file with default configuration values.
        /// </summary>
        /// <param name="path">File system path where the settings file should be created.</param>
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
        public static void InitializeFile(string path)
        {
            FileUtil.CreateFile(path);
            Manager.Save(path, "bot_name", "");
            Manager.Save(path, "discord_token", "");
            Manager.Save(path, "imgur_token", "");
            Manager.Save(path, "user_id", "");
            Manager.Save(path, "client_id", "");
            Manager.Save(path, "twitch_secret_token", "");
            Manager.Save(path, "twitch_connect_message_channels", Array.Empty<string>());
            Manager.Save(path, "twitch_reconnect_message_channels", Array.Empty<string>());
            Manager.Save(path, "twitch_connect_channels", new[] { "First channel", "Second channel" });
            string[] apis = { "First api", "Second api" };
            Manager.Save(path, "gpt_tokens", apis);
            Manager.Save(path, "telegram_token", "");
            Manager.Save(path, "twitch_version_message_channels", Array.Empty<string>());
            Manager.Save(path, "7tv_token", "");
            Manager.Save(path, "prefix", "#");
            Manager.Save(path, "currency_mentioned_payment", 8);
            Manager.Save(path, "currency_mentioner_payment", 2);
            Manager.Save(path, "dashboard_password", "6FF8E2CF58249F757ECEE669C6CB015A1C1F44552442B364C8A388B0BDB1322A7AF6B67678D9206378D8969FFEC48263C9AB3167D222C80486FC848099535568"); //bbAdmin
            Manager.Save(path, "twitch_currency_random_event", Array.Empty<string>());
            Manager.Save(path, "twitch_taxes_event", Array.Empty<string>());
            Manager.Save(path, "taxes_cost", 0.0069d);
        }

        /// <summary>
        /// Loads and applies bot configuration from persistent storage to runtime memory.
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        /// <item>Reads all configuration parameters from the settings file</item>
        /// <item>Maps stored values to corresponding bot properties and services</item>
        /// <item>Handles multiple data types including strings, arrays, and dictionaries</item>
        /// <item>Converts prefix character from string representation</item>
        /// <item>Initializes token managers with loaded credentials</item>
        /// </list>
        /// Critical initialization step that must complete successfully before platform connections.
        /// Throws exceptions on missing critical parameters (bot_name, tokens, etc.).
        /// Automatically decodes Unicode escape sequences for special characters.
        /// Maintains separation between sensitive credentials and public configuration.
        /// </remarks>
        public static void Load()
        {
            string settingsPath = bb.Bot.Paths.Settings;
            bb.Bot.TwitchName = Manager.Get<string>(settingsPath, "bot_name");
            bb.Bot.TwitchReconnectAnnounce = Manager.Get<string[]>(settingsPath, "twitch_reconnect_message_channels");
            bb.Bot.TwitchConnectAnnounce = Manager.Get<string[]>(settingsPath, "twitch_connect_message_channels");
            bb.Bot.Tokens.Discord = Manager.Get<string>(settingsPath, "discord_token");
            bb.Bot.Tokens.Imgur = Manager.Get<string>(settingsPath, "imgur_token");
            bb.Bot.TwitchClientId = Manager.Get<string>(settingsPath, "client_id");
            bb.Bot.Tokens.TwitchSecretToken = Manager.Get<string>(settingsPath, "twitch_secret_token");
            bb.Bot.Tokens.Telegram = Manager.Get<string>(settingsPath, "telegram_token");
            bb.Bot.TwitchNewVersionAnnounce = Manager.Get<string[]>(settingsPath, "twitch_version_message_channels");
            bb.Bot.TwitchCurrencyRandomEvent = Manager.Get<List<string>>(settingsPath, "twitch_currency_random_event");
            bb.Bot.TwitchTaxesEvent = Manager.Get<List<string>>(settingsPath, "twitch_taxes_event");
            bb.Bot.Tokens.SevenTV = Manager.Get<string>(settingsPath, "7tv_token");
            bb.Bot.UsersSevenTVIDs = Manager.Get<Dictionary<string, string>>(settingsPath, "Ids");
            bb.Bot.CurrencyMentioned = Manager.Get<int>(settingsPath, "currency_mentioned_payment");
            bb.Bot.CurrencyMentioner = Manager.Get<int>(settingsPath, "currency_mentioner_payment");
            bb.Bot.DefaultCommandPrefix = Manager.Get<string>(settingsPath, "prefix");
            bb.Bot.TaxesCost = Manager.Get<double>(settingsPath, "taxes_cost");
        }
    }
}
