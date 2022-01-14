/*
Copyright © 2021 by Biblica, Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
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