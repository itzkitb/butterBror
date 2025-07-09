using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Pastel;

namespace hostBror
{
    public class Host
    {
        private static Process _botProcess;
        private static bool _isRestarting = false;
        private static int _restartCount = 0;
        private static string _version = "2.0.0";
        private static readonly string _botExecutablePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Process", "butterBror.exe");

        public static void Main(string[] args)
        {
            Console.Title = $"hostBror | v.{_version}";
            Console.WriteLine($"{"┌".Pastel("#ff7b42")} Starting hostBror v.{_version}...");
            StartBotMonitor();
        }

        private static void StartBotMonitor()
        {
            while (true)
            {
                if (_isRestarting)
                {
                    Console.WriteLine($"{"│".Pastel("#ff7b42")} Bot is restarting...");
                }

                StartBotProcess();

                _botProcess.WaitForExit();

                if (!_isRestarting)
                {
                    _restartCount++;
                    Console.WriteLine($"{"│".Pastel("#ff4f4f")} Bot process exited with code {_botProcess.ExitCode}. Restarting... (Attempt {_restartCount})");
                    _isRestarting = true;
                    Thread.Sleep(5000);
                }
                else
                {
                    _isRestarting = false;
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
                Console.WriteLine($"{"│".Pastel("#ff7b42")} Bot process started.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{"│".Pastel("#ff4f4f")} Error starting bot process: {ex.Message}");
                Thread.Sleep(1000);
            }
        }
    }
}
