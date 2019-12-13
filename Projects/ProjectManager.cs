using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using TptMain.Models;
using TptMain.Util;

namespace TptMain.Projects
{
    /// <summary>
    /// Project manager, provider of Paratext project details.
    /// </summary>
    public class ProjectManager
    {
        /// <summary>
        /// IDTT directory config key.
        /// </summary>
        private const string IdttDirKey = "Docs:IDTT:Directory";

        /// <summary>
        /// IDTT check interval config key.
        /// </summary>
        private const string IdttCheckIntervalInSecKey = "Docs:IDTT:CheckIntervalInSec";

        /// <summary>
        /// Type-specific logger (injected).
        /// </summary>
        private readonly ILogger<ProjectManager> _logger;

        /// <summary>
        /// System configuration (injected).
        /// </summary>
        private readonly IConfiguration _configuration;

        /// <summary>
        /// IDTT directory (configured).
        /// </summary>
        private readonly DirectoryInfo _idttDirectory;

        /// <summary>
        /// IDTT check interval, in seconds (configured).
        /// </summary>
        private readonly int _idttCheckIntervalInSec;

        /// <summary>
        /// IDTT check timer.
        /// </summary>
        private readonly Timer _projectCheckTimer;

        /// <summary>
        /// Found project details.
        /// </summary>
        private IDictionary<string, ProjectDetails> _projectDetails;

        /// <summary>
        /// Basic ctor.
        /// </summary>
        /// <param name="logger">Type-specific logger (required).</param>
        /// <param name="configuration">System configuration (required).</param>
        public ProjectManager(
            ILogger<ProjectManager> logger,
            IConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            _idttDirectory = new DirectoryInfo(_configuration[IdttDirKey]
                ?? throw new ArgumentNullException(IdttDirKey));
            _idttCheckIntervalInSec = int.Parse(_configuration[IdttCheckIntervalInSecKey]
                ?? throw new ArgumentNullException(IdttCheckIntervalInSecKey));
            _projectCheckTimer = new Timer((stateObject) => { CheckProjectFiles(); }, null,
                TimeSpan.FromSeconds(MainConsts.TIMER_STARTUP_DELAY_IN_SEC),
                TimeSpan.FromSeconds(_idttCheckIntervalInSec));

            if (!Directory.Exists(_idttDirectory.FullName))
            {
                Directory.CreateDirectory(_idttDirectory.FullName);
            }
            _logger.LogDebug("ProjectManager()");
        }

        /// <summary>
        /// Inventories project files to build map.
        /// </summary>
        private void CheckProjectFiles()
        {
            lock (this)
            {
                try
                {
                    _logger.LogDebug("Checking IDTT files...");

                    IDictionary<string, ProjectDetails> newProjectDetails = new SortedDictionary<string, ProjectDetails>();
                    foreach (var directoryItem in Directory.EnumerateDirectories(_idttDirectory.FullName))
                    {
                        var dateTime = DateTime.MinValue;
                        foreach (var fileItem in Directory.EnumerateFiles(directoryItem))
                        {
                            var lastWriteTime = File.GetLastWriteTimeUtc(fileItem);
                            if (lastWriteTime > dateTime)
                            {
                                dateTime = lastWriteTime;
                            }
                        }
                        if (dateTime > DateTime.MinValue)
                        {
                            var projectName = Path.GetFileName(directoryItem);
                            newProjectDetails[projectName] = new ProjectDetails
                            { ProjectName = projectName, ProjectUpdated = dateTime };
                        }
                    }

                    _projectDetails = newProjectDetails.ToImmutableDictionary();
                    _logger.LogDebug("...IDTT files checked.");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Can't check IDTT files.");
                }
            }
        }

        /// <summary>
        /// Gets a read-only copy of the current project details map, initiating an inventory if it hasn't happened yet.
        /// </summary>
        /// <param name="projectDetails">Immutable map of project names to details.</param>
        /// <returns>True if any project details found, false otherwise.</returns>
        public bool TryGetProjectDetails(out IDictionary<string, ProjectDetails> projectDetails)
        {
            _logger.LogDebug("TryGetProjectDetails().");
            lock (this)
            {
                if (_projectDetails == null)
                {
                    CheckProjectFiles();
                }

                projectDetails = _projectDetails;
                return (projectDetails.Count > 0);
            }
        }
    }
}