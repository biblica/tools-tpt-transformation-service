using System;
using System.IO;

namespace TptMain.Util
{
    /// <summary>
    /// General-purpose file/directory utilties.
    /// </summary>
    public static class FileUtil
    {
        /// <summary>
        /// Check if a directory exists, otherwise create it.
        /// </summary>
        /// <param name="directoryPath">The path of the directory to create.</param>
        /// <returns>The DirectoryInfo object of the created directory.</returns>
        public static DirectoryInfo CheckAndCreateDirectory(string directoryPath)
        {
            // validate input
            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                throw new ArgumentNullException($"{nameof(directoryPath)} cannot be null or whitespace.");
            }

            return Directory.CreateDirectory(directoryPath);
        }

    }
}