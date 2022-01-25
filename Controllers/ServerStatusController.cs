/*
Copyright © 2021 by Biblica, Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using TptMain.Models;
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
            ILogger<ServerStatusController> logger)
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