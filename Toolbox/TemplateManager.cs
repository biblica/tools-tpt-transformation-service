using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using tools_tpt_transformation_service.Models;
using tools_tpt_transformation_service.Util;

namespace tools_tpt_transformation_service.Toolbox
{
    /// <summary>
    /// Manages access to template (IDTT) files, provided by the Toolbox API.
    /// </summary>
    public class TemplateManager
    {
        /// <summary>
        /// Type-specific logger (injected).
        /// </summary>
        private readonly ILogger<TemplateManager> _logger;

        /// <summary>
        /// System configuration (injected).
        /// </summary>
        private readonly IConfiguration _configuration;

        /// <summary>
        /// URI for Toolbox template service.
        /// </summary>
        private readonly String _templateUri;

        /// <summary>
        /// Timeout for template requests, in seconds.
        /// </summary>
        private readonly int _templateTimeoutInSec;

        /// <summary>
        /// Basic ctor.
        /// </summary>
        /// <param name="logger">Type-specific logger (required).</param>
        /// <param name="configuration">System configuration (required).</param>
        public TemplateManager(
            ILogger<TemplateManager> logger,
            IConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            _templateUri = (_configuration.GetValue<string>("Toolbox:Template:ServerUri")
                ?? throw new ArgumentNullException("Toolbox:Template:ServerUri"));
            _templateTimeoutInSec = int.Parse(_configuration.GetValue<string>("Toolbox:Template:TimeoutInSec")
                ?? throw new ArgumentNullException("Toolbox:Template:TimeoutInSec"));
        }

        /// <summary>
        /// Gets a task that retrieves a template file based on input job specifics.
        /// </summary>
        /// <param name="inputJob">Input job (required).</param>
        /// <param name="outputFile">Output file (required).</param>
        /// <returns></returns>
        public void GetTemplateFile(PreviewJob inputJob, FileInfo outputFile)
        {
            WebRequest webRequest = WebRequest.Create($"{_templateUri}{ToQueryString(inputJob)}");
            webRequest.Method = HttpMethod.Get.Method;
            webRequest.Timeout = _templateTimeoutInSec;

            using (Stream inputStream = webRequest.GetResponse().GetResponseStream())
            {
                using (FileStream outputStream = outputFile.OpenWrite())
                {
                    inputStream.CopyTo(outputStream);
                }
            }
        }

        /// <summary>
        /// Creates template-specific query string from preview job.
        /// </summary>
        /// <param name="inputJob">Preview job (required).</param>
        /// <returns>Template-specific query string.</returns>
        static public String ToQueryString(PreviewJob inputJob)
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
