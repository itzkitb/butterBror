using bb.Models.SevenTVLib;
using bb.Services.External;
using bb.Utils;
using bb.Core.Configuration;
using Microsoft.CodeAnalysis;

//using Microsoft.Extensions.Caching.Memory;
using System.Globalization;
using System.Text.RegularExpressions;
using TwitchLib.Client.Enums;
using static bb.Core.Bot.Logger;
using static System.Runtime.InteropServices.JavaScript.JSType;
using bb.Models.Command;
using bb.Models.Platform;
using bb.Models.Users;

namespace bb.Core.Commands.List
{
    /// <summary>
    /// Команда для получения информации о погоде с использованием Open-Meteo API
    /// </summary>
    public class Weather : CommandBase
    {
        private IWeatherService _weatherService = new OpenMeteoWeatherService(new HttpClient());
        private const int ResultsPerPage = 5;
        private const string WeatherResultKey = "WeatherResultLocations";
        //private static IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());

        public override string Name => "Weather";
        public override string Author => "ItzKITb";
        public override string AuthorsGithub => "https://github.com/itzkitb";
        public override string GithubSource => $"{URLs.githubSource}blob/master/butterBror/Core/Commands/List/Weather.cs";
        public override Version Version => new("2.0.1");
        public override Dictionary<Language, string> Description => new()
        {
            { Language.RuRu, "Узнать погоду в городе с использованием современного API" },
            { Language.EnUs, "Find out the weather in the city using modern API" }
        };
        public override string WikiLink => "https://itzkitb.ru/bot/command?name=weather";
        public override int CooldownPerUser => 15;
        public override int CooldownPerChannel => 1;
        public override string[] Aliases => ["weather", "погода", "wthr", "пгд", "пугода", "meteo"];
        public override string HelpArguments => "[location] | [action] [parameters]";
        public override DateTime CreationDate => DateTime.Parse("2024-07-04T00:00:00.0000000Z");
        public override bool OnlyBotModerator => false;
        public override bool OnlyBotDeveloper => false;
        public override bool OnlyChannelModerator => false;
        public override Models.Platform.Platform[] Platforms => [Models.Platform.Platform.Twitch, Models.Platform.Platform.Telegram, Models.Platform.Platform.Discord];
        public override bool IsAsync => true;


        public override async Task<CommandReturn> ExecuteAsync(CommandData data)
        {
            CommandReturn commandReturn = new CommandReturn();

            if (data.ChannelId == null)
            {
                commandReturn.SetMessage(LocalizationService.GetString(data.User.Language, "error:unknown", string.Empty, data.Platform));
                return commandReturn;
            }

            try
            {
                var action = ParseCommandAction(data.Arguments ?? new List<string>(), data.ArgumentsString);

                switch (action.Type)
                {
                    case WeatherActionType.ShowPage:
                        commandReturn = HandleShowPageAction(action, data.User.Language, data.User.Id, data.ChannelId, data.Platform);
                        break;
                    case WeatherActionType.ShowLocation:
                        commandReturn = await HandleShowLocationActionAsync(action, data.Platform, data.User.Id, data.User.Language, data.ChannelId);
                        break;
                    case WeatherActionType.SetLocation:
                        commandReturn = await HandleSetLocationActionAsync(action, data.Platform, data.User.Id, data.User.Language, data.ChannelId);
                        break;
                    case WeatherActionType.GetLocation:
                        commandReturn = HandleGetLocationAction(data.Platform, data.User.Id, data.User.Language, data.ChannelId);
                        break;
                    case WeatherActionType.GetWeather:
                        commandReturn = await HandleGetWeatherActionAsync(action, data.Platform, data.User.Id, data.User.Language, data.ChannelId);
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
                LogError("API_ERROR", ex, data.User.Name);
                commandReturn.SetMessage(LocalizationService.GetString(
                    data.User.Language,
                    "error:api_unavailable",
                    data.ChannelId,
                    data.Platform));
                commandReturn.SetColor(ChatColorPresets.Red);
            }
            catch (Exception ex)
            {
                LogError("GENERAL_ERROR", ex, data.User.Name);
                commandReturn.SetMessage(LocalizationService.GetString(
                    data.User.Language,
                    "error:general",
                    data.ChannelId,
                    data.Platform));
                commandReturn.SetColor(ChatColorPresets.Red);
            }

            return commandReturn;
        }

        private WeatherAction ParseCommandAction(List<string> arguments, string argumentsAsString)
        {
            var action = new WeatherAction { Type = WeatherActionType.GetWeather };

            if (arguments.Count >= 2)
            {
                var actionType = arguments[0].ToLowerInvariant();
                var parameter = arguments[1];

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
            else if (arguments != null && arguments.Count == 1)
            {
                if (IsGetAction(arguments[0].ToLowerInvariant()))
                {
                    action.Type = WeatherActionType.GetLocation;
                }
                else
                {
                    action.Type = WeatherActionType.GetWeather;
                    action.Location = TextSanitizer.CleanAscii(argumentsAsString);
                }
            }
            else
            {
                action.Type = WeatherActionType.GetWeather;
            }

            return action;
        }

        private CommandReturn HandleShowPageAction(WeatherAction action, Language language, string userId, string channelId, Models.Platform.Platform platform)
        {
            var savedLocations = GetSavedLocations(platform, userId);

            if (savedLocations == null || !savedLocations.Any())
            {
                return CreateErrorReturn("error:no_pages", platform, language, channelId);
            }

            var maxPages = (int)Math.Ceiling((double)savedLocations.Count / ResultsPerPage);
            if (action.Page < 1 || action.Page > maxPages)
            {
                return CreateErrorReturn("error:page_not_found", platform, language, channelId);
            }

            var pageContent = BuildLocationPage(savedLocations, action.Page);

            CommandReturn commandReturn = new CommandReturn();
            commandReturn.SetMessage(LocalizationService.GetString(
                    language,
                    "command:weather:a_few_places",
                    channelId,
                    platform,
                    pageContent,
                    action.Page.ToString(),
                    maxPages.ToString()));

            return commandReturn;
        }

        private async Task<CommandReturn> HandleShowLocationActionAsync(WeatherAction action, Models.Platform.Platform platform, string userId, Language language, string channelId)
        {
            var savedLocations = GetSavedLocations(platform, userId);

            if (savedLocations == null || savedLocations.Count < action.Page)
            {
                return CreateErrorReturn("error:page_not_found", platform, language, channelId);
            }

            var selectedLocation = savedLocations[(int)action.Page - 1];

            var (lat, lon) = ParseCoordinates(selectedLocation.Latitude.ToString(),
                                    selectedLocation.Longitude.ToString());
            if (lat == null || lon == null)
            {
                return CreateErrorReturn("error:invalid_coordinates", platform, language, channelId);
            }

            var weatherData = await _weatherService.GetCurrentWeatherAsync(
                lat.Value.ToString(CultureInfo.InvariantCulture),
                lon.Value.ToString(CultureInfo.InvariantCulture));

            if (weatherData == null || weatherData.Temperature == -400)
            {
                return CreateErrorReturn("error:place_not_found", platform, language, channelId);
            }

            return BuildWeatherMessage(weatherData, selectedLocation.Name, platform, language, channelId);
        }

        private async Task<CommandReturn> HandleSetLocationActionAsync(WeatherAction action, Models.Platform.Platform platform, string userId, Language language, string channelId)
        {
            CommandReturn commandReturn = new CommandReturn();
            if (bb.Program.BotInstance.UsersBuffer == null)
            {
                commandReturn.SetMessage(LocalizationService.GetString(language, "error:unknown", string.Empty, platform));
                return commandReturn;
            }

            var locations = await _weatherService.SearchLocationsAsync(action.Location);

            if (locations == null || locations.Count == 0 || locations[0].Name == "err")
            {
                return CreateErrorReturn("error:place_not_found", platform, language, channelId);
            }

            var firstLocation = locations[0];
            long userLongId = DataConversion.ToLong(userId);

            bb.Program.BotInstance.UsersBuffer.SetParameter(platform, userLongId, Users.Location, firstLocation.Name);
            bb.Program.BotInstance.UsersBuffer.SetParameter(platform, userLongId, Users.Latitude, firstLocation.Latitude.ToString(CultureInfo.InvariantCulture));
            bb.Program.BotInstance.UsersBuffer.SetParameter(platform, userLongId, Users.Longitude, firstLocation.Longitude.ToString(CultureInfo.InvariantCulture));

            commandReturn.SetMessage(LocalizationService.GetString(
                    language,
                    "command:weather:set_location",
                    channelId,
                    platform,
                    firstLocation.Name));
            commandReturn.SetColor(ChatColorPresets.YellowGreen);

            return commandReturn;
        }

        private CommandReturn HandleGetLocationAction(Models.Platform.Platform platform, string userId, Language language, string channelId)
        {
            CommandReturn commandReturn = new CommandReturn();
            if (bb.Program.BotInstance.UsersBuffer == null)
            {
                commandReturn.SetMessage(LocalizationService.GetString(language, "error:unknown", string.Empty, platform));
                return commandReturn;
            }

            var userPlace = (string)bb.Program.BotInstance.UsersBuffer.GetParameter(platform, DataConversion.ToLong(userId), Users.Location);

            if (string.IsNullOrEmpty(userPlace))
            {
                return CreateErrorReturn("error:location_not_set", platform, language, channelId);
            }

            commandReturn.SetMessage(LocalizationService.GetString(
                    language,
                    "command:weather:get_location",
                    channelId,
                    platform,
                    userPlace));

            return commandReturn;
        }

        private async Task<CommandReturn> HandleGetWeatherActionAsync(WeatherAction action, Models.Platform.Platform platform, string userId, Language language, string channelId)
        {
            CommandReturn commandReturn = new CommandReturn();
            if (bb.Program.BotInstance.UsersBuffer == null)
            {
                commandReturn.SetMessage(LocalizationService.GetString(language, "error:unknown", string.Empty, platform));
                return commandReturn;
            }

            if (string.IsNullOrWhiteSpace(action.Location))
            {
                long userLongId = DataConversion.ToLong(userId);

                var userPlace = (string)bb.Program.BotInstance.UsersBuffer.GetParameter(platform, userLongId, Users.Location);
                var userLat = (string)bb.Program.BotInstance.UsersBuffer.GetParameter(platform, userLongId, Users.Latitude);
                var userLon = (string)bb.Program.BotInstance.UsersBuffer.GetParameter(platform, userLongId, Users.Longitude);

                if (string.IsNullOrEmpty(userPlace) ||
                    string.IsNullOrEmpty(userLat) ||
                    string.IsNullOrEmpty(userLon))
                {
                    return CreateErrorReturn("error:location_not_set", platform, language, channelId);
                }

                var (lat, lon) = ParseCoordinates(userLat, userLon);

                if (lat == null || lon == null)
                {
                    throw new Exception($"Invalid coordinate format. Received: {userLat};{userLon}");
                }

                var weatherData = await _weatherService.GetCurrentWeatherAsync(lat.Value.ToString(System.Globalization.CultureInfo.InvariantCulture), lon.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
                if (weatherData == null || weatherData.Temperature == -400)
                {
                    return CreateErrorReturn("error:place_not_found", platform, language, channelId);
                }

                return BuildWeatherMessage(weatherData, userPlace, platform, language, channelId);
            }

            var searchResults = await _weatherService.SearchLocationsAsync(action.Location);

            if (searchResults == null || searchResults.Count == 0)
            {
                return CreateErrorReturn("error:place_not_found", platform, language, channelId);
            }

            if (searchResults.Count == 1 && searchResults[0].Name == "err")
            {
                return CreateErrorReturn("error:place_not_found", platform, language, channelId);
            }

            if (searchResults.Count == 1)
            {
                var weatherData = await _weatherService.GetCurrentWeatherAsync(
                    searchResults[0].Latitude.ToString(CultureInfo.InvariantCulture),
                    searchResults[0].Longitude.ToString(CultureInfo.InvariantCulture));

                if (weatherData == null || weatherData.Temperature == -400)
                {
                    return CreateErrorReturn("error:place_not_found", platform, language, channelId);
                }

                return BuildWeatherMessage(weatherData, searchResults[0].Name, platform, language, channelId);
            }

            SaveSearchResults(searchResults, platform, userId);

            var pageContent = BuildLocationPage(searchResults, 1);
            var maxPages = (int)Math.Ceiling((double)searchResults.Count / ResultsPerPage);

            commandReturn.SetMessage(LocalizationService.GetString(
                    language,
                    "command:weather:a_few_places",
                    channelId,
                    platform,
                    pageContent,
                    "1",
                    maxPages.ToString()));

            return commandReturn;
        }

        private List<LocationResult>? GetSavedLocations(Models.Platform.Platform platform, string userId)
        {
            if (bb.Program.BotInstance.UsersBuffer == null)
            {
                return null;
            }

            string weatherResultLocationsUnworkedString = (string)bb.Program.BotInstance.UsersBuffer.GetParameter(platform, DataConversion.ToLong(userId), Users.WeatherResultLocations);
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

        private void SaveSearchResults(List<LocationResult> locations, Models.Platform.Platform platform, string userId)
        {
            if (bb.Program.BotInstance.UsersBuffer == null)
            {
                throw new Exception("The user buffer is not initialized.");
            }

            List<string> jsons = locations.Select(loc =>
                $"name: \"{loc.Name}\", lat: \"{loc.Latitude}\", lon: \"{loc.Longitude}\"").ToList();

            bb.Program.BotInstance.UsersBuffer.SetParameter(platform, DataConversion.ToLong(userId), Users.WeatherResultLocations, DataConversion.SerializeStringList(jsons));
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

        private CommandReturn BuildWeatherMessage(WeatherData weather, string locationName, Models.Platform.Platform platform, Language language, string channelId)
        {
            CommandReturn commandReturn = new();
            commandReturn.SetMessage(LocalizationService.GetString(
                    language,
                    "command:weather",
                    channelId,
                    platform,
                    GetWeatherEmoji(weather.Temperature),
                    locationName,
                    weather.Temperature.ToString("0.0"),
                    weather.FeelsLike.ToString("0.0"),
                    weather.WindSpeed.ToString("0.0"),
                    GetWeatherSummary(language, weather.WeatherCode, channelId, platform),
                    GetWeatherSummaryEmoji(weather.WeatherCode),
                    weather.Pressure.ToString("0"),
                    weather.UvIndex.ToString("0.0"),
                    weather.Humidity.ToString("0"),
                    weather.Visibility.ToString("0")));

            return commandReturn;
        }

        private CommandReturn CreateErrorReturn(string errorKey, Models.Platform.Platform platform, Language language, string channelId)
        {
            CommandReturn commandReturn = new CommandReturn();
            commandReturn.SetMessage(LocalizationService.GetString(language, errorKey, channelId, platform));
            commandReturn.SetColor(ChatColorPresets.Red);

            return commandReturn;
        }

        private void LogError(string errorType, Exception ex, string userName)
        {
            Write($"Weather: [{errorType}] Command error for user {userName}: {ex.Message}");
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

        private string GetWeatherSummary(Language language, int weatherCode, string channelId, Models.Platform.Platform platform)
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

            double latitude;
            bool isSouth = latStr.EndsWith("S", StringComparison.OrdinalIgnoreCase);
            string latValue = isSouth ?
                latStr.Substring(0, latStr.Length - 1) :
                latStr.TrimEnd('N', 'n');

            latitude = DataConversion.ToDouble(latValue);

            if (isSouth)
                latitude = -latitude;

            // ====

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