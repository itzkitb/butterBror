using static bb.Core.Bot.Console;

namespace bb.Utils
{
    /// <summary>
    /// Provides thread-safe functionality for retrieving and managing 7TV emotes across Twitch channels.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class implements a comprehensive caching system for 7TV emote data with the following features:
    /// <list type="bullet">
    /// <item>Two-level caching mechanism (channel-to-user mapping and emote sets)</item>
    /// <item>Automatic cache expiration based on configurable TTL (Time-To-Live)</item>
    /// <item>Thread-safe access using semaphore synchronization</item>
    /// <item>Integrated error handling for API failures</item>
    /// </list>
    /// </para>
    /// <para>
    /// Key design considerations:
    /// <list type="bullet">
    /// <item>Minimizes API calls to 7TV through efficient caching strategies</item>
    /// <item>Handles channel renames through user ID mapping</item>
    /// <item>Provides random emote selection for chat interactions</item>
    /// <item>Supports manual cache refresh for administrative operations</item>
    /// </list>
    /// </para>
    /// All operations are asynchronous to prevent blocking the main bot execution thread.
    /// </remarks>
    public class SevenTvEmoteCache
    {
        private static readonly SemaphoreSlim _cacheLock = new(1, 1);

        /// <summary>
        /// Retrieves 7TV emotes for a specific channel with automatic cache management.
        /// </summary>
        /// <param name="channel">The channel name (username) to retrieve emotes for.</param>
        /// <param name="channel_id">The unique Twitch channel identifier.</param>
        /// <returns>
        /// A list of emote names if available; <see langword="null"/> if retrieval fails due to API errors.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Operation workflow:
        /// <list type="number">
        /// <item>Checks if valid cached emotes exist for the channel</item>
        /// <item>If cache is valid, returns immediately with cached data</item>
        /// <item>If cache is expired or missing, acquires lock and refreshes cache</item>
        /// <item>Fetches user ID and emote set through 7TV API</item>
        /// <item>Updates cache with new data and expiration timestamp</item>
        /// </list>
        /// </para>
        /// <para>
        /// Cache behavior:
        /// <list type="bullet">
        /// <item>Uses <see cref="Bot.CacheTTL"/> for expiration (default: 30 minutes)</item>
        /// <item>Double-check locking prevents redundant refreshes during concurrent access</item>
        /// <item>Failed API calls preserve previous cache state when possible</item>
        /// </list>
        /// </para>
        /// This method is safe for high-frequency calls in chat message processing.
        /// </remarks>
        /// <example>
        /// <code>
        /// var emotes = await Emotes.GetEmotesForChannel("twitchuser", "123456789");
        /// if (emotes != null &amp;&amp; emotes.Contains("CoolCat"))
        /// {
        ///     // Process emote
        /// }
        /// </code>
        /// </example>
        public static async Task<List<string>?> GetEmotesForChannel(string channel, string channel_id)
        {
            try
            {
                if (bb.Program.BotInstance.ChannelsSevenTVEmotes.TryGetValue(channel_id, out var cached) &&
                    DateTime.UtcNow < cached.expiration)
                {
                    return cached.emotes;
                }

                await _cacheLock.WaitAsync();
                try
                {
                    if (bb.Program.BotInstance.ChannelsSevenTVEmotes.TryGetValue(channel_id, out cached) &&
                        DateTime.UtcNow < cached.expiration)
                    {
                        return cached.emotes;
                    }

                    var emotes = await GetEmotes(channel);
                    bb.Program.BotInstance.ChannelsSevenTVEmotes[channel_id] = (emotes, DateTime.UtcNow.Add(bb.Program.BotInstance.CacheTTL));
                    return emotes;
                }
                finally
                {
                    _cacheLock.Release();
                }
            }
            catch (Exception ex)
            {
                Write(ex);
                return null;
            }
        }

        /// <summary>
        /// Selects a random emote from a channel's 7TV emote set.
        /// </summary>
        /// <param name="channel">The channel name (username) to select from.</param>
        /// <param name="channel_id">The unique Twitch channel identifier.</param>
        /// <returns>
        /// A random emote name from the channel's set; <see langword="null"/> if no emotes are available.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Selection process:
        /// <list type="bullet">
        /// <item>Retrieves channel emotes using <see cref="GetEmotesForChannel"/></item>
        /// <item>Uses cryptographically weak Random (sufficient for emote selection)</item>
        /// <item>Returns null if channel has no emotes or retrieval fails</item>
        /// </list>
        /// </para>
        /// <para>
        /// Usage considerations:
        /// <list type="bullet">
        /// <item>Not suitable for cryptographic purposes (uses standard Random)</item>
        /// <item>May return the same emote consecutively (true randomness)</item>
        /// <item>Thread-safe due to underlying cache mechanisms</item>
        /// </list>
        /// </para>
        /// Commonly used for random emote responses in chat interactions.
        /// </remarks>
        /// <example>
        /// <code>
        /// string randomEmote = await Emotes.RandomEmote("twitchuser", "123456789");
        /// if (!string.IsNullOrEmpty(randomEmote))
        /// {
        ///     // Use random emote in response
        /// }
        /// </code>
        /// </example>
        public static async Task<string> RandomEmote(string channel, string channel_id)
        {
            try
            {
                var emotes = await GetEmotesForChannel(channel, channel_id);
                return emotes?.Count > 0
                    ? emotes[(new Random()).Next(emotes.Count)]
                    : null;
            }
            catch (Exception ex)
            {
                Write(ex);
                return null;
            }
        }

        /// <summary>
        /// Forces an immediate refresh of a channel's 7TV emote cache.
        /// </summary>
        /// <param name="channel">The channel name (username) to update.</param>
        /// <param name="channel_id">The unique Twitch channel identifier.</param>
        /// <remarks>
        /// <para>
        /// This method:
        /// <list type="bullet">
        /// <item>Bypasses normal cache expiration rules</item>
        /// <item>Triggers a fresh API call to 7TV regardless of current cache state</item>
        /// <item>Updates cache with new data and reset expiration timer</item>
        /// <item>Does not return emote data (only updates cache)</item>
        /// </list>
        /// </para>
        /// <para>
        /// Typical use cases:
        /// <list type="bullet">
        /// <item>After channel emote set modifications</item>
        /// <item>When implementing administrative commands</item>
        /// <item>During channel setup procedures</item>
        /// </list>
        /// </para>
        /// Unlike other methods, this does not return data but ensures subsequent calls
        /// to <see cref="GetEmotesForChannel"/> will use fresh data.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Force refresh after emote set modification
        /// await Emotes.EmoteUpdate("twitchuser", "123456789");
        /// </code>
        /// </example>
        public static async Task EmoteUpdate(string channel, string channel_id)
        {
            try
            {
                var emotes = await GetEmotes(channel);
                bb.Program.BotInstance.ChannelsSevenTVEmotes[channel_id] = (emotes, DateTime.UtcNow.Add(bb.Program.BotInstance.CacheTTL));
            }
            catch (Exception ex)
            {
                Write(ex);
            }
        }

        /// <summary>
        /// Retrieves 7TV emotes for a channel with integrated user ID caching.
        /// </summary>
        /// <param name="channel">The channel name (username) to retrieve emotes for.</param>
        /// <returns>
        /// A list of emote names (never <see langword="null"/>, may be empty).
        /// </returns>
        /// <remarks>
        /// <para>
        /// Two-level caching process:
        /// <list type="number">
        /// <item>First checks channel-to-user ID mapping cache</item>
        /// <item>If mapping exists and valid, proceeds to emote retrieval</item>
        /// <item>If mapping expired or missing, queries 7TV for user ID</item>
        /// <item>Stores new mapping with updated expiration time</item>
        /// <item>Fetches emotes using the resolved user ID</item>
        /// </list>
        /// </para>
        /// <para>
        /// Error handling:
        /// <list type="bullet">
        /// <item>Returns empty list (not null) when channel doesn't exist on 7TV</item>
        /// <item>Logs informational messages for missing channels</item>
        /// <item>Preserves existing cache state during temporary failures</item>
        /// </list>
        /// </para>
        /// This method serves as the primary interface for emote data retrieval,
        /// with other methods building upon its functionality.
        /// </remarks>
        public static async Task<List<string>> GetEmotes(string channel)
        {
            try
            {
                if (bb.Program.BotInstance.UsersSearchCache.TryGetValue(channel, out var userCache) &&
                    DateTime.UtcNow < userCache.expiration)
                {
                    return await GetEmotesFromCache(userCache.userId);
                }

                var userId = bb.Program.BotInstance.SevenTv.SearchUser(channel, bb.Program.BotInstance.Tokens.SevenTV).Result;
                if (string.IsNullOrEmpty(userId))
                {
                    Write($"SevenTV: #{channel} doesn't exist on 7tv!");
                    return new List<string>();
                }

                bb.Program.BotInstance.UsersSearchCache[channel] = (userId, DateTime.UtcNow.Add(bb.Program.BotInstance.CacheTTL));
                return await GetEmotesFromCache(userId);
            }
            catch (Exception ex)
            {
                Write(ex);
                return new List<string>();
            }
        }

        /// <summary>
        /// Retrieves 7TV emotes directly from the 7TV API using a user ID.
        /// </summary>
        /// <param name="userId">The 7TV user identifier.</param>
        /// <returns>
        /// A list of emote names (never <see langword="null"/>, may be empty).
        /// </returns>
        /// <remarks>
        /// <para>
        /// Implementation details:
        /// <list type="bullet">
        /// <item>Directly queries 7TV REST API for user's emote set</item>
        /// <item>Processes the API response to extract emote names</item>
        /// <item>Handles cases where user has no emote set configured</item>
        /// <item>Converts API response to simple string list format</item>
        /// </list>
        /// </para>
        /// <para>
        /// Response processing:
        /// <list type="bullet">
        /// <item>Validates API response structure</item>
        /// <item>Extracts first connection's emote set (primary channel)</item>
        /// <item>Maps emote objects to their name properties</item>
        /// <item>Returns empty list for invalid or empty responses</item>
        /// </list>
        /// </para>
        /// This is a low-level method typically called by higher-level caching methods.
        /// It should not be used directly in most application scenarios.
        /// </remarks>
        private static async Task<List<string>> GetEmotesFromCache(string userId)
        {
            var emote = await bb.Program.BotInstance.Clients.SevenTV.rest.GetUser(userId);
            if (emote?.connections?[0].emote_set?.emotes == null)
            {
                Write($"SevenTV: No emotes found for user {userId}");
                return new List<string>();
            }

            return emote.connections[0].emote_set.emotes.Select(e => e.name).ToList();
        }
    }
}
