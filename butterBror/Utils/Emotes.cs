using static butterBror.Core.Bot.Console;

namespace butterBror.Utils
{
    /// <summary>
    /// Provides functionality for retrieving and managing 7TV emotes for Twitch channels.
    /// </summary>
    public class Emotes
    {
        /// <summary>
        /// Gets or sets the semaphore used to synchronize cache access across threads.
        /// </summary>
        private static readonly SemaphoreSlim _cacheLock = new(1, 1);

        /// <summary>
        /// Retrieves cached 7TV emotes for a channel or fetches new ones if cache is expired.
        /// </summary>
        /// <param name="channel">The channel name to retrieve emotes for.</param>
        /// <param name="channel_id">The unique channel identifier.</param>
        /// <returns>A list of emote names, or null if retrieval fails.</returns>
        /// <remarks>
        /// Uses double-check locking pattern with semaphore to ensure thread-safe cache updates.
        /// Returns cached emotes if valid, otherwise fetches and caches new emotes.
        /// </remarks>
        
        public static async Task<List<string>?> GetEmotesForChannel(string channel, string channel_id)
        {
            try
            {
                if (Engine.Bot.ChannelsSevenTVEmotes.TryGetValue(channel_id, out var cached) &&
                    DateTime.UtcNow < cached.expiration)
                {
                    return cached.emotes;
                }

                await _cacheLock.WaitAsync();
                try
                {
                    if (Engine.Bot.ChannelsSevenTVEmotes.TryGetValue(channel_id, out cached) &&
                        DateTime.UtcNow < cached.expiration)
                    {
                        return cached.emotes;
                    }

                    var emotes = await GetEmotes(channel);
                    Engine.Bot.ChannelsSevenTVEmotes[channel_id] = (emotes, DateTime.UtcNow.Add(Engine.Bot.CacheTTL));
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
        /// Gets a random 7TV emote from the specified channel's emote set.
        /// </summary>
        /// <param name="channel">The channel name to select emote from.</param>
        /// <param name="channel_id">The unique channel identifier.</param>
        /// <returns>A random emote name, or null if no emotes are available.</returns>
        
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
        /// Forces an update of cached 7TV emotes for a channel.
        /// </summary>
        /// <param name="channel">The channel name to update emotes for.</param>
        /// <param name="channel_id">The unique channel identifier.</param>
        /// <remarks>
        /// Bypasses cache expiration time and refreshes emotes immediately.
        /// Updates the global cache with new expiration timestamp.
        /// </remarks>
        
        public static async Task EmoteUpdate(string channel, string channel_id)
        {
            try
            {
                var emotes = await GetEmotes(channel);
                Engine.Bot.ChannelsSevenTVEmotes[channel_id] = (emotes, DateTime.UtcNow.Add(Engine.Bot.CacheTTL));
            }
            catch (Exception ex)
            {
                Write(ex);
            }
        }

        /// <summary>
        /// Gets 7TV emotes for a channel with user ID caching mechanism.
        /// </summary>
        /// <param name="channel">The channel name to retrieve emotes for.</param>
        /// <returns>A list of emote names (always non-null, may be empty).</returns>
        /// <remarks>
        /// Uses two-level caching: first checks channel-to-user mapping cache,
        /// then fetches emotes from 7TV API if needed.
        /// </remarks>
        
        public static async Task<List<string>> GetEmotes(string channel)
        {
            try
            {
                if (Engine.Bot.UsersSearchCache.TryGetValue(channel, out var userCache) &&
                    DateTime.UtcNow < userCache.expiration)
                {
                    return await GetEmotesFromCache(userCache.userId);
                }

                var userId = Engine.Bot.SevenTvService.SearchUser(channel, Engine.Bot.Tokens.SevenTV).Result;
                if (string.IsNullOrEmpty(userId))
                {
                    Write($"SevenTV - #{channel} doesn't exist on 7tv!", "info");
                    return new List<string>();
                }

                Engine.Bot.UsersSearchCache[channel] = (userId, DateTime.UtcNow.Add(Engine.Bot.CacheTTL));
                return await GetEmotesFromCache(userId);
            }
            catch (Exception ex)
            {
                Write(ex);
                return new List<string>();
            }
        }

        /// <summary>
        /// Gets 7TV emotes directly from 7TV API using user ID.
        /// </summary>
        /// <param name="userId">The 7TV user ID to fetch emotes for.</param>
        /// <returns>A list of emote names (always non-null, may be empty).</returns>
        /// <remarks>
        /// Processes raw 7TV API response and extracts emote names.
        /// Returns empty list if user has no emotes or API call fails.
        /// </remarks>
        
        private static async Task<List<string>> GetEmotesFromCache(string userId)
        {
            var emote = await Engine.Bot.Clients.SevenTV.rest.GetUser(userId);
            if (emote?.connections?[0].emote_set?.emotes == null)
            {
                Write($"SevenTV - No emotes found for user {userId}", "info");
                return new List<string>();
            }

            return emote.connections[0].emote_set.emotes.Select(e => e.name).ToList();
        }
    }
}
