using System.Text.RegularExpressions;
using Discord;
using TwitchLib.Client.Enums;
using butterBror.Utils;
using butterBror.Utils.DataManagers;
using butterBror.Utils.API;
using butterBror;
using static butterBror.Utils.API.Weather.Place;
using static butterBror.Utils.API.Weather;
using System.Linq;

namespace butterBror
{
    public partial class Commands
    {
        public class Weather
        {
            public static CommandInfo Info = new()
            {
                name = "Weather",
                author = "@ItzKITb",
                author_link = "twitch.tv/itzkitb",
                author_avatar = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                description = new()
                {
                    { "ru", "Узнать погоду в городе" },
                    { "en", "Find out the weather in the city" }
                },
                wiki_link = "https://itzkitb.ru/bot/command?name=weather",
                cooldown_per_user = 10,
                cooldown_global = 5,
                aliases = ["weather", "погода", "wthr", "пгд", "пугода"],
                arguments = "(city name)",
                cooldown_reset = false,
                creation_date = DateTime.Parse("07/04/2024"),
                is_for_bot_moderator = false,
                is_for_bot_developer = false,
                is_for_channel_moderator = false,
                platforms = [Platforms.Twitch, Platforms.Telegram, Platforms.Discord]
            };
            public CommandReturn Index(CommandData data)
            {
                Engine.Statistics.functions_used.Add();
                try
                {
                    string[] show_alias = ["show", "s", "показать", "п"];
                    string[] page_alias = ["page", "p", "страница", "с"];
                    string[] set_alias = ["set", "установить", "у"];
                    string[] get_alias = ["get", "g", "получить"];

                    string location = "";
                    bool is_show_action = false;
                    bool is_page_action = false;
                    bool is_set_action = false;
                    bool is_get_action = false;
                    string? setLocation = "";
                    long page = 0;
                    long show_place_id = 0;

                    string result_message = "";
                    string result_message_title = "";
                    Color result_color = Color.Green;
                    ChatColorPresets result_nickname_color = ChatColorPresets.YellowGreen;

                    if (data.platform is Platforms.Twitch || data.platform is Platforms.Telegram)
                    {
                        location = TextUtil.CleanAscii(data.arguments_string);
                        if (data.arguments.Count >= 2)
                        {
                            if (show_alias.Contains(data.arguments[0].ToLowerInvariant()))
                            {
                                is_show_action = true;
                                show_place_id = Utils.Format.ToInt(data.arguments[1].ToLowerInvariant());
                            }
                            else if (page_alias.Contains(data.arguments[0].ToLowerInvariant()))
                            {
                                is_page_action = true;
                                page = Utils.Format.ToInt(data.arguments[1].ToLowerInvariant());
                            }
                            else if (set_alias.Contains(data.arguments[0].ToLowerInvariant()))
                            {
                                is_set_action = true;
                                setLocation = TextUtil.CleanAscii(data.arguments[1]);
                            }
                        }
                        else if (data.arguments.Count >= 1)
                        {
                            if (get_alias.Contains(data.arguments[0].ToLowerInvariant()))
                            {
                                is_get_action = true;
                            }
                        }
                    }
                    else if (data.platform == Platforms.Discord)
                    {
                        if (data.discord_arguments.ContainsKey("location"))
                        {
                            location = data.discord_arguments.GetValueOrDefault("location");
                        }
                        if (data.discord_arguments.ContainsKey("showpage"))
                        {
                            page = data.discord_arguments.GetValueOrDefault("showpage");
                            is_page_action = true;
                        }
                        else if (data.discord_arguments.ContainsKey("page"))
                        {
                            show_place_id = data.discord_arguments.GetValueOrDefault("page");
                            is_show_action = true;
                        }
                    }

                    if (is_page_action)
                    {
                        var weatherResultLocationsUnworked = UsersData.Get<string[]>(data.user_id, "weatherResultLocations", data.platform);
                        var weatherResultLocations = new List<Place>();

                        foreach (var data2 in weatherResultLocationsUnworked)
                        {
                            string pattern = @"name: ""(.*?)"", lat: ""(.*?)"", lon: ""(.*?)""";
                            Match match = Regex.Match(data2, pattern);

                            if (match.Success)
                            {
                                string name = match.Groups[1].Value;
                                string lat = match.Groups[2].Value;
                                string lon = match.Groups[3].Value;
                                Place placeData = new()
                                {
                                    name = name,
                                    lat = lat,
                                    lon = lon
                                };
                                weatherResultLocations.Add(placeData);
                            }
                        }
                        if (weatherResultLocations != default)
                        {
                            int maxPages = (int)Math.Ceiling((double)(weatherResultLocations.Count / 5));
                            if (maxPages >= page)
                            {
                                string locationPage = "";
                                int startID = (int)((page * 5) - 4);
                                if (weatherResultLocations.Count > 5)
                                {
                                    int index = startID;
                                    for (int i = 0; i < 5; i++)
                                    {
                                        locationPage += $"{index}. {weatherResultLocations[index - 1].name} (lat: {TextUtil.ShortenCoordinate(weatherResultLocations[index - 1].lat)}, lon: {TextUtil.ShortenCoordinate(weatherResultLocations[index - 1].lon)}), ";
                                        index++;
                                    }
                                    locationPage = locationPage.TrimEnd(',', ' ');
                                }
                                else
                                {
                                    int index = startID;
                                    foreach (var location2 in weatherResultLocations)
                                    {
                                        index++;
                                        locationPage += $"{index}. {location2.name} (lat: {location2.lat}, lon: {location2.lon}), ";
                                    }
                                }
                                locationPage = (locationPage + "\n").Replace(", \n", "");
                                result_message = TranslationManager.GetTranslation(data.user.language, "command:weather:a_few_places", data.channel_id, data.platform)
                                    .Replace("%places%", locationPage)
                                    .Replace("%page%", page.ToString())
                                    .Replace("%pages%", maxPages.ToString());
                                result_message_title = TranslationManager.GetTranslation(data.user.language, "discord:weather:a_few_places:title", data.channel_id, data.platform);
                            }
                            else
                            {
                                result_message = TranslationManager.GetTranslation(data.user.language, "error:page_not_found", data.channel_id, data.platform);
                                result_message_title = TranslationManager.GetTranslation(data.user.language, "discord:weather:a_few_places:title", data.channel_id, data.platform);
                                result_color = Color.Red;
                                result_nickname_color = ChatColorPresets.Red;
                            }
                        }
                        else
                        {
                            result_message = TranslationManager.GetTranslation(data.user.language, "error:no_pages", data.channel_id, data.platform);
                            result_message_title = TranslationManager.GetTranslation(data.user.language, "discord:weather:a_few_places:title", data.channel_id, data.platform);
                            result_color = Color.Red;
                            result_nickname_color = ChatColorPresets.Red;
                        }
                    }
                    else if (is_show_action)
                    {
                        var weatherResultLocationsUnworked = UsersData.Get<string[]>(data.user_id, "weatherResultLocations", data.platform);
                        var weatherResultLocations = new List<Place>();
                        foreach (var data2 in weatherResultLocationsUnworked)
                        {
                            string pattern = @"name: ""(.*?)"", lat: ""(.*?)"", lon: ""(.*?)""";
                            Match match = Regex.Match(data2, pattern);

                            if (match.Success)
                            {
                                string name = match.Groups[1].Value;
                                string lat = match.Groups[2].Value;
                                string lon = match.Groups[3].Value;
                                Place placeData = new()
                                {
                                    name = name,
                                    lat = lat,
                                    lon = lon
                                };
                                weatherResultLocations.Add(placeData);
                            }
                        }
                        if (weatherResultLocations?.Count > 1)
                        {
                            if (show_place_id <= weatherResultLocations?.Count)
                            {
                                var weather = Get(weatherResultLocations[(int)show_place_id - 1].lat, weatherResultLocations[(int)show_place_id - 1].lon);
                                var result = weather.Result.current;
                                if (result.temperature != -400)
                                {
                                    result_message = TranslationManager.GetTranslation(data.user.language, "command:weather", data.channel_id, data.platform)
                                        .Replace("%emote%", GetEmoji(result.temperature))
                                        .Replace("%name%", weatherResultLocations[(int)show_place_id - 1].name)
                                        .Replace("%temperature%", result.temperature.ToString())
                                        .Replace("%feelsLike%", result.feels_like.ToString())
                                        .Replace("%windSpeed%", result.wind.speed.ToString())
                                        .Replace("%summary%", GetSummary(data.user.language, result.summary.ToString(), data.channel_id, data.platform))
                                        .Replace("%pressure%", result.pressure.ToString())
                                        .Replace("%uvIndex%", result.uv_index.ToString())
                                        .Replace("%humidity%", result.humidity.ToString())
                                        .Replace("%visibility%", result.visibility.ToString())
                                        .Replace("%skyEmote%", GetSummaryEmoji(result.summary.ToString()));
                                    result_message_title = TranslationManager.GetTranslation(data.user.language, "discord:weather:title", data.channel_id, data.platform);
                                }
                                else
                                {
                                    result_message = TranslationManager.GetTranslation(data.user.language, "error:place_not_found", data.channel_id, data.platform);
                                    result_message_title = TranslationManager.GetTranslation(data.user.language, "discord:error:title", data.channel_id, data.platform);
                                    result_color = Color.Red;
                                    result_nickname_color = ChatColorPresets.Red;
                                }
                            }
                            else
                            {
                                result_message = TranslationManager.GetTranslation(data.user.language, "error:page_not_found", data.channel_id, data.platform);
                                result_message_title = TranslationManager.GetTranslation(data.user.language, "discord:error:title", data.channel_id, data.platform);
                                result_color = Color.Red;
                                result_nickname_color = ChatColorPresets.Red;
                            }
                        }
                        else
                        {
                            result_message = TranslationManager.GetTranslation(data.user.language, "error:no_pages", data.channel_id, data.platform);
                            result_message_title = TranslationManager.GetTranslation(data.user.language, "discord:error:title", data.channel_id, data.platform);
                            result_color = Color.Red;
                            result_nickname_color = ChatColorPresets.Red;
                        }
                    }
                    else if (is_set_action)
                    {
                        var places = GetLocation(setLocation).Result;
                        if (places.Count == 0 || places[0].name == "err")
                        {
                            result_message = TranslationManager.GetTranslation(data.user.language, "error:place_not_found", data.channel_id, data.platform);
                            result_message_title = TranslationManager.GetTranslation(data.user.language, "discord:error:title", data.channel_id, data.platform);
                            result_color = Color.Red;
                            result_nickname_color = ChatColorPresets.Red;
                        }
                        else
                        {
                            var first = places[0];
                            UsersData.Save(data.user_id, "userPlace", first.name, data.platform);
                            UsersData.Save(data.user_id, "userLat", first.lat, data.platform);
                            UsersData.Save(data.user_id, "userLon", first.lon, data.platform);

                            result_message = TranslationManager.GetTranslation(data.user.language, "command:weather:set_location", data.channel_id, data.platform, new Dictionary<string, string> { { "%city%", first.name } });
                            result_message_title = TranslationManager.GetTranslation(data.user.language, "discord:weather:title", data.channel_id, data.platform);
                            result_color = Color.Green;
                            result_nickname_color = ChatColorPresets.YellowGreen;
                        }
                    }
                    else if (is_get_action)
                    {
                        if (UsersData.Get<string>(data.user_id, "userPlace", data.platform) is not "")
                        {
                            result_message = TranslationManager.GetTranslation(data.user.language, "command:weather:get_location", data.channel_id, data.platform, new() { { "city", UsersData.Get<string>(data.user_id, "userPlace", data.platform) } });
                            result_message_title = TranslationManager.GetTranslation(data.user.language, "discord:weather:title", data.channel_id, data.platform);
                        }
                        else
                        {
                            result_message = TranslationManager.GetTranslation(data.user.language, "error:location_not_set", data.channel_id, data.platform);
                            result_message_title = TranslationManager.GetTranslation(data.user.language, "discord:error:title", data.channel_id, data.platform);
                            result_color = Color.Red;
                            result_nickname_color = ChatColorPresets.Red;
                        }
                    }
                    else
                    {
                        if (location is "")
                        {
                            if (UsersData.Get<string>(data.user_id, "userPlace", data.platform) is not "")
                            {
                                var weather = Get(UsersData.Get<string>(data.user_id, "userLat", data.platform), UsersData.Get<string>(data.user_id, "userLon", data.platform));
                                var result = weather.Result.current;
                                if (result.temperature != -400)
                                {
                                    result_message = TranslationManager.GetTranslation(data.user.language, "command:weather", data.channel_id, data.platform)
                                        .Replace("%emote%", GetEmoji(result.temperature))
                                        .Replace("%name%", UsersData.Get<string>(data.user_id, "userPlace", data.platform))
                                        .Replace("%temperature%", result.temperature.ToString())
                                        .Replace("%feelsLike%", result.feels_like.ToString())
                                        .Replace("%windSpeed%", result.wind.speed.ToString())
                                        .Replace("%summary%", GetSummary(data.user.language, result.summary.ToString(), data.channel_id, data.platform))
                                        .Replace("%pressure%", result.pressure.ToString())
                                        .Replace("%uvIndex%", result.uv_index.ToString())
                                        .Replace("%humidity%", result.humidity.ToString())
                                        .Replace("%visibility%", result.visibility.ToString())
                                        .Replace("%skyEmote%", GetSummaryEmoji(result.summary.ToString()));
                                    result_message_title = TranslationManager.GetTranslation(data.user.language, "discord:weather:title", data.channel_id, data.platform);
                                }
                                else
                                {
                                    result_message = TranslationManager.GetTranslation(data.user.language, "error:place_not_found", data.channel_id, data.platform);
                                    result_message_title = TranslationManager.GetTranslation(data.user.language, "discord:error:title", data.channel_id, data.platform);
                                    result_color = Color.Red;
                                    result_nickname_color = ChatColorPresets.Red;
                                }
                            }
                            else
                            {
                                result_message = TranslationManager.GetTranslation(data.user.language, "error:location_not_set", data.channel_id, data.platform);
                                result_message_title = TranslationManager.GetTranslation(data.user.language, "discord:error:title", data.channel_id, data.platform);
                                result_color = Color.Red;
                                result_nickname_color = ChatColorPresets.Red;
                            }
                        }
                        else
                        {
                            try
                            {
                                var result_location = GetLocation(location).Result;
                                if (result_location.Count == 0)
                                {
                                    result_message = TranslationManager.GetTranslation(data.user.language, "error:place_not_found", data.channel_id, data.platform);
                                    result_message_title = TranslationManager.GetTranslation(data.user.language, "discord:error:title", data.channel_id, data.platform);
                                    result_color = Color.Red;
                                    result_nickname_color = ChatColorPresets.Red;
                                }
                                else if (result_location.Count == 1)
                                {
                                    if (result_location.ElementAt(0).name == "err")
                                    {
                                        result_message = TranslationManager.GetTranslation(data.user.language, "error:place_not_found", data.channel_id, data.platform);
                                        result_message_title = TranslationManager.GetTranslation(data.user.language, "discord:error:title", data.channel_id, data.platform);
                                        result_color = Color.Red;
                                        result_nickname_color = ChatColorPresets.Red;
                                    }
                                    else
                                    {
                                        var weather = Get(result_location[0].lat, result_location[0].lon);
                                        weather.Wait();
                                        var result = weather.Result.current;
                                        if (result.temperature != -400)
                                        {
                                            result_message = TranslationManager.GetTranslation(data.user.language, "command:weather", data.channel_id, data.platform)
                                                .Replace("%emote%", GetEmoji(result.temperature))
                                                .Replace("%name%", result_location[0].name)
                                                .Replace("%temperature%", result.temperature.ToString())
                                                .Replace("%feelsLike%", result.feels_like.ToString())
                                                .Replace("%windSpeed%", result.wind.speed.ToString())
                                                .Replace("%summary%", GetSummary(data.user.language, result.summary.ToString(), data.channel_id, data.platform))
                                                .Replace("%pressure%", result.pressure.ToString())
                                                .Replace("%uvIndex%", result.uv_index.ToString())
                                                .Replace("%humidity%", result.humidity.ToString())
                                                .Replace("%visibility%", result.visibility.ToString())
                                                .Replace("%skyEmote%", GetSummaryEmoji(result.summary.ToString()));
                                            result_message_title = TranslationManager.GetTranslation(data.user.language, "discord:weather:title", data.channel_id, data.platform);
                                        }
                                        else
                                        {
                                            result_message = TranslationManager.GetTranslation(data.user.language, "error:place_not_found", data.channel_id, data.platform);
                                            result_message_title = TranslationManager.GetTranslation(data.user.language, "discord:error:title", data.channel_id, data.platform);
                                            result_color = Color.Red;
                                            result_nickname_color = ChatColorPresets.Red;
                                        }
                                    }
                                }
                                else
                                {
                                    List<string> jsons = new();
                                    foreach (var loc in result_location)
                                    {
                                        jsons.Add($"name: \"{loc.name}\", lat: \"{loc.lat}\", lon: \"{loc.lon}\"");
                                    }
                                    UsersData.Save(data.user_id, "weatherResultLocations", jsons, data.platform);
                                    string locationPage = "";
                                    int maxPage = (int)Math.Ceiling((double)(result_location.Count / 5));
                                    if (result_location.Count > 5)
                                    {
                                        int index = 1;
                                        for (int i = 0; i < 5; i++)
                                        {
                                            locationPage += $"{index}. {result_location[index - 1].name} (lat: {TextUtil.ShortenCoordinate(result_location[index - 1].lat)}, lon: {TextUtil.ShortenCoordinate(result_location[index - 1].lon)}), ";
                                            index++;
                                        }
                                        locationPage = locationPage.TrimEnd(',', ' ');
                                    }
                                    else
                                    {
                                        int index = 0;
                                        foreach (var location2 in result_location)
                                        {
                                            index++;
                                            locationPage += $"{index}. {location2.name} (lat: {location2.lat}, lon: {location2.lon}), ";
                                        }
                                    }
                                    locationPage = (locationPage + "\n").Replace(", \n", "");
                                    result_message = TranslationManager.GetTranslation(data.user.language, "command:weather:a_few_places", data.channel_id, data.platform)
                                        .Replace("%places%", locationPage)
                                        .Replace("%page%", "1")
                                        .Replace("%pages%", maxPage.ToString());
                                    result_message_title = TranslationManager.GetTranslation(data.user.language, "discord:weather:a_few_places:title", data.channel_id, data.platform);
                                }
                            }
                            catch (Exception ex)
                            {
                                LogWorker.Log(ex.Message, LogWorker.LogTypes.Err, "command\\Weather\\Index");
                                result_message = TranslationManager.GetTranslation(data.user.language, "error:place_not_found", data.channel_id, data.platform);
                                result_message_title = TranslationManager.GetTranslation(data.user.language, "discord:error:title", data.channel_id, data.platform);
                                result_color = Color.Red;
                                result_nickname_color = ChatColorPresets.Red;
                            }
                        }
                    }
                    return new()
                    {
                        message = result_message,
                        safe_execute = false,
                        description = "",
                        author = "",
                        image_link = "",
                        thumbnail_link = "",
                        footer = "",
                        is_embed = false,
                        is_ephemeral = false,
                        title = result_message_title,
                        embed_color = result_color,
                        nickname_color = result_nickname_color
                    };
                }
                catch (Exception e)
                {
                    return new()
                    {
                        message = "",
                        safe_execute = false,
                        description = "",
                        author = "",
                        image_link = "",
                        thumbnail_link = "",
                        footer = "",
                        is_embed = true,
                        is_ephemeral = false,
                        title = "",
                        embed_color = Color.Green,
                        nickname_color = ChatColorPresets.YellowGreen,
                        is_error = true,
                        exception = e
                    };
                }
            }
        }
    }
}
