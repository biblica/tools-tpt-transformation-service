using System.IO;
using TptMain.Models;

namespace TptMain.Jobs
{
    public interface IJobManager
    {
        bool IsJobId(string jobId);
        bool TryAddJob(PreviewJob inputJob, out PreviewJob outputJob);
        bool TryDeleteJob(string jobId, out PreviewJob outputJob);
        bool TryGetJob(string jobId, out PreviewJob previewJob);
        bool TryGetPreviewStream(string jobId, out FileStream fileStream, bool archive);
        bool TryUpdateJob(PreviewJob nextJob);
    }
}