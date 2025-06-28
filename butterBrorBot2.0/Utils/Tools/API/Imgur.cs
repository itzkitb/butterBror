using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using static butterBror.Utils.Things.Console;

namespace butterBror.Utils.Tools.API
{
    public class Imgur
    {
        [ConsoleSector("butterBror.Utils.Tools.API.Imgur", "DownloadAsync")]
        public static async Task<byte[]> DownloadAsync(string imageUrl)
        {
            Core.Statistics.FunctionsUsed.Add();
            using HttpClient client = new HttpClient();
            return await client.GetByteArrayAsync(imageUrl);
        }

        [ConsoleSector("butterBror.Utils.Tools.API.Imgur", "UploadAsync")]
        public static async Task<string> UploadAsync(byte[] imageBytes, string description, string title, string ImgurClientId, string ImgurUploadUrl)
        {
            Core.Statistics.FunctionsUsed.Add();
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

        [ConsoleSector("butterBror.Utils.Tools.API.Imgur", "GetLinkFromResponse")]
        public static string GetLinkFromResponse(string response)
        {
            Core.Statistics.FunctionsUsed.Add();
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
