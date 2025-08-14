namespace butterBror.Services.External
{
    /// <summary>
    /// Interface for working with weather data services.
    /// Defines methods for retrieving current weather information and location search capabilities.
    /// </summary>
    public interface IWeatherService
    {
        /// <summary>
        /// Retrieves current weather data for the specified geographic coordinates.
        /// </summary>
        /// <param name="latitude">Latitude coordinate in string format</param>
        /// <param name="longitude">Longitude coordinate in string format</param>
        /// <returns>Current weather data object containing temperature, wind speed, and other metrics</returns>
        Task<WeatherData> GetCurrentWeatherAsync(string latitude, string longitude);

        /// <summary>
        /// Searches for locations matching the specified name.
        /// </summary>
        /// <param name="locationName">The name of the location to search for</param>
        /// <returns>A list of location results that match the search query</returns>
        Task<List<LocationResult>> SearchLocationsAsync(string locationName);
    }

    /// <summary>
    /// Represents current weather conditions data including temperature, wind speed, and other meteorological metrics.
    /// </summary>
    public class WeatherData
    {
        public double Temperature { get; set; }
        public double FeelsLike { get; set; }
        public double WindSpeed { get; set; }
        public int WeatherCode { get; set; }
        public double Pressure { get; set; }
        public double UvIndex { get; set; }
        public double Humidity { get; set; }
        public double Visibility { get; set; }
    }

    /// <summary>
    /// Represents a location search result containing geographic coordinates and related information.
    /// </summary>
    public class LocationResult
    {
        public string Name { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Elevation { get; set; }
        public string Timezone { get; set; }
    }
}