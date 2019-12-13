using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using TptMain.Models;
using TptMain.Projects;

namespace TptMain.Controllers
{
    /// <summary>
    /// REST controller for project details resources.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ProjectDetailsController : ControllerBase
    {
        /// <summary>
        /// Type-specific logger (injected).
        /// </summary>
        private readonly ILogger<ProjectDetailsController> _logger;

        /// <summary>
        /// Project manager (injected).
        /// </summary>
        private readonly ProjectManager _projectManager;

        /// <summary>
        /// Basic ctor.
        /// </summary>
        /// <param name="logger">Type-specific logger (required).</param>
        /// <param name="projectManager">Project manager (required).</param>
        public ProjectDetailsController(
            ILogger<ProjectDetailsController> logger,
            ProjectManager projectManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger)); ;
            _projectManager = projectManager ?? throw new ArgumentNullException(nameof(projectManager));

            _logger.LogDebug("ProjectDetailsController()");
        }

        /// <summary>
        /// GET (read) resource for all project details.
        /// </summary>
        /// <returns>Project details list if found, 404 or other error otherwise.</returns>
        [HttpGet]
        public ActionResult<IEnumerable<ProjectDetails>> Get()
        {
            _logger.LogDebug("Get().");
            if (_projectManager.TryGetProjectDetails(out var projectDetails))
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