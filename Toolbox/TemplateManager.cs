using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
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
        /// Web request factory.
        /// </summary>
        private readonly WebRequestFactory _requestFactory;

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
            _ = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _requestFactory = requestFactory ?? throw new ArgumentNullException(nameof(requestFactory));

            _templateUri = (configuration[ToolboxTemplateServerUriKey]
                            ?? throw new ArgumentNullException(ToolboxTemplateServerUriKey));
            _templateTimeoutInMSec = (int)TimeSpan.FromSeconds(int.Parse(configuration[ToolboxTemplateTimeoutInSecKey]
                                                                         ?? throw new ArgumentNullException(
                                                                             ToolboxTemplateTimeoutInSecKey)))
                .TotalMilliseconds;
        }

        /// <summary>
        /// Synchronously downloads template file based on input job specifics.
        /// </summary>
        /// <param name="inputJob">Input job (required).</param>
        /// <param name="outputFile">Output file (required).</param>
        /// <returns></returns>
        public virtual void DownloadTemplateFile(PreviewJob inputJob, FileInfo outputFile)
        {
            this.DownloadTemplateFile(inputJob, outputFile, null);
        }

        /// <summary>
        /// Synchronously downloads template file based on input job specifics, with optional cancellation.
        /// </summary>
        /// <param name="inputJob">Input job (required).</param>
        /// <param name="outputFile">Output file (required).</param>
        /// <param name="cancellationToken">Cancellation token (optional, may be null).</param>
        /// <returns></returns>
        public virtual void DownloadTemplateFile(PreviewJob inputJob, FileInfo outputFile, CancellationToken? cancellationToken)
        {
            _logger.LogDebug($"DownloadTemplateFile() inputJob.Id={inputJob.Id}, outputFile={outputFile}.");
            var webRequest = _requestFactory.CreateWebRequest(
                $"{_templateUri}{ToQueryString(inputJob)}",
                HttpMethod.Get.Method,
                _templateTimeoutInMSec);

            if (cancellationToken == null)
            {
                using var inputStream = webRequest.GetResponse().GetResponseStream();
                using var outputStream = outputFile.OpenWrite();
                inputStream?.CopyTo(outputStream);
            }
            else
            {
                var workCancellationToken = (CancellationToken)cancellationToken;

                // web response
                var webRequestTask = webRequest.GetResponseAsync();
                webRequestTask.Wait(_templateTimeoutInMSec, workCancellationToken);
                if (webRequestTask.IsCanceled)
                    return;

                // input stream
                using var webResponse = webRequestTask.Result;
                using var inputStream = webResponse.GetResponseStream();

                // output stream
                using var outputStream = outputFile.OpenWrite();

                // copy
                inputStream?.CopyToAsync(outputStream, workCancellationToken)
                    .Wait(_templateTimeoutInMSec, workCancellationToken);
            }
        }

        /// <summary>
        /// Creates template-specific query string from preview job.
        /// </summary>
        /// <param name="inputJob">Preview job (required).</param>
        /// <returns>Template-specific query string.</returns>
        private static string ToQueryString(PreviewJob inputJob)
        {
            IDictionary<string, string> queryMap = new SortedDictionary<string, string>
            {
                ["font_size"] = inputJob.FontSizeInPts.ToString(),
                ["leading"] = inputJob.FontLeadingInPts.ToString(),
                ["page_width"] = inputJob.PageWidthInPts.ToString(),
                ["page_height"] = inputJob.PageHeightInPts.ToString(),
                ["header_size"] = inputJob.PageHeaderInPts.ToString(),
                ["book_type"] = inputJob.BookFormat.ToString()
            };

            return StringUtil.ToQueryString(queryMap);
        }
    }
}