using System.IO;
using TptMain.Models;

namespace TptMain.Jobs
{
    public interface IPreviewJobValidator
    {
        /// <summary>
        /// Validate if the fields of a <c>PreviewJob</c> are valid according to our expectations. If a parameter or parameters are invalid, an exception will be thrown detailing what was invalid.
        /// </summary>
        /// <param name="previewJob"><c>PreviewJob</c> to validate.</param>
        public void ValidatePreviewJob(PreviewJob previewJob);
    }
}