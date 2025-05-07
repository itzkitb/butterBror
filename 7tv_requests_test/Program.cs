using System;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http.Headers;

namespace HelloWorld
{
    class Program
    {
        private static readonly HttpClient client = new HttpClient();

        static void Main(string[] args)
        {
            Console.WriteLine(PerformSearchEmote("lol", "").Result);
        }

        public static async Task<string> PerformSearchUser(string userId)
        {
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

                return responseContent;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        public static async Task<string> PerformSearchEmote(string emoteName, string bearer_token)
        {
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
                        exact_match = true, // Убедимся, что мы ищем точное совпадение
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

                return responseContent; // Если эмоут не найден, вернется null
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

    }
}