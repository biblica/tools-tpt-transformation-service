using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using TptMain.Http;
using TptMain.Models;
using TptMain.Util;

namespace TptMain.Toolbox
{
    /// <summary>
    /// Manages access to template (IDTT) files, provided by the Toolbox API.
    /// </summary>
    public class TemplateManager
    {
        /// <summary>
        /// Toolbox template server URI config key.
        /// </summary>
        private const string ToolboxTemplateServerUriKey = "Toolbox:Template:ServerUri";

        /// <summary>
        /// Toolbox template timeout config key.
        /// </summary>
        private const string ToolboxTemplateTimeoutInSecKey = "Toolbox:Template:TimeoutInSec";

        /// <summary>
        /// Type-specific logger (injected).
        /// </summary>
        private readonly ILogger<TemplateManager> _logger;

        /// <summary>
        /// System configuration (injected).
        /// </summary>
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Web request factory.
        /// </summary>
        private WebRequestFactory _requestFactory;

        /// <summary>
        /// URI for Toolbox template service.
        /// </summary>
        private readonly string _templateUri;

        /// <summary>
        /// Timeout for template requests, in milliseconds.
        /// </summary>
        private readonly int _templateTimeoutInMSec;

        /// <summary>
        /// Basic ctor.
        /// </summary>
        /// <param name="logger">Type-specific logger (required).</param>
        /// <param name="configuration">System configuration (required).</param>
        /// <param name="requestFactory">Web request factory (required).</param>
        public TemplateManager(
            ILogger<TemplateManager> logger,
            IConfiguration configuration,
            WebRequestFactory requestFactory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _requestFactory = requestFactory ?? throw new ArgumentNullException(nameof(requestFactory));

            _templateUri = (_configuration[ToolboxTemplateServerUriKey]
                ?? throw new ArgumentNullException(ToolboxTemplateServerUriKey));
            _templateTimeoutInMSec = int.Parse(_configuration[ToolboxTemplateTimeoutInSecKey]
                ?? throw new ArgumentNullException(ToolboxTemplateTimeoutInSecKey))
                * MainConsts.MILLISECONDS_PER_SECOND;
        }

        /// <summary>
        /// Gets a task that retrieves a template file based on input job specifics.
        /// </summary>
        /// <param name="inputJob">Input job (required).</param>
        /// <param name="outputFile">Output file (required).</param>
        /// <returns></returns>
        public virtual void DownloadTemplateFile(PreviewJob inputJob, FileInfo outputFile)
        {
            _logger.LogDebug($"DownloadTemplateFile() inputJob.Id={inputJob.Id}, outputFile={outputFile}.");
            var webRequest = _requestFactory.CreateWebRequest(
                $"{_templateUri}{ToQueryString(inputJob)}",
                HttpMethod.Get.Method,
                _templateTimeoutInMSec);

            using var inputStream = webRequest.GetResponse().GetResponseStream();
            using var outputStream = outputFile.OpenWrite();
            inputStream?.CopyTo(outputStream);
        }

        /// <summary>
        /// Creates template-specific query string from preview job.
        /// </summary>
        /// <param name="inputJob">Preview job (required).</param>
        /// <returns>Template-specific query string.</returns>
        public static string ToQueryString(PreviewJob inputJob)
        {
            IDictionary<string, string> queryMap = new Dictionary<string, string>();

            queryMap["font_size"] = inputJob.FontSizeInPts.ToString();
            queryMap["leading"] = inputJob.FontLeadingInPts.ToString();
            queryMap["page_width"] = inputJob.PageWidthInPts.ToString();
            queryMap["page_height"] = inputJob.PageHeightInPts.ToString();
            queryMap["header_size"] = inputJob.PageHeaderInPts.ToString();
            queryMap["book_type"] = inputJob.BookFormat.ToString();

            return StringUtil.ToQueryString(queryMap);
        }
    }
}