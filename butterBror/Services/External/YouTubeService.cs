using System.Net;
using System.Text.RegularExpressions;
using static butterBror.Core.Bot.Console;

namespace butterBror.Services.External
{
    /// <summary>
    /// Provides functionality for interacting with YouTube content through basic web scraping techniques.
    /// </summary>
    public class YouTubeService
    {
        /// <summary>
        /// Extracts video URLs from a YouTube playlist page using regular expression pattern matching.
        /// </summary>
        /// <param name="playlistUrl">The URL of the YouTube playlist to process.</param>
        /// <returns>An array of full video URLs, or null if extraction fails.</returns>
        /// <exception cref="Exception">All exceptions during execution are caught and logged internally.</exception>
        /// <remarks>
        /// Uses a simple regex approach to find video links in the playlist HTML. This method is sensitive to 
        /// YouTube's HTML structure changes and may break if the page format changes. For production use, 
        /// consider using the official YouTube Data API instead.
        /// </remarks>
        
        public static string[] GetPlaylistVideos(string playlistUrl)
        {
            Engine.Statistics.FunctionsUsed.Add();
            try
            {
                MatchCollection matches = new Regex(@"watch\?v=[a-zA-Z0-9_-]{11}").Matches(new WebClient().DownloadString(playlistUrl));

                string[] videoLinks = new string[matches.Count];
                int i = 0;

                foreach (Match match in matches)
                {
                    videoLinks[i] = "https://www.youtube.com/" + match.Value;
                    i++;
                }

                return videoLinks;
            }
            catch (Exception ex)
            {
                Write(ex);
                return null;
            }
        }
    }
}
