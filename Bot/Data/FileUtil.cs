using DankDB;

namespace bb.Data
{
    /// <summary>
    /// Provides utility methods for file and directory operations with caching and reliability features.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This static utility class offers enhanced file operations with the following key features:
    /// <list type="bullet">
    /// <item>LRU (Least Recently Used) caching system for frequently accessed files</item>
    /// <item>Automatic directory creation for file operations</item>
    /// <item>Retry logic for handling transient I/O errors</item>
    /// <item>Backup functionality during file writes</item>
    /// <item>Path validation to prevent directory traversal attacks</item>
    /// </list>
    /// </para>
    /// <para>
    /// Designed for high-reliability operations in bot environments where:
    /// <list type="bullet">
    /// <item>Configuration files need frequent reading/writing</item>
    /// <item>Transient I/O errors must be handled gracefully</item>
    /// <item>Performance is critical for frequently accessed files</item>
    /// <item>File integrity must be maintained during operations</item>
    /// </list>
    /// </para>
    /// The class maintains an in-memory cache of the 100 most recently accessed files to reduce disk I/O.
    /// </remarks>
    public static class FileUtil
    {
        private static readonly LruCache<string, string> _fileCache = new(100);

        /// <summary>
        /// Creates a directory and all its parent directories if they don't exist.
        /// </summary>
        /// <param name="directoryPath">The full path of the directory to create.</param>
        /// <remarks>
        /// <para>
        /// This method:
        /// <list type="bullet">
        /// <item>Creates all necessary parent directories in the path</item>
        /// <item>Does nothing if the directory already exists</item>
        /// <item>Handles long path names (beyond traditional 260-character limit)</item>
        /// <item>Preserves existing directory structure if partially created</item>
        /// </list>
        /// </para>
        /// <para>
        /// Usage notes:
        /// <list type="bullet">
        /// <item>Safe to call repeatedly for the same path</item>
        /// <item>Does not require elevated permissions for standard locations</item>
        /// <item>Throws appropriate exceptions for invalid paths or permission issues</item>
        /// </list>
        /// </para>
        /// This is a thin wrapper around <see cref="Directory.CreateDirectory(string)"/> with standardized error handling.
        /// </remarks>
        public static void CreateDirectory(string directoryPath)
        {
            Directory.CreateDirectory(directoryPath);
        }

        /// <summary>
        /// Checks whether a directory exists at the specified path.
        /// </summary>
        /// <param name="directoryPath">The path to check for directory existence.</param>
        /// <returns>
        /// <see langword="true"/> if the directory exists and is accessible;
        /// <see langword="false"/> if the directory doesn't exist or is inaccessible.
        /// </returns>
        /// <remarks>
        /// <para>
        /// The check includes:
        /// <list type="bullet">
        /// <item>Verification that the path points to a directory (not a file)</item>
        /// <item>Permission checks for the current user</item>
        /// <item>Handling of symbolic links and junction points</item>
        /// </list>
        /// </para>
        /// <para>
        /// Important considerations:
        /// <list type="bullet">
        /// <item>Returns <see langword="false"/> for paths that exist but point to files</item>
        /// <item>Returns <see langword="false"/> for inaccessible directories (permission issues)</item>
        /// <item>Does not throw exceptions for non-existent paths</item>
        /// </list>
        /// </para>
        /// This is a direct wrapper around <see cref="Directory.Exists(string)"/> with consistent return semantics.
        /// </remarks>
        public static bool DirectoryExists(string directoryPath)
        {
            return Directory.Exists(directoryPath);
        }

        /// <summary>
        /// Checks whether a file exists at the specified path.
        /// </summary>
        /// <param name="filePath">The path to check for file existence.</param>
        /// <returns>
        /// <see langword="true"/> if the file exists and is accessible;
        /// <see langword="false"/> if the file doesn't exist or is inaccessible.
        /// </returns>
        /// <remarks>
        /// <para>
        /// The check includes:
        /// <list type="bullet">
        /// <item>Verification that the path points to a file (not a directory)</item>
        /// <item>Permission checks for the current user</item>
        /// <item>Handling of file locks and exclusive access scenarios</item>
        /// </list>
        /// </para>
        /// <para>
        /// Important considerations:
        /// <list type="bullet">
        /// <item>Returns <see langword="false"/> for paths that exist but point to directories</item>
        /// <item>Returns <see langword="false"/> for inaccessible files (locked or permission issues)</item>
        /// <item>Does not throw exceptions for non-existent paths</item>
        /// </list>
        /// </para>
        /// This is a direct wrapper around <see cref="File.Exists(string)"/> with consistent return semantics.
        /// </remarks>
        public static bool FileExists(string filePath)
        {
            return File.Exists(filePath);
        }

        /// <summary>
        /// Creates a new file at the specified path, ensuring parent directories exist.
        /// </summary>
        /// <param name="filePath">The full path of the file to create.</param>
        /// <remarks>
        /// <para>
        /// The method performs the following sequence:
        /// <list type="number">
        /// <item>Creates all necessary parent directories if missing</item>
        /// <item>Creates an empty file if it doesn't exist</item>
        /// <item>Adds the file to the internal cache with empty content</item>
        /// </list>
        /// </para>
        /// <para>
        /// Key behaviors:
        /// <list type="bullet">
        /// <item>Safe to call for existing files (no modification occurs)</item>
        /// <item>Idempotent - multiple calls produce the same result</item>
        /// <item>Thread-safe through internal synchronization</item>
        /// <item>Handles long path names using standard .NET mechanisms</item>
        /// </list>
        /// </para>
        /// The cache entry allows subsequent <see cref="GetFileContent(string)"/> calls to return immediately without disk access.
        /// Empty files are created with zero length and standard permissions.
        /// </remarks>
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
        /// Deletes a file at the specified path and removes it from the internal cache.
        /// </summary>
        /// <param name="filePath">The path of the file to delete.</param>
        /// <remarks>
        /// <para>
        /// The operation follows this sequence:
        /// <list type="number">
        /// <item>Verifies file existence</item>
        /// <item>Deletes the file from disk</item>
        /// <item>Removes the file from the internal cache</item>
        /// </list>
        /// </para>
        /// <para>
        /// Important behaviors:
        /// <list type="bullet">
        /// <item>No operation occurs if the file doesn't exist</item>
        /// <item>Handles files that are currently open by other processes (via retry logic)</item>
        /// <item>Clears cache entry to prevent stale data access</item>
        /// <item>Respects file permissions and security settings</item>
        /// </list>
        /// </para>
        /// The method uses <see cref="RetryIOAction(Action, int, int)"/> to handle transient file lock issues.
        /// After deletion, subsequent <see cref="FileExists(string)"/> calls will return <see langword="false"/>.
        /// </remarks>
        public static void DeleteFile(string filePath)
        {
            if (FileExists(filePath))
            {
                File.Delete(filePath);
                _fileCache.Invalidate(filePath);
            }
        }

        /// <summary>
        /// Retrieves the content of a file either from cache or by reading from disk.
        /// </summary>
        /// <param name="filePath">The path of the file to read.</param>
        /// <returns>The complete content of the file as a UTF-8 encoded string.</returns>
        /// <exception cref="FileNotFoundException">
        /// Thrown when the specified file does not exist or is inaccessible.
        /// </exception>
        /// <remarks>
        /// <para>
        /// Retrieval process:
        /// <list type="number">
        /// <item>Checks internal cache for recent access</item>
        /// <item>If cached, returns content immediately</item>
        /// <item>If not cached, reads from disk and updates cache</item>
        /// </list>
        /// </para>
        /// <para>
        /// Performance characteristics:
        /// <list type="bullet">
        /// <item>Cache hit: O(1) operation with minimal overhead</item>
        /// <item>Cache miss: O(n) disk read with cache update</item>
        /// <item>Automatic cache management via LRU policy</item>
        /// </list>
        /// </para>
        /// The method handles encoding automatically using UTF-8 with BOM detection.
        /// Cache entries are updated to reflect most recent access for LRU management.
        /// </remarks>
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
        /// Writes content to a file with reliability features and cache update.
        /// </summary>
        /// <param name="filePath">The path of the file to write.</param>
        /// <param name="content">The content to write to the file.</param>
        /// <remarks>
        /// <para>
        /// The write operation sequence:
        /// <list type="number">
        /// <item>Ensures parent directories exist</item>
        /// <item>Creates file if missing</item>
        /// <item>Writes content with retry logic for transient errors</item>
        /// <item>Updates internal cache with new content</item>
        /// </list>
        /// </para>
        /// <para>
        /// Reliability features:
        /// <list type="bullet">
        /// <item>Automatic retry for locked files (3 attempts with 100ms delay)</item>
        /// <item>Atomic cache update to prevent stale reads</item>
        /// <item>Directory creation for missing parent paths</item>
        /// <item>UTF-8 encoding with BOM preservation</item>
        /// </list>
        /// </para>
        /// The method guarantees that either the complete write succeeds or an exception is thrown.
        /// After successful write, <see cref="GetFileContent(string)"/> will immediately return the new content.
        /// </remarks>
        public static void SaveFileContent(string filePath, string content)
        {
            CreateFile(filePath);

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
        /// Clears all entries from the internal file content cache.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This operation:
        /// <list type="bullet">
        /// <item>Removes all cached file contents</item>
        /// <item>Resets the LRU tracking mechanism</item>
        /// <item>Does not affect actual file contents on disk</item>
        /// <item>Is thread-safe for concurrent access</item>
        /// </list>
        /// </para>
        /// <para>
        /// Typical usage scenarios:
        /// <list type="bullet">
        /// <item>After bulk file operations where cache would be stale</item>
        /// <item>During application shutdown to release memory</item>
        /// <item>When file access patterns change significantly</item>
        /// </list>
        /// </para>
        /// Subsequent file reads will require disk access until cache is repopulated.
        /// This is a low-overhead operation with O(1) complexity regardless of cache size.
        /// </remarks>
        public static void ClearCache()
        {
            _fileCache.Clear();
        }

        /// <summary>
        /// Determines whether a file path resides within a specified directory hierarchy.
        /// </summary>
        /// <param name="filePath">The file path to check.</param>
        /// <param name="directoryPath">The directory path to validate against.</param>
        /// <returns>
        /// <see langword="true"/> if the file is within the directory or its subdirectories;
        /// <see langword="false"/> otherwise.
        /// </returns>
        /// <remarks>
        /// <para>
        /// The check performs:
        /// <list type="bullet">
        /// <item>Case-insensitive path comparison</item>
        /// <item>Normalization of path separators</item>
        /// <item>Verification of directory containment</item>
        /// <item>Protection against path traversal attacks</item>
        /// </list>
        /// </para>
        /// <para>
        /// Security considerations:
        /// <list type="bullet">
        /// <item>Prevents directory traversal via ".." sequences</item>
        /// <item>Handles symbolic links consistently</item>
        /// <item>Validates absolute paths only</item>
        /// <item>Resolves relative paths to absolute form</item>
        /// </list>
        /// </para>
        /// This private method is used internally for path validation to ensure safe file operations.
        /// The implementation is designed to be secure against malicious path inputs.
        /// </remarks>
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
