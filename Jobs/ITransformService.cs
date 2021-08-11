using TptMain.Models;
using static TptMain.Jobs.TransformService;

namespace TptMain.Jobs
{
    /// <summary>
    /// This class is for submitting jobs, either TEMPLATE GENERATION, or TAGGED TEXT, to the SQS queue in AWS.
    /// These jobs will then be picked up by the template generation ability and processed.
    /// </summary>
    public interface ITransformService
    {
        /// <summary>
        /// Places a job on the SQS queue for creating the IDML, the input to the InDesign step of the preview job process
        /// </summary>
        /// <param name="previewJob">The job to submit for Template generation</param>
        public void GenerateTemplate(PreviewJob previewJob);

        /// <summary>
        /// Places a job on the SQS queue for creating the Tagged Text from the USX
        /// </summary>
        /// <param name="previewJob">The job to submit for Tagged Text generation</param>
        public void GenerateTaggedText(PreviewJob previewJob);

        /// <summary>
        /// Checks the status of the job based on what is found in the S3 bucket that corresponds to the
        /// job.
        /// </summary>
        /// <param name="previewJobId">The preview job id</param>
        /// <returns>TransformJobStatus based on the state of the S3 bucket</returns>
        public TransformJobStatus GetTransformJobStatus(string previewJobId);

        /// <summary>
        /// Cancels transform jobs by placing a .cancel marker into the job directory
        /// </summary>
        /// <param name="previewJobId"></param>
        public void CancelTransformJobs(string previewJobId);
    }
}