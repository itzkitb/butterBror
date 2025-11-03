using System.Text.Json;
using System.Text.Json.Serialization;
using static bb.Core.Bot.Logger;
using feels.Dank.Cache.LRU;
using bb.Core.Commands.List.Utility;

namespace bb.Services.External
{
    /// <summary>
    /// Implementation of a weather service using the Open-Meteo API.
    /// Provides current weather data and location search capabilities with caching and retry mechanisms.
    /// </summary>
    public class OpenMeteoWeatherService : IWeatherService
    {
        private readonly HttpClient _httpClient;
        private readonly ILruCache<string, WeatherData> _weatherCache;
        private readonly ILruCache<string, List<LocationResult>> _locationsCache;

        private const string GeocodingUrl = "https://geocoding-api.open-meteo.com/v1/search";
        private const string WeatherUrl = "https://api.open-meteo.com/v1/forecast";
        private const int MaxRetries = 3;
        private const int CacheDurationMinutes = 60;
        private const int LocationsCacheDurationMinutes = 15;

        public OpenMeteoWeatherService(HttpClient httpClient)
        {
            _httpClient = httpClient;

            _weatherCache = new LruCache<string, WeatherData>(
                capacity: 1000,
                defaultTtl: TimeSpan.FromMinutes(CacheDurationMinutes),
                expirationMode: ExpirationMode.Absolute,
                cleanupInterval: TimeSpan.FromMinutes(2)
            );

            _locationsCache = new LruCache<string, List<LocationResult>>(
                capacity: 500,
                defaultTtl: TimeSpan.FromMinutes(LocationsCacheDurationMinutes),
                expirationMode: ExpirationMode.Absolute,
                cleanupInterval: TimeSpan.FromMinutes(2)
            );
        }

        /// <summary>
        /// Retrieves current weather data for the specified geographic coordinates.
        /// Implements caching to reduce API calls and retry logic to handle temporary failures.
        /// </summary>
        /// <param name="latitude">The latitude coordinate in decimal degrees</param>
        /// <param name="longitude">The longitude coordinate in decimal degrees</param>
        /// <returns>A WeatherData object containing current weather conditions</returns>
        /// <exception cref="WeatherApiException">Thrown when the API request fails after multiple attempts</exception>
        public async Task<WeatherData> GetCurrentWeatherAsync(string latitude, string longitude)
        {
            string cacheKey = $"weather_{latitude}_{longitude}";

            var queryParams = new Dictionary<string, string>
            {
                { "latitude", latitude },
                { "longitude", longitude },
                { "current", "temperature_2m,relative_humidity_2m,apparent_temperature,weather_code,wind_speed_10m,surface_pressure,pressure_msl,uv_index,visibility" },
                { "forecast_days", "0" }
            };

            var queryString = string.Join("&", queryParams.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));
            var url = $"{WeatherUrl}?{queryString}";

            WeatherData result = null;
            for (int i = 0; i < MaxRetries; i++)
            {
                try
                {
                    var response = await _httpClient.GetAsync(url);
                    response.EnsureSuccessStatusCode();

                    var json = await response.Content.ReadAsStringAsync();
                    var weatherResponse = JsonSerializer.Deserialize<OpenMeteoResponse>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (weatherResponse?.Current == null)
                    {
                        throw new Exception("Invalid weather response structure");
                    }

                    result = new WeatherData
                    {
                        Temperature = weatherResponse.Current.Temperature2m,
                        FeelsLike = weatherResponse.Current.ApparentTemperature,
                        WindSpeed = weatherResponse.Current.WindSpeed10m,
                        WeatherCode = weatherResponse.Current.WeatherCode,
                        Pressure = weatherResponse.Current.PressureMsl,
                        UvIndex = weatherResponse.Current.UvIndex,
                        Humidity = weatherResponse.Current.RelativeHumidity2m,
                        Visibility = weatherResponse.Current.Visibility / 1000
                    };

                    return result;
                }
                catch (Exception ex)
                {
                    Write($"Weather API request failed (attempt {i + 1}/{MaxRetries}): {ex.Message}", LogLevel.Warning);
                    if (i == MaxRetries - 1)
                    {
                        throw new WeatherApiException("Failed to get weather data after multiple attempts", ex);
                    }
                    await Task.Delay(200 * (i + 1));
                }
            }

            return new WeatherData { Temperature = -400 };
        }

        /// <summary>
        /// Searches for locations matching the specified name using geocoding API.
        /// Implements caching to reduce API calls for frequently searched locations and retry logic for reliability.
        /// </summary>
        /// <param name="locationName">The name of the location to search for</param>
        /// <returns>A list of location results with geographic coordinates and additional information</returns>
        /// <exception cref="WeatherApiException">Thrown when the API request fails after multiple attempts</exception>
        public async Task<List<LocationResult>> SearchLocationsAsync(string locationName)
        {
            string cacheKey = $"locations_{locationName.ToLowerInvariant()}";

            return await _locationsCache.GetOrAddAsync(
                cacheKey,
                async (key, ct) =>
                {
                    var url = $"{GeocodingUrl}?name={Uri.EscapeDataString(locationName)}&count=10";

                    for (int i = 0; i < MaxRetries; i++)
                    {
                        try
                        {
                            var response = await _httpClient.GetAsync(url, ct);
                            response.EnsureSuccessStatusCode();

                            var json = await response.Content.ReadAsStringAsync(ct);
                            var geocodingResponse = JsonSerializer.Deserialize<GeocodingResponse>(json, new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });

                            if (geocodingResponse?.Results == null || !geocodingResponse.Results.Any())
                            {
                                return new List<LocationResult>();
                            }

                            return geocodingResponse.Results
                                .Select(r => new LocationResult
                                {
                                    Name = $"{r.Name}{(!string.IsNullOrEmpty(r.Admin1) ? $", {r.Admin1}" : "")}{(!string.IsNullOrEmpty(r.Country) ? $", {r.Country}" : "")}",
                                    Latitude = r.Latitude,
                                    Longitude = r.Longitude,
                                    Elevation = r.Elevation,
                                    Timezone = r.Timezone
                                })
                                .ToList();
                        }
                        catch (Exception ex)
                        {
                            Write($"Location search failed (attempt {i + 1}/{MaxRetries}): {ex.Message}", LogLevel.Warning);

                            if (i == MaxRetries - 1)
                            {
                                throw new WeatherApiException("Failed to search locations after multiple attempts", ex);
                            }

                            // Задержка с учётом cancellation
                            await Task.Delay(200 * (i + 1), ct);
                        }
                    }

                    // Недостижимый код
                    throw new InvalidOperationException("Unexpected error in location search");
                },
                timeout: TimeSpan.FromSeconds(10) // Защита от зависаний
            );
        }

        /// <summary>
        /// Represents the response structure from the Open-Meteo weather API.
        /// </summary>
        private class OpenMeteoResponse
        {
            public CurrentWeather Current { get; set; }
        }

        /// <summary>
        /// Contains current weather conditions data from the Open-Meteo API response.
        /// </summary>
        private class CurrentWeather
        {
            [JsonPropertyName("temperature_2m")]
            public double Temperature2m { get; set; }

            [JsonPropertyName("relative_humidity_2m")]
            public double RelativeHumidity2m { get; set; }

            [JsonPropertyName("apparent_temperature")]
            public double ApparentTemperature { get; set; }

            [JsonPropertyName("weather_code")]
            public int WeatherCode { get; set; }

            [JsonPropertyName("wind_speed_10m")]
            public double WindSpeed10m { get; set; }

            [JsonPropertyName("surface_pressure")]
            public double SurfacePressure { get; set; }

            [JsonPropertyName("pressure_msl")]
            public double PressureMsl { get; set; }

            [JsonPropertyName("uv_index")]
            public double UvIndex { get; set; }

            [JsonPropertyName("visibility")]
            public double Visibility { get; set; }
        }

        /// <summary>
        /// Represents the response structure from the Open-Meteo geocoding API.
        /// </summary>
        private class GeocodingResponse
        {
            public List<GeocodingResult> Results { get; set; }
        }

        /// <summary>
        /// Contains detailed information about a geocoding result including location coordinates and metadata.
        /// </summary>
        private class GeocodingResult
        {
            public string Name { get; set; }
            public string Admin1 { get; set; }
            public string Country { get; set; }
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public double Elevation { get; set; }
            public string Timezone { get; set; }
        }
    }
}