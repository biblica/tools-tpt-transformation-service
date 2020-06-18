using Paratext.Data.ProjectSettingsAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;
using TptMain.ParatextProjects.Models;

namespace TptMain.ParatextProjects
{
    /// <summary>
    /// <c>ParatextProjectService</c> is the abstracted service for handling requests about local Paratext Project information.
    /// </summary>
    public class ParatextProjectService
    {
        // TODO Create a function that returns the custom footnotes for a project
        // TODO Bring over the HostUtils utility class from console.
        // TODO Create a function that get's a project's settings.
        // TODO Create a function that returns the LDML data for a specified project by shortname.

        public List<String> GetFootnoteCallerSequence(ProjectSettings projectSettings)
        {
            // validate input
            _ = projectSettings ?? throw new ArgumentNullException(nameof(projectSettings));



            // TODO replace
            return null;
        }
    }
}
