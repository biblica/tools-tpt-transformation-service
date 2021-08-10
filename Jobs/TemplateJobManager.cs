
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
        /// Constructor to pass in the logger, config, and the transform service...
        /// Used for passing in a mocked transform service in tests
        /// </summary>
        /// <param name="logger">The factory to create the localized logger</param>
        /// <param name="configuration">The set of configuration parameters</param>
        /// <param name="transformService">Injected service instance</param>
        public TemplateJobManager(
            ILogger<TemplateJobManager> logger,
            IConfiguration configuration,
            TransformService transformService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _ = configuration ?? throw new ArgumentNullException(nameof(configuration));

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

            /// This adds another state record to the list of states every time the status is checked. There may be multiple
            /// of the same state with different timestamps to show that the processs is still working
            switch (status)
            {
                case TransformJobStatus.WAITING:
                case TransformJobStatus.PROCESSING:
                    _logger.LogDebug($"Status reported as {JobStateEnum.GeneratingTemplate} for {previewJob.Id}");
                    break;
                case TransformJobStatus.TEMPLATE_COMPLETE:
                    previewJob.State.Add(new PreviewJobState(JobStateEnum.TemplateGenerated, JobStateSourceEnum.TemplateGeneration));
                    _logger.LogDebug($"Status reported as {JobStateEnum.TemplateGenerated} for {previewJob.Id}");
                    break;
                case TransformJobStatus.CANCELED:
                    previewJob.State.Add(new PreviewJobState(JobStateEnum.Cancelled, JobStateSourceEnum.TemplateGeneration));
                    _logger.LogDebug($"Status reported as {JobStateEnum.Cancelled} for {previewJob.Id}");
                    break;
                case TransformJobStatus.ERROR:
                    var errorMessage = $"Status reported as {JobStateEnum.Error} for {previewJob.Id}";
                    _logger.LogDebug(errorMessage);
                    previewJob.SetError(errorMessage, null, JobStateSourceEnum.TemplateGeneration);
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
                var errorMessage = $"PreviewJob has not started Template generation even when asked for status update. Cancelling: {previewJob.Id}";
                _logger.LogError(errorMessage);
                previewJob.SetError("Template generation error.", errorMessage, JobStateSourceEnum.TemplateGeneration);
                _transformService.CancelTransformJobs(previewJob.Id);
            }
            else
            {
                TimeSpan diff = DateTime.UtcNow.Subtract(previewJobState.DateSubmitted);

                if (diff.TotalMilliseconds >= _timeoutMills)
                {
                    var errorMessage = $"The Template generation timed out on job {previewJob.Id} - timeout:{_timeoutMills}, diff:{diff}";
                    _logger.LogError(errorMessage);
                    previewJob.SetError("Template generation error.", errorMessage, JobStateSourceEnum.TemplateGeneration);
                    _transformService.CancelTransformJobs(previewJob.Id);
                }
            }
        }
    }
}