using butterBror.Utils.DataManagers;
using butterBror.Utils.Types;
using DankDB;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static butterBror.Utils.Bot.Console;

namespace butterBror.Utils.Tools.API
{
    [Obsolete("Will be rewritten to another API.")]
    public class Weather
    {
        [ConsoleSector("butterBror.Utils.Tools.API.Weather", "Get")]
        public static async Task<Data> Get(string lat, string lon)
        {
            Core.Statistics.FunctionsUsed.Add();

            Data GetErrorData()
                => new Data
                {
                    current = new Current
                    {
                        summary = "",
                        temperature = -400,
                        feels_like = 0,
                        wind = new() { speed = 0 },
                        pressure = 0,
                        uv_index = 0,
                        humidity = 0,
                        visibility = 0
                    }
                };

            try
            {
                var tokens = Manager.Get<string[]>(Core.Bot.Pathes.Settings, "weather_token");
                string dateKey = DateTime.UtcNow.ToString("ddMMyyyy");
                using var client = new HttpClient();

                foreach (var token in tokens)
                {
                    string cacheKey = dateKey + token;
                    int usage = Manager.Get<int>(Core.Bot.Pathes.Cache, cacheKey);
                    if (usage >= 10) continue;

                    SafeManager.Save(Core.Bot.Pathes.Cache, cacheKey, ++usage);

                    var uri = new Uri(
                        $"https://ai-weather-by-meteosource.p.rapidapi.com/current" +
                        $"?lat={lat.TrimEnd('N', 'S')}&lon={lon.TrimEnd('E', 'W')}" +
                        $"&timezone=auto&language=en&units=auto"
                    );
                    using var req = new HttpRequestMessage(HttpMethod.Get, uri);
                    req.Headers.Add("X-RapidAPI-Key", token);
                    req.Headers.Add("X-RapidAPI-Host", "ai-weather-by-meteosource.p.rapidapi.com");

                    using var resp = await client.SendAsync(req);
                    if (resp.IsSuccessStatusCode)
                        return JsonConvert.DeserializeObject<Data>(await resp.Content.ReadAsStringAsync());

                    Write($"API WEATHER ERROR ({token}): #{resp.StatusCode}, {resp.ReasonPhrase}", "info", LogLevel.Warning);
                }

                return GetErrorData();
            }
            catch (Exception ex)
            {
                Write(ex);
                return GetErrorData();
            }
        }

        [ConsoleSector("butterBror.Utils.Tools.API.Weather", "GetLocation")]
        public static async Task<List<Place>> GetLocation(string placeName)
        {
            Core.Statistics.FunctionsUsed.Add();

            List<Place> ErrorResult() => new() { new Place { name = "err", lat = "", lon = "" } };

            try
            {
                var tokens = Manager.Get<string[]>(Core.Bot.Pathes.Settings, "weather_token");
                string dateKey = DateTime.UtcNow.ToString("ddMMyyyy");
                var cache = Manager.Get<Dictionary<string, LocationCacheData>>(Core.Bot.Pathes.Cache, "Data")
                            ?? new Dictionary<string, LocationCacheData>();
                var cached = cache.Values
                                  .Where(c => c.Tags?.Contains(placeName, StringComparer.OrdinalIgnoreCase) == true
                                           || string.Equals(c.CityName, placeName, StringComparison.OrdinalIgnoreCase))
                                  .Select(c => new Place { name = c.CityName, lat = c.Lat, lon = c.Lon })
                                  .ToList();
                if (cached.Count > 0)
                    return cached;

                using var client = new HttpClient();
                foreach (var token in tokens)
                {
                    string usageKey = dateKey + token;
                    int uses = Manager.Get<int>(Core.Bot.Pathes.APIUses, usageKey);
                    if (uses >= 10)
                        continue;

                    SafeManager.Save(Core.Bot.Pathes.APIUses, usageKey, ++uses);

                    var uri = new Uri(
                        $"https://ai-weather-by-meteosource.p.rapidapi.com/find_places" +
                        $"?text={Uri.EscapeDataString(placeName)}&language=en"
                    );
                    using var req = new HttpRequestMessage(HttpMethod.Get, uri);
                    req.Headers.Add("X-RapidAPI-Key", token);
                    req.Headers.Add("X-RapidAPI-Host", "ai-weather-by-meteosource.p.rapidapi.com");

                    using var resp = await client.SendAsync(req);
                    if (!resp.IsSuccessStatusCode)
                    {
                        Write($"API WEATHER ERROR ({token}): #{resp.StatusCode}, {resp.ReasonPhrase}", "info", LogLevel.Warning);
                        continue;
                    }

                    var places = JsonConvert.DeserializeObject<List<Place>>(await resp.Content.ReadAsStringAsync());
                    foreach (var p in places)
                    {
                        string key = (p.name.ToLowerInvariant() + p.lat + p.lon);
                        if (cache.TryGetValue(key, out var entry))
                        {
                            var tags = entry.Tags?.ToList() ?? new List<string>();
                            if (!tags.Contains(placeName, StringComparer.OrdinalIgnoreCase))
                                tags.Add(placeName);
                            entry.Tags = tags.ToArray();
                        }
                        else
                        {
                            cache[key] = new LocationCacheData
                            {
                                CityName = p.name,
                                Lat = p.lat,
                                Lon = p.lon,
                                Tags = new[] { placeName }
                            };
                        }
                    }

                    SafeManager.Save(Core.Bot.Pathes.Cache, "Data", cache);
                    return places;
                }

                return ErrorResult();
            }
            catch (Exception ex)
            {
                Write(ex);
                return new() { new Place { name = "err", lat = "", lon = "" } };
            }
        }

        public class Data
        {
            public required Current current { get; set; }
        }
        public class Current
        {
            public required string summary { get; set; }
            public double temperature { get; set; }
            public double feels_like { get; set; }
            public required WindInfo wind { get; set; }
            public int pressure { get; set; }
            public double uv_index { get; set; }
            public int humidity { get; set; }
            public double visibility { get; set; }
        }
        public class WindInfo
        {
            public double speed { get; set; }
        }
        public class Place
        {
            public required string name { get; set; }
            public required string lat { get; set; }
            public required string lon { get; set; }
        }
        public class LocationCacheData
        {
            public string CityName { get; set; }
            public string Lat { get; set; }
            public string Lon { get; set; }
            public string[] Tags { get; set; }
        }
        public static string GetEmoji(double temperature)
        {
            Core.Statistics.FunctionsUsed.Add();
            if (temperature > 35)
                return "🔥";
            else if (temperature > 30)
                return "🥵";
            else if (temperature > 25)
                return "😓";
            else if (temperature > 20)
                return "😥";
            else if (temperature > 15)
                return "😐";
            else if (temperature > 10)
                return "😰";
            else if (temperature > 0)
                return "😨";
            else
                return "🥶";
        }
        public static string GetSummary(string lang, string summary, string channelID, Platforms platform)
        {
            Core.Statistics.FunctionsUsed.Add();
            switch (summary.ToLower())
            {
                case "sunny":
                    return TranslationManager.GetTranslation(lang, "text:weather:clear", channelID, platform);
                case "partly cloudy":
                    return TranslationManager.GetTranslation(lang, "text:weather:cloudy", channelID, platform);
                case "mostly cloudy":
                    return TranslationManager.GetTranslation(lang, "text:weather:mostly_cloudy", channelID, platform);
                case "partly sunny":
                    return TranslationManager.GetTranslation(lang, "text:weather:partly_cloudy", channelID, platform);
                case "cloudy":
                    return TranslationManager.GetTranslation(lang, "text:weather:cloudy", channelID, platform);
                case "overcast":
                    return TranslationManager.GetTranslation(lang, "text:weather:overcast", channelID, platform);
                case "rain":
                    return TranslationManager.GetTranslation(lang, "text:weather:rain", channelID, platform);
                case "thunderstorm":
                    return TranslationManager.GetTranslation(lang, "text:weather:thunderstorm", channelID, platform);
                case "snow":
                    return TranslationManager.GetTranslation(lang, "text:weather:snow", channelID, platform);
                case "fog":
                    return TranslationManager.GetTranslation(lang, "text:weather:fog", channelID, platform);
                default:
                    return summary;
            }
        }
        public static string GetSummaryEmoji(string summary)
        {
            Core.Statistics.FunctionsUsed.Add();
            switch (summary.ToLower())
            {
                case "sunny":
                    return "☀️";
                case "partly cloudy":
                    return "🌤️";
                case "mostly cloudy":
                    return "⛅";
                case "partly sunny":
                    return "🌤";
                case "cloudy":
                    return "☁️";
                case "overcast":
                    return "🌥️";
                case "rain":
                    return "🌧️";
                case "thunderstorm":
                    return "⛈️";
                case "snow":
                    return "🌨️";
                case "fog":
                    return "🌫️";
                default:
                    return $":{summary}:";
            }
        }
    }
}
