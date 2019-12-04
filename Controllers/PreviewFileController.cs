using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using tools_tpt_transformation_service.Jobs;

namespace tools_tpt_transformation_service.Controllers
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
        private readonly JobManager _jobManager;

        /// <summary>
        /// Basic ctor.
        /// </summary>
        /// <param name="logger">Logger (required).</param>
        /// <param name="jobManager">Job manager (required).</param>
        public PreviewFileController(
            ILogger<PreviewFileController> logger,
            JobManager jobManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger)); ;
            _jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));

            _logger.LogDebug("PreviewFileController()");
        }

        /// <summary>
        /// GET (read) resource method.
        /// </summary>
        /// <param name="jobId">Job ID (required).</param>
        /// <returns>Preview file stream if found, 404 or other error otherwise.</returns>
        [HttpGet("{jobId}")]
        public ActionResult Get(string jobId)
        {
            if (!_jobManager.TryGetPreviewStream(jobId, out FileStream fileStream))
            {
                return NotFound();
            }
            return File(fileStream, "application/pdf", "preview.pdf");
        }
    }
}
