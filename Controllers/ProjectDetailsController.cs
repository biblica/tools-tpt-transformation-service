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
        private readonly IProjectManager _projectManager;

        /// <summary>
        /// Basic ctor.
        /// </summary>
        /// <param name="logger">Type-specific logger (required).</param>
        /// <param name="projectManager">Project manager (required).</param>
        public ProjectDetailsController(
            ILogger<ProjectDetailsController> logger,
            IProjectManager projectManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _projectManager = projectManager ?? throw new ArgumentNullException(nameof(projectManager));

            _logger.LogDebug("ProjectDetailsController()");
        }

        /// <summary>
        /// GET (read) resource for all project details.
        /// </summary>
        /// <returns>Project details list if found, 404 or other error otherwise.</returns>
        [HttpGet]
        public ActionResult<IList<ProjectDetails>> Get()
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