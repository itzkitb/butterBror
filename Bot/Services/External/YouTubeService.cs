using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using feels.Dank.Cache.LRU;
using static bb.Core.Bot.Logger;

namespace bb.Services.External
{
    /// <summary>
    /// Provides functionality for interacting with YouTube content through basic web scraping techniques with caching.
    /// </summary>
    public class YouTubeService : IYouTubeService
    {
        private readonly HttpClient _httpClient;
        private readonly ILruCache<string, string[]> _playlistCache;
        private const int CacheDurationMinutes = 30;
        private const int MaxRetries = 3;

        public YouTubeService(HttpClient httpClient)
        {
            _httpClient = httpClient;

            _playlistCache = new LruCache<string, string[]>(
                capacity: 200,
                defaultTtl: TimeSpan.FromMinutes(CacheDurationMinutes),
                expirationMode: ExpirationMode.Absolute,
                cleanupInterval: TimeSpan.FromMinutes(5)
            );
        }

        /// <summary>
        /// Extracts video URLs from a YouTube playlist page using regular expression pattern matching with caching.
        /// </summary>
        /// <param name="playlistUrl">The URL of the YouTube playlist to process.</param>
        /// <returns>An array of full video URLs.</returns>
        /// <exception cref="Exception">Thrown when extraction fails after multiple attempts.</exception>
        public async Task<string[]> GetPlaylistVideosAsync(string playlistUrl)
        {
            string cacheKey = NormalizePlaylistUrl(playlistUrl);

            return await _playlistCache.GetOrAddAsync(
                cacheKey,
                async (key, ct) =>
                {
                    for (int i = 0; i < MaxRetries; i++)
                    {
                        try
                        {
                            string html = await _httpClient.GetStringAsync(playlistUrl, ct);
                            return ExtractVideoUrls(html);
                        }
                        catch (HttpRequestException ex)
                        {
                            Write($"YouTube: API request failed (attempt {i + 1}/{MaxRetries}): {ex.Message}", LogLevel.Warning);

                            if (i == MaxRetries - 1)
                            {
                                throw new Exception("Failed to get playlist after multiple attempts", ex);
                            }

                            await Task.Delay(500 * (int)Math.Pow(2, i), ct);
                        }
                    }

                    throw new InvalidOperationException("Unexpected error in playlist extraction");
                },
                timeout: TimeSpan.FromSeconds(10)
            );
        }

        /// <summary>
        /// Нормализует URL плейлиста для создания стабильного ключа кэша
        /// </summary>
        private string NormalizePlaylistUrl(string url)
        {
            var uriBuilder = new UriBuilder(url);
            var query = System.Web.HttpUtility.ParseQueryString(uriBuilder.Query);

            var cleanQuery = new Dictionary<string, string>();
            if (query["list"] != null)
                cleanQuery["list"] = query["list"];

            uriBuilder.Query = string.Join("&", cleanQuery.Select(p => $"{p.Key}={p.Value}"));
            return uriBuilder.ToString().ToLowerInvariant();
        }

        /// <summary>
        /// Извлекает URL видео из HTML страницы плейлиста
        /// </summary>
        private string[] ExtractVideoUrls(string html)
        {
            var matches = new Regex(@"watch\?v=[^""&\?]{11}").Matches(html);

            return matches
                .Cast<Match>()
                .Select(m => "https://www.youtube.com/" + m.Value)
                .Distinct()
                .ToArray();
        }
    }

    /// <summary>
    /// Интерфейс для YouTube сервиса (для DI и тестирования)
    /// </summary>
    public interface IYouTubeService
    {
        Task<string[]> GetPlaylistVideosAsync(string playlistUrl);
    }
}