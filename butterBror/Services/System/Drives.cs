
namespace butterBror.Services.System
{
    /// <summary>
    /// Provides functionality to retrieve information about logical drives on the computer.
    /// </summary>
    public class Drives
    {
        /// <summary>
        /// Retrieves all logical drives on the computer.
        /// </summary>
        /// <remarks>
        /// Records function usage in application statistics before retrieving drive information.
        /// </remarks>
        /// <returns>An array of DriveInfo objects representing all logical drives.</returns>
        public static DriveInfo[] Get()
        {
            Engine.Statistics.FunctionsUsed.Add();
            DriveInfo[] drives = DriveInfo.GetDrives();
            return drives;
        }
    }
}
