using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using TptMain.Paratext;
using TptMain.Paratext.Models;

namespace TptMain.Controllers
{
    /// <summary>
    /// REST Controller for preview file resources.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class TestParatextApiController : ControllerBase
    {
        /// <summary>
        /// Type-specific logger (injected).
        /// </summary>
        private readonly ILogger<TestParatextApiController> _logger;

        /// <summary>
        /// Paratext Api.
        /// </summary>
        private readonly ParatextApi _paratextApi;

        /// <summary>
        /// Basic ctor.
        /// </summary>
        /// <param name="logger">Logger (required).</param>
        /// <param name="jobManager">Job manager (required).</param>
        public TestParatextApiController(
            ILogger<TestParatextApiController> logger,
            ParatextApi paratextApi)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger)); ;
            _paratextApi = paratextApi ?? throw new ArgumentNullException(nameof(paratextApi));

            _logger.LogDebug("TestParatextApiController()");
        }

        /// <summary>
        /// GET (read) resource method.
        /// </summary>
        /// <param name="projectName">Project name (required).</param>
        /// <returns>Preview file stream if found, 404 or other error otherwise.</returns>
        [HttpGet("{projectname}")]
        public ActionResult<IEnumerable<ProjectMember>> Get(string projectName)
        {
            _logger.LogDebug($"Get() - projectName={projectName}.");
            //if (!_jobManager.TryGetPreviewStream(jobId, out var fileStream))
            //{
            //    return NotFound();
            //}
            //return File(fileStream, "application/pdf", "preview.pdf");

            var projects = _paratextApi.GetAllowedProjectMembersAsync(projectName).Result;
            return projects;
        }

        [HttpGet("projects/{projectname}")]
        public ActionResult<IEnumerable<Project>> GetProjects(string projectName)
        {
            _logger.LogDebug($"Get() - projectName={projectName}.");
            //if (!_jobManager.TryGetPreviewStream(jobId, out var fileStream))
            //{
            //    return NotFound();
            //}
            //return File(fileStream, "application/pdf", "preview.pdf");

            return _paratextApi.GetParatextProjectsAsync().Result;
        }

        [HttpGet("isauth/{projectname}/{username}")]
        public ActionResult<bool> IsAuth(string projectName, string username)
        {
            _logger.LogDebug($"Get() - isauth/projectName={projectName}/username={username}.");
            //if (!_jobManager.TryGetPreviewStream(jobId, out var fileStream))
            //{
            //    return NotFound();
            //}
            //return File(fileStream, "application/pdf", "preview.pdf");

            return _paratextApi.IsUserAuthorizedOnProject(username, projectName).Result;
        }

    }
}