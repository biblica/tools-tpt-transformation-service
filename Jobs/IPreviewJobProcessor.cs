using System.Threading;
using TptMain.Models;

namespace TptMain.Jobs
{
    /// <summary>
    /// Preview Job Processor interface.
    /// </summary>
    public interface IPreviewJobProcessor
    {
        /// <summary>
        /// Process a preview job.
        /// </summary>
        /// <param name="previewJob">Preview job to process (required).</param>
        /// <param name="additionalPreviewParams">Additional preview job params (optional).</param>
        void ProcessJob(ref PreviewJob previewJob, AdditionalPreviewParameters additionalPreviewParams);

        /// <summary>
        /// Process a preview job.
        /// </summary>
        /// <param name="previewJob">Preview job to process (required).</param>
        void ProcessJob(ref PreviewJob previewJob);

        /// <summary>
        /// Query the status of the PreviewJob and update the job itself appropriately.
        /// </summary>
        /// <param name="previewJob">The PreviewJob to query the status of.</param>
        void GetStatus(ref PreviewJob previewJob);

        /// <summary>
        /// Initiate the cancellation of a PreviewJob.
        /// </summary>
        /// <param name="previewJob">The PreviewJob to cancel.</param>
        void CancelJob(ref PreviewJob previewJob);
    }
}