using bb.Core.Bot;
using bb.Core.Bot.SQLColumnNames;
using bb.Models;
using bb.Services.External;
using bb.Utils;
//using Microsoft.Extensions.Caching.Memory;
using System.Globalization;
using System.Text.RegularExpressions;
using TwitchLib.Client.Enums;
using static bb.Core.Bot.Console;

namespace bb.Core.Commands.List
{
    /// <summary>
    /// Команда для получения информации о погоде с использованием Open-Meteo API
    /// </summary>
    public class Weather : CommandBase
    {
        private IWeatherService _weatherService;
        private const int ResultsPerPage = 5;
        private const string WeatherResultKey = "WeatherResultLocations";
        //private static IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());

        public override string Name => "Weather";
        public override string Author => "ItzKITb";
        public override string AuthorsGithub => "https://github.com/itzkitb";
        public override string GithubSource => $"{URLs.githubSource}blob/master/butterBror/Core/Commands/List/Weather.cs";
        public override Version Version => new("2.0.1");
        public override Dictionary<string, string> Description => new()
        {
            { "ru-RU", "Узнать погоду в городе с использованием современного API" },
            { "en-US", "Find out the weather in the city using modern API" }
        };
        public override string WikiLink => "https://itzkitb.ru/bot/command?name=weather";
        public override int CooldownPerUser => 15;
        public override int CooldownPerChannel => 10;
        public override string[] Aliases => ["weather", "погода", "wthr", "пгд", "пугода", "meteo"];
        public override string HelpArguments => "[location] | [action] [parameters]";
        public override DateTime CreationDate => DateTime.Parse("2024-07-04T00:00:00.0000000Z");
        public override bool OnlyBotModerator => false;
        public override bool OnlyBotDeveloper => false;
        public override bool OnlyChannelModerator => false;
        public override PlatformsEnum[] Platforms => [PlatformsEnum.Twitch, PlatformsEnum.Telegram, PlatformsEnum.Discord];
        public override bool IsAsync => true;


        public override async Task<CommandReturn> ExecuteAsync(CommandData data)
        {
            CommandReturn commandReturn = new CommandReturn();

            _weatherService = new OpenMeteoWeatherService(new HttpClient());

            try
            {
                var action = ParseCommandAction(data);

                switch (action.Type)
                {
                    case WeatherActionType.ShowPage:
                        commandReturn = await HandleShowPageActionAsync(data, action);
                        break;
                    case WeatherActionType.ShowLocation:
                        commandReturn = await HandleShowLocationActionAsync(data, action);
                        break;
                    case WeatherActionType.SetLocation:
                        commandReturn = await HandleSetLocationActionAsync(data, action);
                        break;
                    case WeatherActionType.GetLocation:
                        commandReturn = await HandleGetLocationActionAsync(data);
                        break;
                    case WeatherActionType.GetWeather:
                        commandReturn = await HandleGetWeatherActionAsync(data, action);
                        break;
                    default:
                        commandReturn.SetMessage(LocalizationService.GetString(
                            data.User.Language,
                            "error:invalid_command",
                            data.ChannelId,
                            data.Platform));
                        commandReturn.SetColor(ChatColorPresets.Red);
                        break;
                }
            }
            catch (WeatherApiException ex)
            {
                LogError(data, "API_ERROR", ex);
                commandReturn.SetMessage(LocalizationService.GetString(
                    data.User.Language,
                    "error:api_unavailable",
                    data.ChannelId,
                    data.Platform));
                commandReturn.SetColor(ChatColorPresets.Red);
            }
            catch (Exception ex)
            {
                LogError(data, "GENERAL_ERROR", ex);
                commandReturn.SetMessage(LocalizationService.GetString(
                    data.User.Language,
                    "error:general",
                    data.ChannelId,
                    data.Platform));
                commandReturn.SetColor(ChatColorPresets.Red);
            }

            return commandReturn;
        }

        private WeatherAction ParseCommandAction(CommandData data)
        {
            var action = new WeatherAction { Type = WeatherActionType.GetWeather };

            if (data.Arguments.Count >= 2)
            {
                var actionType = data.Arguments[0].ToLowerInvariant();
                var parameter = data.Arguments[1];

                if (IsShowAction(actionType))
                {
                    action.Type = WeatherActionType.ShowLocation;
                    action.Page = long.TryParse(parameter, out var page) ? page : 1;
                }
                else if (IsPageAction(actionType))
                {
                    action.Type = WeatherActionType.ShowPage;
                    action.Page = long.TryParse(parameter, out var page) ? page : 1;
                }
                else if (IsSetAction(actionType))
                {
                    action.Type = WeatherActionType.SetLocation;
                    action.Location = TextSanitizer.CleanAscii(parameter);
                }
            }
            else if (data.Arguments.Count == 1)
            {
                if (IsGetAction(data.Arguments[0].ToLowerInvariant()))
                {
                    action.Type = WeatherActionType.GetLocation;
                }
                else
                {
                    action.Type = WeatherActionType.GetWeather;
                    action.Location = TextSanitizer.CleanAscii(data.ArgumentsString);
                }
            }
            else
            {
                action.Type = WeatherActionType.GetWeather;
            }

            return action;
        }

        private async Task<CommandReturn> HandleShowPageActionAsync(CommandData data, WeatherAction action)
        {
            var savedLocations = await GetSavedLocationsAsync(data);

            if (savedLocations == null || !savedLocations.Any())
            {
                return CreateErrorReturn(data, "error:no_pages");
            }

            var maxPages = (int)Math.Ceiling((double)savedLocations.Count / ResultsPerPage);
            if (action.Page < 1 || action.Page > maxPages)
            {
                return CreateErrorReturn(data, "error:page_not_found");
            }

            var pageContent = BuildLocationPage(savedLocations, action.Page);

            CommandReturn commandReturn = new CommandReturn();
            commandReturn.SetMessage(LocalizationService.GetString(
                    data.User.Language,
                    "command:weather:a_few_places",
                    data.ChannelId,
                    data.Platform,
                    pageContent,
                    action.Page.ToString(),
                    maxPages.ToString()));

            return commandReturn;
        }

        private async Task<CommandReturn> HandleShowLocationActionAsync(CommandData data, WeatherAction action)
        {
            var savedLocations = await GetSavedLocationsAsync(data);

            if (savedLocations == null || savedLocations.Count < action.Page)
            {
                return CreateErrorReturn(data, "error:page_not_found");
            }

            var selectedLocation = savedLocations[(int)action.Page - 1];

            var (lat, lon) = ParseCoordinates(selectedLocation.Latitude.ToString(),
                                    selectedLocation.Longitude.ToString());
            if (lat == null || lon == null)
            {
                return CreateErrorReturn(data, "error:invalid_coordinates");
            }

            var weatherData = await _weatherService.GetCurrentWeatherAsync(
                lat.Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
                lon.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));

            if (weatherData == null || weatherData.Temperature == -400)
            {
                return CreateErrorReturn(data, "error:place_not_found");
            }

            return BuildWeatherMessage(data, weatherData, selectedLocation.Name);
        }

        private async Task<CommandReturn> HandleSetLocationActionAsync(CommandData data, WeatherAction action)
        {
            var locations = await _weatherService.SearchLocationsAsync(action.Location);

            if (locations == null || locations.Count == 0 || locations[0].Name == "err")
            {
                return CreateErrorReturn(data, "error:place_not_found");
            }

            var firstLocation = locations[0];
            bb.Bot.UsersBuffer.SetParameter(data.Platform, DataConversion.ToLong(data.User.ID), Users.Location, firstLocation.Name);
            bb.Bot.UsersBuffer.SetParameter(data.Platform, DataConversion.ToLong(data.User.ID), Users.Latitude, firstLocation.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture));
            bb.Bot.UsersBuffer.SetParameter(data.Platform, DataConversion.ToLong(data.User.ID), Users.Longitude, firstLocation.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture));

            CommandReturn commandReturn = new CommandReturn();
            commandReturn.SetMessage(LocalizationService.GetString(
                    data.User.Language,
                    "command:weather:set_location",
                    data.ChannelId,
                    data.Platform,
                    firstLocation.Name));
            commandReturn.SetColor(ChatColorPresets.YellowGreen);

            return commandReturn;
        }

        private async Task<CommandReturn> HandleGetLocationActionAsync(CommandData data)
        {
            var userPlace = (string)bb.Bot.UsersBuffer.GetParameter(data.Platform, DataConversion.ToLong(data.User.ID), Users.Location);

            if (string.IsNullOrEmpty(userPlace))
            {
                return CreateErrorReturn(data, "error:location_not_set");
            }

            CommandReturn commandReturn = new CommandReturn();
            commandReturn.SetMessage(LocalizationService.GetString(
                    data.User.Language,
                    "command:weather:get_location",
                    data.ChannelId,
                    data.Platform,
                    userPlace));

            return commandReturn;
        }

        private async Task<CommandReturn> HandleGetWeatherActionAsync(CommandData data, WeatherAction action)
        {
            if (string.IsNullOrWhiteSpace(action.Location))
            {
                var userPlace = (string)bb.Bot.UsersBuffer.GetParameter(data.Platform, DataConversion.ToLong(data.User.ID), Users.Location);
                var userLat = (string)bb.Bot.UsersBuffer.GetParameter(data.Platform, DataConversion.ToLong(data.User.ID), Users.Latitude);
                var userLon = (string)bb.Bot.UsersBuffer.GetParameter(data.Platform, DataConversion.ToLong(data.User.ID), Users.Longitude);

                if (string.IsNullOrEmpty(userPlace) ||
                    string.IsNullOrEmpty(userLat) ||
                    string.IsNullOrEmpty(userLon))
                {
                    return CreateErrorReturn(data, "error:location_not_set");
                }

                var (lat, lon) = ParseCoordinates(userLat, userLon);
                var weatherData = await _weatherService.GetCurrentWeatherAsync(lat.Value.ToString(System.Globalization.CultureInfo.InvariantCulture), lon.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
                if (weatherData == null || weatherData.Temperature == -400)
                {
                    return CreateErrorReturn(data, "error:place_not_found");
                }

                return BuildWeatherMessage(data, weatherData, userPlace);
            }

            var searchResults = await _weatherService.SearchLocationsAsync(action.Location);

            if (searchResults == null || searchResults.Count == 0)
            {
                return CreateErrorReturn(data, "error:place_not_found");
            }

            if (searchResults.Count == 1 && searchResults[0].Name == "err")
            {
                return CreateErrorReturn(data, "error:place_not_found");
            }

            if (searchResults.Count == 1)
            {
                var weatherData = await _weatherService.GetCurrentWeatherAsync(
                    searchResults[0].Latitude.ToString(CultureInfo.InvariantCulture),
                    searchResults[0].Longitude.ToString(CultureInfo.InvariantCulture));

                if (weatherData == null || weatherData.Temperature == -400)
                {
                    return CreateErrorReturn(data, "error:place_not_found");
                }

                return BuildWeatherMessage(data, weatherData, searchResults[0].Name);
            }

            await SaveSearchResultsAsync(data, searchResults);

            var pageContent = BuildLocationPage(searchResults, 1);
            var maxPages = (int)Math.Ceiling((double)searchResults.Count / ResultsPerPage);

            CommandReturn commandReturn = new CommandReturn();
            commandReturn.SetMessage(LocalizationService.GetString(
                    data.User.Language,
                    "command:weather:a_few_places",
                    data.ChannelId,
                    data.Platform,
                    pageContent,
                    "1",
                    maxPages.ToString()));

            return commandReturn;
        }

        private async Task<List<LocationResult>> GetSavedLocationsAsync(CommandData data)
        {
            string weatherResultLocationsUnworkedString = (string)bb.Bot.UsersBuffer.GetParameter(data.Platform, DataConversion.ToLong(data.User.ID), Users.WeatherResultLocations);
            if (weatherResultLocationsUnworkedString == null || weatherResultLocationsUnworkedString.Length == 0)
            {
                return null;
            }

            var weatherResultLocationsUnworked = DataConversion.ParseStringArray(weatherResultLocationsUnworkedString);

            if (weatherResultLocationsUnworked == null || weatherResultLocationsUnworked.Length == 0)
            {
                return null;
            }

            var locations = new List<LocationResult>();
            foreach (var locationData in weatherResultLocationsUnworked)
            {
                var match = Regex.Match(locationData, @"name:\s*""(.*?)""[\s,]+lat:\s*""(.*?)""[\s,]+lon:\s*""(.*?)""");
                if (match.Success)
                {
                    locations.Add(new LocationResult
                    {
                        Name = match.Groups[1].Value,
                        Latitude = DataConversion.ToDouble(match.Groups[2].Value),
                        Longitude = DataConversion.ToDouble(match.Groups[3].Value)
                    });
                }
            }

            return locations;
        }

        private async Task SaveSearchResultsAsync(CommandData data, List<LocationResult> locations)
        {
            List<string> jsons = locations.Select(loc =>
                $"name: \"{loc.Name}\", lat: \"{loc.Latitude}\", lon: \"{loc.Longitude}\"").ToList();

            bb.Bot.UsersBuffer.SetParameter(data.Platform, DataConversion.ToLong(data.User.ID), Users.WeatherResultLocations, DataConversion.SerializeStringList(jsons));
        }

        private string BuildLocationPage(List<LocationResult> locations, long page)
        {
            var startIndex = (int)((page - 1) * ResultsPerPage);
            var endIndex = Math.Min(startIndex + ResultsPerPage, locations.Count);

            var pageContent = new System.Text.StringBuilder();

            for (var i = startIndex; i < endIndex; i++)
            {
                var index = i + 1;
                var location = locations[i];
                pageContent.Append($"{index}. {location.Name} (lat: {location.Latitude.ToString()}, lon: {location.Longitude.ToString()}), ");
            }

            return pageContent.ToString().TrimEnd(',', ' ');
        }

        private CommandReturn BuildWeatherMessage(CommandData data, WeatherData weather, string locationName)
        {
            CommandReturn commandReturn = new CommandReturn();
            commandReturn.SetMessage(LocalizationService.GetString(
                    data.User.Language,
                    "command:weather",
                    data.ChannelId,
                    data.Platform,
                    GetWeatherEmoji(weather.Temperature),
                    locationName,
                    weather.Temperature.ToString("0.0"),
                    weather.FeelsLike.ToString("0.0"),
                    weather.WindSpeed.ToString("0.0"),
                    GetWeatherSummary(data.User.Language, weather.WeatherCode, data.ChannelId, data.Platform),
                    GetWeatherSummaryEmoji(weather.WeatherCode),
                    weather.Pressure.ToString("0"),
                    weather.UvIndex.ToString("0.0"),
                    weather.Humidity.ToString("0"),
                    weather.Visibility.ToString("0")));

            return commandReturn;
        }

        private CommandReturn CreateErrorReturn(CommandData data, string errorKey)
        {
            CommandReturn commandReturn = new CommandReturn();
            commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, errorKey, data.ChannelId, data.Platform));
            commandReturn.SetColor(ChatColorPresets.Red);

            return commandReturn;
        }

        private void LogError(CommandData data, string errorType, Exception ex)
        {
            Write($"[{errorType}] Weather command error for user {data.User.Name}: {ex.Message}");
            Write(ex);
        }

        private bool IsShowAction(string action) =>
            new[] { "show", "s", "показать", "п" }.Contains(action);

        private bool IsPageAction(string action) =>
            new[] { "page", "p", "страница", "с" }.Contains(action);

        private bool IsSetAction(string action) =>
            new[] { "set", "установить", "у" }.Contains(action);

        private bool IsGetAction(string action) =>
            new[] { "get", "g", "получить" }.Contains(action);

        private string GetWeatherEmoji(double temperature) =>
            temperature switch
            {
                < -10 => "❄️",
                < 0 => "🌨️",
                < 10 => "🧥",
                < 20 => "🌤️",
                < 30 => "☀️",
                _ => "🔥"
            };

        private string GetWeatherSummary(string language, int weatherCode, string channelId, PlatformsEnum platform)
        {
            // https://open-meteo.com/en/docs
            var summary = weatherCode switch
            {
                0 => "clear_sky",
                1 => "mainly_clear",
                2 => "partly_cloudy",
                3 => "overcast",
                45 => "fog",
                48 => "depositing_rime_fog",
                51 => "light_drizzle",
                53 => "medium_drizzle",
                55 => "intensity_drizzle",
                56 => "light_freezing_drizzle",
                57 => "intensity_freezing_drizzle",
                61 => "light_rain",
                63 => "medium_rain",
                65 => "intensity_rain",
                66 => "light_freezing_rain",
                67 => "intensity_freezing_rain",
                71 => "light_snowfall",
                73 => "medium_snowfall",
                75 => "intensity_snowfall",
                77 => "snow_grains",
                80 => "light_rain_showers",
                81 => "medium_rain_showers",
                82 => "intensity_rain_showers",
                85 => "light_snow_showers",
                86 => "intensity_snow_showers",
                95 => "thunderstorm",
                96 => "thunderstorm_with_light_hail",
                99 => "thunderstorm_with_intensity_hail",
                _ => "unknown"
            };

            return LocalizationService.GetString(
                language,
                $"text:weather:{summary}",
                channelId,
                platform);
        }

        private string GetWeatherSummaryEmoji(int weatherCode) =>
            weatherCode switch
            {
                0 => "☀️",                    // Clear sky
                1 or 2 or 3 => "🌤️",         // Mainly clear, partly cloudy, overcast
                45 or 48 => "🌫️",            // Fog
                51 or 53 or 55 => "🌦️",      // Drizzle
                56 or 57 => "🌧️",            // Freezing drizzle
                61 or 63 or 65 => "🌧️",      // Rain
                66 or 67 => "🌨️",            // Freezing rain
                71 or 73 or 75 or 77 => "❄️", // Snow
                80 or 81 or 82 => "🌦️",      // Rain showers
                85 or 86 => "🌨️",            // Snow showers
                95 => "⛈️",                  // Thunderstorm
                96 or 99 => "⛈️❄️",          // Thunderstorm with hail
                _ => "❓"
            };

        private (double? latitude, double? longitude) ParseCoordinates(string latStr, string lonStr)
        {
            if (string.IsNullOrEmpty(latStr) || string.IsNullOrEmpty(lonStr))
                return (null, null);

            // Обработка широты
            double latitude;
            bool isSouth = latStr.EndsWith("S", StringComparison.OrdinalIgnoreCase);
            string latValue = isSouth ?
                latStr.Substring(0, latStr.Length - 1) :
                latStr.TrimEnd('N', 'n');

            latitude = DataConversion.ToDouble(latValue);

            if (isSouth)
                latitude = -latitude;

            // Обработка долготы
            double longitude;
            bool isWest = lonStr.EndsWith("W", StringComparison.OrdinalIgnoreCase);
            string lonValue = isWest ?
                lonStr.Substring(0, lonStr.Length - 1) :
                lonStr.TrimEnd('E', 'e');

            longitude = DataConversion.ToDouble(lonValue);

            if (isWest)
                longitude = -longitude;

            return (latitude, longitude);
        }
    }

    // Вспомогательные классы для структурирования кода
    internal class WeatherAction
    {
        public WeatherActionType Type { get; set; }
        public string Location { get; set; } = string.Empty;
        public long Page { get; set; } = 1;
    }

    internal enum WeatherActionType
    {
        GetWeather,
        ShowPage,
        ShowLocation,
        SetLocation,
        GetLocation
    }

    public class WeatherApiException : Exception
    {
        public WeatherApiException(string message) : base(message) { }
        public WeatherApiException(string message, Exception inner) : base(message, inner) { }
    }
}