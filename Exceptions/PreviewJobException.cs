using System;
using TptMain.Models;

namespace TptMain.Exceptions
{
    /// <summary>
    /// <c>PreviewJobException</c> is for exceptions related to executing a <c>PreviewJob</c>.
    /// </summary>
    public class PreviewJobException : Exception
    {
        /// <summary>
        /// The active PreviewJob when the exception occurred.
        /// </summary>
        public PreviewJob PreviewJob { get; private set; }

        /// <summary>
        /// Simple <c>PreviewJobException</c> constructor tracking the <c>PreviewJob</c>.
        /// </summary>
        /// <param name="previewJob">Running <c>PreviewJob</c> when the exception occurred.</param>
        public PreviewJobException(PreviewJob previewJob)
        {
            // validate inputs
            PreviewJob = previewJob ?? throw new ArgumentNullException(nameof(previewJob));
        }

        /// <summary>
        /// <c>PreviewJobException</c> constructor tracking the <c>PreviewJob</c> with a descriptive message.
        /// </summary>
        /// <param name="previewJob">Running <c>PreviewJob</c> when the exception occurred.</param>
        /// <param name="message">The exception message.</param>
        public PreviewJobException(PreviewJob previewJob, string message)
            : base(message)
        {
            // validate inputs
            PreviewJob = previewJob ?? throw new ArgumentNullException(nameof(previewJob));
        }

        /// <summary>
        /// <c>PreviewJobException</c> constructor tracking the <c>PreviewJob</c> with a descriptive message.
        /// </summary>
        /// <param name="previewJob">Running <c>PreviewJob</c> when the exception occurred.</param>
        /// <param name="message">The exception message.</param>
        /// <param name="inner">The exception that is the cause of the current exception.</param>
        public PreviewJobException(PreviewJob previewJob, string message, Exception inner)
            : base(message, inner)
        {
            // validate inputs
            PreviewJob = previewJob ?? throw new ArgumentNullException(nameof(previewJob));
        }

        /// <summary>
        /// Creates and returns a string representation of the current exception.
        /// </summary>
        /// <returns>A string representation of the current exception.</returns>
        public override string ToString()
        {
            //return base.ToString();
            return string.Format("Error: {0}\r\n\tJob ID: '{1}'", this.Message, this.PreviewJob.Id);
        }
    }
}
