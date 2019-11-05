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
using tools_tpt_transformation_service.Util;

namespace tools_tpt_transformation_service.Jobs
{
    /// <summary>
    /// Job manager for handling typesetting preview job request management and execution.
    /// </summary>
    public partial class JobManager : IDisposable
    {
        private readonly ILogger<JobManager> _logger;
        private readonly IConfiguration _configuration;
        private readonly PreviewContext _previewContext;
        private readonly ScriptRunner _scriptRunner;
        private readonly JobScheduler _jobScheduler;
        private readonly DirectoryInfo _pdfDirectory;
        private readonly int _maxPdfAgeInSec;
        private readonly Timer _jobCheckTimer;
        private readonly Timer _pdfCheckTimer;

        /// <summary>
        /// Constructor. Built using .NETs dependency injection.
        /// </summary>
        /// <param name="logger">Logger.</param>
        /// <param name="configuration">Service configuration object.</param>
        /// <param name="previewContext">Preview DB Context.</param>
        /// <param name="scriptRunner">InDesign script runner.</param>
        /// <param name="jobScheduler"></param>
        public JobManager(
            ILogger<JobManager> logger,
            IConfiguration configuration,
            PreviewContext previewContext,
            ScriptRunner scriptRunner,
            JobScheduler jobScheduler)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _previewContext = previewContext ?? throw new ArgumentNullException(nameof(previewContext));
            _scriptRunner = scriptRunner ?? throw new ArgumentNullException(nameof(scriptRunner));
            _jobScheduler = jobScheduler ?? throw new ArgumentNullException(nameof(jobScheduler));

            _pdfDirectory = new DirectoryInfo(_configuration.GetValue<string>("PDF.Directory") ?? "C:\\Work\\Output");
            _maxPdfAgeInSec = int.Parse(_configuration.GetValue<string>("PDF.MaxAgeInSec") ?? "86400");
            _jobCheckTimer = new Timer((stateObject) => { CheckPreviewJobs(); }, null,
                TimeSpan.FromSeconds(60.0),
                TimeSpan.FromSeconds(_maxPdfAgeInSec / 10.0));
            _pdfCheckTimer = new Timer((stateObject) => { CheckPdfFiles(); }, null,
                TimeSpan.FromSeconds(60.0),
                TimeSpan.FromSeconds(_maxPdfAgeInSec / 10.0));

            if (!Directory.Exists(_pdfDirectory.FullName))
            {
                Directory.CreateDirectory(_pdfDirectory.FullName);
            }
            _logger.LogDebug("JobManager()");
        }

        /// <summary>
        /// Iterate through jobs and clean up old ones.
        /// </summary>
        private void CheckPreviewJobs()
        {
            try
            {
                lock (_previewContext)
                {
                    _logger.LogDebug("Checking preview jobs...");

                    DateTime checkTime = DateTime.UtcNow.Subtract(TimeSpan.FromSeconds(_maxPdfAgeInSec));
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
        private void CheckPdfFiles()
        {
            try
            {
                _logger.LogDebug("Checking PDF files...");

                DateTime checkTime = DateTime.UtcNow.Subtract(TimeSpan.FromSeconds(_maxPdfAgeInSec));
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
                            _logger.LogWarning(ex, $"Can't delete preview file (will retry): {fileItem}.");
                        }
                    }
                }

                _logger.LogDebug("...PDF files checked.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Can't check PDF files.");
            }
        }

        /// <summary>
        /// Initiate and schedule a new preview job. 
        /// </summary>
        /// <param name="inputJob">The job to be initiated.</param>
        /// <param name="outputJob">Set to the initiated job if successful, otherwise null.</param>
        /// <returns>True if job initiated successfully, false otherwise.</returns>
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

                _jobScheduler.AddEntry(new SchedulerEntry(_logger, this, _scriptRunner, inputJob));

                outputJob = inputJob;
                return true;
            }
        }

        /// <summary>
        /// Creates an initiated <c>PreviewJob</c> with expected initial values.
        /// </summary>
        /// <param name="previewJob"><c>PreviewJob</c> to initiate.</param>
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
        /// <param name="jobId">The job to be initiated.</param>
        /// <param name="outputJob">The deleted job if successful, otherwise null.</param>
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
        /// Update a preview job.
        /// </summary>
        /// <param name="nextJob">Preview job to update with.</param>
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
        /// Retrieve a job.
        /// </summary>
        /// <param name="jobId">ID of preview job to retrieve.</param>
        /// <param name="previewJob">The retrieve retrieve job if successful, null otherwise.</param>
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
        /// Get <c>FileStream</c> for preview file retrieval.
        /// </summary>
        /// <param name="jobId">ID of preview  file to retrieve.</param>
        /// <param name="fileStream"><c>FileStream</c> if successful, otherwise null.</param>
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
            _pdfCheckTimer.Dispose();
        }
    }
}