using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static butterBror.Utils.Things.Console;

namespace butterBror.Utils.Tools
{
    public class Emotes
    {
        /// <summary>
        /// Getting channel emotes from cache
        /// </summary>
        private static readonly SemaphoreSlim cache_lock = new(1, 1);

        [ConsoleSector("butterBror.Utils.Tools.Emotes", "GetEmotesForChannel")]
        public static async Task<List<string>?> GetEmotesForChannel(string channel, string channel_id)
        {
            Core.Statistics.FunctionsUsed.Add();
            try
            {
                if (Core.Bot.ChannelsSevenTVEmotes.TryGetValue(channel_id, out var cached) &&
                    DateTime.UtcNow < cached.expiration)
                {
                    return cached.emotes;
                }

                await cache_lock.WaitAsync();
                try
                {
                    if (Core.Bot.ChannelsSevenTVEmotes.TryGetValue(channel_id, out cached) &&
                        DateTime.UtcNow < cached.expiration)
                    {
                        return cached.emotes;
                    }

                    var emotes = await GetEmotes(channel);
                    Core.Bot.ChannelsSevenTVEmotes[channel_id] = (emotes, DateTime.UtcNow.Add(Core.Bot.CacheTTL));
                    return emotes;
                }
                finally
                {
                    cache_lock.Release();
                }
            }
            catch (Exception ex)
            {
                Write(ex);
                return null;
            }
        }

        [ConsoleSector("butterBror.Utils.Tools.Emotes", "RandomEmote")]
        public static async Task<string> RandomEmote(string channel, string channel_id)
        {
            Core.Statistics.FunctionsUsed.Add();
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

        [ConsoleSector("butterBror.Utils.Tools.Emotes", "EmoteUpdate")]
        public static async Task EmoteUpdate(string channel, string channel_id)
        {
            Core.Statistics.FunctionsUsed.Add();
            try
            {
                var emotes = await GetEmotes(channel);
                Core.Bot.ChannelsSevenTVEmotes[channel_id] = (emotes, DateTime.UtcNow.Add(Core.Bot.CacheTTL));
            }
            catch (Exception ex)
            {
                Write(ex);
            }
        }

        [ConsoleSector("butterBror.Utils.Tools.Emotes", "GetEmotes")]
        public static async Task<List<string>> GetEmotes(string channel)
        {
            Core.Statistics.FunctionsUsed.Add();
            try
            {
                if (Core.Bot.UsersSearchCache.TryGetValue(channel, out var userCache) &&
                    DateTime.UtcNow < userCache.expiration)
                {
                    return await GetEmotesFromCache(userCache.userId);
                }

                var userId = Core.Bot.SevenTvService.SearchUser(channel, Core.Bot.Tokens.SevenTV).Result;
                if (string.IsNullOrEmpty(userId))
                {
                    Write($"SevenTV - #{channel} doesn't exist on 7tv!", "info");
                    return new List<string>();
                }

                Core.Bot.UsersSearchCache[channel] = (userId, DateTime.UtcNow.Add(Core.Bot.CacheTTL));
                return await GetEmotesFromCache(userId);
            }
            catch (Exception ex)
            {
                Write(ex);
                return new List<string>();
            }
        }

        [ConsoleSector("butterBror.Utils.Tools.Emotes", "GetEmotesFromCache")]
        private static async Task<List<string>> GetEmotesFromCache(string userId)
        {
            Core.Statistics.FunctionsUsed.Add();
            var emote = await Core.Bot.Clients.SevenTV.rest.GetUser(userId);
            if (emote?.connections?[0].emote_set?.emotes == null)
            {
                Write($"SevenTV - No emotes found for user {userId}", "info");
                return new List<string>();
            }

            return emote.connections[0].emote_set.emotes.Select(e => e.name).ToList();
        }
    }
}
