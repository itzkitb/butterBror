using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace butterBror.Utils
{
    public class NoPaste
    {
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
