using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using static butterBror.Core.Bot.Console;

namespace butterBror.Services.External
{
    /// <summary>
    /// Provides functionality for interacting with the Imgur API to download and upload images.
    /// </summary>
    public class ImgurService
    {
        /// <summary>
        /// Asynchronously downloads an image from the specified URL.
        /// </summary>
        /// <param name="imageUrl">The URL of the image to download.</param>
        /// <returns>A byte array representing the downloaded image.</returns>
        /// <exception cref="HttpRequestException">Thrown when the HTTP request fails.</exception>
        
        public static async Task<byte[]> DownloadAsync(string imageUrl)
        {
            Engine.Statistics.FunctionsUsed.Add();
            using HttpClient client = new HttpClient();
            return await client.GetByteArrayAsync(imageUrl);
        }

        /// <summary>
        /// Uploads an image to Imgur with specified metadata and credentials.
        /// </summary>
        /// <param name="imageBytes">The binary image data to upload.</param>
        /// <param name="description">A description for the uploaded image.</param>
        /// <param name="title">The title for the uploaded image.</param>
        /// <param name="ImgurClientId">Imgur API client ID for authentication.</param>
        /// <param name="ImgurUploadUrl">The endpoint URL for image uploads.</param>
        /// <returns>The API response as a JSON string, or null if upload fails.</returns>
        /// <remarks>
        /// Uses multipart/form-data encoding and requires valid Imgur credentials.
        /// </remarks>
        
        public static async Task<string> UploadAsync(byte[] imageBytes, string description, string title, string ImgurClientId, string ImgurUploadUrl)
        {
            Engine.Statistics.FunctionsUsed.Add();
            try
            {
                using HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Client-ID", ImgurClientId);

                using MultipartFormDataContent content = new();
                ByteArrayContent byteContent = new(imageBytes);
                byteContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");

                content.Add(byteContent, "image");
                content.Add(new StringContent(description), "description");
                content.Add(new StringContent(title), "title");

                HttpResponseMessage response = await client.PostAsync(ImgurUploadUrl, content);
                response.EnsureSuccessStatusCode();

                string responseString = await response.Content.ReadAsStringAsync();
                return responseString;
            }
            catch (Exception ex)
            {
                Write(ex);
                return null;
            }
        }

        /// <summary>
        /// Extracts the image link from an Imgur API JSON response.
        /// </summary>
        /// <param name="response">The JSON response string from Imgur.</param>
        /// <returns>
        /// The image URL if successful, "Upload failed." for API errors, 
        /// or null if parsing fails.
        /// </returns>
        /// <remarks>
        /// Validates the "success" flag in the response before extracting the link.
        /// </remarks>
        
        public static string GetLinkFromResponse(string response)
        {
            Engine.Statistics.FunctionsUsed.Add();
            try
            {
                JObject jsonResponse = JObject.Parse(response);
                bool success = jsonResponse["success"].Value<bool>();

                if (success)
                {
                    string link = jsonResponse["data"]["link"].Value<string>();
                    return link;
                }
                else
                    return "Upload failed.";
            }
            catch (Exception ex)
            {
                Write(ex);
                return null;
            }
        }
    }
}
