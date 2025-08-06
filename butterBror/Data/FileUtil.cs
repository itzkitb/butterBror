using DankDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace butterBror.Data
{
    /// <summary>
    /// Provides utility methods for file and directory operations with caching and backup functionality.
    /// </summary>
    public static class FileUtil
    {
        private static readonly LruCache<string, string> _fileCache = new(100);

        /// <summary>
        /// Creates a directory at the specified path if it doesn't exist.
        /// </summary>
        /// <param name="directoryPath">The path of the directory to create.</param>
        public static void CreateDirectory(string directoryPath)
        {
            Directory.CreateDirectory(directoryPath);
        }

        /// <summary>
        /// Checks if a directory exists at the specified path.
        /// </summary>
        /// <param name="directoryPath">The path to check.</param>
        /// <returns>True if the directory exists; otherwise, false.</returns>
        public static bool DirectoryExists(string directoryPath)
        {
            return Directory.Exists(directoryPath);
        }

        /// <summary>
        /// Checks if a file exists at the specified path.
        /// </summary>
        /// <param name="filePath">The path to check.</param>
        /// <returns>True if the file exists; otherwise, false.</returns>
        public static bool FileExists(string filePath)
        {
            return File.Exists(filePath);
        }

        /// <summary>
        /// Creates a new file at the specified path and ensures parent directories exist.
        /// Adds the file to the cache if created successfully.
        /// </summary>
        /// <param name="filePath">The path of the file to create.</param>
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

        /// <summary>
        /// Deletes a file at the specified path and removes it from the cache.
        /// </summary>
        /// <param name="filePath">The path of the file to delete.</param>
        public static void DeleteFile(string filePath)
        {
            if (FileExists(filePath))
            {
                File.Delete(filePath);
                _fileCache.Invalidate(filePath);
            }
        }

        /// <summary>
        /// Reads the contents of a file from cache or disk.
        /// </summary>
        /// <param name="filePath">The path of the file to read.</param>
        /// <returns>The file's content as a string.</returns>
        /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
        public static string GetFileContent(string filePath)
        {
            return _fileCache.GetOrAdd(filePath, key =>
            {
                if (FileExists(key))
                    return File.ReadAllText(key);

                throw new FileNotFoundException($"File {key} not found");
            });
        }

        /// <summary>
        /// Saves content to a file, creates a backup, and updates the cache.
        /// </summary>
        /// <param name="filePath">The path of the file to write.</param>
        /// <param name="content">The content to write to the file.</param>
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

        /// <summary>
        /// Clears all cached file contents.
        /// </summary>
        public static void ClearCache()
        {
            _fileCache.Clear();
        }

        /// <summary>
        /// Checks if a file path is contained within a specific directory.
        /// </summary>
        /// <param name="filePath">The file path to check.</param>
        /// <param name="directoryPath">The directory path to compare against.</param>
        /// <returns>True if the file is in the directory; otherwise, false.</returns>
        private static bool IsPathInDirectory(string filePath, string directoryPath)
        {
            var directory = Path.GetDirectoryName(filePath);
            return !string.IsNullOrEmpty(directory) &&
                   directory.StartsWith(directoryPath, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Creates a backup copy of the specified file if it doesn't already exist.
        /// </summary>
        /// <param name="filePath">The path of the file to back up.</param>
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

        /// <summary>
        /// Generates a backup file path based on the original file path.
        /// </summary>
        /// <param name="originalPath">The original file path.</param>
        /// <returns>A new path for the backup file.</returns>
        public static string GetBackupPath(string originalPath)
        {
            var fileName = $"{Engine.Bot.Pathes.Reserve}{originalPath.Replace(Engine.Bot.Pathes.Main, "")}";

            return fileName;
        }

        /// <summary>
        /// Executes an I/O operation with retry logic for transient failures.
        /// </summary>
        /// <param name="action">The I/O action to perform.</param>
        /// <param name="retries">Maximum number of retry attempts (default: 3).</param>
        /// <param name="delay">Delay between retries in milliseconds (default: 100).</param>
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
