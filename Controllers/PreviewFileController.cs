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
    /// REST Controller for the PreviewFile endpoint.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class PreviewFileController : ControllerBase
    {
        private readonly ILogger<PreviewFileController> _logger;
        private readonly JobManager _jobManager;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="logger">Logger.</param>
        /// <param name="jobManager">Preview Job Manager</param>
        public PreviewFileController(
            ILogger<PreviewFileController> logger,
            JobManager jobManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger)); ;
            _jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));

            _logger.LogDebug("PreviewFileController()");
        }

        // GET: api/PreviewFile/5
        [HttpGet("{jobId}", Name = "Get")]
        public ActionResult Get(string jobId)
        {
            if (!_jobManager.TryGetFileStream(jobId, out FileStream fileStream))
            {
                return NotFound();
            }
            return File(fileStream, "application/pdf", "preview.pdf");
        }
    }
}
