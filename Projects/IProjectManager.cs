using System.Collections.Generic;
using TptMain.Models;

namespace TptMain.Projects
{
    public interface IProjectManager
    {
        bool TryGetProjectDetails(out IDictionary<string, ProjectDetails> projectDetails);
    }
}