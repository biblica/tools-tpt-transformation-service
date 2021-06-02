using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
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
        private List<InDesignScriptRunner> IndesignScriptRunners { get; } = new List<InDesignScriptRunner>();

        /// <summary>
        /// The map for tracking running tasks against IDS script runners.
        /// </summary>
        private Dictionary<InDesignScriptRunner, Task> IdsTaskMap { get; } = new Dictionary<InDesignScriptRunner, Task>();

        /// <summary>
        /// The map for cancellation token sources by jobs.
        /// </summary>
        private Dictionary<PreviewJob, CancellationTokenSource> CancellationSourceMap { get; } = new Dictionary<PreviewJob, CancellationTokenSource>();

        /// <summary>
        /// This FIFO collection tracks the order in which <code>PreviewJob</code>s came in so that they're processed in-order.
        /// </summary>
        private ConcurrentQueue<PreviewJob> JobQueue { get; } = new ConcurrentQueue<PreviewJob>();

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
                               ConfigConsts.IdttDocDirKey));
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
        public void ProcessJob(PreviewJob previewJob)
        {
            // validate inputs
            _ = previewJob ?? throw new ArgumentNullException(nameof(previewJob));

            // put the job on the queue for processing
            JobQueue.Enqueue(previewJob);

            CheckPreviewProcessing();
        }

        /// <summary>
        /// This function ensures that Jobs are continuously processing as InDesign runners become available.
        /// </summary>
        private void CheckPreviewProcessing()
        {
            _logger.LogInformation("Checking and updating preview processing...");

            // Keep queuing jobs while we have a runner and jobs available.
            InDesignScriptRunner availableRunner = GetAvailableRunner();
            while (availableRunner != null && !JobQueue.IsEmpty)
            {
                // grab the next prioritized preview job
                if (!JobQueue.TryDequeue(out var previewJob))
                {
                    // nothing to dequeue
                    return;
                }

                // hold on to the ability to cancel the task
                var tokenSource = new CancellationTokenSource();

                // track the token so that we can support job cancellation requests
                CancellationSourceMap.TryAdd(previewJob, tokenSource);

                // we copy the reference of the chosen runner, otherwise the task may run with an unexpected runner due to looping
                var taskRunner = availableRunner;
                var task = new Task(() =>
                {
                    _logger.LogDebug($"Assigning preview generation job '{previewJob.Id}' to IDS runner '{taskRunner.Name}'.");
                    var runner = taskRunner;

                    previewJob.State = PreviewJobState.GeneratingPreview;

                    try
                    {
                        runner.CreatePreview(previewJob, tokenSource.Token);
                        previewJob.State = PreviewJobState.PreviewGenerated;
                    }
                    catch (Exception ex)
                    {
                        previewJob.SetError("An error occurred while generating preview.", ex.Message);
                    }
                }, tokenSource.Token);

                IdsTaskMap.TryAdd(taskRunner, task);

                task.Start();

                // check to see if there's a still a runner available for the next job
                availableRunner = GetAvailableRunner();
            }
        }

        /// <summary>
        /// Query the status of the PreviewJob and update the job itself appropriately.
        /// </summary>
        /// <param name="previewJob">The PreviewJob to query the status of.</param>
        public void GetStatus(PreviewJob previewJob)
        {
            CheckPreviewProcessing();
        }

        /// <summary>
        /// Initiate the cancellation of a PreviewJob.
        /// </summary>
        /// <param name="previewJob">The PreviewJob to cancel.</param>
        public void CancelJob(PreviewJob previewJob)
        {
            if (CancellationSourceMap.TryGetValue(previewJob, out var cancellationTokenSource))
            {
                cancellationTokenSource.Cancel();
                _logger.LogInformation($"Preview job '{previewJob.Id}' has been cancelled.");
                previewJob.State = PreviewJobState.Cancelled;
            } else
            {
                _logger.LogWarning($"Preview job '{previewJob.Id}' has no running task to cancel.");
            }
        }

        /// <summary>
        /// Get the next available <code>InDesignScriptRunner</code>; Otherwise: null.
        /// </summary>
        /// <returns>The next available <code>InDesignScriptRunner</code>; Otherwise: null.</returns>
        private InDesignScriptRunner GetAvailableRunner()
        {
            InDesignScriptRunner availableRunner = null;

            IndesignScriptRunners.ForEach((idsRunner) => {
                _logger.LogDebug($"Assessing '{idsRunner.Name}' for availability.") ;

                // break out if we've found a runner already
                if (availableRunner == null)
                {
                    // determine if we have any running tasks for the selected runner
                    IdsTaskMap.TryGetValue(idsRunner, out var task);

                    // track if there's any actively running task
                    if (task == null || task.IsCompleted)
                    {
                        availableRunner = idsRunner;
                    }
                    else
                    {
                        _logger.LogDebug($"'{idsRunner.Name}' is currently running a task in state '${task.Status}'.");
                    }
                }
            });

            if (availableRunner == null)
            {
                _logger.LogDebug("No available IDS runner found.");
            }

            return availableRunner;
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

            _logger.LogDebug($"{serverConfigs.Count} InDesign Server configurations found. \r\n" + JsonConvert.SerializeObject(serverConfigs));

            foreach(var config in serverConfigs)
            {
                var serverName = config.Name;

                if (serverName == null || serverName.Trim().Length <= 0)
                {
                    throw new ArgumentException($"Server.Name cannot be null or empty.'");
                }

                var logger = loggerFactory.CreateLogger(nameof(InDesignScriptRunner) + $":{config.Name}");
                IndesignScriptRunners.Add(
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