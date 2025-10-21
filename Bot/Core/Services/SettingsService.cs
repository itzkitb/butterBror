using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace bb.Core.Services
{
    public class SettingsService(string path)
    {
        private readonly string _path = path;

        public void Initialize()
        {
            XDocument doc = new XDocument(
                new XElement("Settings",
                    new XElement("open_router_tokens",
                        new XElement("item", "first_api"),
                        new XElement("item", "second_api")
                    ),
                    new XElement("discord_token", "Your bot's Discord token. You can get it here: https://discord.com/developers/applications"),
                    new XElement("seventv_token", "Your bot's Twitch nickname"),
                    new XElement("prefix", "!"),
                    new XElement("taxes_cost", "0.0069"),
                    new XElement("currency_mentioner_payment", "2"),
                    new XElement("currency_mentioned_payment", "8"),
                    new XElement("dashboard_password", "6FF8E2CF58249F757ECEE669C6CB015A1C1F44552442B364C8A388B0BDB1322A7AF6B67678D9206378D8969FFEC48263C9AB3167D222C80486FC848099535568"), // Pass: bbAdmin
                    new XElement("bot_name", "Your bot's Twitch nickname"),
                    new XElement("github_token", "Your github token (https://github.com/settings/tokens - Create with public_repo)"),
                    new XElement("telegram_token", "Your telegram bot token (https://t.me/BotFather)"),
                    new XElement("twitch_user_id", "Your Twitch account ID. You can find it here: https://twitch.tv/butterbror. Enter _id <bot nickname> in the chat."),
                    new XElement("twitch_client_id", "ClientId of your twitch app (https://dev.twitch.tv/console/apps)"),
                    new XElement("twitch_secret_token", "Secret token of your twitch app (https://dev.twitch.tv/console/apps)"),
                    new XElement("twitch_connect_message_channels",
                        new XElement("item", "first_twitch_channel_id"),
                        new XElement("item", "second_twitch_channel_id")
                    ),
                    new XElement("twitch_reconnect_message_channels",
                        new XElement("item", "first_twitch_channel_id"),
                        new XElement("item", "second_twitch_channel_id")
                    ),
                    new XElement("twitch_version_message_channels",
                        new XElement("item", "first_twitch_channel_id"),
                        new XElement("item", "second_twitch_channel_id")
                    ),
                    new XElement("twitch_currency_random_event",
                        new XElement("item", "first_twitch_channel_id"),
                        new XElement("item", "second_twitch_channel_id")
                    ),
                    new XElement("twitch_taxes_event",
                        new XElement("item", "first_twitch_channel_id"),
                        new XElement("item", "second_twitch_channel_id")
                    ),
                    new XElement("twitch_connect_channels",
                        new XElement("item", "first_twitch_channel_id"),
                        new XElement("item", "second_twitch_channel_id")
                    ),
                    new XElement("twitch_dev_channels",
                        new XElement("item", "first_twitch_channel_id"),
                        new XElement("item", "second_twitch_channel_id")
                    )
                )
            );

            doc.Save(_path);
        }

        public void Set(string key, object obj)
        {
            XDocument doc;
            if (File.Exists(_path))
            {
                doc = XDocument.Load(_path);
            }
            else
            {
                doc = new XDocument(new XElement("Settings"));
            }

            // Remove existing element for the key
            XElement existingElement = doc.Root?.Element(key);
            if (existingElement != null)
            {
                existingElement.Remove();
            }

            // Create new element based on obj type
            XElement newElement;
            if (obj is IEnumerable enumerable && !(obj is string))
            {
                newElement = new XElement(key);
                foreach (var item in enumerable)
                {
                    newElement.Add(new XElement("item", item?.ToString() ?? ""));
                }
            }
            else
            {
                newElement = new XElement(key, obj?.ToString() ?? "");
            }

            doc.Root?.Add(newElement);
            doc.Save(_path);
        }

        public T Get<T>(string key)
        {
            if (!File.Exists(_path))
                return default;

            XDocument doc = XDocument.Load(_path);
            XElement element = doc.Root?.Element(key);

            if (element == null)
                return default;

            if (element.Elements().Any())
            {
                if (typeof(T).IsArray)
                {
                    Type elementType = typeof(T).GetElementType()!;
                    var items = element.Elements().Select(e => Convert.ChangeType(e.Value, elementType));
                    Array array = Array.CreateInstance(elementType, items.Count());
                    int index = 0;
                    foreach (var item in items)
                    {
                        array.SetValue(item, index++);
                    }
                    return (T)(object)array;
                }
                else if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(List<>))
                {
                    Type elementType = typeof(T).GetGenericArguments()[0];
                    var items = element.Elements().Select(e => Convert.ChangeType(e.Value, elementType));
                    var list = (T)Activator.CreateInstance(typeof(T));
                    foreach (var item in items)
                    {
                        (list as System.Collections.IList)?.Add(item);
                    }
                    return list;
                }
                else
                {
                    throw new InvalidOperationException($"Key {key} is stored as array but requested as non-array type");
                }
            }
            else
            {
                string value = element.Value;
                if (string.IsNullOrEmpty(value))
                    return default;

                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return default;
                }
            }
        }
    }
}