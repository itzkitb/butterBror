using Pastel;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace hostBror
{
    public class Host
    {
        private static Process _botProcess;
        private static bool _isRestarting = false;
        private static int _restartCount = 0;
        private static string _version = "2.0.2";
        private static readonly string _botExecutablePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Process", "butterBror.exe");

        private static Timer _updateTimer;
        private static bool _updateRequired = false;
        private static bool _isCheckingForUpdate = false;
        private static readonly string _updateZipPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Updater", "HostUpdate.zip");
        private static string _botVersion = GetBotVersion();

        public static void Main(string[] args)
        {
            Console.Title = $"hostBror | v.{_version}";
            Console.WriteLine($"{"┌".Pastel("#ff7b42")} Starting hostBror v.{_version}...");

            _updateTimer = new Timer(CheckForUpdates, null, TimeSpan.FromMinutes(1), TimeSpan.FromHours(1));

            StartBotMonitor();

            if (_updateRequired)
            {
                StartUpdate();
            }
        }

        #region Bot monitor
        private static void StartBotMonitor()
        {
            while (!_updateRequired)
            {
                if (_isRestarting)
                {
                    Console.WriteLine($"{"│".Pastel("#ff7b42")} {GetTime()} Bot is restarting...");
                }

                StartBotProcess();
                _botProcess.WaitForExit();

                if (_botProcess == null || _botProcess.HasExited)
                {
                    if (!_isRestarting)
                    {
                        _restartCount++;
                        Console.WriteLine($"{"│".Pastel("#ff4f4f")} {GetTime()} Bot process exited with code {_botProcess?.ExitCode ?? -1}. Restarting... (Attempt {_restartCount})");
                        _isRestarting = true;
                        Thread.Sleep(5000);
                    }
                    else
                    {
                        _isRestarting = false;
                    }
                }
            }
        }

        private static void StartBotProcess()
        {
            try
            {
                if (_botProcess != null && !_botProcess.HasExited)
                {
                    _botProcess.Kill();
                    _botProcess.WaitForExit();
                }

                _botProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = _botExecutablePath,
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
        #region Update
        private static string GetBotVersion()
        {
            try
            {
                string versionFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Process", "version.json");
                if (File.Exists(versionFilePath))
                {
                    var json = File.ReadAllText(versionFilePath);
                    var doc = JsonDocument.Parse(json);
                    return doc.RootElement.GetProperty("Version").GetString();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{"│".Pastel("#ff4f4f")} {GetTime()} Error reading version.json: {ex.Message}");
            }
            return "0.0.0";
        }

        private static async void CheckForUpdates(object state)
        {
            if (_updateRequired || _isCheckingForUpdate)
                return;

            _isCheckingForUpdate = true;

            try
            {
                var latestRelease = await GetLatestReleaseInfo();
                if (latestRelease != null && IsNewVersionAvailable(latestRelease.Version))
                {
                    Console.WriteLine($"{"│".Pastel("#ff4f4f")} {GetTime()} New version available: {latestRelease.Version}. Current: {_version}. Downloading update...");

                    if (File.Exists(_updateZipPath))
                        File.Delete(_updateZipPath);

                    await DownloadFileAsync(latestRelease.ZipUrl, _updateZipPath);

                    if (File.Exists(_updateZipPath))
                    {
                        _updateRequired = true;
                        if (_botProcess != null && !_botProcess.HasExited)
                        {
                            _botProcess.Kill();
                            _botProcess.WaitForExit();
                        }

                        _updateTimer.Change(Timeout.Infinite, Timeout.Infinite);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{"│".Pastel("#ff4f4f")} {GetTime()} Error checking for updates: {ex.Message}");
            }
            finally
            {
                _isCheckingForUpdate = false;
            }
        }

        private static async Task<ReleaseInfo> GetLatestReleaseInfo()
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("hostBror-Updater");
                var response = await client.GetAsync("https://api.github.com/repos/itzkitb/butterBror/releases/latest ");
                response.EnsureSuccessStatusCode();

                var jsonContent = await response.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(jsonContent);

                var tagName = doc.RootElement.GetProperty("tag_name").GetString().TrimStart('v').TrimStart('.');

                var assets = doc.RootElement.GetProperty("assets");
                foreach (var asset in assets.EnumerateArray())
                {
                    var name = asset.GetProperty("name").GetString();
                    var url = asset.GetProperty("browser_download_url").GetString();

                    if (name.EndsWith(".zip"))
                    {
                        return new ReleaseInfo { Version = tagName, ZipUrl = url };
                    }
                }
            }
            return null;
        }

        private static bool IsNewVersionAvailable(string latestVersion)
        {
            try
            {
                var currentVer = new Version(_botVersion);
                var latestVer = new Version(latestVersion);
                return latestVer > currentVer;
            }
            catch
            {
                return false;
            }
        }

        private static async Task DownloadFileAsync(string url, string outputPath)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("hostBror-Updater");
                var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                using (var contentStream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = File.Create(outputPath))
                {
                    await contentStream.CopyToAsync(fileStream);
                }
            }
        }

        private static void StartUpdate()
        {
            string updaterPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Updater", "Updater.exe");
            string hostExePath = Process.GetCurrentProcess().MainModule.FileName;

            if (!File.Exists(updaterPath))
            {
                Console.WriteLine($"{"│".Pastel("#ff4f4f")} {GetTime()} Updater.exe not found. Cannot proceed with update.");
                return;
            }

            try
            {
                Process.Start(updaterPath, $"\"{_updateZipPath}\" \"{hostExePath}\"");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{"│".Pastel("#ff4f4f")} {GetTime()} Failed to start Updater.exe: {ex.Message}");
            }

            Console.WriteLine($"{"│".Pastel("#ff4f4f")} {GetTime()} Shutting down for update...");
            Environment.Exit(0);
        }

        private class ReleaseInfo
        {
            public string Version { get; set; }
            public string ZipUrl { get; set; }
        }
        #endregion
    }
}
