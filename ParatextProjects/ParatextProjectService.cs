/*
Copyright © 2021 by Biblica, Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using TptMain.Models;
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
        /// Return a Paratext project's text direction.
        /// </summary>
        /// <param name="projectShortName">The Paratext project's shortname.</param>
        /// <returns>The paratext project's text direction.</returns>
        public virtual TextDirection GetTextDirection(string projectShortName)
        {
            // validate input
            _ = projectShortName ?? throw new ArgumentNullException(nameof(projectShortName));

            // Grab the Paratext project settings (for the LDML path).
            var projectPath = Path.Combine(_paratextDirectory.FullName, projectShortName);
            var projectSettings = ParatextProjectHelper.GetProjectSettings(projectPath);

            // Get the project's text direction from the LDML file
            var ldmlPath = Path.Combine(projectPath, projectSettings.LdmlFileName);
            var textDirection = ParatextProjectHelper.ExtractTextDirection(ldmlPath);

            return textDirection;
        }

        /// <summary>
        /// Get the font associated with a Paratext project.
        /// </summary>
        /// <param name="projectShortName">The Paratext project's shortname.</param>
        /// <returns>The font name specified by the Paratext project.</returns>
        public virtual string GetProjectFont(string projectShortName)
        {
            if (String.IsNullOrEmpty(projectShortName))
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
