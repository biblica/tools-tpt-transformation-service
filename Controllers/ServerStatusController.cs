using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using TptMain.Models;
using TptMain.Projects;
using TptMain.Util;

namespace TptMain.Controllers
{
    /// <summary>
    /// REST controller for the Server's status.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ServerStatusController : ControllerBase
    {
        /// <summary>
        /// Type-specific logger (injected).
        /// </summary>
        private readonly ILogger<ServerStatusController> _logger;

        /// <summary>
        /// Basic ctor.
        /// </summary>
        /// <param name="logger">Type-specific logger (required).</param>
        public ServerStatusController(
            ILogger<ServerStatusController> logger,
            ProjectManager projectManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _logger.LogDebug("ServerStatusController()");
        }

        /// <summary>
        /// GET (read) resource for the server's status.
        /// </summary>
        /// <returns><c>ServerStatus</c> if found, 404 or other error otherwise.</returns>
        [HttpGet]
        public ActionResult<ServerStatus> Get()
        {
            _logger.LogDebug("Get().");

            // get the server's version
            Version serverVersion = null;

            // Attempt to retrieve the server's version.
            try
            {
                serverVersion = AssemblyUtil.GetAssemblyVersion();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"We weren't able to retrieve the Server's version.");
            }

            return new ServerStatus
            {
                Version = serverVersion?.ToString()
            };
        }
    }
}