using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using tools_tpt_transformation_service.InDesign;
using tools_tpt_transformation_service.Models;

namespace tools_tpt_transformation_service.Jobs
{
    /// <summary>
    /// Facilitates the execution of scheduled preview jobs scheduled by the <c>JobScheduler</c>
    /// </summary>
    public class SchedulerEntry
    {
        private readonly ILogger<JobManager> _logger;
        private readonly JobManager _jobManager;
        private readonly ScriptRunner _scriptRunner;
        private readonly PreviewJob _job;
        private readonly CancellationTokenSource _cancellationTokenSource;

        public PreviewJob Job => _job;
        public CancellationTokenSource CancellationTokenSource => _cancellationTokenSource;

        /// <summary>
        /// Constructor. 
        /// </summary>
        /// <param name="logger">Logger.</param>
        /// <param name="jobManager">JobManager that constructed this entry.</param>
        /// <param name="scriptRunner">ScriptRunner for calls to InDesign server.</param>
        /// <param name="job">The job to be executed.</param>
        public SchedulerEntry(
            ILogger<JobManager> logger,
            JobManager jobManager,
            ScriptRunner scriptRunner,
            PreviewJob job)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
            _scriptRunner = scriptRunner ?? throw new ArgumentNullException(nameof(scriptRunner));
            _job = job ?? throw new ArgumentNullException(nameof(job));

            _cancellationTokenSource = new CancellationTokenSource();
            _logger.LogDebug("JobEntry()");
        }

        /// <summary>
        /// Execute the associated job.
        /// </summary>
        public void RunJob()
        {
            try
            {
                _logger.LogInformation($"Job started: {_job.Id}");
                _job.DateStarted = DateTime.UtcNow;
                _jobManager.TryUpdateJob(_job);

                Task jobTask = Task.Run(() =>
                {
                    return _scriptRunner.RunScriptAsync(_job);
                },
                _cancellationTokenSource.Token);
                jobTask.Wait();

                _logger.LogInformation($"Job finsihed: {_job.Id}, status: {jobTask.Status}");
            }
            catch (Exception ex)
            {
                _job.IsError = true;
                _logger.LogWarning(ex, $"Can't run job: {_job.Id}");
            }
            finally
            {
                _job.DateCompleted = DateTime.UtcNow;
                _jobManager.TryUpdateJob(_job);
            }
        }

        /// <summary>
        /// Attempt cancellation of a job's execution.
        /// </summary>
        public void CancelJob()
        {
            try
            {
                _logger.LogInformation($"Canceling job: {_job.Id}");
                _cancellationTokenSource.Cancel();
                _logger.LogInformation($"Job canceled: {_job.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Can't cancel job: {_job.Id}");
            }
            finally
            {
                _job.DateCancelled = DateTime.UtcNow;
                _jobManager.TryUpdateJob(_job);
            }
        }

        /// <summary>
        /// Whether or not a job is cancelled.
        /// </summary>
        /// <returns>True if job canceled, false otherwise.</returns>
        public bool IsJobCanceled()
        {
            return _cancellationTokenSource.IsCancellationRequested;
        }
    }
}