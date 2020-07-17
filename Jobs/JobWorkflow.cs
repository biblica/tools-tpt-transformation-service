using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using TptMain.Exceptions;
using TptMain.InDesign;
using TptMain.Models;
using TptMain.ParatextProjects;
using TptMain.Toolbox;
using TptMain.Util;

namespace TptMain.Jobs
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
        /// Paratext API service used to authorize user access.
        /// </summary>
        private readonly ParatextApi _paratextApi;

        /// <summary>
        /// Paratext Project service used to get information related to local Paratext projects.
        /// </summary>
        private readonly ParatextProjectService _paratextProjectService;

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
        public PreviewJob Job => _previewJob;

        /// <summary>
        /// Cancellation token accessor.
        /// </summary>
        public CancellationTokenSource CancellationTokenSource => _cancellationTokenSource;

        /// <summary>
        /// Basic ctor.
        /// </summary>
        /// <param name="logger">Type-specific logger (required).</param>
        /// <param name="jobManager">Job manager constructing this entry (required).</param>
        /// <param name="scriptRunner">Script runner for IDS calls (required).</param>
        /// <param name="templateManager">Template manager for IDML retrieval (required).</param>
        /// <param name="paratextApi">Paratext API for verifiying user authorization on projects (required).</param>
        /// <param name="paratextProjectService">Paratext Project service for getting information related to local Paratext projects. (required).</param>
        /// <param name="previewJob">Job to be executed (required).</param>
        public JobWorkflow(
            ILogger<JobManager> logger,
            JobManager jobManager,
            ScriptRunner scriptRunner,
            TemplateManager templateManager,
            ParatextApi paratextApi,
            ParatextProjectService paratextProjectService,
            PreviewJob previewJob
            )
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
            _scriptRunner = scriptRunner ?? throw new ArgumentNullException(nameof(scriptRunner));
            _templateManager = templateManager ?? throw new ArgumentNullException(nameof(templateManager));
            _previewJob = previewJob ?? throw new ArgumentNullException(nameof(previewJob));
            _paratextApi = paratextApi ?? throw new ArgumentNullException(nameof(paratextApi));
            _paratextProjectService = paratextProjectService ?? throw new ArgumentNullException(nameof(paratextProjectService));

            _cancellationTokenSource = new CancellationTokenSource();
            _logger.LogDebug("JobEntry()");
        }

        /// <summary>
        /// Execute the associated job.
        /// </summary>
        public virtual void RunJob()
        {
            try
            {
                _logger.LogInformation($"Job started: {_previewJob.Id}");
                _previewJob.DateStarted = DateTime.UtcNow;
                _jobManager.TryUpdateJob(_previewJob);

                if (!IsJobCanceled)
                {
                    _paratextApi.IsUserAuthorizedOnProject(_previewJob);
                }

                // Grab the project's footnote markers if configured to do so.
                string[] customFootnoteMarkers = null;
                if (!IsJobCanceled && _previewJob.UseCustomFootnotes)
                {
                    customFootnoteMarkers = _paratextProjectService.GetFootnoteCallerSequence(_previewJob.ProjectName);
                    // Throw an error, if custom footnotes are requested but are not available.
                    // This allows us to set the user's expectations early, rather than waiting
                    // for a preview.
                    if (customFootnoteMarkers == null || customFootnoteMarkers.Length == 0)
                    {
                        throw new PreviewJobException(_previewJob, "Custom footnotes requested, but aren't specified in the project.");
                    }

                    _logger.LogInformation("Custom footnotes requested and found. Custom footnotes: " + String.Join(", ", customFootnoteMarkers));
                }

                if (!IsJobCanceled)
                {
                    _templateManager.DownloadTemplateFile(_previewJob,
                        new FileInfo(Path.Combine(_jobManager.IdmlDirectory.FullName,
                            $"{MainConsts.PREVIEW_FILENAME_PREFIX}{_previewJob.Id}.idml")),
                        _cancellationTokenSource.Token);
                }

                if (!IsJobCanceled)
                {
                    _scriptRunner.RunScript(_previewJob, 
                        customFootnoteMarkers,
                        _cancellationTokenSource.Token);
                }

                _logger.LogInformation($"Job finished: {_previewJob.Id}.");
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogDebug(ex, $"Can't run job: {_previewJob.Id} (cancelled, ignoring).");
            }
            catch (PreviewJobException ex)
            {
                _logger.LogWarning($"Can't run job: {ex}");
                _previewJob.SetError("Can't generate preview.", ex.Message);
            }
            catch (Exception ex)
            {
                _previewJob.SetError("An internal server error occurred.", ex.Message);
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
        public virtual void CancelJob()
        {
            try
            {
                _logger.LogInformation($"Canceling job: {_previewJob.Id}");
                _cancellationTokenSource.Cancel();
                _logger.LogInformation($"Job canceled: {_previewJob.Id}");
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogDebug(ex, $"Can't cancel job: {_previewJob.Id} (cancelled, ignoring).");
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
        public virtual bool IsJobCanceled => _cancellationTokenSource.IsCancellationRequested;
    }
}