﻿/*
Copyright © 2021 by Biblica, Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using TptMain.Models;
using TptMain.Util;

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
                BibleSelectionParams = new BibleSelectionParams
                {
                    ProjectName = TestConsts.TEST_PROJECT_NAME,
                    SelectedBooks = null
                },
                User = TestConsts.TEST_USER,
                TypesettingParams = new TypesettingParams
                {
                    BookFormat = BookFormat.cav,
                    FontSizeInPts = MainConsts.ALLOWED_FONT_SIZE_IN_PTS.Default,
                    FontLeadingInPts = MainConsts.ALLOWED_FONT_LEADING_IN_PTS.Default,
                    PageHeightInPts = MainConsts.ALLOWED_PAGE_HEIGHT_IN_PTS.Default,
                    PageWidthInPts = MainConsts.ALLOWED_PAGE_WIDTH_IN_PTS.Default,
                    PageHeaderInPts = MainConsts.ALLOWED_PAGE_HEADER_IN_PTS.Default,
                    UseProjectFont = false
                },
                AdditionalParams = new AdditionalPreviewParams()
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