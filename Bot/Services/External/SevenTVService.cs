using bb.Core.Bot;
using bb.Models;
using bb.Models.SevenTVLib;
using bb.Utils;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using static bb.Core.Bot.Console;

namespace bb.Services.External
{
    /// <summary>
    /// Provides interaction with 7TV API for user and emote operations with caching capabilities.
    /// </summary>
    public class SevenTvService
    {
        private readonly HttpClient _client;

        /// <summary>
        /// Initializes a new instance of the SevenTvService class with HTTP client.
        /// </summary>
        /// <param name="httpClient">Pre-configured HttpClient for API requests.</param>
        /// <remarks>
        /// Sets up base address to 7TV's GraphQL endpoint and configures caching policies.
        /// Uses in-memory caching with 30-minute sliding expiration for both users and emotes.
        /// </remarks>
        public SevenTvService(HttpClient httpClient)
        {
            _client = httpClient;
            _client.BaseAddress = new Uri($"https://{URLs.seventvAPI}/v4/gql");
        }

        /// <summary>
        /// Searches for a 7TV user by nickname with caching.
        /// </summary>
        /// <param name="nickname">The username to search for.</param>
        /// <param name="bearer_token">Authentication token for API access.</param>
        /// <returns>User ID string if found, null otherwise.</returns>
        /// <remarks>
        /// - First checks memory cache for existing results
        /// - Performs fresh search if cache is empty or expired
        /// - Stores successful results in cache for future requests
        /// </remarks>

        public async Task<string> SearchUser(string nickname, string bearer_token)
        {
            /*if (_cache.TryGetValue<string>($"user_{nickname}", out var cached) && _cache is not null)
            {
                Write($"SevenTV - Getted cache data for @{nickname}", "info");
                return cached;
            }*/

            var result = await PerformSearchUser(nickname);

            /*if (!string.IsNullOrEmpty(result))    
                _cache.Set($"user_{nickname}", result, _cacheOptions);*/
            Write($"SevenTV - Loaded data for @{nickname} ({result is null}): {TextSanitizer.CheckNull(result)}");
            return result;
        }

        /// <summary>
        /// Searches for a 7TV emote by name with caching.
        /// </summary>
        /// <param name="emoteName">The emote name to search for.</param>
        /// <param name="bearer_token">Authentication token for API access.</param>
        /// <returns>Emote ID string if found, null otherwise.</returns>
        /// <remarks>
        /// - First checks memory cache for existing results
        /// - Performs fresh search if cache is empty or expired
        /// - Stores successful results in cache for future requests
        /// </remarks>

        public async Task<string> SearchEmote(string emoteName, string bearer_token)
        {
            /*if (_cache.TryGetValue<string>($"emote_{emoteName}", out var cached))
            {
                Write($"SevenTV - Getted cache data for @{emoteName}", "info");
                return cached;
            }*/

            var result = await PerformSearchEmote(emoteName);

            /*if (!string.IsNullOrEmpty(result))
                _cache.Set($"emote_{emoteName}", result, _cacheOptions);*/
            Write($"SevenTV - Loaded emote {emoteName} ({result is null})");
            return result;
        }

        /// <summary>
        /// Adds an emote to a user's emote set.
        /// </summary>
        /// <param name="set_id">Target emote set identifier.</param>
        /// <param name="emote_name">Name for the emote in the set.</param>
        /// <param name="emote_id">ID of the emote to add.</param>
        /// <param name="bearer_token">Authentication token for API access.</param>
        /// <returns>True if successfully added, false otherwise.</returns>
        /// <remarks>
        /// Uses GraphQL mutation to modify emote sets.
        /// Requires valid bearer token with edit permissions.
        /// </remarks>

        public async Task<bool> Add(string set_id, string emote_name, string emote_id, string bearer_token)
        {
            var request = new
            {
                operationName = "AddEmoteToSet",
                query = @"mutation AddEmoteToSet($setId: Id!, $emote: EmoteSetEmoteId!) {
                        emoteSets {
                            emoteSet(id: $setId) {
                                addEmote(id: $emote) {
                                    id
                                }
                            }
                        }
                    }",
                variables = new
                {
                    setId = set_id,
                    emote = new
                    {
                        alias = emote_name,
                        emoteId = emote_id
                    }
                }
            };

            return await SendRequestAsync(request, bearer_token);
        }

        /// <summary>
        /// Removes an emote from a user's emote set.
        /// </summary>
        /// <param name="set_id">Target emote set identifier.</param>
        /// <param name="emote_id">ID of the emote to remove.</param>
        /// <param name="bearer_token">Authentication token for API access.</param>
        /// <returns>True if successfully removed, false otherwise.</returns>
        /// <remarks>
        /// Uses GraphQL mutation to modify emote sets.
        /// Requires valid bearer token with edit permissions.
        /// </remarks>

        public async Task<bool> Remove(string set_id, string emote_id, string bearer_token)
        {
            var request = new
            {
                operationName = "RemoveEmoteFromSet",
                query = @"mutation RemoveEmoteFromSet($setId: Id!, $emote: EmoteSetEmoteId!) {
                        emoteSets {
                            emoteSet(id: $setId) {
                                removeEmote(id: $emote) {
                                    id
                                }
                            }
                        }
                    }",
                variables = new
                {
                    setId = set_id,
                    emote = new
                    {
                        emoteId = emote_id
                    }
                }
            };

            return await SendRequestAsync(request, bearer_token);
        }

        /// <summary>
        /// Renames an existing emote in a user's emote set.
        /// </summary>
        /// <param name="set_id">Target emote set identifier.</param>
        /// <param name="new_name">New name for the emote.</param>
        /// <param name="emote_id">ID of the emote to rename.</param>
        /// <param name="bearer_token">Authentication token for API access.</param>
        /// <returns>True if successfully renamed, false otherwise.</returns>
        /// <remarks>
        /// Uses GraphQL mutation to update emote alias.
        /// Requires valid bearer token with edit permissions.
        /// </remarks>

        public async Task<bool> Rename(string set_id, string new_name, string emote_id, string bearer_token)
        {
            var request = new
            {
                operationName = "RenameEmoteInSet",
                query = @"mutation RenameEmoteInSet($setId: Id!, $emote: EmoteSetEmoteId!, $alias: String!) {
                        emoteSets {
                            emoteSet(id: $setId) {
                                updateEmoteAlias(id: $emote, alias: $alias) {
                                    id
                                }
                            }
                        }
                    }",
                variables = new
                {
                    setId = set_id,
                    alias = new_name,
                    emote = new
                    {
                        emoteId = emote_id
                    }
                }
            };

            return await SendRequestAsync(request, bearer_token);
        }

        /// <summary>
        /// Sends a GraphQL request to 7TV API.
        /// </summary>
        /// <param name="requestData">GraphQL operation data.</param>
        /// <param name="bearerToken">Authentication token for API access.</param>
        /// <returns>True if request succeeded, false if failed.</returns>
        /// <remarks>
        /// Handles HTTP POST requests with Bearer authentication.
        /// Returns false for non-success status codes.
        /// </remarks>

        private async Task<bool> SendRequestAsync(object requestData, string bearerToken)
        {
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                IgnoreNullValues = true
            };

            var content = new StringContent(
                JsonSerializer.Serialize(requestData, jsonOptions),
                Encoding.UTF8,
                "application/json");

            using var request = new HttpRequestMessage(HttpMethod.Post, "");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
            request.Content = content;

            try
            {
                var response = await _client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Performs actual user search operation against 7TV API.
        /// </summary>
        /// <param name="nickname">The Twitch username to find on 7TV.</param>
        /// <returns>User response data or null if not found.</returns>
        /// <remarks>
        /// - Converts Twitch username to 7TV user ID
        /// - Uses 7TV's user search GraphQL endpoint
        /// - Returns first matching user ID from results
        /// </remarks>

        public async Task<string> PerformSearchUser(string nickname)
        {
            var userId = UsernameResolver.GetUserID(nickname, PlatformsEnum.Twitch);
            var requestUrl = "https://7tv.io/v3/gql";

            var requestBody = new
            {
                operationName = "SearchUsers",
                variables = new { query = userId },
                query = @"query SearchUsers($query: String!) {
  users(query: $query, limit: 1) {
    id
    username
  }
}"
            };

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, requestUrl)
            {
                Content = new StringContent(JsonSerializer.Serialize(new[] { requestBody }), Encoding.UTF8, "application/json")
            };

            try
            {
                var response = await _client.SendAsync(httpRequest);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();

                var userResponse = JsonSerializer.Deserialize<List<UserResponse>>(responseContent);
                var firstUser = userResponse?.FirstOrDefault()?.Data?.Users.FirstOrDefault();

                Write($"SevenTV - PerformSearchUser> USER ID ({userId}): {TextSanitizer.CheckNull(firstUser?.Id)}");
                return firstUser?.Id;
            }
            catch (Exception ex)
            {
                Write(ex);
                return null;
            }
        }

        /// <summary>
        /// Performs actual emote search operation against 7TV API.
        /// </summary>
        /// <param name="emoteName">The emote name to search for.</param>
        /// <returns>Emote response data or null if not found.</returns>
        /// <remarks>
        /// - Searches with exact match filter enabled
        /// - Sorts results by popularity descending
        /// - Returns first matching emote ID from results
        /// - Supports various search filters through GraphQL parameters
        /// </remarks>

        public async Task<string> PerformSearchEmote(string emoteName)
        {
            var request = new
            {
                operationName = "SearchEmotes",
                variables = new
                {
                    query = emoteName,
                    sort = new
                    {
                        value = "popularity",
                        order = "DESCENDING"
                    },
                    filter = new
                    {
                        category = "TOP",
                        exact_match = true,
                        ignore_tags = false,
                        zero_width = false,
                        animated = false,
                        aspect_ratio = ""
                    }
                },
                query = @"query SearchEmotes($query: String!, $sort: Sort, $filter: EmoteSearchFilter) {
	emotes(query: $query, sort: $sort, limit: 1, filter: $filter) {
		items {
			id
			name
		}
	}
}"
            };

            try
            {
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    IgnoreNullValues = true
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(request, jsonOptions),
                    Encoding.UTF8,
                    "application/json");

                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://7tv.io/v3/gql");
                //httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer_token);
                httpRequest.Content = content;

                var response = await _client.SendAsync(httpRequest);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<SearchResponse>(responseContent, jsonOptions);

                var emote = result?.Data?.Emotes?.Items.FirstOrDefault(e => e.Name.Equals(emoteName, StringComparison.OrdinalIgnoreCase));

                return emote?.Id;
            }
            catch (Exception ex)
            {
                Write(ex);
                return null;
            }
        }
    }
}
