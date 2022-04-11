/*
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
        /// Max doc upload size in bytes.
        /// </summary>
        private readonly int _maxDocUploadSizeInBytes;

        /// <summary>
        /// Max doc uploads per request.
        /// </summary>
        private readonly int _maxDocUploadsPerRequest;

        /// <summary>
        /// Max project name length for uploads.
        /// </summary>
        private readonly int _maxProjectNameLengthForUploads;

        /// <summary>
        /// Max username length for uploads.
        /// </summary>
        private readonly int _maxUserNameLengthForUploads;

        /// <summary>
        /// Paratext document directory.
        /// </summary>
        private readonly DirectoryInfo _paratextDocDir;

        /// <summary>
        /// Set of allowable USX filenames, either directly in a request or in an uploaded archive.
        /// </summary>
        private readonly ISet<string> _allowableFileNames;

        /// <summary>
        /// Basic ctor.
        /// </summary>
        /// <param name="logger">Logger (required).</param>
        /// <param name="configuration"></param>
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
            _maxProjectNameLengthForUploads = int.Parse(configuration[ConfigConsts.MaxProjectNameLengthForUploadsKey]
                                                        ?? throw new ArgumentNullException(ConfigConsts.MaxProjectNameLengthForUploadsKey));
            _maxUserNameLengthForUploads = int.Parse(configuration[ConfigConsts.MaxUserNameLengthForUploadsKey]
                                                     ?? throw new ArgumentNullException(ConfigConsts.MaxUserNameLengthForUploadsKey));
            _paratextDocDir = new DirectoryInfo(configuration[ConfigConsts.ParatextDocDirKey]
                                                ?? throw new ArgumentNullException(ConfigConsts.ParatextDocDirKey));

            // create dictionary of allowable filenames
            _allowableFileNames = BookUtil.BookIdList.Select(foundIdItem => $"{foundIdItem.BookCode}.usx").ToImmutableHashSet();

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

            // check project name
            if (!ProjectUtil.ValidateProjectName(previewJob.BibleSelectionParams?.ProjectName)
                || previewJob.BibleSelectionParams?.ProjectName.Length > _maxProjectNameLengthForUploads)
            {
                return BadRequest($"Invalid \"bibleSelectionParams.projectName\" parameter (must be 1-{_maxProjectNameLengthForUploads} alphanumeric characters long).");
            }

            // check username
            if (!ProjectUtil.ValidateUserName(previewJob.User)
                || previewJob.User.Length > _maxUserNameLengthForUploads)
            {
                return BadRequest($"Invalid \"user\" parameter (must be 1-{_maxUserNameLengthForUploads} alphanumeric characters long).");
            }

            // check request files for basic conformity
            if (Request.Form.Files.Count < 1
                || Request.Form.Files.Count > _maxDocUploadsPerRequest
                || Request.Form.Files.Any(foundFile => foundFile.Length < 1
                    || foundFile.Length > _maxDocUploadSizeInBytes))
            {
                return BadRequest($"Invalid file count (must be 1-{_maxDocUploadsPerRequest} in request) or size (each must be 1-{_maxDocUploadSizeInBytes} bytes).");
            }

            // set up preview job with needed field values
            var projectName = previewJob.BibleSelectionParams?.ProjectName
                              ?? throw new ArgumentNullException(nameof(previewJob.BibleSelectionParams.ProjectName)); ;
            previewJob.ContentSource = ContentSource.PreviewJobRequest;
            previewJob.TypesettingParams ??= new TypesettingParams();
            previewJob.TypesettingParams.BookFormat = BookFormat.cav;
            previewJob.TypesettingParams.UseCustomFootnotes = false;
            previewJob.TypesettingParams.UseProjectFont = false;
            previewJob.TypesettingParams.UseHyphenation = false;
            previewJob.AdditionalParams ??= new AdditionalPreviewParams();
            previewJob.AdditionalParams.TextDirection = TextDirection.LTR;

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
                        var outputFile = Path.Combine(projectFolder.FullName, foundFile.FileName);
                        if (System.IO.File.Exists(outputFile))
                        {
                            throw new ArgumentException(
                                $"\"{foundFile.FileName}\" is a duplicate filename (must be unique in uploaded files).");
                        }
                        // write file
                        using var outputWriter = new StreamWriter(outputFile);
                        foundFile.CopyTo(outputWriter.BaseStream);
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
                                var outputFile = Path.Combine(projectFolder.FullName, foundEntry.Name);
                                if (System.IO.File.Exists(outputFile))
                                {
                                    throw new ArgumentException(
                                        $"\"{foundEntry.FullName}\" is a duplicate filename (must be unique in uploaded archives).");
                                }
                                // write file
                                using var outputWriter = new StreamWriter(outputFile);
                                using var entryStream = foundEntry.Open();
                                entryStream.CopyTo(outputWriter.BaseStream);
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