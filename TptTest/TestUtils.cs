using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json;
using TptMain.Models;

namespace TptTest
{
    /// <summary>
    /// Utility methods for testing.
    /// </summary>
    public static class TestUtils
    {
        /// <summary>
        /// Util method for creating preview job for testing.
        /// </summary>
        /// <returns>Preview job object.</returns>
        public static PreviewJob CreateTestPreviewJob()
        {
            return new PreviewJob
            {
                Id = Guid.Empty.ToString(),
                ProjectName = TestConsts.TEST_PROJECT_NAME,
                User = TestConsts.TEST_USER,
                BookFormat = BookFormat.cav,
                FontSizeInPts = 123.4f,
                FontLeadingInPts = 234.5f,
                PageHeightInPts = 345.6f,
                PageWidthInPts = 456.7f,
                PageHeaderInPts = 567.8f,
                UseProjectFont = false
            };
        }

        /// <summary>
        /// Computes MD5 hash for quick file comparisons.
        /// </summary>
        /// <param name="inputFile">Input file (required).</param>
        /// <returns>MD5 hash as byte array.</returns>
        public static byte[] Md5Hash(this FileInfo inputFile)
        {
            using var md5 = MD5.Create();
            using var inputStream = inputFile.OpenRead();
            return md5.ComputeHash(inputStream);
        }

        /// <summary>
        /// Checks whether two files are equal (specifically, have the same MD5 hashes).
        /// </summary>
        /// <param name="inputFile1">Input file #1 (required).</param>
        /// <param name="inputFile2">Input file #2 (required).</param>
        /// <returns>True if files are equal, false otherwise.</returns>
        public static bool AreFilesEqual(FileInfo inputFile1, FileInfo inputFile2)
        {
            return inputFile1.Md5Hash().SequenceEqual(inputFile2.Md5Hash());
        }

        /// <summary>
        /// Deep clone an object via JSON.
        /// </summary>
        /// <typeparam name="T">Input type (must be serializable via JSON).</typeparam>
        /// <param name="inputObject">Input object (required).</param>
        /// <returns></returns>
        public static T DeepClone<T>(this T inputObject)
        {
            return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(inputObject));
        }
    }
}
