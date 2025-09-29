using bb.Core.Services;
using bb.Data;
using bb.Utils;
using System.Diagnostics;
using System.IO.Compression;
using static bb.Core.Bot.Console;

namespace bb.Core.Bot
{
    /// <summary>
    /// Provides functionality for creating and managing system backups.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item>Creates compressed backups of all bot data and configuration files</item>
    /// <item>Handles special processing for SQLite database files</item>
    /// <item>Provides progress notifications through Twitch chat</item>
    /// <item>Automatically cleans up temporary resources after operation</item>
    /// <item>Generates timestamped archive files in reserve storage location</item>
    /// </list>
    /// Backups are triggered automatically at midnight UTC through Engine.StartRepeater().
    /// </remarks>
    public class Backup
    {
        /// <summary>
        /// Creates a comprehensive backup of all bot data with database consistency protection.
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        /// <item>Creates timestamped ZIP archive in reserve storage directory</item>
        /// <item>First processes non-database files through direct copy operation</item>
        /// <item>Uses specialized database backup methods to ensure data consistency</item>
        /// <item>Sends real-time progress notifications to Twitch chat</item>
        /// <item>Measures and reports total operation duration and archive size</item>
        /// <item>Implements robust cleanup of temporary resources</item>
        /// </list>
        /// Database files receive special handling through SqlDatabaseBase.CreateBackup() to prevent 
        /// corruption during active usage. Non-database files are copied directly from source directory.
        /// Temporary working directory is always deleted regardless of operation success or failure.
        /// Archive naming convention: "backup_YYYYMMDD.zip" (UTC timestamp format).
        /// </remarks>
        /// <returns>Task representing the asynchronous backup operation</returns>
        public static async Task BackupDataAsync()
        {
            try
            {
                PlatformMessageSender.TwitchSend(bb.Bot.TwitchName.ToLower(), "🗃️ Backup started...", "", "", "", true, false);
                Write("Backup started...");

                string reservePath = bb.Bot.Paths.Reserve;
                Directory.CreateDirectory(reservePath);

                string archiveName = $"backup_{DateTime.UtcNow:yyyyMMdd}.zip";
                string archivePath = Path.Combine(reservePath, archiveName);

                if (File.Exists(archivePath))
                    File.Delete(archivePath);

                EmoteCacheService.Save();

                string tempBackupDir = Path.Combine(reservePath, $"temp_backup_{DateTime.UtcNow:yyyyMMddHHmmss}");
                Directory.CreateDirectory(tempBackupDir);

                Stopwatch stopwatch = Stopwatch.StartNew();

                try
                {
                    // Use EnumerateFiles instead of GetFiles for line-by-line reading
                    var allFiles = Directory.EnumerateFiles(
                        bb.Bot.Paths.Main,
                        "*",
                        SearchOption.AllDirectories
                    );

                    foreach (string file in allFiles)
                    {
                        if (!file.EndsWith(".db", StringComparison.OrdinalIgnoreCase))
                        {
                            string relativePath = Path.GetRelativePath(bb.Bot.Paths.Main, file);
                            string destFile = Path.Combine(tempBackupDir, relativePath);

                            Directory.CreateDirectory(Path.GetDirectoryName(destFile));
                            File.Copy(file, destFile, true);
                        }
                    }

                    var databaseManagers = GetDatabaseManagers();

                    foreach (var dbManager in databaseManagers)
                    {
                        string dbFileName = Path.GetFileName(dbManager.DbPath);
                        string backupDbPath = Path.Combine(tempBackupDir, dbFileName);

                        dbManager.CreateBackup(backupDbPath);
                    }

                    // We use the stream compression method
                    await Task.Run(() =>
                    {
                        using (FileStream zipToOpen = new FileStream(archivePath, FileMode.Create))
                        using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Create))
                        {
                            foreach (string file in Directory.EnumerateFiles(tempBackupDir, "*", SearchOption.AllDirectories))
                            {
                                string relativePathInArchive = Path.GetRelativePath(tempBackupDir, file);
                                archive.CreateEntryFromFile(file, relativePathInArchive, CompressionLevel.Optimal);
                            }
                        }
                    });
                }
                finally
                {
                    if (Directory.Exists(tempBackupDir))
                    {
                        try { Directory.Delete(tempBackupDir, true); }
                        catch { }
                    }
                }

                stopwatch.Stop();

                long archiveSize = new FileInfo(archivePath).Length;
                double archiveSizeMB = archiveSize / (1024.0 * 1024.0);

                Write($"Backup completed in {stopwatch.Elapsed.TotalSeconds:0} seconds (Archive size: {archiveSizeMB:0.00} MB)!");
                PlatformMessageSender.TwitchSend(bb.Bot.TwitchName.ToLower(),
                    $"🗃️ Backup completed in {stopwatch.Elapsed.TotalSeconds:0} seconds (Archive size: {archiveSizeMB:0.00} MB)",
                    "", "", "", true, false);
            }
            catch (Exception ex)
            {
                Write(ex);
            }
            finally
            {
                // ДОБАВЛЕНО: Явная очистка памяти после операции
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        /// <summary>
        /// Retrieves all active database managers for backup operations.
        /// </summary>
        /// <returns>
        /// Enumerable collection of all SQL database managers:
        /// <list type="table">
        /// <item><term>Games</term><description>Manages game statistics database</description></item>
        /// <item><term>Channels</term><description>Manages channel configuration database</description></item>
        /// <item><term>Messages</term><description>Manages message history database</description></item>
        /// <item><term>Roles</term><description>Manages role configuration database</description></item>
        /// <item><term>Users</term><description>Manages user data database</description></item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// Used during backup process to ensure proper database file handling.
        /// Each manager implements CreateBackup() method for consistent snapshot creation.
        /// Database files require special handling compared to regular configuration files.
        /// </remarks>
        private static IEnumerable<SqlDatabaseBase> GetDatabaseManagers()
        {
            yield return bb.Bot.DataBase.Games;
            yield return bb.Bot.DataBase.Channels;
            yield return bb.Bot.DataBase.Messages;
            yield return bb.Bot.DataBase.Roles;
            yield return bb.Bot.DataBase.Users;
        }
    }
}
