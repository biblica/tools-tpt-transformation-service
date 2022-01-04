/*
Copyright © 2021 by Biblica, Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using TptMain.Jobs;

namespace TptMain.Controllers
{
    /// <summary>
    /// REST Controller for preview file resources.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class PreviewFileController : ControllerBase
    {
        /// <summary>
        /// Type-specific logger (injected).
        /// </summary>
        private readonly ILogger<PreviewFileController> _logger;

        /// <summary>
        /// Job manager (injected).
        /// </summary>
        private readonly IJobManager _jobManager;

        /// <summary>
        /// Basic ctor.
        /// </summary>
        /// <param name="logger">Logger (required).</param>
        /// <param name="jobManager">Job manager (required).</param>
        public PreviewFileController(
            ILogger<PreviewFileController> logger,
            IJobManager jobManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));

            _logger.LogDebug("PreviewFileController()");
        }

        /// <summary>
        /// GET (read) resource method.
        /// </summary>
        /// <param name="jobId">Job ID (required).</param>
        /// <param name="archive">Whether or not to return an archive of all the typesetting files or just the PDF itself (optional). 
        /// True: all typesetting files zipped in an archive. False: Output only the preview PDF itself. . Default: false.</param>
        /// <returns>Preview file stream if found, 404 or other error otherwise.</returns>
        [HttpGet("{jobId}")]
        public ActionResult Get(string jobId, bool archive = false)
        {
            _logger.LogDebug($"Get() - jobId={jobId}.");
            if (!_jobManager.TryGetPreviewStream(jobId, out var fileStream, archive))
            {
                return NotFound();
            }

            // Defaults for PDF output
            var outputMimeType = "application/pdf";
            var outputExtension = ".pdf";

            // Determine whether or not we're returning an archive.
            if (archive)
            {
                outputMimeType = "application/zip";
                outputExtension = ".zip";
            }

            return File(fileStream, outputMimeType, $"preview{outputExtension}");
        }
    }
}