using DankDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace butterBror.Utils.DataManagers
{
    /// <summary>
    /// Provides safe data persistence operations with optional backup functionality.
    /// </summary>
    public static class SafeManager
    {
        /// <summary>
        /// Saves data to a file with optional backup creation before writing.
        /// </summary>
        /// <param name="filePath">The path where the data should be saved.</param>
        /// <param name="key">The key/section name to store the data under.</param>
        /// <param name="data">The data object to persist.</param>
        /// <param name="createBackup">Optional flag indicating whether to create a backup before saving (default: true).</param>
        /// <remarks>
        /// This method ensures data integrity by optionally creating a backup copy of the file
        /// before performing the save operation. Uses Manager for actual data serialization.
        /// </remarks>
        public static void Save(string filePath, string key, object data, bool createBackup = true)
        {
            if (FileUtil.FileExists(filePath) && createBackup)
            {
                FileUtil.CreateBackup(filePath);
            }
            Manager.Save(filePath, key, data);
        }
    }
}
