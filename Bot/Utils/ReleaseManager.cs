using Pastel;
using System;
using System.IO;
using System.Xml.Linq;

namespace bb.Utils
{
    public class ReleaseManager
    {
        public static ReleaseInfo GetReleaseInfo()
        {
            try
            {
                string releaseXmlPath = "release.xml";

                if (!File.Exists(releaseXmlPath))
                {
                    Core.Bot.Logger.Write("release.xml not found");
                    return null;
                }

                XDocument doc = XDocument.Load(releaseXmlPath);
                XElement releaseElement = doc.Root;

                if (releaseElement == null)
                    return null;

                string branch = releaseElement.Element("branch")?.Value;
                string commit = releaseElement.Element("commit")?.Value;

                return new ReleaseInfo { Branch = branch, Commit = commit };
            }
            catch (Exception ex)
            {
                Core.Bot.Logger.Write(ex);
                return null;
            }
        }
    }

    public class ReleaseInfo
    {
        public string Branch { get; set; }
        public string Commit { get; set; }
    }
}
