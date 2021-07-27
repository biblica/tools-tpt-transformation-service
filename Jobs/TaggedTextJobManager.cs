
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using TptMain.Models;
using TptMain.Util;
using static TptMain.Jobs.TransformService;

namespace TptMain.Jobs
{
    public class TaggedTextJobManager : IPreviewJobProcessor
    {
        /// <summary>
        /// Logger (injected).
        /// </summary>
        private readonly ILogger<TaggedTextJobManager> _logger;

        /// <summary>
        /// The service that's processing the transform job
        /// </summary>
        TransformService _transformService;

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
        public TaggedTextJobManager(
            ILogger<TaggedTextJobManager> logger,
            IConfiguration configuration,
            TransformService transformService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _ = configuration ?? throw new ArgumentNullException(nameof(configuration));

            /// this is the only difference for this constructor
            _transformService = transformService;

            // grab global settings for tagged text generation timeout

            _timeoutMills = 1000 * int.Parse(configuration[ConfigConsts.TaggedTextGenerationTimeoutInSecKey] ?? throw new ArgumentNullException(ConfigConsts
                                                                        .TaggedTextGenerationTimeoutInSecKey));
        }

        /// <summary>
        /// Start a job process
        /// </summary>
        /// <param name="previewJob">The job to be worked</param>
        public void ProcessJob(PreviewJob previewJob)
        {
            previewJob.State.Add(new PreviewJobState(JobStateEnum.GeneratingTaggedText, JobStateSourceEnum.TaggedTextGeneration));
            _transformService.GenerateTaggedText(previewJob);
        }

        /// <summary>
        /// Force a cancelation of the job
        /// </summary>
        /// <param name="previewJob">The job to cancel</param>
        public void CancelJob(PreviewJob previewJob)
        {
            previewJob.State.Add(new PreviewJobState(JobStateEnum.Cancelled, JobStateSourceEnum.TaggedTextGeneration));
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
                    previewJob.State.Add(new PreviewJobState(JobStateEnum.GeneratingTaggedText, JobStateSourceEnum.TaggedTextGeneration));
                    _logger.LogDebug($"Status reported as {JobStateEnum.GeneratingTaggedText} for {previewJob.Id}");
                    break;
                case TransformJobStatus.TAGGED_TEXT_COMPLETE:
                    previewJob.State.Add(new PreviewJobState(JobStateEnum.GeneratingTaggedText, JobStateSourceEnum.TaggedTextGeneration));
                    _logger.LogDebug($"Status reported as {JobStateEnum.TaggedTextGenerated} for {previewJob.Id}");
                    break;
                case TransformJobStatus.CANCELED:
                    previewJob.State.Add(new PreviewJobState(JobStateEnum.Cancelled, JobStateSourceEnum.TaggedTextGeneration));
                    _logger.LogDebug($"Status reported as {JobStateEnum.Cancelled} for {previewJob.Id}");
                    break;
                case TransformJobStatus.ERROR:
                    previewJob.State.Add(new PreviewJobState(JobStateEnum.Error, JobStateSourceEnum.TaggedTextGeneration));
                    _logger.LogDebug($"Status reported as {JobStateEnum.Error} for {previewJob.Id}");
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
                    return previewJobState.State == JobStateEnum.GeneratingTaggedText;
                }
            );

            if (previewJobState == null)
            {
                _logger.LogError($"PreviewJob has not started TaggedText generation even when asked for status update. Cancelling: {previewJob.Id}");
                previewJob.State.Add(new PreviewJobState(JobStateEnum.Error, JobStateSourceEnum.TaggedTextGeneration));

                previewJob.SetError("TaggedText generation error.", "Could not get state indicating that the TaggedText generation had been started.");
                _transformService.CancelTransformJobs(previewJob.Id);
            }
            else
            {
                TimeSpan diff = DateTime.UtcNow.Subtract(previewJobState.DateSubmitted);

                if (diff.TotalMilliseconds >= _timeoutMills)
                {
                    _logger.LogError($"PreviewJob has not completed TaggedText generation, timed out! Cancelling: {previewJob.Id}");
                    previewJob.State.Add(new PreviewJobState(JobStateEnum.Error, JobStateSourceEnum.TaggedTextGeneration));

                    previewJob.SetError("TaggedText generation error, timed-out", $"The TaggedText generation timed out - timeout:{_timeoutMills}, diff:{diff}");
                    _transformService.CancelTransformJobs(previewJob.Id);
                }
            }
        }
    }
}