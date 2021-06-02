
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using TptMain.Models;
using TptMain.Util;
using static TptMain.Jobs.TransformService;

namespace TptMain.Jobs
{
    public class JobTaggedTextManager : IPreviewJobProcessor
    {
        /// <summary>
        /// Logger (injected).
        /// </summary>
        private readonly ILogger<JobTaggedTextManager> _logger;

        /// <summary>
        /// The service that's processing the transform job
        /// </summary>
        TransformService _transformService;

        private readonly int _timeoutMills;

        /// <summary>
        /// Constructor to pass in the logger
        /// </summary>
        /// <param name="logger"></param>
        public JobTaggedTextManager(
            ILoggerFactory loggerFactory,
            IConfiguration configuration)
        {
            _ = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _ = configuration ?? throw new ArgumentNullException(nameof(configuration));

            _logger = loggerFactory.CreateLogger<JobTaggedTextManager>();
            _transformService = new TransformService(_logger);

            // grab global settings for tagged text generation timeout

            _timeoutMills = 1000 * int.Parse(configuration[ConfigConsts.TaggedTextGenerationTimeoutInSecKey] ?? throw new ArgumentNullException(ConfigConsts
                                                                        .TaggedTextGenerationTimeoutInSecKey));
        }

        /// <summary>
        /// Start a job process
        /// </summary>
        /// <param name="previewJob"></param>
        public void ProcessJob(PreviewJob previewJob)
        {
            previewJob.State.Add(new PreviewJobState(JobStateEnum.GeneratingTaggedText, JobStateSourceEnum.TaggedTextGeneration));
            _transformService.GenerateTaggedText(previewJob);
        }

        /// <summary>
        /// Force a cancelation of the job
        /// </summary>
        /// <param name="previewJob"></param>
        public void CancelJob(PreviewJob previewJob)
        {
            previewJob.State.Add(new PreviewJobState(JobStateEnum.Cancelled, JobStateSourceEnum.TaggedTextGeneration));
            _transformService.CancelTransformJobs(previewJob.Id);
        }

        /// <summary>
        /// Get the current status of the job
        /// </summary>
        /// <param name="previewJob"></param>
        public void GetStatus(PreviewJob previewJob)
        {
            _logger.LogDebug($"Requesting status update for {previewJob.Id}");
            TransformJobStatus status = _transformService.GetTransformJobStatus(previewJob.Id);
            switch(status)
            {
                case TransformJobStatus.WAITING:
                case TransformJobStatus.PROCESSING:
                    previewJob.State.Add(new PreviewJobState(JobStateEnum.GeneratingTaggedText, JobStateSourceEnum.TaggedTextGeneration));
                    _logger.LogDebug($"Status reported as {JobStateEnum.GeneratingTaggedText.ToString()} for {previewJob.Id}");
                    break;
                case TransformJobStatus.TAGGED_TEXT_COMPLETE:
                    previewJob.State.Add(new PreviewJobState(JobStateEnum.GeneratingTaggedText, JobStateSourceEnum.TaggedTextGeneration));
                    _logger.LogDebug($"Status reported as {JobStateEnum.TaggedTextGenerated.ToString()} for {previewJob.Id}");
                    break;
                case TransformJobStatus.CANCELED:
                    previewJob.State.Add(new PreviewJobState(JobStateEnum.Cancelled, JobStateSourceEnum.TaggedTextGeneration));
                    _logger.LogDebug($"Status reported as {JobStateEnum.Cancelled.ToString()} for {previewJob.Id}");
                    break;
                case TransformJobStatus.ERROR:
                    previewJob.State.Add(new PreviewJobState(JobStateEnum.Error, JobStateSourceEnum.TaggedTextGeneration));
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
            delegate (PreviewJobState previewJobState)
                {
                    return previewJobState.State == JobStateEnum.GeneratingTaggedText;
                }
            );

            if(previewJobState == null)
            {
                _logger.LogError($"PreviewJob has not started tagged text generation even when asked for status update. Cancelling: {previewJob.Id}");
                previewJob.State.Add(new PreviewJobState(JobStateEnum.Error, JobStateSourceEnum.TaggedTextGeneration));
                CancelJob(previewJob);
            }
            else
            {
                DateTime baseDate = new DateTime(1970, 1, 1);
                long nowMills = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                long jobMills = (long)(previewJobState.DateSubmitted - baseDate).TotalMilliseconds;

                long timeSpent = nowMills - jobMills;

                if (timeSpent >= _timeoutMills)
                {
                    _logger.LogError($"PreviewJob has not completed tagged text generation, timed out! Cancelling: {previewJob.Id}");
                    previewJob.State.Add(new PreviewJobState(JobStateEnum.Error, JobStateSourceEnum.TaggedTextGeneration));
                    CancelJob(previewJob);
                }
            }
        }
    }
}