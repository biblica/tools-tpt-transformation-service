/*
Copyright © 2021 by Biblica, Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.Collections.Generic;

namespace TptMain.Models
{
    /// <summary>
    /// Model for tracking Typesetting Preview jobs.
    /// </summary>
    public class PreviewJob
    {
        /// <summary>
        /// Unique identifier for jobs.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The <c>DateTime</c> a job was submitted.
        /// </summary>
        public DateTime? DateSubmitted
        {
            get
            {
                State.Sort();
                var previewJobState = State.FindLast(
                   previewJobState => previewJobState.State == JobStateEnum.Submitted);
                return previewJobState?.DateSubmitted;
            }
        }

        /// <summary>
        /// The <c>DateTime</c> a job actually started execution.
        /// </summary>
        public DateTime? DateStarted
        {
            get
            {
                State.Sort();
                var previewJobState = State.FindLast(
                   previewJobState => previewJobState.State == JobStateEnum.Started);
                return previewJobState?.DateSubmitted;
            }
        }

        /// <summary>
        /// The <c>DateTime</c> a job was completed/
        /// </summary>
        public DateTime? DateCompleted
        {
            get
            {
                State.Sort();
                var previewJobState = State.FindLast(
                   previewJobState => previewJobState.State == JobStateEnum.PreviewGenerated);
                return previewJobState?.DateSubmitted;
            }
        }

        /// <summary>
        /// The <c>DateTime</c> a job was cancelled.
        /// </summary>
        public DateTime? DateCancelled
        {
            get
            {
                State.Sort();
                var previewJobState = State.FindLast(
                    previewJobState => previewJobState.State == JobStateEnum.Cancelled
                );
                return previewJobState?.DateSubmitted;
            }
        }

        /// <summary>
        /// The user requesting the job.
        /// </summary>
        public string User { get; set; }

        /// <summary>
        /// Whether the job was submitted or not.
        /// </summary>
        public bool IsSubmitted => State.Contains(new PreviewJobState(JobStateEnum.Submitted));

        /// <summary>
        /// Whether or not the job has started execution or not.
        /// </summary>
        public bool IsStarted => State.Contains(new PreviewJobState(JobStateEnum.Started));

        /// <summary>
        /// Whether the job has been completed or not.
        /// </summary>
        public bool IsCompleted => State.Contains(new PreviewJobState(JobStateEnum.PreviewGenerated));

        /// <summary>
        ///  Whether the job has been cancelled or not.
        /// </summary>
        public bool IsCancelled => State.Contains(new PreviewJobState(JobStateEnum.Cancelled));

        /// <summary>
        /// Whether or not there was an error during job execution.
        /// </summary>
        public bool IsError => State.Contains(new PreviewJobState(JobStateEnum.Error));

        /// <summary>
        /// The set of all states of the Preview Job over time
        /// </summary>
        public List<PreviewJobState> State { get; set; } = new();

        /// <summary>
        /// User-friendly message regarding the error; <c>null</c> otherwise.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// More technical reason as to why the error occurred; <c>null</c> otherwise.
        /// </summary>
        public string ErrorDetail { get; set; }

        /// <summary>
        /// Which Bible books to include.
        /// </summary>
        public virtual BibleSelectionParams BibleSelectionParams { get; set; }

        /// <summary>
        /// Parameters to use for the typesetting preview.
        /// </summary>
        public virtual TypesettingParams TypesettingParams { get; set; }

        /// <summary>
        /// Additional parameters that are needed and calculated.
        /// </summary>
        public AdditionalPreviewParams AdditionalParams { get; set; }

        /// <summary>
        /// Source of content for this preview job.
        /// </summary>
        public ContentSource ContentSource { get; set; }

        /// <summary>
        /// The body of USX archive.
        /// </summary>
        public string UsxArchive { get; set; }

        /// <summary>
        /// Function used for indicating an error occurred and provide a message for the reason.
        /// </summary>
        /// <param name="errorMessage">User-friendly error message. (Required)</param>
        /// <param name="errorDetail">Information about why the error occurred.</param>
        public void SetError(string errorMessage, string errorDetail)
        {
            SetError(errorMessage, errorDetail, JobStateSourceEnum.GeneralManagement);
        }

        /// <summary>
        /// Function used for indicating an error occurred and provide a message for the reason.
        /// </summary>
        /// <param name="errorMessage">User-friendly error message. (Required)</param>
        /// <param name="errorDetail">Information about why the error occurred.</param>
        /// <param name="jobStateSourceEnum">The source entity of the error job state. (Required)</param>
        public void SetError(string errorMessage, string errorDetail, JobStateSourceEnum jobStateSourceEnum)
        {
            // validate inputs
            ErrorMessage = errorMessage ?? throw new ArgumentNullException(nameof(errorMessage));
            ErrorDetail = errorDetail;

            State.Add(new PreviewJobState(JobStateEnum.Error, jobStateSourceEnum));
        }
    }
}