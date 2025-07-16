using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;

class Program
{
    static string version = "2.0.0";

    static void Main(string[] args)
    {
        Console.Title = $"updateBror | v.{version}";
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: Updater.exe <zipPath> <hostExePath>");
            Thread.Sleep(-1);
        }

        string zipPath = args[0];
        string hostExePath = args[1];

        string updaterDir = AppDomain.CurrentDomain.BaseDirectory;
        string currentDir = Path.GetDirectoryName(Path.GetDirectoryName(updaterDir));

        string updaterFolder = Path.Combine(currentDir, "Updater");
        string updaterExeName = "Updater.exe";

        WaitForProcessExit("butterBror");

        foreach (var file in Directory.GetFiles(currentDir))
        {
            string fileName = Path.GetFileName(file);
            if (!fileName.Equals(updaterExeName, StringComparison.OrdinalIgnoreCase))
            {
                TryDeleteFile(file);
            }
        }

        foreach (var dir in Directory.GetDirectories(currentDir))
        {
            if (Path.GetFullPath(dir).Equals(Path.GetFullPath(updaterFolder), StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            TryDeleteDirectory(dir);
        }

        try
        {
            ZipFile.ExtractToDirectory(zipPath, currentDir);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Extract error: {ex.Message}");
            Thread.Sleep(-1);
        }

        TryDeleteFile(zipPath);

        try
        {
            Process.Start(hostExePath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to start host: {ex.Message}");
            Thread.Sleep(-1);
        }
    }

    static void WaitForProcessExit(string processName)
    {
        foreach (var process in Process.GetProcessesByName(processName))
        {
            try
            {
                process.Kill();
                process.WaitForExit();
            }
            catch { }
        }
    }

    static void TryDeleteFile(string path)
    {
        for (int i = 0; i < 5; i++)
        {
            try
            {
                File.Delete(path);
                return;
            }
            catch
            {
                Thread.Sleep(500);
            }
        }
    }

    static void TryDeleteDirectory(string path)
    {
        for (int i = 0; i < 5; i++)
        {
            try
            {
                Directory.Delete(path, true);
                return;
            }
            catch
            {
                Thread.Sleep(500);
            }
        }
    }
}