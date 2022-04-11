/*
Copyright © 2021 by Biblica, Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using TptMain.Models;
using TptMain.Text;
using TptMain.Util;

namespace TptMain.Jobs
{
    /// <summary>
    /// Job manager for handling typesetting preview job request management and execution.
    /// </summary>
    public class JobManager : IDisposable, IJobManager
    {
        /// <summary>
        /// Type-specific logger (injected).
        /// </summary>
        private readonly ILogger<JobManager> _logger;

        /// <summary>
        /// Job preview context (persistence; injected).
        /// </summary>
        private readonly TptServiceContext _tptServiceContext;

        /// <summary>
        /// Job File Manager (injected).
        /// </summary>
        private readonly JobFileManager _jobFileManager;

        /// <summary>
        /// Job Validation service for ensuring feasible and authorized jobs (injected).
        /// </summary>
        private readonly IPreviewJobValidator _jobValidator;

        /// <summary>
        /// Template manager (injected).
        /// </summary>
        private readonly TemplateJobManager _templateManager;

        /// <summary>
        /// Tagged text manager (injected).
        /// </summary>
        private readonly TaggedTextJobManager _taggedTextManager;

        /// <summary>
        /// Preview Manager (injected).
        /// </summary>
        private readonly IPreviewManager _previewManager;

        /// <summary>
        /// Max document (IDML, PDF) age, in seconds (configured).
        /// </summary>
        private readonly int _maxDocAgeInSec;

        /// <summary>
        /// Check timer and files
        /// </summary>
        private readonly Timer _docCheckTimer;

        /// <summary>
        /// Interval in between job processing functions run.
        /// </summary>
        private readonly int _jobProcessIntervalInSec;

        /// <summary>
        /// Check timer for jobs
        /// </summary>
        private readonly Timer _processRunTimer;

        /// <summary>
        /// This dictionary maps the state that will determine which processor will process a job next.
        /// </summary>
        private Dictionary<JobStateEnum, IPreviewJobProcessor> StateToProcessorProcessMap { get; set; }

        /// <summary>
        /// This dictionary maps the state that will determine which processor will update the job next.
        /// </summary>
        private Dictionary<JobStateEnum, IPreviewJobProcessor> StateToProcessorUpdateMap { get; set; }

        // Cache of jobs as they'll be updated repeatedly in processors.
        private Dictionary<string, PreviewJob> PreviewJobs { get; set; }

        /// <summary>
        /// Basic ctor.
        /// </summary>
        /// <param name="logger">Type-specific logger (required).</param>
        /// <param name="configuration">System configuration (required).</param>
        /// <param name="tptServiceContext">Database context (persistence; required).</param>
        /// <param name="jobFileManager">Job File Manager (required).</param>
        /// <param name="jobValidator">Preview Job Validator used for ensuring job feasibility and authorization on projects (required).</param>
        /// <param name="templateManager">Template manager (required).</param>
        /// <param name="taggedTextManager">Tagged Text manager (required).</param>
        /// <param name="previewManager">Preview manager (required).</param>
        public JobManager(
            ILogger<JobManager> logger,
            IConfiguration configuration,
            TptServiceContext tptServiceContext,
            JobFileManager jobFileManager,
            IPreviewJobValidator jobValidator,
            TemplateJobManager templateManager,
            TaggedTextJobManager taggedTextManager,
            IPreviewManager previewManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _ = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _tptServiceContext = tptServiceContext ?? throw new ArgumentNullException(nameof(tptServiceContext));
            _jobFileManager = jobFileManager ?? throw new ArgumentNullException(nameof(jobFileManager));
            _jobValidator = jobValidator ?? throw new ArgumentNullException(nameof(jobValidator));
            _templateManager = templateManager ?? throw new ArgumentNullException(nameof(templateManager));
            _taggedTextManager = taggedTextManager ?? throw new ArgumentNullException(nameof(taggedTextManager));
            _previewManager = previewManager ?? throw new ArgumentNullException(nameof(previewManager));

            _maxDocAgeInSec = int.Parse(configuration[ConfigConsts.MaxDocAgeInSecKey]
                                        ?? throw new ArgumentNullException(ConfigConsts.MaxDocAgeInSecKey));
            _jobProcessIntervalInSec = int.Parse(configuration[ConfigConsts.JobProcessIntervalInSecKey]
                                        ?? throw new ArgumentNullException(ConfigConsts.JobProcessIntervalInSecKey));

            _docCheckTimer = new Timer((stateObject) => { CheckDocFiles(); },
                null,
                 TimeSpan.FromSeconds(MainConsts.TIMER_STARTUP_DELAY_IN_SEC),
                 TimeSpan.FromSeconds(_maxDocAgeInSec));
            _processRunTimer = new Timer((stateObject) => { ProcessJobs(); },
                null,
                 TimeSpan.FromSeconds(MainConsts.TIMER_STARTUP_DELAY_IN_SEC),
                 TimeSpan.FromSeconds(_jobProcessIntervalInSec));

            // map the events that kick them off to their respective processors
            StateToProcessorProcessMap = new Dictionary<JobStateEnum, IPreviewJobProcessor>()
            {
                { JobStateEnum.Submitted, _jobValidator },
                { JobStateEnum.Started, _templateManager },
                { JobStateEnum.TemplateGenerated, _taggedTextManager },
                { JobStateEnum.TaggedTextGenerated, _previewManager }
            };
            StateToProcessorUpdateMap = new Dictionary<JobStateEnum, IPreviewJobProcessor>()
            {
                { JobStateEnum.GeneratingTemplate, _templateManager },
                { JobStateEnum.GeneratingTaggedText, _taggedTextManager },
                { JobStateEnum.GeneratingPreview, _previewManager }
            };

            // grab and cache the jobs from the database
            if (TryGetJobs(out var previewJobs))
            {
                PreviewJobs = previewJobs.ToDictionary(
                    job => job.Id,
                    job => job
                );
            }
            else
            {
                PreviewJobs = new Dictionary<string, PreviewJob>();
            }

            _logger.LogDebug("JobManager() initialized");
        }

        /// <summary>
        /// This function handles the processing of TPT jobs, and delgating work to individual managers based on state.
        /// </summary>
        public virtual void ProcessJobs()
        {
            _logger.LogDebug("JobManager.ProcessJobs()");

            // handle initial processing
            foreach (var test in StateToProcessorProcessMap)
            {
                var initiationState = test.Key;
                var handlingProcessor = test.Value;

                if (TryGetJobsByCurrentState(initiationState, out var previewJobsByState))
                {
                    previewJobsByState.ForEach(previewJob =>
                    {
                        if (!IsJobTerminated(previewJob))
                        {
                            handlingProcessor.ProcessJob(previewJob);
                        }
                    });
                }
            }

            // handle follow-on updates
            foreach (var test in StateToProcessorUpdateMap)
            {
                var initiationState = test.Key;
                var handlingProcessor = test.Value;

                if (TryGetJobsByCurrentState(initiationState, out var previewJobsByState))
                {
                    previewJobsByState.ForEach(previewJob =>
                    {
                        if (!IsJobTerminated(previewJob))
                        {
                            handlingProcessor.GetStatus(previewJob);
                        }
                    });
                }
            }

            // update all jobs against the DB.
            if (TryGetJobs(out var previewJobs))
            {
                previewJobs.ForEach(job =>
                {
                    TryUpdateJob(job);
                });
            }
        }

        /// <summary>
        /// Iterate through generated preview job directories and clean up old ones.
        /// </summary>
        private void CheckDocFiles()
        {
            try
            {
                _logger.LogDebug("Checking job directories...");

                var checkTime = DateTime.UtcNow.Subtract(TimeSpan.FromSeconds(_maxDocAgeInSec));

                foreach (var jobDirectory in _jobFileManager.JobFilesRootDir.GetDirectories())
                {
                    if (jobDirectory.CreationTimeUtc < checkTime)
                    {
                        try
                        {
                            jobDirectory.Delete(true);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, $"Can't delete directory (will retry): {jobDirectory}.");
                        }
                    }
                }

                _logger.LogDebug("...Job directories checked.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Can't check job directories.");
            }
        }

        /// <summary>
        /// Return whether a job has entered a terminal state.
        /// </summary>
        /// <param name="job">Job to assess.</param>
        /// <returns>true: job has terminated; false job has not terminated.</returns>
        public virtual bool IsJobTerminated(PreviewJob job)
        {
            return job.IsCancelled || job.IsCompleted || job.IsError;
        }

        /// <summary>
        /// Create and schedule a new preview job.
        /// </summary>
        /// <param name="inputJob">Preview job to be added (required).</param>
        /// <param name="outputJob">Persisted preview job if created, null otherwise.</param>
        /// <returns>True if job created successfully, false otherwise.</returns>
        public virtual bool TryAddJob(PreviewJob inputJob, out PreviewJob outputJob)
        {
            if (inputJob.Id != null
                || !ProjectUtil.ValidateProjectName(inputJob.BibleSelectionParams?.ProjectName))
            {
                outputJob = null;
                return false;
            }

            InitPreviewJob(inputJob);

            _logger.LogDebug($"TryAddJob() - inputJob.Id={inputJob.Id}.");

            lock (_tptServiceContext)
            {
                PreviewJobs.Add(inputJob.Id, inputJob);
                _tptServiceContext.PreviewJobs.Add(inputJob);
                _tptServiceContext.SaveChanges();

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
            previewJob.BibleSelectionParams ??= new BibleSelectionParams();
            previewJob.BibleSelectionParams.Id = Guid.NewGuid().ToString();
            previewJob.TypesettingParams ??= new TypesettingParams();
            previewJob.TypesettingParams.Id = Guid.NewGuid().ToString();
            previewJob.AdditionalParams ??= new AdditionalPreviewParams();
            previewJob.AdditionalParams.Id = Guid.NewGuid().ToString();
            previewJob.State.Add(new PreviewJobState(JobStateEnum.Submitted));

            // project defaults
            previewJob.TypesettingParams.FontSizeInPts ??= MainConsts.ALLOWED_FONT_SIZE_IN_PTS.Default;
            previewJob.TypesettingParams.FontLeadingInPts ??= MainConsts.ALLOWED_FONT_LEADING_IN_PTS.Default;
            previewJob.TypesettingParams.PageWidthInPts ??= MainConsts.ALLOWED_PAGE_WIDTH_IN_PTS.Default;
            previewJob.TypesettingParams.PageHeightInPts ??= MainConsts.ALLOWED_PAGE_HEIGHT_IN_PTS.Default;
            previewJob.TypesettingParams.PageHeaderInPts ??= MainConsts.ALLOWED_PAGE_HEADER_IN_PTS.Default;
            previewJob.TypesettingParams.BookFormat ??= MainConsts.DEFAULT_BOOK_FORMAT;
        }

        /// <summary>
        /// Delete a preview job.
        /// </summary>
        /// <param name="jobId">Job ID to delete (required).</param>
        /// <param name="outputJob">The deleted job if found, null otherwise.</param>
        /// <returns>True if successful, false otherwise.</returns>
        public virtual bool TryDeleteJob(string jobId, out PreviewJob outputJob)
        {
            _logger.LogDebug($"TryDeleteJob() - jobId={jobId}.");
            lock (_tptServiceContext)
            {
                if (TryGetJob(jobId, out var foundJob))
                {
                    // try to cancel against every processor
                    foreach (var stateToProcessor in StateToProcessorProcessMap)
                    {
                        stateToProcessor.Value.CancelJob(foundJob);
                    }

                    PreviewJobs.Remove(foundJob.Id);
                    _tptServiceContext.PreviewJobs.Remove(foundJob);
                    _tptServiceContext.SaveChanges();

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
        /// <param name="job">Preview job to update (required).</param>
        /// <returns>True if successful, false otherwise.</returns>
        public virtual bool TryUpdateJob(PreviewJob job)
        {
            _logger.LogDebug($"TryUpdateJob() - job={job.Id}.");
            lock (_tptServiceContext)
            {
                if (TryGetJob(job.Id, out var existing))
                {
                    _tptServiceContext.Entry(existing).CurrentValues.SetValues(job);
                    _tptServiceContext.Entry(existing).State = EntityState.Modified;
                    _tptServiceContext.SaveChanges();

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
        public virtual bool TryGetJob(string jobId, out PreviewJob previewJob)
        {
            _logger.LogDebug($"TryGetJob() - jobId={jobId}.");
            lock (_tptServiceContext)
            {
                PreviewJobs.TryGetValue(jobId, out previewJob);
                return (previewJob != null);
            }
        }

        /// <summary>
        /// Retrieve all jobs
        /// </summary>
        /// <param name="previewJobs">Retrieved preview jobs.</param>
        /// <returns>True if successful, false otherwise.</returns>
        public virtual bool TryGetJobs(out List<PreviewJob> previewJobs)
        {
            _logger.LogDebug($"TryGetJobs().");
            lock (_tptServiceContext)
            {
                if (PreviewJobs is null)
                {
                    previewJobs = _tptServiceContext.PreviewJobs
                        .Include(x => x.State)
                        .Include(x => x.BibleSelectionParams)
                        .Include(x => x.TypesettingParams)
                        .Include(x => x.AdditionalParams)
                        .ToList();

                    // populate the cache
                    PreviewJobs = previewJobs.ToDictionary(
                        job => job.Id,
                        job => job
                    );
                }
                else
                {
                    previewJobs = PreviewJobs
                        .Select(jobKvp => jobKvp.Value)
                        .ToList();
                }

                return previewJobs != null && previewJobs.Count > 0;
            }
        }

        /// <summary>
        /// Retrieve jobs by their current state
        /// </summary>
        /// <param name="targetState">Target current state (required).</param>
        /// <param name="previewJobs">Retrieved preview jobs.</param>
        /// <returns>True if successful, false otherwise.</returns>
        public virtual bool TryGetJobsByCurrentState(JobStateEnum targetState, out List<PreviewJob> previewJobs)
        {
            _logger.LogDebug($"TryGetJobsByCurrentState() - targetState={targetState}.");
            lock (_tptServiceContext)
            {
                previewJobs = PreviewJobs
                    .Where(jobKvp => jobKvp.Value?.State?.Last()?.State == targetState)
                    .Select(jobKvp => jobKvp.Value)
                    .ToList();

                return previewJobs != null && previewJobs.Count > 0;
            }
        }

        /// <summary>
        /// Checks whether a given job exists, based on ID.
        /// </summary>
        /// <param name="jobId">Job ID to retrieve (required).</param>
        /// <returns>True if job exists, false otherwise.</returns>
        public virtual bool IsJobId(string jobId)
        {
            _logger.LogDebug($"IsJobId() - jobId={jobId}.");
            lock (_tptServiceContext)
            {
                return PreviewJobs.ContainsKey(jobId);
            }
        }

        /// <summary>
        /// Retrieves file stream for preview job PDF.
        /// </summary>
        /// <param name="jobId">Job ID of preview to retrieve file for.</param>
        /// <param name="fileStream">File stream if found, null otherwise.</param>
        /// <param name="archive">Whether or not to return an archive of all the typesetting files or just the PDF itself (optional). 
        /// True: all typesetting files zipped in an archive. False: Output only the preview PDF itself. . Default: false.</param>
        /// <returns>True if successful, false otherwise.</returns>
        public virtual bool TryGetPreviewStream(string jobId, out FileStream fileStream, bool archive)
        {
            _logger.LogDebug($"TryGetPreviewStream() - jobId={jobId}.");
            lock (_tptServiceContext)
            {
                if (!TryGetJob(jobId, out var previewJob))
                {
                    fileStream = null;
                    return false;
                }
                else
                {
                    try
                    {
                        var pdfDirectory = _jobFileManager.GetPreviewDirectoryById(jobId).FullName;

                        if (archive)
                        {
                            _logger.LogDebug($"Preparing archive for job: {jobId}");

                            // readable string format of book format used for find associated files in directories.
                            var bookFormatStr = Enum.GetName(typeof(BookFormat), BookFormat.cav);

                            /// Archive all typesetting preview files

                            // Create the initial zip file.
                            var archiveDirectory = _jobFileManager.GetArchiveDirectoryById(jobId).FullName;
                            FileUtil.CheckAndCreateDirectory(archiveDirectory);
                            var zipFilePath = Path.Combine(archiveDirectory, $"{previewJob.Id}.zip");

                            // Return file if it already exists
                            if (File.Exists(zipFilePath))
                            {
                                // Return a stream of the file
                                fileStream = File.Open(
                                    zipFilePath,
                                    FileMode.Open, FileAccess.Read);

                                // Return that we have the necessary file
                                return true;
                            }

                            var zip = ZipFile.Open(zipFilePath, ZipArchiveMode.Create);

                            //IDML
                            //"{{Properties::Docs::IDML::Directory}}\{{PreviewJob::id}}.idml"
                            var idmlDirectory = _jobFileManager.GetTemplateDirectoryById(jobId).FullName;
                            var idmlFilePattern = $"{previewJob.Id}.idml";
                            AddFilesToZip(zip, idmlDirectory, idmlFilePattern, "IDML");

                            //IDTT / TXT
                            //"{{Properties::Docs::IDTT::Directory}}\book*.txt"
                            var idttDirectory = _jobFileManager.GetTaggedTextDirectoryById(jobId).FullName;
                            var idttFilePattern = "book*.txt";
                            AddFilesToZip(zip, idttDirectory, idttFilePattern, "IDTT");

                            //INDD
                            //"{{Properties::Docs::IDML::Directory}}\{{PreviewJob::id}}-*.indd"
                            var inddDirectory = idmlDirectory;
                            var inddFilePattern = $"{previewJob.Id}-*.indd";
                            AddFilesToZip(zip, inddDirectory, inddFilePattern, "INDD");

                            //INDB
                            //"{{Properties::Docs::IDML::Directory}}\{{PreviewJob::id}}.indb"
                            var indbDirectory = idmlDirectory;
                            var indbFilePattern = $"{previewJob.Id}.indb";
                            AddFilesToZip(zip, indbDirectory, indbFilePattern, "INDB");

                            //PDF
                            //"{{Properties::Docs::PDF::Directory}}\{{PreviewJob::id}}.pdf"
                            var pdfFilePattern = $"{previewJob.Id}.pdf";
                            AddFilesToZip(zip, pdfDirectory, pdfFilePattern, "PDF");

                            // finalize the zip file writing
                            zip.Dispose();

                            // Return a stream of the file
                            fileStream = File.Open(
                                zipFilePath,
                                FileMode.Open, FileAccess.Read);

                            // Return that we have the necessary file
                            return true;
                        }
                        else
                        {
                            // Return the preview PDF
                            fileStream = File.Open(
                                Path.Combine(pdfDirectory, $"{previewJob.Id}.pdf"),
                                FileMode.Open, FileAccess.Read);
                            return true;
                        }
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
        /// A utility function for adding files to a directory in a zip file. The files added are based on a source directory and a file search pattern using wildcards.
        /// </summary>
        /// <param name="zipArchive">The zip file to append files to. (required)</param>
        /// <param name="sourceDirectoryPath">The source directory containing the files we want to add to the zip. (required)</param>
        /// <param name="sourceFileSearchPattern">A file search pattern for filtering out wanted files in the source directory. EG: "preview-*.*" (required)</param>
        /// <param name="targetDirectoryInZip">A directory inside of the zip file to stick the found files. (required)</param>
        private void AddFilesToZip(ZipArchive zipArchive, string sourceDirectoryPath, string sourceFileSearchPattern, string targetDirectoryInZip)
        {
            // Validate inputs
            _ = zipArchive ?? throw new ArgumentNullException(nameof(zipArchive));
            _ = sourceDirectoryPath ?? throw new ArgumentNullException(nameof(sourceDirectoryPath));
            _ = sourceFileSearchPattern ?? throw new ArgumentNullException(nameof(sourceFileSearchPattern));
            _ = targetDirectoryInZip ?? throw new ArgumentNullException(nameof(targetDirectoryInZip));

            // ensure we address missing directories
            if (!Directory.Exists(sourceDirectoryPath))
            {
                _logger.LogWarning($"There's no directory at '${sourceDirectoryPath}'");
                return;
            }

            // Add files found using provided directory and file search pattern to the archive.
            var foundFiles = Directory.GetFiles(sourceDirectoryPath, sourceFileSearchPattern);
            foreach (var file in foundFiles)
            {
                // Add the entry for each file
                zipArchive.CreateEntryFromFile(file, Path.Combine(targetDirectoryInZip, Path.GetFileName(file)), CompressionLevel.Optimal);
            }

            if (foundFiles.Length <= 0)
            {
                _logger.LogWarning($"There were no files found in the directory '${sourceDirectoryPath}' using the file search pattern '${sourceFileSearchPattern}'");
            }
        }

        /// <summary>
        /// Disposes of class resources.
        /// </summary>
        public void Dispose()
        {
            _logger.LogDebug("Dispose().");

            _docCheckTimer.Dispose();
            _processRunTimer.Dispose();
        }
    }
}