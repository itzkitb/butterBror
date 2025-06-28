using DankDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace butterBror.Utils.DataManagers
{
    public static class FileUtil
    {
        private static readonly LruCache<string, string> _fileCache = new(100);

        public static void CreateDirectory(string directoryPath)
        {
            Directory.CreateDirectory(directoryPath);
        }

        public static bool DirectoryExists(string directoryPath)
        {
            return Directory.Exists(directoryPath);
        }

        public static bool FileExists(string filePath)
        {
            return File.Exists(filePath);
        }

        public static void CreateFile(string filePath)
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !DirectoryExists(directory))
            {
                CreateDirectory(directory);
            }
            if (!FileExists(filePath))
            {
                using (File.Create(filePath)) { }
                _fileCache.AddOrUpdate(filePath, "");
            }
        }

        public static void DeleteFile(string filePath)
        {
            if (FileExists(filePath))
            {
                File.Delete(filePath);
                _fileCache.Invalidate(filePath);
            }
        }

        public static string GetFileContent(string filePath)
        {
            return _fileCache.GetOrAdd(filePath, key =>
            {
                if (FileExists(key))
                    return File.ReadAllText(key);

                throw new FileNotFoundException($"File {key} not found");
            });
        }

        public static void SaveFileContent(string filePath, string content)
        {
            CreateFile(filePath);
            CreateBackup(filePath);

            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !DirectoryExists(directory))
            {
                CreateDirectory(directory);
            }

            RetryIOAction(() =>
            {
                File.WriteAllText(filePath, content);
                _fileCache.AddOrUpdate(filePath, content);
            });
        }

        public static void ClearCache()
        {
            _fileCache.Clear();
        }

        private static bool IsPathInDirectory(string filePath, string directoryPath)
        {
            var directory = Path.GetDirectoryName(filePath);
            return !string.IsNullOrEmpty(directory) &&
                   directory.StartsWith(directoryPath, StringComparison.OrdinalIgnoreCase);
        }

        public static void CreateBackup(string filePath)
        {
            var backupPath = GetBackupPath(filePath);
            var backupDir = Path.GetDirectoryName(backupPath);

            if (!FileExists(backupPath) && FileExists(filePath))
            {
                if (!string.IsNullOrEmpty(backupDir))
                {
                    Directory.CreateDirectory(backupDir);
                    RetryIOAction(() =>
                    {
                        File.Copy(filePath, backupPath, overwrite: true);
                    });
                }
            }
        }

        public static string GetBackupPath(string originalPath)
        {
            Core.Statistics.FunctionsUsed.Add();
            var fileName = $"{Core.Bot.Pathes.Reserve}{originalPath.Replace(Core.Bot.Pathes.Main, "")}";

            return fileName;
        }

        private static void RetryIOAction(Action action, int retries = 3, int delay = 100)
        {
            for (int i = 0; i < retries; i++)
            {
                try
                {
                    action();
                    return;
                }
                catch (IOException) when (i < retries - 1)
                {
                    Thread.Sleep(delay);
                }
            }
        }
    }
}
