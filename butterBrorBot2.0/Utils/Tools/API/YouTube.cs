using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static butterBror.Utils.Things.Console;

namespace butterBror.Utils.Tools.API
{
    public class YouTube
    {
        [ConsoleSector("butterBror.Utils.Tools.API.YouTube", "GetPlaylistVideos")]
        public static string[] GetPlaylistVideos(string playlistUrl)
        {
            Core.Statistics.FunctionsUsed.Add();
            try
            {
                MatchCollection matches = new Regex(@"watch\?v=[a-zA-Z0-9_-]{11}").Matches(new WebClient().DownloadString(playlistUrl));

                string[] videoLinks = new string[matches.Count];
                int i = 0;

                foreach (Match match in matches)
                {
                    videoLinks[i] = "https://www.youtube.com/" + match.Value;
                    i++;
                }

                return videoLinks;
            }
            catch (Exception ex)
            {
                Write(ex);
                return null;
            }
        }
    }
}
