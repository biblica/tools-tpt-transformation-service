/*
Copyright © 2021 by Biblica, Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
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
        /// Default basic constructor that allows for a message only exception
        /// </summary>
        /// <param name="message"></param>
        public PreviewJobException(string message)
            : base(message)
        {
        }

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
            if(this.PreviewJob is null)
            {
                return string.Format("Error: {0}", this.Message);
            }
            return string.Format("Error: {0}\r\n\tJob ID: '{1}'", this.Message, this.PreviewJob.Id);
        }
    }
}
