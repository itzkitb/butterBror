namespace bb.Services.External
{
    /// <summary>
    /// Utility class for integrating with nopaste.net temporary clipboard service.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class provides a simple interface to the nopaste.net service, which is:
    /// <list type="bullet">
    /// <item>A cross-platform clipboard sharing solution</item>
    /// <item>A temporary file hosting service with expiration policies</item>
    /// <item>Accessible via multiple endpoints (Web UI, curl, netcat, CLI)</item>
    /// </list>
    /// </para>
    /// <para>
    /// Service limitations to be aware of:
    /// <list type="bullet">
    /// <item><c>Total clipboard size limit:</c> 2.0 GB</item>
    /// <item><c>Total number of files:</c> Maximum 10,000 files</item>
    /// <item><c>Per-file size limit:</c> 2.0 MB</item>
    /// <item><c>Expiration policy:</c> 730 days for text content, 30 days for binary files</item>
    /// </list>
    /// </para>
    /// The implementation uses the HTTP endpoint with custom headers for configuration.
    /// </remarks>
    public class NoPasteService
    {
        /// <summary>
        /// Uploads text content to the nopaste.net temporary clipboard service with specified time-to-live.
        /// </summary>
        /// <param name="text">The text content to upload. Must not exceed 2.0 MB in size.</param>
        /// <param name="ttlSeconds">Time-to-live duration in seconds before automatic deletion. 
        /// Default is 3600 seconds (1 hour). Note that actual expiration may vary based on service policies:
        /// <list type="bullet">
        /// <item>Text content: Maximum 730 days (63,072,000 seconds)</item>
        /// <item>Other content: Maximum 30 days (2,592,000 seconds)</item>
        /// </list>
        /// </param>
        /// <returns>
        /// A direct URL to access the uploaded content if successful; otherwise <see langword="null"/>.
        /// The URL format depends on the service response:
        /// <list type="bullet">
        /// <item>When "X-URL" header is present: Returns the exact URL from the header</item>
        /// <item>When "X-File" header is present: Returns "https://nopaste.net/{fileId}"</item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// <para>
        /// The upload process:
        /// <list type="number">
        /// <item>Sends a PUT request to the nopaste.net base endpoint</item>
        /// <item>Includes "X-TTL" header with specified expiration time</item>
        /// <item>Processes response headers to determine access URL</item>
        /// </list>
        /// </para>
        /// <para>
        /// Error handling:
        /// <list type="bullet">
        /// <item>Returns <see langword="null"/> for network failures or service errors</item>
        /// <item>Logs exceptions to the bot console for debugging</item>
        /// <item>Common failure reasons: content too large, service limits exceeded</item>
        /// </list>
        /// </para>
        /// <para>
        /// Usage notes:
        /// <list type="bullet">
        /// <item>For text content under 2.0 MB, this provides a quick sharing solution</item>
        /// <item>The service may truncate or reject content exceeding size limits</item>
        /// <item>Consider using this for temporary data sharing between bot instances</item>
        /// </list>
        /// </para>
        /// This method is thread-safe and can be called concurrently from multiple bot components.
        /// </remarks>
        /// <example>
        /// Upload error logs for remote debugging:
        /// <code>
        /// string logUrl = await NoPaste.Upload("Error: Connection timeout", ttlSeconds: 86400);
        /// if (logUrl != null)
        /// {
        ///     Console.WriteLine($"Error log available at: {logUrl}");
        /// }
        /// </code>
        /// </example>
        public static async Task<string> Upload(string text, int ttlSeconds = 3600)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    string fileId = string.Empty;
                    string url = $"https://nopaste.net/{fileId}";

                    var content = new StringContent(text);
                    content.Headers.Add("X-TTL", ttlSeconds.ToString());

                    var response = await client.PutAsync(url, content);

                    if (response.IsSuccessStatusCode)
                    {
                        if (response.Headers.TryGetValues("X-URL", out var urlValues))
                        {
                            return urlValues.FirstOrDefault();
                        }
                    }

                    if (response.Headers.TryGetValues("X-File", out var fileValues))
                    {
                        string file = fileValues.FirstOrDefault();
                        return $"https://nopaste.net/{file}";
                    }
                }
            }
            catch (Exception ex)
            {
                Core.Bot.Console.Write(ex);
            }

            return null;
        }
    }
}
