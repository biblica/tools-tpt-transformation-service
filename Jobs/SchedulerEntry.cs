using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using tools_tpt_transformation_service.InDesign;
using tools_tpt_transformation_service.Models;

namespace tools_tpt_transformation_service.Jobs
{
    public class SchedulerEntry
    {
        private readonly ILogger<JobManager> _logger;
        private readonly JobManager _jobManager;
        private readonly ScriptRunner _scriptRunner;
        private readonly PreviewJob _job;
        private readonly CancellationTokenSource _cancellationTokenSource;

        public PreviewJob Job => _job;
        public CancellationTokenSource CancellationTokenSource => _cancellationTokenSource;

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

        public void RunJob()
        {
            try
            {
                _logger.LogInformation($"Job started: {_job.Id}");
                _job.DateStarted = DateTime.UtcNow;
                _jobManager.TryUpdateJob(_job);

                Task scriptTask = Task.Run(
                    () => _scriptRunner.RunScriptAsync(_job),
                    _cancellationTokenSource.Token);
                scriptTask.Wait();

                _logger.LogInformation($"Job finsihed: {_job.Id}, status: {scriptTask.Status}");
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

        public bool IsJobCanceled()
        {
            return _cancellationTokenSource.IsCancellationRequested;
        }
    }
}