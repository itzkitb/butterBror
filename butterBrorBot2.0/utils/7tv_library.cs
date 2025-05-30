using butterBror.Utils;
using Microsoft.Extensions.Caching.Memory;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace butterBror.BotUtils
{
    public class SevenTvService
    {
        private readonly HttpClient client;
        private readonly MemoryCache cache = new MemoryCache(new MemoryCacheOptions());
        private readonly MemoryCacheEntryOptions cache_options = new()
        {
            SlidingExpiration = TimeSpan.FromMinutes(30)
        };
        public SevenTvService(HttpClient httpClient)
        {
            Engine.Statistics.functions_used.Add();
            client = httpClient;
            client.BaseAddress = new Uri("https://7tv.io/v4/gql");
        }

        public async Task<string> SearchUser(string nickname, string bearer_token)
        {
            Engine.Statistics.functions_used.Add();
            if (cache.TryGetValue<string>($"user_{nickname}", out var cached) && cache is not null)
            {
                Utils.Console.WriteLine($"[7tv] Getted cache data for @{nickname}", "info");
                return cached;
            }

            var result = await PerformSearchUser(nickname);

            if (!string.IsNullOrEmpty(result))
                cache.Set($"user_{nickname}", result, cache_options);
            Utils.Console.WriteLine($"[7tv] Loaded data for @{nickname} ({result is null}): {TextUtil.CheckNull(result)}", "info");
            return result;
        }

        public async Task<string> SearchEmote(string emoteName, string bearer_token)
        {
            Engine.Statistics.functions_used.Add();
            if (cache.TryGetValue<string>($"emote_{emoteName}", out var cached))
            {
                Utils.Console.WriteLine($"[7tv] Getted cache emote {emoteName}", "info");
                return cached;
            }

            var result = await PerformSearchEmote(emoteName);

            if (!string.IsNullOrEmpty(result))
                cache.Set($"emote_{emoteName}", result, cache_options);
            Utils.Console.WriteLine($"[7tv] Loaded emote {emoteName} ({result is null})", "info");
            return result;
        }

        public async Task<bool> Add(string set_id, string emote_name, string emote_id, string bearer_token)
        {
            Engine.Statistics.functions_used.Add();
            var request = new
            {
                operationName = "AddEmoteToSet",
                query = @"mutation AddEmoteToSet($setId: Id!, $emote: EmoteSetEmoteId!) {
                        emoteSets {
                            emoteSet(id: $setId) {
                                addEmote(id: $emote) {
                                    id
                                    __typename
                                }
                                __typename
                            }
                            __typename
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

        public async Task<bool> Remove(string set_id, string emote_id, string bearer_token)
        {
            Engine.Statistics.functions_used.Add();
            var request = new
            {
                operationName = "RemoveEmoteFromSet",
                query = @"mutation RemoveEmoteFromSet($setId: Id!, $emote: EmoteSetEmoteId!) {
                        emoteSets {
                            emoteSet(id: $setId) {
                                removeEmote(id: $emote) {
                                    id
                                    __typename
                                }
                                __typename
                            }
                            __typename
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

        public async Task<bool> Rename(string set_id, string new_name, string emote_id, string bearer_token)
        {
            Engine.Statistics.functions_used.Add();
            var request = new
            {
                operationName = "RenameEmoteInSet",
                query = @"mutation RenameEmoteInSet($setId: Id!, $emote: EmoteSetEmoteId!, $alias: String!) {
                        emoteSets {
                            emoteSet(id: $setId) {
                                updateEmoteAlias(id: $emote, alias: $alias) {
                                    id
                                    __typename
                                }
                                __typename
                            }
                            __typename
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

        private async Task<bool> SendRequestAsync(object requestData, string bearerToken)
        {
            Engine.Statistics.functions_used.Add();
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
                var response = await client.SendAsync(request);
                System.Console.WriteLine(Encoding.UTF8.GetString(response.Content.ReadAsByteArrayAsync().Result));
                response.EnsureSuccessStatusCode();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<string> PerformSearchUser(string nickname)
        {
            Engine.Statistics.functions_used.Add();
            var userId = Names.GetUserID(nickname, Platforms.Twitch);
            var requestUrl = "https://7tv.io/v3/gql";

            var requestBody = new
            {
                operationName = "SearchUsers",
                variables = new { query = userId },
                query = "query SearchUsers($query: String!) {\n  users(query: $query) {\n    id\n    username\n    display_name\n    roles\n    style {\n      color\n      __typename\n    }\n    avatar_url\n    __typename\n  }\n}"
            };

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, requestUrl)
            {
                Content = new StringContent(JsonSerializer.Serialize(new[] { requestBody }), Encoding.UTF8, "application/json")
            };

            try
            {
                var response = await client.SendAsync(httpRequest);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                //File.WriteAllText("C:/Users/siste/Desktop/lol.txt", responseContent); Test thing 0_0

                var userResponse = JsonSerializer.Deserialize<List<UserResponse>>(responseContent);
                var firstUser = userResponse?.FirstOrDefault()?.Data?.Users.FirstOrDefault();

                Utils.Console.WriteLine($"[7tv] PerformSearchUser> USER ID ({userId}): {TextUtil.CheckNull(firstUser?.Id)}", "info");
                return firstUser?.Id;
            }
            catch (Exception ex)
            {
                Utils.Console.WriteError(ex, $"EmoteUtils/PerformSearchUser #{nickname}");
                return null;
            }
        }


        public async Task<string> PerformSearchEmote(string emoteName)
        {
            Engine.Statistics.functions_used.Add();
            var request = new
            {
                operationName = "SearchEmotes",
                variables = new
                {
                    query = emoteName,
                    limit = 24,
                    page = 1,
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
                query = @"query SearchEmotes($query: String!, $page: Int, $sort: Sort, $limit: Int, $filter: EmoteSearchFilter) {
            emotes(query: $query, page: $page, sort: $sort, limit: $limit, filter: $filter) {
                count
                max_page
                items {
                    id
                    name
                    state
                    trending
                    owner {
                        id
                        username
                        display_name
                        style {
                            color
                            paint_id
                            __typename
                        }
                        __typename
                    }
                    flags
                    host {
                        url
                        files {
                            name
                            format
                            width
                            height
                            __typename
                        }
                        __typename
                    }
                    __typename
                }
                __typename
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

                var response = await client.SendAsync(httpRequest);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<SearchResponse>(responseContent, jsonOptions);

                var emote = result?.Data?.Emotes?.Items.FirstOrDefault(e => e.Name.Equals(emoteName, StringComparison.OrdinalIgnoreCase));

                return emote?.Id;
            }
            catch (Exception ex)
            {
                Utils.Console.WriteError(ex, $"EmoteUtils/PerformSearchEmote#{emoteName}");
                return null;
            }
        }

        public class SearchResponse
        {
            [JsonPropertyName("data")]
            public Data Data { get; set; }
        }

        public class Data
        {
            [JsonPropertyName("emotes")]
            public Emotes Emotes { get; set; }
        }

        public class Emotes
        {
            [JsonPropertyName("count")]
            public int Count { get; set; }
            [JsonPropertyName("max_page")]
            public int MaxPage { get; set; }
            [JsonPropertyName("items")]
            public List<EmoteItem> Items { get; set; }
        }

        public class EmoteItem
        {
            [JsonPropertyName("id")]
            public string Id { get; set; }
            [JsonPropertyName("name")]
            public string Name { get; set; }
            [JsonPropertyName("state")]
            public List<string> State { get; set; }
            [JsonPropertyName("trending")]
            public dynamic? Trending { get; set; }
            [JsonPropertyName("owner")]
            public Owner Owner { get; set; }
            [JsonPropertyName("flags")]
            public int Flags { get; set; }
            [JsonPropertyName("host")]
            public Host Host { get; set; }
        }

        public class Owner
        {
            [JsonPropertyName("id")]
            public string Id { get; set; }
            [JsonPropertyName("username")]
            public string Username { get; set; }
            [JsonPropertyName("display_name")]
            public string DisplayName { get; set; }
            [JsonPropertyName("style")]
            public Style Style { get; set; }
        }

        public class Style
        {
            [JsonPropertyName("color")]
            public long Color { get; set; }
            [JsonPropertyName("paint_id")]
            public string PaintId { get; set; }
        }

        public class Host
        {
            [JsonPropertyName("url")]
            public string Url { get; set; }
            [JsonPropertyName("files")]
            public List<File> Files { get; set; }
        }

        public class File
        {
            [JsonPropertyName("name")]
            public string Name { get; set; }
            [JsonPropertyName("format")]
            public string Format { get; set; }
            [JsonPropertyName("width")]
            public int Width { get; set; }
            [JsonPropertyName("height")]
            public int Height { get; set; }
        }

        public class UserResponse
        {
            [JsonPropertyName("data")]
            public UserData Data { get; set; }
        }

        public class UserData
        {
            [JsonPropertyName("users")]
            public List<User> Users { get; set; }
        }

        public class User
        {
            [JsonPropertyName("id")]
            public string Id { get; set; }

            [JsonPropertyName("username")]
            public string Username { get; set; }

            [JsonPropertyName("display_name")]
            public string DisplayName { get; set; }

            [JsonPropertyName("roles")]
            public List<string> Roles { get; set; }

            [JsonPropertyName("style")]
            public UserStyle Style { get; set; }

            [JsonPropertyName("avatar_url")]
            public string AvatarUrl { get; set; }
        }

        public class UserStyle
        {
            [JsonPropertyName("color")]
            public int Color { get; set; }

            [JsonPropertyName("__typename")]
            public string TypeName { get; set; }
        }
    }
}
