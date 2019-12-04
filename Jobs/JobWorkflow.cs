using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using tools_tpt_transformation_service.InDesign;
using tools_tpt_transformation_service.Models;
using tools_tpt_transformation_service.Toolbox;

namespace tools_tpt_transformation_service.Jobs
{
    /// <summary>
    /// Facilitates execution of a single preview job.
    /// </summary>
    public class JobWorkflow
    {
        /// <summary>
        /// Type-specific logger.
        /// </summary>
        private readonly ILogger<JobManager> _logger;

        /// <summary>
        /// Job manager.
        /// </summary>
        private readonly JobManager _jobManager;

        /// <summary>
        /// Script runner.
        /// </summary>
        private readonly ScriptRunner _scriptRunner;

        /// <summary>
        /// Template manager.
        /// </summary>
        private readonly TemplateManager _templateManager;

        /// <summary>
        /// Preview job.
        /// </summary>
        private readonly PreviewJob _previewJob;

        /// <summary>
        /// Cancellation token, for aborting jobs in progress.
        /// </summary>
        private readonly CancellationTokenSource _cancellationTokenSource;

        /// <summary>
        /// Preview job accessor.
        /// </summary>
        public PreviewJob Job { get => _previewJob; }

        /// <summary>
        /// Cancellation token accessor.
        /// </summary>
        public CancellationTokenSource CancellationTokenSource { get => _cancellationTokenSource; }

        /// <summary>
        /// Basic ctor. 
        /// </summary>
        /// <param name="logger">Type-specific logger (required).</param>
        /// <param name="jobManager">Job manager constructing this entry (required).</param>
        /// <param name="scriptRunner">Script runner for IDS calls (required).</param>
        /// <param name="templateManager">Template manager for IDML retrieval (required).</param>
        /// <param name="previewJob">Job to be executed (required).</param>
        public JobWorkflow(
            ILogger<JobManager> logger,
            JobManager jobManager,
            ScriptRunner scriptRunner,
            TemplateManager templateManager,
            PreviewJob previewJob)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
            _scriptRunner = scriptRunner ?? throw new ArgumentNullException(nameof(scriptRunner));
            _templateManager = templateManager ?? throw new ArgumentNullException(nameof(templateManager));
            _previewJob = previewJob ?? throw new ArgumentNullException(nameof(previewJob));

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
                _logger.LogInformation($"Job started: {_previewJob.Id}");
                _previewJob.DateStarted = DateTime.UtcNow;
                _jobManager.TryUpdateJob(_previewJob);


                TaskStatus? templateStatus = null;
                if (!IsJobCanceled)
                {
                    Task idmlTask = Task.Run(() =>
                    {
                        _templateManager.GetTemplateFile(_previewJob,
                            new FileInfo(Path.Combine(_jobManager.IdmlDirectory.FullName, $"preview-{_previewJob.Id}.idml")));
                    },
                    _cancellationTokenSource.Token);

                    idmlTask.Wait();
                    templateStatus = idmlTask.Status;
                }

                TaskStatus? pdfStatus = null;
                if (!IsJobCanceled)
                {
                    Task pdfTask = Task.Run(() =>
                    {
                        return _scriptRunner.RunScriptAsync(_previewJob);
                    },
                    _cancellationTokenSource.Token);

                    pdfTask.Wait();
                    pdfStatus = pdfTask.Status;
                }

                _logger.LogInformation($"Job finsihed: {_previewJob.Id} (IDML status: {templateStatus}, PDF status: {pdfStatus}).");
            }
            catch (Exception ex)
            {
                _previewJob.IsError = true;
                _logger.LogWarning(ex, $"Can't run job: {_previewJob.Id}");
            }
            finally
            {
                _previewJob.DateCompleted = DateTime.UtcNow;
                _jobManager.TryUpdateJob(_previewJob);
            }
        }

        /// <summary>
        /// Attempt cancellation of a job's execution.
        /// </summary>
        public void CancelJob()
        {
            try
            {
                _logger.LogInformation($"Canceling job: {_previewJob.Id}");
                _cancellationTokenSource.Cancel();
                _logger.LogInformation($"Job canceled: {_previewJob.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Can't cancel job: {_previewJob.Id}");
            }
            finally
            {
                _previewJob.DateCancelled = DateTime.UtcNow;
                _jobManager.TryUpdateJob(_previewJob);
            }
        }

        /// <summary>
        /// Whether or not a job is cancelled.
        /// </summary>
        /// <returns>True if job canceled, false otherwise.</returns>
        public bool IsJobCanceled
        {
            get => _cancellationTokenSource.IsCancellationRequested;
        }
    }
}