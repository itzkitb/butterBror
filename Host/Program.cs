using Pastel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace hostBror
{
    public class Host
    {
        private static Process _botProcess;
        private static int _restartCount = 0;
        private static string _version = "2.0.4";
        private static string _repo = "itzkitb/butterBror";
        private static string _branch = "master";

        public static bool IsElevated
        {
            get
            {
                return new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        public static void Main(string[] args)
        {
            Console.Title = $"hostBror | v.{_version}";
            Console.WriteLine($"{"┌".Pastel("#ff7b42")} Starting hostBror v.{_version}...");

            if (!IsElevated)
            {
                Console.WriteLine($"{"│".Pastel("#ff7b42")} {GetTime()} Host requires administrator privileges to run!");
                Console.WriteLine($"{"│".Pastel("#ff7b42")} {GetTime()} Restarting as administrator in 3 seconds...");

                Thread.Sleep(3000);

                try
                {
                    string exePath = Process.GetCurrentProcess().MainModule.FileName;

                    var startInfo = new ProcessStartInfo
                    {
                        FileName = exePath,
                        UseShellExecute = true,
                        Verb = "runas",
                        WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory
                    };

                    Process.Start(startInfo);
                    Environment.Exit(0);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{"│".Pastel("#ff4f4f")} {GetTime()} Failed to restart as administrator: {ex.Message}");
                    Console.WriteLine($"{"│".Pastel("#ff4f4f")} {GetTime()} Please restart the program manually with administrator privileges.");

                    Thread.Sleep(5000);
                    Environment.Exit(1);
                }

                return;
            }

            foreach (var arg in args)
            {
                if (arg.StartsWith("--repo="))
                    _repo = arg.Split('=')[1];
                else if (arg.StartsWith("--branch="))
                    _branch = arg.Split('=')[1];
            }

            string currentXmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Current.xml");
            if (!File.Exists(currentXmlPath))
            {
                Console.WriteLine($"{"│".Pastel("#ff7b42")} {GetTime()} Current.xml not found. Compiling from repo...");
                CompileFromRepo();
            }

            StartBotMonitor();
        }

        #region Bot monitor
        private static void StartBotMonitor()
        {
            while (true)
            {
                StartBotProcess();
                _botProcess.WaitForExit();

                if (_botProcess?.ExitCode == 5051)
                {
                    Console.WriteLine($"{"│".Pastel("#ff7b42")} {GetTime()} Bot exited with code 5051. Updating...");
                    CompileFromRepo();
                    StartBotProcess();
                }
                else if (_botProcess?.ExitCode == 5001)
                {
                    Console.WriteLine($"{"│".Pastel("#ff7b42")} {GetTime()} Bot exited with code 5001. Exiting...");
                    Environment.Exit(0);
                }
                else
                {
                    _restartCount++;
                    Console.WriteLine($"{"│".Pastel("#ff4f4f")} {GetTime()} Bot process exited with code {_botProcess?.ExitCode ?? -1}. Restarting... (Attempt {_restartCount})");
                    Thread.Sleep(5000);
                }
            }
        }

        private static void StartBotProcess()
        {
            try
            {
                string currentXmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Current.xml");
                if (!File.Exists(currentXmlPath))
                {
                    Console.WriteLine($"{"│".Pastel("#ff4f4f")} {GetTime()} Current.xml not found. Cannot start bot.");
                    return;
                }

                XDocument doc = XDocument.Load(currentXmlPath);
                string branch = doc.Root.Element("branch")?.Value;
                string commitId = doc.Root.Element("commit")?.Value;

                if (string.IsNullOrEmpty(branch) || string.IsNullOrEmpty(commitId))
                {
                    Console.WriteLine($"{"│".Pastel("#ff4f4f")} {GetTime()} Invalid Current.xml. Cannot start bot.");
                    return;
                }

                string botPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Versions", branch, commitId, "butterBror.exe");
                if (!File.Exists(botPath))
                {
                    Console.WriteLine($"{"│".Pastel("#ff4f4f")} {GetTime()} Bot executable not found at {botPath}. Cannot start.");
                    return;
                }

                if (_botProcess != null && !_botProcess.HasExited)
                {
                    _botProcess.Kill();
                    _botProcess.WaitForExit();
                }

                _botProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = botPath,
                        Arguments = $"--core-version {_version} --core-name hostBror",
                        RedirectStandardOutput = false,
                        UseShellExecute = true,
                        CreateNoWindow = false
                    }
                };

                _botProcess.Start();
                Console.WriteLine($"{"│".Pastel("#ff7b42")} {GetTime()} Bot process started.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{"│".Pastel("#ff4f4f")} {GetTime()} Error starting bot process: {ex.Message}");
                Thread.Sleep(1000);
            }
        }

        private static string GetTime()
        {
            return DateTime.Now.ToString("dd.MM HH:mm.ss").Pastel("#696969");
        }
        #endregion

        #region Compilation
        private static void CompileFromRepo()
        {
            try
            {
                string commitId = GetCommitId(_repo, _branch);
                if (string.IsNullOrEmpty(commitId))
                {
                    Console.WriteLine($"{"│".Pastel("#ff4f4f")} {GetTime()} Failed to get commit ID for {_branch} in {_repo}");
                    return;
                }

                string zipUrl = $"https://github.com/{_repo}/archive/refs/heads/{_branch}.zip";
                string tempDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Temp");
                string zipPath = Path.Combine(tempDir, "source.zip");

                if (Directory.Exists(tempDir))
                {
                    Console.WriteLine($"{"│".Pastel("#ff7b42")} {GetTime()} Deleting old temp dir...");
                    Directory.Delete(tempDir, true);
                }

                Directory.CreateDirectory(tempDir);

                DownloadFile(zipUrl, zipPath);
                ExtractZip(zipPath, tempDir);

                string sourceDir = FindSourceDirectory(tempDir);
                if (string.IsNullOrEmpty(sourceDir))
                {
                    Console.WriteLine($"{"│".Pastel("#ff4f4f")} {GetTime()} Source directory not found");
                    return;
                }

                if (!RunDotNetBuild(sourceDir))
                {
                    Console.WriteLine($"{"│".Pastel("#ff4f4f")} {GetTime()} Build failed");
                    return;
                }

                string outputDir = Path.Combine(sourceDir, "Bot", "bin", "Release", "net8.0");
                string targetDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Versions", _branch, commitId);

                Directory.CreateDirectory(targetDir);

                var files = Directory.GetFiles(outputDir, "*.*", SearchOption.AllDirectories);

                foreach (var file in files)
                {
                    string relativePath = file.Substring(outputDir.Length + 1);
                    string destinationFilePath = Path.Combine(targetDir, relativePath);

                    Directory.CreateDirectory(Path.GetDirectoryName(destinationFilePath));

                    File.Copy(file, destinationFilePath, true);
                }

                string releaseXmlPath = Path.Combine(targetDir, "release.xml");
                string currentXmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Current.xml");
                XDocument releaseDoc = new XDocument(
                    new XElement("release",
                        new XElement("branch", _branch),
                        new XElement("commit", commitId)
                    )
                );
                releaseDoc.Save(releaseXmlPath);
                releaseDoc.Save(currentXmlPath);

                CreateCurrentXml(_branch, commitId);
                Console.WriteLine($"{"│".Pastel("#ff7b42")} {GetTime()} Deleting temp dir...");
                Directory.Delete(tempDir, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{"│".Pastel("#ff4f4f")} {GetTime()} Compilation failed: {ex.Message}");
            }
        }

        private static string GetCommitId(string repo, string branch)
        {
            string owner = repo.Split('/')[0];
            string repoName = repo.Split('/')[1];

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("hostBror");
                string url = $"https://api.github.com/repos/{owner}/{repoName}/branches/{branch}";
                try
                {
                    var response = client.GetAsync(url).Result;
                    response.EnsureSuccessStatusCode();
                    string json = response.Content.ReadAsStringAsync().Result;
                    var doc = JsonDocument.Parse(json);
                    var commit = doc.RootElement.GetProperty("commit");
                    return commit.GetProperty("sha").GetString().Substring(0, 7);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{"│".Pastel("#ff4f4f")} {GetTime()} Error getting commit ID: {ex.Message}");
                }
            }
            return null;
        }

        private static void DownloadFile(string url, string outputPath)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("hostBror");
                var response = client.GetAsync(url).Result;
                response.EnsureSuccessStatusCode();
                var content = response.Content.ReadAsByteArrayAsync().Result;
                File.WriteAllBytes(outputPath, content);
            }
        }

        private static void ExtractZip(string zipPath, string extractPath)
        {
            try
            {
                ZipFile.ExtractToDirectory(zipPath, extractPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{"│".Pastel("#ff4f4f")} {GetTime()} Error extracting ZIP: {ex.Message}");
            }
        }

        private static string FindSourceDirectory(string tempDir)
        {
            string[] dirs = Directory.GetDirectories(tempDir);
            foreach (string dir in dirs)
            {
                if (Directory.GetFiles(dir, "*.sln").Length > 0 || Directory.GetFiles(dir, "*.csproj").Length > 0)
                {
                    return dir;
                }
            }
            return null;
        }

        private static bool RunDotNetBuild(string sourceDir)
        {
            try
            {
                Process process = new Process();
                process.StartInfo.FileName = "dotnet";
                process.StartInfo.Arguments = $"build \"{sourceDir}\" --property:WarningLevel=0 --configuration Release";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.StandardOutputEncoding = Encoding.UTF8;

                process.Start();

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                Console.WriteLine($"{"│".Pastel("#ff7b42")} {GetTime()} Build output:");
                Console.WriteLine(output);
                if (!string.IsNullOrEmpty(error))
                {
                    Console.WriteLine($"{"│".Pastel("#ff4f4f")} {GetTime()} Build errors:");
                    Console.WriteLine(error);
                }

                return process.ExitCode == 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{"│".Pastel("#ff4f4f")} {GetTime()} Error running dotnet build: {ex.Message}");
                return false;
            }
        }

        private static void CreateCurrentXml(string branch, string commitId)
        {
            string currentXmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Current.xml");
            XDocument doc = new XDocument(
                new XElement("current",
                    new XElement("branch", branch),
                    new XElement("commit", commitId)
                )
            );
            doc.Save(currentXmlPath);
        }
        #endregion
    }
}