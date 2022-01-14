/*
Copyright © 2021 by Biblica, Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
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
        void ProcessJob(PreviewJob previewJob);

        /// <summary>
        /// Query the status of the PreviewJob and update the job itself appropriately.
        /// </summary>
        /// <param name="previewJob">The PreviewJob to query the status of.</param>
        void GetStatus(PreviewJob previewJob);

        /// <summary>
        /// Initiate the cancellation of a PreviewJob.
        /// </summary>
        /// <param name="previewJob">The PreviewJob to cancel.</param>
        void CancelJob(PreviewJob previewJob);
    }
}