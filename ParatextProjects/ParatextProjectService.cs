using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Paratext.Data.ProjectSettingsAccess;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;
using TptMain.ParatextProjects.Models;
using TptMain.Util;

namespace TptMain.ParatextProjects
{
    /// <summary>
    /// <c>ParatextProjectService</c> is the abstracted service for handling requests about local Paratext Project information.
    /// </summary>
    public class ParatextProjectService
    {
        /// <summary>
        /// Type-specific logger (injected).
        /// </summary>
        private readonly ILogger<ParatextProjectService> _logger;

        /// <summary>
        /// Paratext project root directory (configured).
        /// </summary>
        private readonly DirectoryInfo _paratextDirectory;

        /// <summary>
        /// Service Constructor.
        /// </summary>
        /// <param name="logger">Type-specific logger (required).</param>
        /// <param name="configuration">System configuration (required).</param>
        public ParatextProjectService(
            ILogger<ParatextProjectService> logger,
            IConfiguration configuration
        )
        {
            // validate inputs
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _ = configuration ?? throw new ArgumentNullException(nameof(configuration));

            // settings fields based on configuration
            _paratextDirectory = new DirectoryInfo(configuration[ConfigConsts.ParatextDocDirKey]
                                             ?? throw new ArgumentNullException($"Unset configuration parameter: '{ConfigConsts.ParatextDocDirKey}'"));
        }

        /// <summary>
        /// Return a Paratext project's footnote caller sequence.
        /// </summary>
        /// <param name="projectShortName">The Paratext project's shortname.</param>
        /// <returns>The paratext project's footnote caller sequence, if found; Otherwise, <c>null</c>.</returns>
        public virtual string[] GetFootnoteCallerSequence(string projectShortName)
        {
            // validate input
            _ = projectShortName ?? throw new ArgumentNullException(nameof(projectShortName));

            // Grab the Paratext project settings (for the LDML path).
            var projectPath = Path.Combine(_paratextDirectory.FullName, projectShortName);
            var projectSettings = ParatextProjectHelper.GetProjectSettings(projectPath);

            // Get the project's footnote markers, given the LDML path
            var ldmlPath = Path.Combine(projectPath, projectSettings.LdmlFileName);
            var footnoteMarkers = ParatextProjectHelper.ExtractFootnoteMarkers(ldmlPath);

            return footnoteMarkers;
        }

        /// <summary>
        /// Get the font associated with a Paratext project.
        /// </summary>
        /// <param name="projectShortName">The Paratext project's shortname.</param>
        /// <returns>The font name specified by the Paratext project.</returns>
        public virtual string GetProjectFont(string projectShortName)
        {
            if(String.IsNullOrEmpty(projectShortName))
            {
                throw new ArgumentException($"{nameof(projectShortName)} must be a non-empty string.");
            }

            // Grab the Paratext project settings (for the LDML path).
            var projectPath = Path.Combine(_paratextDirectory.FullName, projectShortName);
            var projectSettings = ParatextProjectHelper.GetProjectSettings(projectPath);

            return projectSettings.DefaultFont;
        }
    }
}
