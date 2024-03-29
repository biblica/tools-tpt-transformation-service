﻿/*
Copyright © 2021 by Biblica, Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using TptMain.Util;

namespace TptMain.Jobs
{
    /// <summary>
    /// This class is for accessing the TPT processed job files.
    /// </summary>
    public class JobFileManager
    {
        /// <summary>
        /// Type-specific logger (injected).
        /// </summary>
        private readonly ILogger<JobFileManager> _logger;

        /// <summary>
        /// Base directory of the processed job files.
        /// </summary>
        public DirectoryInfo JobFilesRootDir { get; private set; }

        /// <summary>
        /// Simple constructor.
        /// </summary>
        public JobFileManager(
            ILogger<JobFileManager> logger,
            IConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            JobFilesRootDir = new DirectoryInfo(configuration[ConfigConsts.ProcessedJobFilesRootDirKey]
                                                 ?? throw new ArgumentNullException(ConfigConsts.ProcessedJobFilesRootDirKey));

            _logger.LogInformation("JobFileManager()");
        }

        /// <summary>
        /// Return a project's processed files directory based on ID. EG: {Jobs Root}\{job GUID}\
        /// </summary>
        /// <param name="id">Project ID to get directory for (required)</param>
        /// <returns>Project directory.</returns>
        public DirectoryInfo GetProjectDirectoryById(string id)
        {
            CheckString(id);
            return new DirectoryInfo(Path.Combine(JobFilesRootDir.FullName, id));
        }

        /// <summary>
        /// Return a project's template directory based on ID. EG: {Jobs Root}\{job GUID}\template
        /// </summary>
        /// <param name="id">Project ID to get directory for (required)</param>
        /// <returns>Project template directory.</returns>
        public virtual DirectoryInfo GetTemplateDirectoryById(string id)
        {
            CheckString(id);
            return new DirectoryInfo(Path.Combine(JobFilesRootDir.FullName, id, "template"));
        }

        /// <summary>
        /// Return a project's tagged text directory based on ID. EG: {Jobs Root}\{job GUID}\idtt
        /// </summary>
        /// <param name="id">Project ID to get directory for (required)</param>
        /// <returns>Project tagged text directory.</returns>
        public virtual DirectoryInfo GetTaggedTextDirectoryById(string id)
        {
            CheckString(id);
            return new DirectoryInfo(Path.Combine(JobFilesRootDir.FullName, id, "idtt"));
        }

        /// <summary>
        /// Return a project's preview directory based on ID. EG: {Jobs Root}\{job GUID}\pdf
        /// </summary>
        /// <param name="id">Project ID to get directory for (required)</param>
        /// <returns>Project preview directory.</returns>
        public virtual DirectoryInfo GetPreviewDirectoryById(string id)
        {
            CheckString(id);
            return new DirectoryInfo(Path.Combine(JobFilesRootDir.FullName, id, "pdf"));
        }

        /// <summary>
        /// Return a project's archive directory based on ID. EG: {Jobs Root}\{job GUID}\zip
        /// </summary>
        /// <param name="id">Project ID to get directory for (required)</param>
        /// <returns>Project archive directory.</returns>
        public DirectoryInfo GetArchiveDirectoryById(string id)
        {
            CheckString(id);
            return new DirectoryInfo(Path.Combine(JobFilesRootDir.FullName, id, "zip"));
        }

        /// <summary>
        /// Check that the input string is valid.
        /// </summary>
        /// <param name="input">input string to validate.</param>
        private static void CheckString(string input)
        {
            if (String.IsNullOrWhiteSpace(input))
            {
                throw new ArgumentNullException($"'{nameof(input)}' cannot be empty/null");
            }
        }
    }
}