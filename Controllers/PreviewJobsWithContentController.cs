﻿/*
Copyright © 2021 by Biblica, Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Microsoft.Extensions.Configuration;
using SIL.Linq;
using TptMain.Jobs;
using TptMain.Models;
using TptMain.Text;
using TptMain.Util;

namespace TptMain.Controllers
{
    /// <summary>
    /// REST Controller for preview jobs resources.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class PreviewJobsWithContentController : ControllerBase
    {
        /// <summary>
        /// Type-specific logger (injected).
        /// </summary>
        private readonly ILogger<PreviewJobsWithContentController> _logger;

        /// <summary>
        /// Job manager (injected).
        /// </summary>
        private readonly IJobManager _jobManager;

        /// <summary>
        /// Max doc upload size in bytes (injected).
        /// </summary>
        private readonly int _maxDocUploadSizeInBytes;

        /// <summary>
        /// Max doc uploads per request (injected).
        /// </summary>
        private readonly int _maxDocUploadsPerRequest;

        /// <summary>
        /// Paratext document directory (injected).
        /// </summary>
        private readonly DirectoryInfo _paratextDocDir;

        /// <summary>
        /// Auth token for uploads (injected).
        /// </summary>
        private readonly string _uploadsAuthToken;

        /// <summary>
        /// Project name prefix for uploads (injected).
        /// </summary>
        private readonly string _projectNamePrefix;

        /// <summary>
        /// Set of allowable USX filenames, either directly in a request or in an uploaded archive.
        /// </summary>
        private readonly ISet<string> _allowableFileNames;

        /// <summary>
        /// Dictionary of input to output USX filenames, to maintain compatibility with USX transform capability.
        /// </summary>
        private readonly IDictionary<string, string> _inputToOutputFileNames;

        /// <summary>
        /// Basic ctor.
        /// </summary>
        /// <param name="logger">Logger (required).</param>
        /// <param name="configuration">Service configuration (required).</param>
        /// <param name="jobManager">Job manager (required).</param>
        public PreviewJobsWithContentController(
            ILogger<PreviewJobsWithContentController> logger,
            IConfiguration configuration,
            IJobManager jobManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _ = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));

            // settings fields based on configuration
            _maxDocUploadSizeInBytes = int.Parse(configuration[ConfigConsts.MaxDocUploadSizeInBytesKey]
                                                 ?? throw new ArgumentNullException(ConfigConsts.MaxDocUploadSizeInBytesKey));
            _maxDocUploadsPerRequest = int.Parse(configuration[ConfigConsts.MaxDocUploadsPerRequestKey]
                                                 ?? throw new ArgumentNullException(ConfigConsts.MaxDocUploadsPerRequestKey));
            _uploadsAuthToken = configuration[ConfigConsts.UploadsAuthTokenKey]
                                ?? throw new ArgumentNullException(ConfigConsts.UploadsAuthTokenKey);
            _projectNamePrefix = configuration[ConfigConsts.UploadsProjectNamePrefixKey]
                                 ?? throw new ArgumentNullException(ConfigConsts.UploadsProjectNamePrefixKey);
            _paratextDocDir = new DirectoryInfo(configuration[ConfigConsts.ParatextDocDirKey]
                                                ?? throw new ArgumentNullException(ConfigConsts.ParatextDocDirKey));

            // create dictionary of allowable filenames
            _allowableFileNames = BookUtil.BookIdList.Select(foundIdItem => $"{foundIdItem.BookCode}.usx").ToImmutableHashSet();
            _inputToOutputFileNames = BookUtil.BookIdList.ToImmutableDictionary(
                foundIdItem => $"{foundIdItem.BookCode}.usx",
                foundIdItem => $"{foundIdItem.BookNum:000}{foundIdItem.BookCode}.usx");

            _logger.LogDebug("PreviewJobsWithContentController()");
        }

        /// <summary>
        /// POST (create) resource for preview jobs.
        /// </summary>
        /// <param name="previewJob">Preview job (required).</param>
        /// <returns>Saved preview job if created, error otherwise.</returns>
        [HttpPost]
        public ActionResult<PreviewJob> PostPreviewJob([FromForm] PreviewJob previewJob)
        {
            _logger.LogDebug($"PostPreviewJob() - previewJob.Id={previewJob.Id}.");

            // check for authorization
            if (!Request.Headers.Authorization.Any(foundValue => foundValue.Equals(_uploadsAuthToken)))
            {
                return Unauthorized();
            }

            // check request files for basic usability
            if (Request.Form.Files.Count < 1
                    || Request.Form.Files.Count > _maxDocUploadsPerRequest
                    || Request.Form.Files.Any(foundFile => foundFile.Length < 1
                        || foundFile.Length > _maxDocUploadSizeInBytes))
            {
                return BadRequest($"Invalid file count (must be 1-{_maxDocUploadsPerRequest} in request) or size (each must be 1-{_maxDocUploadSizeInBytes} bytes).");
            }

            // set up preview job with needed field values
            var projectName = $"{_projectNamePrefix}{Guid.NewGuid():N}";
            previewJob.ContentSource = ContentSource.PreviewJobRequest;
            previewJob.BibleSelectionParams ??= new BibleSelectionParams();
            previewJob.BibleSelectionParams.ProjectName = projectName;

            // save/extract files
            var projectFolder = new DirectoryInfo(Path.Combine(_paratextDocDir.FullName, projectName));

            try
            {
                Request.Form.Files.ForEach(foundFile =>
                {
                    // support uploading USX files directly
                    if (_allowableFileNames.Contains(foundFile.FileName))
                    {
                        // only create the directory when we know we have a usable file
                        if (!projectFolder.Exists)
                        {
                            Directory.CreateDirectory(projectFolder.FullName);
                            projectFolder.Refresh();
                        }
                        // create output path & deal with duplicates
                        var outputFile = Path.Combine(projectFolder.FullName, _inputToOutputFileNames[foundFile.FileName]);
                        if (System.IO.File.Exists(outputFile))
                        {
                            throw new ArgumentException(
                                $"\"{foundFile.FileName}\" is a duplicate filename (must be unique in uploaded files).");
                        }
                        // write file
                        using var outputStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write);
                        foundFile.CopyTo(outputStream);
                    }
                    else // else, assume we have an archive
                    {
                        try
                        {
                            using var zipArchive = new ZipArchive(foundFile.OpenReadStream());
                            zipArchive.Entries.ForEach(foundEntry =>
                            {
                                // ensure this is one of the allowable filenames
                                if (!_allowableFileNames.Contains(foundEntry.Name))
                                {
                                    throw new ArgumentException(
                                        $"\"{foundEntry.FullName}\" is not an allowed USX filename (must be \"<book-code>.usx\").");
                                }
                                // only create the directory when we know we have a usable file
                                if (!projectFolder.Exists)
                                {
                                    Directory.CreateDirectory(projectFolder.FullName);
                                    projectFolder.Refresh();
                                }
                                // create output path & deal with duplicates
                                var outputFile = Path.Combine(projectFolder.FullName, _inputToOutputFileNames[foundEntry.Name]);
                                if (System.IO.File.Exists(outputFile))
                                {
                                    throw new ArgumentException(
                                        $"\"{foundEntry.FullName}\" is a duplicate filename (must be unique in uploaded archives).");
                                }
                                // write file
                                using var outputStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write);
                                using var entryStream = foundEntry.Open();
                                entryStream.CopyTo(outputStream);
                            });
                        }
                        catch (InvalidDataException ex)
                        {
                            // also the case when the uploaded file (a) doesn't match one of the USX filenames and (b) isn't a Zip archive
                            throw new ArgumentException($"\"{foundFile.FileName}\" is not an allowed USX filename or valid Zip archive (error: {ex.Message}).");
                        }
                    }
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }

            // create and return job
            if (!_jobManager.TryAddJob(previewJob, out var outputJob))
            {
                return BadRequest();
            }

            return Created(new Uri($"/api/PreviewJobs/{outputJob.Id}", UriKind.Relative), outputJob);
        }
    }
}