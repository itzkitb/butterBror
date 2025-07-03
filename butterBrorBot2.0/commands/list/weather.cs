using System.Text.RegularExpressions;
using Discord;
using TwitchLib.Client.Enums;
using butterBror.Utils;
using butterBror.Utils.DataManagers;
using butterBror.Utils.Tools.API;
using static butterBror.Utils.Tools.API.Weather.Place;
using static butterBror.Utils.Tools.API.Weather;
using System.Linq;
using butterBror.Utils.Tools;
using static butterBror.Utils.Bot.Console;
using butterBror.Utils.Types;

namespace butterBror
{
    public partial class Commands
    {
        public class Weather
        {
            public static CommandInfo Info = new()
            {
                Name = "Weather",
                Author = "@ItzKITb",
                AuthorLink = "twitch.tv/itzkitb",
                AuthorAvatar = "https://static-cdn.jtvnw.net/jtv_user_pictures/c3a9af55-d7af-4b4a-82de-39a4d8b296d3-profile_image-70x70.png",
                Description = new()
                {
                    { "ru", "Узнать погоду в городе" },
                    { "en", "Find out the weather in the city" }
                },
                WikiLink = "https://itzkitb.ru/bot/command?name=weather",
                CooldownPerUser = 10,
                CooldownPerChannel = 5,
                Aliases = ["weather", "погода", "wthr", "пгд", "пугода"],
                Arguments = "(city name)",
                CooldownReset = false,
                CreationDate = DateTime.Parse("07/04/2024"),
                IsForBotModerator = false,
                IsForBotDeveloper = false,
                IsForChannelModerator = false,
                Platforms = [Platforms.Twitch, Platforms.Telegram, Platforms.Discord]
            };

            [ConsoleSector("butterBror.Commands.Weather", "Index")]
            public CommandReturn Index(CommandData data)
            {
                Core.Statistics.FunctionsUsed.Add();
                CommandReturn commandReturn = new CommandReturn();

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

                    if (data.Platform is Platforms.Twitch || data.Platform is Platforms.Telegram)
                    {
                        location = Text.CleanAscii(data.ArgumentsString);
                        if (data.Arguments.Count >= 2)
                        {
                            if (show_alias.Contains(data.Arguments[0].ToLowerInvariant()))
                            {
                                is_show_action = true;
                                show_place_id = Utils.Tools.Format.ToInt(data.Arguments[1].ToLowerInvariant());
                            }
                            else if (page_alias.Contains(data.Arguments[0].ToLowerInvariant()))
                            {
                                is_page_action = true;
                                page = Utils.Tools.Format.ToInt(data.Arguments[1].ToLowerInvariant());
                            }
                            else if (set_alias.Contains(data.Arguments[0].ToLowerInvariant()))
                            {
                                is_set_action = true;
                                setLocation = Text.CleanAscii(data.Arguments[1]);
                            }
                        }
                        else if (data.Arguments.Count >= 1)
                        {
                            if (get_alias.Contains(data.Arguments[0].ToLowerInvariant()))
                            {
                                is_get_action = true;
                            }
                        }
                    }
                    else if (data.Platform == Platforms.Discord)
                    {
                        if (data.DiscordArguments.ContainsKey("location"))
                        {
                            location = data.DiscordArguments.GetValueOrDefault("location");
                        }
                        if (data.DiscordArguments.ContainsKey("showpage"))
                        {
                            page = data.DiscordArguments.GetValueOrDefault("showpage");
                            is_page_action = true;
                        }
                        else if (data.DiscordArguments.ContainsKey("page"))
                        {
                            show_place_id = data.DiscordArguments.GetValueOrDefault("page");
                            is_show_action = true;
                        }
                    }

                    if (is_page_action)
                    {
                        var weatherResultLocationsUnworked = UsersData.Get<string[]>(data.UserID, "weatherResultLocations", data.Platform);
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
                                        locationPage += $"{index}. {weatherResultLocations[index - 1].name} (lat: {Text.ShortenCoordinate(weatherResultLocations[index - 1].lat)}, lon: {Text.ShortenCoordinate(weatherResultLocations[index - 1].lon)}), ";
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
                                commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:weather:a_few_places", data.ChannelID, data.Platform)
                                    .Replace("%places%", locationPage)
                                    .Replace("%page%", page.ToString())
                                    .Replace("%pages%", maxPages.ToString()));
                            }
                            else
                            {
                                commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:page_not_found", data.ChannelID, data.Platform));
                                commandReturn.SetColor(ChatColorPresets.Red);
                            }
                        }
                        else
                        {
                            commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:no_pages", data.ChannelID, data.Platform));
                            commandReturn.SetColor(ChatColorPresets.Red);
                        }
                    }
                    else if (is_show_action)
                    {
                        var weatherResultLocationsUnworked = UsersData.Get<string[]>(data.UserID, "weatherResultLocations", data.Platform);
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
                                    commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:weather", data.ChannelID, data.Platform)
                                        .Replace("%emote%", GetEmoji(result.temperature))
                                        .Replace("%name%", weatherResultLocations[(int)show_place_id - 1].name)
                                        .Replace("%temperature%", result.temperature.ToString())
                                        .Replace("%feelsLike%", result.feels_like.ToString())
                                        .Replace("%windSpeed%", result.wind.speed.ToString())
                                        .Replace("%summary%", GetSummary(data.User.Language, result.summary.ToString(), data.ChannelID, data.Platform))
                                        .Replace("%pressure%", result.pressure.ToString())
                                        .Replace("%uvIndex%", result.uv_index.ToString())
                                        .Replace("%humidity%", result.humidity.ToString())
                                        .Replace("%visibility%", result.visibility.ToString())
                                        .Replace("%skyEmote%", GetSummaryEmoji(result.summary.ToString())));
                                }
                                else
                                {
                                    commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:place_not_found", data.ChannelID, data.Platform));
                                    commandReturn.SetColor(ChatColorPresets.Red);
                                }
                            }
                            else
                            {
                                commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:page_not_found", data.ChannelID, data.Platform));
                                commandReturn.SetColor(ChatColorPresets.Red);
                            }
                        }
                        else
                        {
                            commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:no_pages", data.ChannelID, data.Platform));
                            commandReturn.SetColor(ChatColorPresets.Red);
                        }
                    }
                    else if (is_set_action)
                    {
                        var places = GetLocation(setLocation).Result;
                        if (places.Count == 0 || places[0].name == "err")
                        {
                            commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:place_not_found", data.ChannelID, data.Platform));
                            commandReturn.SetColor(ChatColorPresets.Red);
                        }
                        else
                        {
                            var first = places[0];
                            UsersData.Save(data.UserID, "userPlace", first.name, data.Platform);
                            UsersData.Save(data.UserID, "userLat", first.lat, data.Platform);
                            UsersData.Save(data.UserID, "userLon", first.lon, data.Platform);

                            commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:weather:set_location", data.ChannelID, data.Platform, new Dictionary<string, string> { { "%city%", first.name } }));
                            commandReturn.SetColor(ChatColorPresets.YellowGreen);
                        }
                    }
                    else if (is_get_action)
                    {
                        if (UsersData.Get<string>(data.UserID, "userPlace", data.Platform) is not "")
                        {
                            commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:weather:get_location", data.ChannelID, data.Platform, new() { { "city", UsersData.Get<string>(data.UserID, "userPlace", data.Platform) } }));
                        }
                        else
                        {
                            commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:location_not_set", data.ChannelID, data.Platform));
                            commandReturn.SetColor(ChatColorPresets.Red);
                        }
                    }
                    else
                    {
                        if (location is "")
                        {
                            if (UsersData.Get<string>(data.UserID, "userPlace", data.Platform) is not "")
                            {
                                var weather = Get(UsersData.Get<string>(data.UserID, "userLat", data.Platform), UsersData.Get<string>(data.UserID, "userLon", data.Platform));
                                var result = weather.Result.current;
                                if (result.temperature != -400)
                                {
                                    commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:weather", data.ChannelID, data.Platform)
                                        .Replace("%emote%", GetEmoji(result.temperature))
                                        .Replace("%name%", UsersData.Get<string>(data.UserID, "userPlace", data.Platform))
                                        .Replace("%temperature%", result.temperature.ToString())
                                        .Replace("%feelsLike%", result.feels_like.ToString())
                                        .Replace("%windSpeed%", result.wind.speed.ToString())
                                        .Replace("%summary%", GetSummary(data.User.Language, result.summary.ToString(), data.ChannelID, data.Platform))
                                        .Replace("%pressure%", result.pressure.ToString())
                                        .Replace("%uvIndex%", result.uv_index.ToString())
                                        .Replace("%humidity%", result.humidity.ToString())
                                        .Replace("%visibility%", result.visibility.ToString())
                                        .Replace("%skyEmote%", GetSummaryEmoji(result.summary.ToString())));
                                }
                                else
                                {
                                    commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:place_not_found", data.ChannelID, data.Platform));
                                    commandReturn.SetColor(ChatColorPresets.Red);
                                }
                            }
                            else
                            {
                                commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:location_not_set", data.ChannelID, data.Platform));
                                commandReturn.SetColor(ChatColorPresets.Red);
                            }
                        }
                        else
                        {
                            try
                            {
                                var result_location = GetLocation(location).Result;
                                if (result_location.Count == 0)
                                {
                                    commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:place_not_found", data.ChannelID, data.Platform));
                                    commandReturn.SetColor(ChatColorPresets.Red);
                                }
                                else if (result_location.Count == 1)
                                {
                                    if (result_location.ElementAt(0).name == "err")
                                    {
                                        commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:place_not_found", data.ChannelID, data.Platform));
                                        commandReturn.SetColor(ChatColorPresets.Red);
                                    }
                                    else
                                    {
                                        var weather = Get(result_location[0].lat, result_location[0].lon);
                                        weather.Wait();
                                        var result = weather.Result.current;
                                        if (result.temperature != -400)
                                        {
                                            commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:weather", data.ChannelID, data.Platform)
                                                .Replace("%emote%", GetEmoji(result.temperature))
                                                .Replace("%name%", result_location[0].name)
                                                .Replace("%temperature%", result.temperature.ToString())
                                                .Replace("%feelsLike%", result.feels_like.ToString())
                                                .Replace("%windSpeed%", result.wind.speed.ToString())
                                                .Replace("%summary%", GetSummary(data.User.Language, result.summary.ToString(), data.ChannelID, data.Platform))
                                                .Replace("%pressure%", result.pressure.ToString())
                                                .Replace("%uvIndex%", result.uv_index.ToString())
                                                .Replace("%humidity%", result.humidity.ToString())
                                                .Replace("%visibility%", result.visibility.ToString())
                                                .Replace("%skyEmote%", GetSummaryEmoji(result.summary.ToString())));
                                        }
                                        else
                                        {
                                            commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:place_not_found", data.ChannelID, data.Platform));
                                            commandReturn.SetColor(ChatColorPresets.Red);
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
                                    UsersData.Save(data.UserID, "weatherResultLocations", jsons, data.Platform);
                                    string locationPage = "";
                                    int maxPage = (int)Math.Ceiling((double)(result_location.Count / 5));
                                    if (result_location.Count > 5)
                                    {
                                        int index = 1;
                                        for (int i = 0; i < 5; i++)
                                        {
                                            locationPage += $"{index}. {result_location[index - 1].name} (lat: {Text.ShortenCoordinate(result_location[index - 1].lat)}, lon: {Text.ShortenCoordinate(result_location[index - 1].lon)}), ";
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
                                    commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "command:weather:a_few_places", data.ChannelID, data.Platform)
                                        .Replace("%places%", locationPage)
                                        .Replace("%page%", "1")
                                        .Replace("%pages%", maxPage.ToString()));
                                }
                            }
                            catch (Exception ex)
                            {
                                Write(ex);
                                commandReturn.SetMessage(TranslationManager.GetTranslation(data.User.Language, "error:place_not_found", data.ChannelID, data.Platform));
                                commandReturn.SetColor(ChatColorPresets.Red);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    commandReturn.SetError(e);
                }

                return commandReturn;
            }
        }
    }
}
