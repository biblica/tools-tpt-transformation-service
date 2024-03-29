﻿/*
Copyright © 2021 by Biblica, Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using TptMain.Jobs;
using TptMain.Models;

namespace TptMain.Controllers
{
    /// <summary>
    /// REST Controller for preview jobs resources.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class PreviewJobsController : ControllerBase
    {
        /// <summary>
        /// Type-specific logger (injected).
        /// </summary>
        private readonly ILogger<PreviewJobsController> _logger;

        /// <summary>
        /// Job manager (injected).
        /// </summary>
        private readonly IJobManager _jobManager;

        /// <summary>
        /// Basic ctor.
        /// </summary>
        /// <param name="logger">Logger (required).</param>
        /// <param name="jobManager">Job manager (required).</param>
        public PreviewJobsController(
            ILogger<PreviewJobsController> logger,
            IJobManager jobManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));

            _logger.LogDebug("PreviewJobsController()");
        }

        /// <summary>
        /// GET (read) resource for preview jobs.
        /// </summary>
        /// <param name="jobId">Job ID (required).</param>
        /// <returns>Preview job if found, 404 or other error otherwise.</returns>
        [HttpGet("{jobId}")]
        public ActionResult<PreviewJob> GetPreviewJob(string jobId)
        {
            _logger.LogDebug($"GetPreviewJob() - jobId={jobId}.");
            if (!_jobManager.TryGetJob(jobId, out var previewJob))
            {
                return NotFound();
            }
            return previewJob;
        }

        /// <summary>
        /// POST (create) resource for preview jobs.
        /// </summary>
        /// <param name="previewJob">Preview job (required).</param>
        /// <returns>Saved preview job if created, error otherwise.</returns>
        [HttpPost]
        public ActionResult<PreviewJob> PostPreviewJob(PreviewJob previewJob)
        {
            _logger.LogDebug($"PostPreviewJob() - previewJob.Id={previewJob.Id}.");
            if (!_jobManager.TryAddJob(previewJob, out var outputJob))
            {
                return BadRequest();
            }
            return CreatedAtAction("GetPreviewJob", new { jobId = outputJob.Id }, outputJob);
        }

        /// <summary>
        /// Delete resource for preview jobs.
        /// </summary>
        /// <param name="jobId">Job ID (required).</param>
        /// <returns>Deleted preview job if found, 404 or other error otherwise.</returns>
        [HttpDelete("{jobId}")]
        public ActionResult<PreviewJob> DeletePreviewJob(string jobId)
        {
            _logger.LogDebug($"DeletePreviewJob() - jobId={jobId}.");
            if (!_jobManager.TryDeleteJob(jobId, out var outputJob))
            {
                return NotFound();
            }
            return outputJob;
        }
    }
}