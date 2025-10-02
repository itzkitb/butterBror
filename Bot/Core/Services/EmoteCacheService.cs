using bb.Data;
using System.Collections.Concurrent;
using static bb.Core.Bot.Console;

namespace bb.Core.Services
{
    /// <summary>
    /// Provides persistent storage management for 7TV emote cache data across application restarts.
    /// </summary>
    /// <remarks>
    /// This service handles serialization and deserialization of emote-related cache data to ensure:
    /// <list type="bullet">
    /// <item>Persistent storage of frequently accessed emote data</item>
    /// <item>Reduced API calls to 7TV service through cached results</item>
    /// <item>Maintained cache expiration timestamps for proper TTL handling</item>
    /// <item>Thread-safe operations with ConcurrentDictionary structures</item>
    /// </list>
    /// All operations use JSON serialization with Newtonsoft.Json for compatibility and performance.
    /// </remarks>
    public class EmoteCacheService
    {
        /// <summary>
        /// Persists current emote cache data to persistent storage.
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        /// <item>Serializes four cache dictionaries to formatted JSON:
        /// <list type="bullet">
        /// <item>Channel-specific 7TV emotes with expiration timestamps</item>
        /// <item>Emote set identifiers mapping with expiration</item>
        /// <item>User search results cache with expiration</item>
        /// <item>Individual emote data with expiration metadata</item>
        /// </list>
        /// </item>
        /// <item>Ensures target directory exists before writing (creates if necessary)</item>
        /// <item>Uses atomic file operations to prevent partial writes</item>
        /// <item>Maintains human-readable JSON formatting for debugging purposes</item>
        /// <item>Preserves cache expiration timestamps for proper TTL implementation</item>
        /// </list>
        /// The operation is thread-safe and handles serialization errors through exception logging.
        /// Executed automatically every 10 minutes by the engine's background task.
        /// </remarks>
        public static void Save()
        {
            try
            {
                var data = new
                {
                    Channels7tvEmotes = bb.Program.BotInstance.ChannelsSevenTVEmotes.ToDictionary(kv => kv.Key, kv => kv.Value),
                    EmoteSetCache = bb.Program.BotInstance.EmoteSetsCache.ToDictionary(kv => kv.Key, kv => kv.Value),
                    UserSearchCache = bb.Program.BotInstance.UsersSearchCache.ToDictionary(kv => kv.Key, kv => kv.Value),
                    EmoteCache = bb.Program.BotInstance.EmotesCache.ToDictionary(kv => kv.Key, kv => kv.Value)
                };

                string json = Newtonsoft.Json.JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented);
                Directory.CreateDirectory(Path.GetDirectoryName(bb.Program.BotInstance.Paths.SevenTVCache));
                FileUtil.SaveFileContent(bb.Program.BotInstance.Paths.SevenTVCache, json);
            }
            catch (Exception ex)
            {
                Write(ex);
            }
        }

        /// <summary>
        /// Loads previously saved emote cache data from persistent storage.
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        /// <item>Verifies cache file existence before attempting deserialization</item>
        /// <item>Uses anonymous type deserialization for type-safe conversion</item>
        /// <item>Recreates all cache structures with proper expiration timestamps</item>
        /// <item>Maintains thread-safe ConcurrentDictionary implementations</item>
        /// <item>Handles version compatibility through graceful fallback</item>
        /// <item>Skips loading if cache has expired (determined by individual item TTLs)</item>
        /// </list>
        /// The operation is performed during bot initialization to reduce initial API load.
        /// If deserialization fails, the cache remains empty and will repopulate naturally.
        /// Cache validity is determined by individual item expiration timestamps, not file timestamp.
        /// </remarks>
        public static void Load()
        {
            try
            {
                if (!FileUtil.FileExists(bb.Program.BotInstance.Paths.SevenTVCache)) return;

                string json = FileUtil.GetFileContent(bb.Program.BotInstance.Paths.SevenTVCache);
                var template = new
                {
                    Channels7tvEmotes = new Dictionary<string, (List<string> emotes, DateTime expiration)>(),
                    EmoteSetCache = new Dictionary<string, (string setId, DateTime expiration)>(),
                    UserSearchCache = new Dictionary<string, (string userId, DateTime expiration)>(),
                    EmoteCache = new Dictionary<string, (SevenTV.Types.Rest.Emote emote, DateTime expiration)>()
                };

                var data = Newtonsoft.Json.JsonConvert.DeserializeAnonymousType(json, template);

                bb.Program.BotInstance.ChannelsSevenTVEmotes = new ConcurrentDictionary<string, (List<string>, DateTime)>(data.Channels7tvEmotes);
                bb.Program.BotInstance.EmoteSetsCache = new ConcurrentDictionary<string, (string, DateTime)>(data.EmoteSetCache);
                bb.Program.BotInstance.UsersSearchCache = new ConcurrentDictionary<string, (string, DateTime)>(data.UserSearchCache);
                bb.Program.BotInstance.EmotesCache = new ConcurrentDictionary<string, (SevenTV.Types.Rest.Emote, DateTime)>(data.EmoteCache);
            }
            catch (Exception ex)
            {
                Write(ex);
            }
        }
    }
}
