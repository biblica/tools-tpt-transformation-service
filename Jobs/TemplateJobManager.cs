
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using TptMain.Models;
using TptMain.Util;
using static TptMain.Jobs.TransformService;

namespace TptMain.Jobs
{
    public class TemplateJobManager : IPreviewJobProcessor
    {
        /// <summary>
        /// Logger (injected).
        /// </summary>
        private readonly ILogger<TemplateJobManager> _logger;

        /// <summary>
        /// The service that's processing the transform job
        /// </summary>
        private TransformService _transformService;

        /// <summary>
        /// The timeout period, in milliseconds, before the job is considered to be over-due, thus needing to be canceled and errored out
        /// </summary>
        private readonly int _timeoutMills;

        /// <summary>
        /// Constructor to pass in the logger factory and configuration
        /// </summary>
        /// <param name="loggerFactory">The factory to create the localized logger</param>
        /// <param name="configuration">The set of configuration parameters</param>
        public TemplateJobManager(
            ILoggerFactory loggerFactory,
            IConfiguration configuration)
        {
            _ = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _ = configuration ?? throw new ArgumentNullException(nameof(configuration));

            _logger = loggerFactory.CreateLogger<TemplateJobManager>();
            _transformService = new TransformService(_logger);

            // grab global settings for template generation timeout

            _timeoutMills = 1000 * int.Parse(configuration[ConfigConsts.TemplateGenerationTimeoutInSecKey] ?? throw new ArgumentNullException(ConfigConsts
                                                                        .TemplateGenerationTimeoutInSecKey));
        }

        /// <summary>
        /// Constructor to pass in the logger, config, and the transform service...
        /// Used for passing in a mocked transform service in tests
        /// </summary>
        /// <param name="loggerFactory">The factory to create the localized logger</param>
        /// <param name="configuration">The set of configuration parameters</param>
        /// <param name="transformService">When in a test, the mocked transform service</param>
        public TemplateJobManager(
            ILoggerFactory loggerFactory,
            IConfiguration configuration,
            TransformService transformService)
        {
            _ = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _ = configuration ?? throw new ArgumentNullException(nameof(configuration));

            _logger = loggerFactory.CreateLogger<TemplateJobManager>();

            /// this is the only difference for this constructor
            _transformService = transformService;

            // grab global settings for template generation timeout

            _timeoutMills = 1000 * int.Parse(configuration[ConfigConsts.TemplateGenerationTimeoutInSecKey] ?? throw new ArgumentNullException(ConfigConsts
                                                                        .TemplateGenerationTimeoutInSecKey));
        }

        /// <summary>
        /// Start a job process
        /// </summary>
        /// <param name="previewJob">The job to be worked</param>
        public void ProcessJob(PreviewJob previewJob)
        {
            previewJob.State.Add(new PreviewJobState(JobStateEnum.GeneratingTemplate, JobStateSourceEnum.TemplateGeneration));
            _transformService.GenerateTemplate(previewJob);
        }

        /// <summary>
        /// Force a cancelation of the job
        /// </summary>
        /// <param name="previewJob">The job to cancel</param>
        public void CancelJob(PreviewJob previewJob)
        {
            previewJob.State.Add(new PreviewJobState(JobStateEnum.Cancelled, JobStateSourceEnum.TemplateGeneration));
            _transformService.CancelTransformJobs(previewJob.Id);
        }

        /// <summary>
        /// Get the current status of the job
        /// </summary>
        /// <param name="previewJob">The job who's status needs updating</param>
        public void GetStatus(PreviewJob previewJob)
        {
            _logger.LogDebug($"Requesting status update for {previewJob.Id}");
            TransformJobStatus status = _transformService.GetTransformJobStatus(previewJob.Id);
            switch (status)
            {
                case TransformJobStatus.WAITING:
                case TransformJobStatus.PROCESSING:
                    previewJob.State.Add(new PreviewJobState(JobStateEnum.GeneratingTemplate, JobStateSourceEnum.TemplateGeneration));
                    _logger.LogDebug($"Status reported as {JobStateEnum.GeneratingTemplate.ToString()} for {previewJob.Id}");
                    break;
                case TransformJobStatus.TEMPLATE_COMPLETE:
                    previewJob.State.Add(new PreviewJobState(JobStateEnum.TemplateGenerated, JobStateSourceEnum.TemplateGeneration));
                    _logger.LogDebug($"Status reported as {JobStateEnum.TemplateGenerated.ToString()} for {previewJob.Id}");
                    break;
                case TransformJobStatus.CANCELED:
                    previewJob.State.Add(new PreviewJobState(JobStateEnum.Cancelled, JobStateSourceEnum.TemplateGeneration));
                    _logger.LogDebug($"Status reported as {JobStateEnum.Cancelled.ToString()} for {previewJob.Id}");
                    break;
                case TransformJobStatus.ERROR:
                    previewJob.State.Add(new PreviewJobState(JobStateEnum.Error, JobStateSourceEnum.TemplateGeneration));
                    _logger.LogDebug($"Status reported as {JobStateEnum.Error.ToString()} for {previewJob.Id}");
                    break;
            }

            CheckOverdue(previewJob);
        }

        /// <summary>
        /// Check to see if the job has taken too long to finish. If so, error out, and cancel the job to clean up.
        /// </summary>
        /// <param name="previewJob"></param>
        private void CheckOverdue(PreviewJob previewJob)
        {
            previewJob.State.Sort();
            PreviewJobState previewJobState = previewJob.State.Find(
               (previewJobState) =>
                {
                    return previewJobState.State == JobStateEnum.GeneratingTemplate;
                }
            );

            if (previewJobState == null)
            {
                _logger.LogError($"PreviewJob has not started template generation even when asked for status update. Cancelling: {previewJob.Id}");
                previewJob.State.Add(new PreviewJobState(JobStateEnum.Error, JobStateSourceEnum.TemplateGeneration));
                CancelJob(previewJob);
            }
            else
            {
                TimeSpan diff = DateTime.UtcNow.Subtract(previewJobState.DateSubmitted);

                if (diff.TotalMilliseconds >= _timeoutMills)
                {
                    _logger.LogError($"PreviewJob has not completed template generation, timed out! Cancelling: {previewJob.Id}");
                    previewJob.State.Add(new PreviewJobState(JobStateEnum.Error, JobStateSourceEnum.TemplateGeneration));
                    CancelJob(previewJob);
                }
            }
        }
    }
}