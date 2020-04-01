using Microsoft.Extensions.Logging;
using System;
using System.Net;

namespace TptMain.Http
{
    /// <summary>
    /// Factory service for web requests and related.
    ///
    /// Created to improve testability.
    /// </summary>
    public class WebRequestFactory
    {
        /// <summary>
        /// Type-specific logger (injected).
        /// </summary>
        private readonly ILogger<WebRequestFactory> _logger;

        /// <summary>
        /// Basic ctor.
        /// </summary>
        /// <param name="logger">Logger (required).</param>
        public WebRequestFactory(ILogger<WebRequestFactory> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogDebug("WebRequestFactory()");
        }

        /// <summary>
        /// Creates a web request from a supplied URI, HTTP method, and timeout.
        /// </summary>
        /// <param name="requestUri">Request URI (required).</param>
        /// <param name="httpMethod">HTTP method (required).</param>
        /// <param name="timeoutInMSec">Timeout in milliseconds.</param>
        /// <returns></returns>
        public virtual WebRequest CreateWebRequest(string requestUri, string httpMethod, int timeoutInMSec)
        {
            _logger.LogDebug($"CreateWebRequest() - requestUri={requestUri}, httpMethod={httpMethod}, timeoutInMSec={timeoutInMSec}.");

            var webRequest = WebRequest.Create(requestUri);
            webRequest.Method = httpMethod;
            webRequest.Timeout = timeoutInMSec;

            return webRequest;
        }
    }
}