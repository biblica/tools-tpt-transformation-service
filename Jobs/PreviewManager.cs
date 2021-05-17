using InDesignServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using TptMain.InDesign;
using TptMain.Models;
using TptMain.Util;

namespace TptMain.Jobs
{
    /// <summary>
    /// Preview Manager class that will address the preview generation portion of PreviewJobs.
    /// </summary>
    public class PreviewManager : IPreviewManager
    {
        /// <summary>
        /// Type-specific logger (injected).
        /// </summary>
        private readonly ILogger<PreviewManager> _logger;

        /// <summary>
        /// IDS request timeout in seconds (configured).
        /// </summary>
        private readonly int _idsTimeoutInMSec;

        /// <summary>
        /// Preview script (JSX) path (configured).
        /// </summary>
        private readonly DirectoryInfo _idsPreviewScriptDirectory;

        /// <summary>
        /// Directory where IDTT files are located.
        /// </summary>
        private readonly DirectoryInfo _idttDocDir;

        /// <summary>
        /// Directory where IDML files are located.
        /// </summary>
        private readonly DirectoryInfo _idmlDocDir;

        /// <summary>
        /// Directory where PDF files are output.
        /// </summary>
        private readonly DirectoryInfo _pdfDocDir;

        /// <summary>
        /// All configured InDesign script runners.
        /// </summary>
        private List<InDesignScriptRunner> _indesignScriptRunners { get; } = new List<InDesignScriptRunner>();

        /// <summary>
        /// Basic ctor.
        /// </summary>
        /// <param name="logger">Type-specific logger (required).</param>
        /// <param name="configuration">System configuration (required).</param>
        public PreviewManager(
            ILoggerFactory loggerFactory,
            IConfiguration configuration)
        {
            _ = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _ = configuration ?? throw new ArgumentNullException(nameof(configuration));

            _logger = loggerFactory.CreateLogger<PreviewManager>();

            // grab global settings that apply to every InDesignScriptRunner
            _idsTimeoutInMSec = (int)TimeSpan.FromSeconds(int.Parse(configuration[ConfigConsts.IdsTimeoutInSecKey]
                                                                     ?? throw new ArgumentNullException(ConfigConsts
                                                                         .IdsTimeoutInSecKey)))
                .TotalMilliseconds;
            _idsPreviewScriptDirectory = new DirectoryInfo((configuration[ConfigConsts.IdsPreviewScriptDirKey]
                                                            ?? throw new ArgumentNullException(ConfigConsts
                                                                .IdsPreviewScriptDirKey)));
            _idttDocDir = new DirectoryInfo(configuration[ConfigConsts.IdttDocDirKey]
                           ?? throw new ArgumentNullException(
                               ConfigConsts.IdsPreviewScriptNameFormatKey));
            _idmlDocDir = new DirectoryInfo(configuration[ConfigConsts.IdmlDocDirKey]
                           ?? throw new ArgumentNullException(
                               ConfigConsts.IdmlDocDirKey));
            _pdfDocDir = new DirectoryInfo(configuration[ConfigConsts.PdfDocDirKey]
                          ?? throw new ArgumentNullException(
                              ConfigConsts.PdfDocDirKey));

            // grab the individual InDesignScriptRunner settings and create servers for each configuration
            var serversSection = configuration.GetSection(ConfigConsts.IdsServersSectionKey);
            var serversConfig = serversSection.Get<List<InDesignServerConfig>>();
            SetUpInDesignScriptRunners(loggerFactory, serversConfig);

            _logger.LogDebug("PreviewManager()");
        }

        /// <summary>
        /// Process a preview job.
        /// </summary>
        /// <param name="previewJob">Preview job to process (required).</param>
        public void ProcessJob(ref PreviewJob previewJob)
        {
            // pass along to the functional overloaded function.
            ProcessJob(ref previewJob, null);
        }

        /// <summary>
        /// Process a preview job.
        /// </summary>
        /// <param name="previewJob">Preview job to process (required).</param>
        /// <param name="additionalPreviewParams">Additional preview job params (optional).</param>
        public void ProcessJob(ref PreviewJob previewJob, AdditionalPreviewParameters additionalPreviewParams)
        {
            // validate inputs
            _ = previewJob ?? throw new ArgumentNullException(nameof(previewJob));

            previewJob.State = PreviewJobState.GeneratingPreview;
        }

        /// <summary>
        /// Query the status of the PreviewJob and update the job itself appropriately.
        /// </summary>
        /// <param name="previewJob">The PreviewJob to query the status of.</param>
        public void GetStatus(ref PreviewJob previewJob)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Initiate the cancellation of a PreviewJob.
        /// </summary>
        /// <param name="previewJob">The PreviewJob to cancel.</param>
        public void CancelJob(ref PreviewJob previewJob)
        {
            _logger.LogInformation($"Preview job '{previewJob.Id}' has been cancelled.");
            previewJob.State = PreviewJobState.Cancelled;
        }

        /// <summary>
        /// Create an InDesignScriptRunner object for each server configuration.
        /// </summary>
        /// <param name="loggerFactory">Logger Factory (required).</param>
        /// <param name="serverConfigs">Server configurations (required).</param>
        private void SetUpInDesignScriptRunners(ILoggerFactory loggerFactory, List<InDesignServerConfig> serverConfigs)
        {
            if (serverConfigs == null || serverConfigs.Count <= 0)
            {
                throw new ArgumentException($"No server configurations were found in the configuration section '{ConfigConsts.IdsServersSectionKey}'");
            }

            foreach(var config in serverConfigs)
            {
                var serverName = config.Name;

                if (serverName == null || serverName.Trim().Length <= 0)
                {
                    throw new ArgumentException($"Server.Name cannot be null or empty.'");
                }

                var logger = loggerFactory.CreateLogger(nameof(InDesignScriptRunner) + $":{config.Name}");
                _indesignScriptRunners.Add(
                    new InDesignScriptRunner(
                        logger, 
                        config, 
                        _idsTimeoutInMSec,
                        _idsPreviewScriptDirectory,
                        _idmlDocDir,
                        _idttDocDir,
                        _pdfDocDir
                        ));
            }
        }
    }
}