using System.Linq;

namespace TptMain.Text
{
    /// <summary>
    /// Paratext project-related utilities.
    /// </summary>
    public class ProjectUtil
    {
        /// <summary>
        /// Returns true when a project name is usable.
        /// </summary>
        /// <param name="projectName">Candidate project name (optional, may be null).</param>
        /// <returns>True if a project name is usable, false otherwise.</returns>
        public static bool ValidateProjectName(string projectName)
        {
            return !string.IsNullOrEmpty(projectName)
                   && projectName.All(char.IsLetterOrDigit);
        }
    }
}
