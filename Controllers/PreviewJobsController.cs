using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using tools_tpt_transformation_service.InDesign;
using tools_tpt_transformation_service.Jobs;
using tools_tpt_transformation_service.Models;

namespace tools_tpt_transformation_service.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PreviewJobsController : ControllerBase
    {
        private readonly ILogger<PreviewJobsController> _logger;
        private readonly JobManager _jobManager;

        public PreviewJobsController(
            ILogger<PreviewJobsController> logger,
            JobManager jobManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));

            _logger.LogDebug("PreviewJobsController()");
        }

        // GET: api/PreviewJobs/5
        [HttpGet("{jobId}")]
        public ActionResult<PreviewJob> GetPreviewJob(string jobId)
        {
            if (!_jobManager.TryGetJob(jobId, out PreviewJob previewJob))
            {
                return NotFound();
            }
            return previewJob;
        }

        // POST: api/PreviewJobs
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        [HttpPost]
        public ActionResult<PreviewJob> PostPreviewJob(PreviewJob previewJob)
        {
            if (!_jobManager.TryAddJob(previewJob, out PreviewJob outputJob))
            {
                return BadRequest();
            }
            return CreatedAtAction("GetPreviewJob", new { jobId = outputJob.Id }, outputJob);
        }

        // DELETE: api/PreviewJobs/5
        [HttpDelete("{jobId}")]
        public ActionResult<PreviewJob> DeletePreviewJob(string jobId)
        {
            if (!_jobManager.TryDeleteJob(jobId, out PreviewJob outputJob))
            {
                return NotFound();
            }
            return outputJob;
        }
    }
}
