using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using tools_tpt_transformation_service.Models;
using tools_tpt_transformation_service.Projects;

namespace tools_tpt_transformation_service.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProjectDetailsController : ControllerBase
    {
        private readonly ILogger<ProjectDetailsController> _logger;
        private readonly ProjectManager _projectManager;

        public ProjectDetailsController(
            ILogger<ProjectDetailsController> logger,
            ProjectManager projectManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger)); ;
            _projectManager = projectManager ?? throw new ArgumentNullException(nameof(projectManager));

            _logger.LogDebug("ProjectDetailsController()");
        }

        // GET: api/ProjectDetails
        [HttpGet]
        public ActionResult<IEnumerable<ProjectDetails>> Get()
        {
            if (_projectManager.TryGetProjectDateTimes(out IDictionary<String, ProjectDetails> projectDetails))
            {
                return projectDetails.Values.ToArray();
            }
            else
            {
                return NotFound();
            }
        }
    }
}
