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

        /// <summary>
        /// Returns true when a username is usable.
        /// </summary>
        /// <param name="userName">Candidate username (optional, may be null).</param>
        /// <returns>True if a username is usable, false otherwise.</returns>
        public static bool ValidateUserName(string userName)
        {
            return !string.IsNullOrEmpty(userName)
                   && userName.All(char.IsLetterOrDigit);
        }
    }
}
