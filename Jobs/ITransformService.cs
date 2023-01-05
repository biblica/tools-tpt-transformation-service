/*
Copyright © 2021 by Biblica, Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
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
        /// Moves (copies & deletes) a local project's files to S3, optionally not deleting it after copying.
        /// </summary>
        /// <param name="previewJob">Preview job (required).</param>
        /// <param name="deleteAfterCopy">Delete project after copying (optional; default = true).</param>
        /// <exception cref="ArgumentNullException"></exception>
        public void MoveProjectToS3(PreviewJob previewJob, bool deleteAfterCopy = true);

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