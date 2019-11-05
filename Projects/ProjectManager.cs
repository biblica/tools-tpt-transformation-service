using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using tools_tpt_transformation_service.Models;

namespace tools_tpt_transformation_service.Projects
{
    public class ProjectManager
    {
        private readonly ILogger<ProjectManager> _logger;
        private readonly IConfiguration _configuration;
        private readonly DirectoryInfo _idttDirectory;
        private readonly int _idttCheckIntervalInSec;
        private readonly Timer _idttCheckTimer;
        private IDictionary<String, ProjectDetails> _projectDetails;

        public ProjectManager(
            ILogger<ProjectManager> logger,
            IConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            _idttDirectory = new DirectoryInfo(_configuration.GetValue<string>("IDTT.Directory") ?? "C:\\Work\\IDTT");
            _idttCheckIntervalInSec = int.Parse(_configuration.GetValue<string>("IDTT.CheckIntervalInSec") ?? "120");
            _idttCheckTimer = new Timer((stateObject) => { CheckIdttFiles(); }, null,
                TimeSpan.FromSeconds(60.0),
                TimeSpan.FromSeconds(_idttCheckIntervalInSec));
            _projectDetails = ImmutableDictionary<String, ProjectDetails>.Empty;

            if (!Directory.Exists(_idttDirectory.FullName))
            {
                Directory.CreateDirectory(_idttDirectory.FullName);
            }
            _logger.LogDebug("ProjectManager()");
        }


        /// <summary>
        /// Iterate through preview files and clean up old ones.
        /// </summary>
        private void CheckIdttFiles()
        {
            try
            {
                _logger.LogDebug("Checking IDTT files...");

                IDictionary<String, ProjectDetails> newProjectDateTimes = new SortedDictionary<String, ProjectDetails>();
                foreach (string directoryItem in Directory.EnumerateDirectories(_idttDirectory.FullName))
                {
                    DateTime dateTime = DateTime.MinValue;
                    foreach (string fileItem in Directory.EnumerateFiles(directoryItem))
                    {
                        DateTime lastWriteTime = File.GetLastWriteTimeUtc(fileItem);
                        if (lastWriteTime > dateTime)
                        {
                            dateTime = lastWriteTime;
                        }
                    }
                    if (dateTime > DateTime.MinValue)
                    {
                        string projectName = Path.GetFileName(directoryItem);
                        newProjectDateTimes[projectName] = new ProjectDetails
                        { ProjectName = projectName, ProjectUpdated = dateTime };
                    }
                }

                _projectDetails = newProjectDateTimes.ToImmutableDictionary();
                _logger.LogDebug("...IDTT files checked.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Can't check IDTT files.");
            }
        }

        public bool TryGetProjectDateTimes(out IDictionary<String, ProjectDetails> projectDetails)
        {
            projectDetails = _projectDetails;
            return (projectDetails.Count > 0);
        }
    }
}
