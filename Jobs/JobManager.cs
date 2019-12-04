using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using tools_tpt_transformation_service.InDesign;
using tools_tpt_transformation_service.Models;
using tools_tpt_transformation_service.Toolbox;
using tools_tpt_transformation_service.Util;

namespace tools_tpt_transformation_service.Jobs
{
    /// <summary>
    /// Job manager for handling typesetting preview job request management and execution.
    /// </summary>
    public partial class JobManager : IDisposable
    {
        /// <summary>
        /// Type-specific logger (injected).
        /// </summary>
        private readonly ILogger<JobManager> _logger;

        /// <summary>
        /// System configuration (injected).
        /// </summary>
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Job preview context (persistence; injected).
        /// </summary>
        private readonly PreviewContext _previewContext;

        /// <summary>
        /// IDS server script runner (injected).
        /// </summary>
        private readonly ScriptRunner _scriptRunner;

        /// <summary>
        /// Template manager.
        /// </summary>
        private readonly TemplateManager _templateManager;

        /// <summary>
        /// Job scheduler service (injected).
        /// </summary>
        private readonly JobScheduler _jobScheduler;

        /// <summary>
        /// Template (IDML) storage directory (configured).
        /// </summary>
        private readonly DirectoryInfo _idmlDirectory;

        /// <summary>
        /// Preview PDF storage directory (configured).
        /// </summary>
        private readonly DirectoryInfo _pdfDirectory;

        /// <summary>
        /// Max document (IDML, PDF) age, in seconds (configured).
        /// </summary>
        private readonly int _maxDocAgeInSec;

        /// <summary>
        /// Job expiration check timer.
        /// </summary>
        private readonly Timer _jobCheckTimer;

        /// <summary>
        /// PDF exipiration check timer.
        /// </summary>
        private readonly Timer _docCheckTimer;

        /// <summary>
        /// Template (IDML) storage directory.
        /// </summary>
        public DirectoryInfo IdmlDirectory { get => _idmlDirectory; }

        /// <summary>
        /// Preview PDF storage directory.
        /// </summary>
        public DirectoryInfo PdfDirectory { get => _pdfDirectory; }

        /// <summary>
        /// Basic ctor.
        /// </summary>
        /// <param name="logger">Type-specific logger (required).</param>
        /// <param name="configuration">System configuration (required).<param>
        /// <param name="previewContext">Job preview context (persistence; required).</param>
        /// <param name="scriptRunner">Script runner (required).</param>
        /// <param name="templateManager">Template manager (required).</param>
        /// <param name="jobScheduler">Job scheduler (required).</param>
        public JobManager(
            ILogger<JobManager> logger,
            IConfiguration configuration,
            PreviewContext previewContext,
            ScriptRunner scriptRunner,
            TemplateManager templateManager,
            JobScheduler jobScheduler)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _previewContext = previewContext ?? throw new ArgumentNullException(nameof(previewContext));
            _scriptRunner = scriptRunner ?? throw new ArgumentNullException(nameof(scriptRunner));
            _templateManager = templateManager ?? throw new ArgumentNullException(nameof(templateManager));
            _jobScheduler = jobScheduler ?? throw new ArgumentNullException(nameof(jobScheduler));

            _idmlDirectory = new DirectoryInfo(_configuration.GetValue<string>("Docs:IDML:Directory")
                ?? throw new ArgumentNullException("Docs:IDML:Directory"));
            _pdfDirectory = new DirectoryInfo(_configuration.GetValue<string>("Docs:PDF:Directory")
                ?? throw new ArgumentNullException("Docs:PDF:Directory"));
            _maxDocAgeInSec = int.Parse(_configuration.GetValue<string>("Docs:MaxAgeInSec")
                ?? throw new ArgumentNullException("Docs:MaxAgeInSec"));
            _jobCheckTimer = new Timer((stateObject) => { CheckPreviewJobs(); }, null,
                 TimeSpan.FromSeconds(MainConsts.TIMER_STARTUP_DELAY_IN_SEC),
                 TimeSpan.FromSeconds(_maxDocAgeInSec / MainConsts.MAX_AGE_CHECK_DIVISOR));
            _docCheckTimer = new Timer((stateObject) => { CheckDocFiles(); }, null,
                 TimeSpan.FromSeconds(MainConsts.TIMER_STARTUP_DELAY_IN_SEC),
                 TimeSpan.FromSeconds(_maxDocAgeInSec / MainConsts.MAX_AGE_CHECK_DIVISOR));

            if (!Directory.Exists(_pdfDirectory.FullName))
            {
                Directory.CreateDirectory(_pdfDirectory.FullName);
            }
            _logger.LogDebug("JobManager()");
        }

        /// <summary>
        /// Iterate through PDFs and clean up old ones.
        /// </summary>
        private void CheckPreviewJobs()
        {
            try
            {
                lock (_previewContext)
                {
                    _logger.LogDebug("Checking preview jobs...");

                    DateTime checkTime = DateTime.UtcNow.Subtract(TimeSpan.FromSeconds(_maxDocAgeInSec));
                    IList<PreviewJob> toRemove = new List<PreviewJob>();

                    foreach (PreviewJob jobItem in _previewContext.PreviewJobs)
                    {
                        DateTime? refTime = jobItem.DateCompleted
                            ?? jobItem.DateCancelled
                            ?? jobItem.DateStarted
                            ?? jobItem.DateSubmitted;

                        if (refTime != null
                            && refTime < checkTime)
                        {
                            toRemove.Add(jobItem);
                        }
                    }
                    if (toRemove.Count > 0)
                    {
                        _previewContext.PreviewJobs.RemoveRange(toRemove);
                        _previewContext.SaveChanges();
                    }

                    _logger.LogDebug("...Preview jobs checked.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Can't check preview jobs.");
            }
        }

        /// <summary>
        /// Iterate through preview files and clean up old ones.
        /// </summary>
        private void CheckDocFiles()
        {
            try
            {
                _logger.LogDebug("Checking document files...");
                DateTime checkTime = DateTime.UtcNow.Subtract(TimeSpan.FromSeconds(_maxDocAgeInSec));

                foreach (string fileItem in Directory.EnumerateFiles(_pdfDirectory.FullName, "preview-*.pdf"))
                {
                    FileInfo foundFile = new FileInfo(fileItem);
                    if (foundFile.CreationTimeUtc < checkTime)
                    {
                        try
                        {
                            foundFile.Delete();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, $"Can't delete PDF file (will retry): {fileItem}.");
                        }
                    }
                }
                foreach (string fileItem in Directory.EnumerateFiles(_idmlDirectory.FullName, "preview-*.idml"))
                {
                    FileInfo foundFile = new FileInfo(fileItem);
                    if (foundFile.CreationTimeUtc < checkTime)
                    {
                        try
                        {
                            foundFile.Delete();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, $"Can't delete IDML file (will retry): {fileItem}.");
                        }
                    }
                }

                _logger.LogDebug("...Document files checked.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Can't check document files.");
            }
        }

        /// <summary>
        /// Create and schedule a new preview job. 
        /// </summary>
        /// <param name="inputJob">Preview job to be added (required).</param>
        /// <param name="outputJob">Persisted preview job if created, null otherwise.</param>
        /// <returns>True if job created successfully, false otherwise.</returns>
        public bool TryAddJob(PreviewJob inputJob, out PreviewJob outputJob)
        {
            if (inputJob.Id != null
                || inputJob.ProjectName == null
                || inputJob.ProjectName.Any(charItem => !Char.IsLetterOrDigit(charItem)))
            {
                outputJob = null;
                return false;
            }
            this.InitPreviewJob(inputJob);

            lock (_previewContext)
            {
                _previewContext.PreviewJobs.Add(inputJob);
                _previewContext.SaveChanges();

                _jobScheduler.AddEntry(new JobWorkflow(_logger, this, _scriptRunner, _templateManager, inputJob));

                outputJob = inputJob;
                return true;
            }
        }

        /// <summary>
        /// Initializes a new preview job with defaults.
        /// </summary>
        /// <param name="previewJob">Preview job to initialize (required).</param>
        private void InitPreviewJob(PreviewJob previewJob)
        {
            // identifying information
            previewJob.Id = Guid.NewGuid().ToString();
            previewJob.IsError = false;
            previewJob.DateSubmitted = DateTime.UtcNow;
            previewJob.DateStarted = null;
            previewJob.DateCompleted = null;
            previewJob.DateCancelled = null;

            // project defaults
            previewJob.FontSizeInPts = previewJob.FontSizeInPts ?? MainConsts.DEFAULT_FONT_SIZE_IN_PTS;
            previewJob.FontLeadingInPts = previewJob.FontLeadingInPts ?? MainConsts.DEFAULT_FONT_LEADING_IN_PTS;
            previewJob.PageWidthInPts = previewJob.PageWidthInPts ?? MainConsts.DEFAULT_PAGE_WIDTH_IN_PTS;
            previewJob.PageHeightInPts = previewJob.PageHeightInPts ?? MainConsts.DEFAULT_PAGE_HEIGHT_IN_PTS;
            previewJob.PageHeaderInPts = previewJob.PageHeaderInPts ?? MainConsts.DEFAULT_PAGE_HEADER_IN_PTS;
            previewJob.BookFormat = previewJob.BookFormat ?? MainConsts.DEFAULT_BOOK_FORMAT;
        }

        /// <summary>
        /// Delete a preview job.
        /// </summary>
        /// <param name="jobId">Job ID to delete (required).</param>
        /// <param name="outputJob">The deleted job if found, null otherwise.</param>
        /// <returns>True if successful, false otherwise.</returns>
        public bool TryDeleteJob(string jobId, out PreviewJob outputJob)
        {
            lock (_previewContext)
            {
                if (TryGetJob(jobId, out PreviewJob foundJob))
                {
                    _jobScheduler.RemoveEntry(foundJob.Id);

                    _previewContext.PreviewJobs.Remove(foundJob);
                    _previewContext.SaveChanges();

                    outputJob = foundJob;
                    return true;
                }
                else
                {
                    outputJob = null;
                    return false;
                }
            }
        }

        /// <summary>
        /// Update preview job.
        /// </summary>
        /// <param name="nextJob">Preview job to update (required).</param>
        /// <returns>True if successful, false otherwise.</returns>
        public bool TryUpdateJob(PreviewJob nextJob)
        {
            lock (_previewContext)
            {
                if (TryGetJob(nextJob.Id, out PreviewJob prevJob))
                {
                    _previewContext.Entry(nextJob).State = EntityState.Modified;
                    _previewContext.SaveChanges();

                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Retrieves a preview job.
        /// </summary>
        /// <param name="jobId">Job ID to retrieve (required).</param>
        /// <param name="previewJob">Retrieved preview job if found, null otherwise.</param>
        /// <returns>True if successful, false otherwise.</returns>
        public bool TryGetJob(String jobId, out PreviewJob previewJob)
        {
            lock (_previewContext)
            {
                previewJob = _previewContext.PreviewJobs.Find(jobId);
                return (previewJob != null);
            }
        }

        /// <summary>
        /// Retrieves file stream for preview job PDF.
        /// </summary>
        /// <param name="jobId">Job ID of preview to retrieve file for.</param>
        /// <param name="fileStream">File stream if found, null otherwise.</param>
        /// <returns>True if successful, false otherwise.</returns>
        public bool TryGetPreviewStream(String jobId, out FileStream fileStream)
        {
            lock (_previewContext)
            {
                if (!TryGetJob(jobId, out PreviewJob previewJob))
                {
                    fileStream = null;
                    return false;
                }
                else
                {
                    try
                    {
                        fileStream = File.Open(
                            Path.Combine(_pdfDirectory.FullName, $"preview-{previewJob.Id}.pdf"),
                            FileMode.Open, FileAccess.Read);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Can't open file for job: {jobId}");

                        fileStream = null;
                        return false;
                    }
                }
            }
        }

        /// <summary>
        /// Disposes of class resources.
        /// </summary>
        public void Dispose()
        {
            _jobScheduler.Dispose();
            _jobCheckTimer.Dispose();
            _docCheckTimer.Dispose();
        }
    }
}