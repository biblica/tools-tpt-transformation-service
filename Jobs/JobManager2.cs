using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using TptMain.InDesign;
using TptMain.Models;
using TptMain.ParatextProjects;
using TptMain.Toolbox;
using TptMain.Util;

namespace TptMain.Jobs
{
    /// <summary>
    /// Job manager for handling typesetting preview job request management and execution.
    /// </summary>
    public class JobManager2 : IDisposable, IJobManager
    {
        /// <summary>
        /// Type-specific logger (injected).
        /// </summary>
        private readonly ILogger<JobManager2> _logger;

        /// <summary>
        /// Job preview context (persistence; injected).
        /// </summary>
        private readonly TptServiceContext _tptServiceContext;

        /// <summary>
        /// Job Validation service for ensuring feasible and authorized jobs.
        /// </summary>
        private readonly IPreviewJobValidator _jobValidator;

        /// <summary>
        /// Template manager.
        /// </summary>
        private readonly TemplateJobManager _templateManager;

        /// <summary>
        /// Tagged text manager.
        /// </summary>
        private readonly TaggedTextJobManager _taggedTextManager;

        /// <summary>
        /// Preview Manager (injected).
        /// </summary>
        private readonly IPreviewManager _previewManager;

        /// <summary>
        /// Template (IDML) storage directory (configured).
        /// </summary>
        private readonly DirectoryInfo _idmlDirectory;

        /// <summary>
        /// Tagged Text (IDTT) storage directory (configured).
        /// </summary>
        private readonly DirectoryInfo _idttDirectory;

        /// <summary>
        /// Preview PDF storage directory (configured).
        /// </summary>
        private readonly DirectoryInfo _pdfDirectory;

        /// <summary>
        /// Preview zip storage directory (configured).
        /// </summary>
        private readonly DirectoryInfo _zipDirectory;

        /// <summary>
        /// Max document (IDML, PDF) age, in seconds (configured).
        /// </summary>
        private readonly int _maxDocAgeInSec;

        /// <summary>
        /// TODO rename
        /// </summary>
        private readonly Timer _docCheckTimer;

        /// <summary>
        /// Template (IDML) storage directory.
        /// </summary>
        public DirectoryInfo IdmlDirectory => _idmlDirectory;

        /// <summary>
        /// TODO doc
        /// </summary>
        private Dictionary<JobStateEnum, IPreviewJobProcessor> StateToProcessorMap { get; set; }

        // TODO create cache of jobs as they'll be updated repeatedly in processors.
        private Dictionary<string, PreviewJob> PreviewJobs{ get; set; }

        /// <summary>
        /// Basic ctor.
        /// </summary>
        /// <param name="logger">Type-specific logger (required).</param>
        /// <param name="configuration">System configuration (required).</param>
        /// <param name="tptServiceContext">Database context (persistence; required).</param>
        /// <param name="scriptRunner">Script runner (required).</param>
        /// <param name="templateManager">Template manager (required).</param>
        /// <param name="jobValidator">Preview Job Validator used for ensuring job feasibility and authorization on projects (required).</param>
        /// <param name="jobScheduler">Job scheduler (required).</param>
        public JobManager2(
            ILogger<JobManager2> logger,
            IConfiguration configuration,
            TptServiceContext tptServiceContext,
            IPreviewJobValidator jobValidator,
            TemplateJobManager templateManager,
            TaggedTextJobManager taggedTextManager,
            IPreviewManager previewManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _ = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _tptServiceContext = tptServiceContext ?? throw new ArgumentNullException(nameof(tptServiceContext));
            _jobValidator = jobValidator ?? throw new ArgumentNullException(nameof(jobValidator));
            _templateManager = templateManager ?? throw new ArgumentNullException(nameof(templateManager));
            _taggedTextManager = taggedTextManager ?? throw new ArgumentNullException(nameof(taggedTextManager));
            _previewManager = previewManager ?? throw new ArgumentNullException(nameof(previewManager));

            _idmlDirectory = new DirectoryInfo(configuration[ConfigConsts.IdmlDocDirKey]
                                               ?? throw new ArgumentNullException(ConfigConsts.IdmlDocDirKey));
            _idttDirectory = new DirectoryInfo(configuration[ConfigConsts.IdttDocDirKey]
                                               ?? throw new ArgumentNullException(ConfigConsts.IdttDocDirKey));
            _pdfDirectory = new DirectoryInfo(configuration[ConfigConsts.PdfDocDirKey]
                                              ?? throw new ArgumentNullException(ConfigConsts.PdfDocDirKey));
            _zipDirectory = new DirectoryInfo(configuration[ConfigConsts.ZipDocDirKey]
                                              ?? throw new ArgumentNullException(ConfigConsts.ZipDocDirKey));

            _maxDocAgeInSec = int.Parse(configuration[ConfigConsts.MaxDocAgeInSecKey]
                                        ?? throw new ArgumentNullException(ConfigConsts.MaxDocAgeInSecKey));
            // TODO use variables based on the expected usage. EG: job check timer
            _docCheckTimer = new Timer((stateObject) => { ProcessJobs(); }, null,
                 TimeSpan.FromSeconds(MainConsts.TIMER_STARTUP_DELAY_IN_SEC),
                 TimeSpan.FromSeconds(_maxDocAgeInSec / MainConsts.MAX_AGE_CHECK_DIVISOR));

            if (!Directory.Exists(_pdfDirectory.FullName))
            {
                Directory.CreateDirectory(_pdfDirectory.FullName);
            }

            if (!Directory.Exists(_zipDirectory.FullName))
            {
                Directory.CreateDirectory(_zipDirectory.FullName);
            }

            // map the events that kick them off to their respective processors
            StateToProcessorMap = new Dictionary<JobStateEnum, IPreviewJobProcessor>()
            {
                { JobStateEnum.Submitted, _jobValidator },
                { JobStateEnum.Started, _templateManager },
                { JobStateEnum.TemplateGenerated, _taggedTextManager },
                { JobStateEnum.TaggedTextGenerated, _previewManager }
            };

            // grab and cache the jobs from the database
            if(TryGetJobs(out var previewJobs))
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

            _logger.LogDebug("JobManager2() initialized");
        }

        /// <summary>
        /// TODO name and doc
        /// </summary>
        public void ProcessJobs()
        {
            // TODO handle cancels

            foreach (var test in StateToProcessorMap)
            {
                var initiationState = test.Key;
                var handlingProcessor = test.Value;

                if(TryGetJobsByCurrentState(initiationState, out var previewJobsByState))
                {
                    previewJobsByState.ForEach(previewJob =>
                    {
                        handlingProcessor.ProcessJob(previewJob);
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
        /// Create and schedule a new preview job.
        /// </summary>
        /// <param name="inputJob">Preview job to be added (required).</param>
        /// <param name="outputJob">Persisted preview job if created, null otherwise.</param>
        /// <returns>True if job created successfully, false otherwise.</returns>
        public virtual bool TryAddJob(PreviewJob inputJob, out PreviewJob outputJob)
        {
            _logger.LogDebug($"TryAddJob() - inputJob.Id={inputJob.Id}.");
            if (inputJob.Id != null
                || inputJob.BibleSelectionParams.ProjectName == null
                || inputJob.BibleSelectionParams.ProjectName.Any(charItem => !char.IsLetterOrDigit(charItem)))
            {
                outputJob = null;
                return false;
            }
            this.InitPreviewJob(inputJob);

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
                        .Include(x => x.BibleSelectionParams)
                        .Include(x => x.TypesettingParams)
                        .Include(x => x.State)
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
                        if (archive)
                        {
                            _logger.LogDebug($"Preparing archive for job: {jobId}");

                            // readable string format of book format used for find associated files in directories.
                            var bookFormatStr = Enum.GetName(typeof(BookFormat), BookFormat.cav);

                            /// Archive all typesetting preview files
                            // Create the initial zip file.
                            var zipFilePath = Path.Combine(_zipDirectory.FullName, $"{MainConsts.PREVIEW_FILENAME_PREFIX}{previewJob.Id}.zip");

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
                            //"{{Properties::Docs::IDML::Directory}}\preview-{{PreviewJob::id}}.idml"
                            var idmlDirectory = $@"{_idmlDirectory}";
                            var idmlFilePattern = $"{MainConsts.PREVIEW_FILENAME_PREFIX}{previewJob.Id}.idml";
                            AddFilesToZip(zip, idmlDirectory, idmlFilePattern, "IDML");

                            //IDTT / TXT
                            //"{{Properties::Docs::IDTT::Directory}}\{{PreviewJob::bookFormat}}\{{PreviewJob::projectName}}\book*.txt"
                            var idttDirectory = Path.Combine(_idttDirectory.FullName, bookFormatStr, previewJob.BibleSelectionParams.ProjectName);
                            var idttFilePattern = "book*.txt";
                            AddFilesToZip(zip, idttDirectory, idttFilePattern, "IDTT");

                            //INDD
                            //"{{Properties::Docs::IDML::Directory}}\preview-{{PreviewJob::id}}-*.indd"
                            var inddDirectory = _idmlDirectory.FullName;
                            var inddFilePattern = $"{ MainConsts.PREVIEW_FILENAME_PREFIX}{ previewJob.Id}-*.indd";
                            AddFilesToZip(zip, inddDirectory, inddFilePattern, "INDD");

                            //INDB
                            //"{{Properties::Docs::IDML::Directory}}\preview-{{PreviewJob::id}}.indb"
                            var indbDirectory = _idmlDirectory.FullName;
                            var indbFilePattern = $"{MainConsts.PREVIEW_FILENAME_PREFIX}{previewJob.Id}.indb";
                            AddFilesToZip(zip, indbDirectory, indbFilePattern, "INDB");

                            //PDF
                            //"{{Properties::Docs::PDF::Directory}}\preview-{{PreviewJob::id}}.pdf"
                            var pdfDirectory = _pdfDirectory.FullName;
                            var pdfFilePattern = $"{MainConsts.PREVIEW_FILENAME_PREFIX}{previewJob.Id}.pdf";
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
                                Path.Combine(_pdfDirectory.FullName, $"preview-{previewJob.Id}.pdf"),
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
        }
    }
}