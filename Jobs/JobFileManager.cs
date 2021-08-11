
using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using TptMain.Exceptions;
using TptMain.Models;
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

        // A number of consts for later processing

        /// <summary>
        /// Base directory of the processed job files.
        /// </summary>
        private readonly DirectoryInfo _jobFilesRootDir;

        /// <summary>
        /// The S3Service to talk to S3 to verify status and get results
        /// </summary>
        private readonly S3Service _s3Service;

        /// <summary>
        /// Simple constructor.
        /// </summary>
        public JobFileManager(
            ILogger<JobFileManager> logger,
            IConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _jobFilesRootDir = new DirectoryInfo(configuration[ConfigConsts.ProcessedJobFilesRootDirKey]
                                                 ?? throw new ArgumentNullException(ConfigConsts.ProcessedJobFilesRootDirKey));

            _s3Service = new S3Service();
        }

        /// <summary>
        /// Return a project's processed files directory based on ID. EG: {Jobs Root}\{job GUID}\
        /// </summary>
        /// <param name="id">Project ID to get directory for (required)</param>
        /// <returns>Project directory.</returns>
        public DirectoryInfo GetProjectDirectoryById(string id)
        {
            CheckString(id);
            return new DirectoryInfo(Path.Combine(_jobFilesRootDir.FullName, id));
        }

        /// <summary>
        /// Return a project's template directory based on ID. EG: {Jobs Root}\{job GUID}\template
        /// </summary>
        /// <param name="id">Project ID to get directory for (required)</param>
        /// <returns>Project template directory.</returns>
        public virtual DirectoryInfo GetTemplateDirectoryById(string id)
        {
            CheckString(id);
            return new DirectoryInfo(Path.Combine(_jobFilesRootDir.FullName, id, "template"));
        }

        /// <summary>
        /// Return a project's tagged text directory based on ID. EG: {Jobs Root}\{job GUID}\idtt
        /// </summary>
        /// <param name="id">Project ID to get directory for (required)</param>
        /// <returns>Project tagged text directory.</returns>
        public virtual DirectoryInfo GetTaggedTextDirectoryById(string id)
        {
            CheckString(id);
            return new DirectoryInfo(Path.Combine(_jobFilesRootDir.FullName, id, "idtt"));
        }

        /// <summary>
        /// Return a project's preview directory based on ID. EG: {Jobs Root}\{job GUID}\pdf
        /// </summary>
        /// <param name="id">Project ID to get directory for (required)</param>
        /// <returns>Project preview directory.</returns>
        public virtual DirectoryInfo GetPreviewDirectoryById(string id)
        {
            CheckString(id);
            return new DirectoryInfo(Path.Combine(_jobFilesRootDir.FullName, id, "pdf"));
        }

        /// <summary>
        /// Return a project's archive directory based on ID. EG: {Jobs Root}\{job GUID}\zip
        /// </summary>
        /// <param name="id">Project ID to get directory for (required)</param>
        /// <returns>Project archive directory.</returns>
        public DirectoryInfo GetArchiveDirectoryById(string id)
        {
            CheckString(id);
            return new DirectoryInfo(Path.Combine(_jobFilesRootDir.FullName, id, "zip"));
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