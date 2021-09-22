using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

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
                this.State.Sort();
                PreviewJobState previewJobState = this.State.FindLast(
                   (previewJobState) =>
                    {
                        return previewJobState.State == JobStateEnum.Submitted;
                    }
                );
                return previewJobState != null ? previewJobState.DateSubmitted : null;
            }
        }

        /// <summary>
        /// The <c>DateTime</c> a job actually started execution.
        /// </summary>
        public DateTime? DateStarted
        {
            get
            {
                this.State.Sort();
                PreviewJobState previewJobState = this.State.FindLast(
                   (previewJobState) =>
                   {
                       return previewJobState.State == JobStateEnum.Started;
                   }
                );
                return previewJobState != null ? previewJobState.DateSubmitted : null;
            }
        }

        /// <summary>
        /// The <c>DateTime</c> a job was completed/
        /// </summary>
        public DateTime? DateCompleted
        {
            get
            {
                this.State.Sort();
                PreviewJobState previewJobState = this.State.FindLast(
                   (previewJobState) =>
                   {
                       return previewJobState.State == JobStateEnum.PreviewGenerated;
                   }
                );
                return previewJobState != null ? previewJobState.DateSubmitted : null;
            }
        }

        /// <summary>
        /// The <c>DateTime</c> a job was cancelled.
        /// </summary>
        public DateTime? DateCancelled
        {
            get
            {
                this.State.Sort();
                PreviewJobState previewJobState = this.State.FindLast(
                    delegate (PreviewJobState previewJobState)
                    {
                        return previewJobState.State == JobStateEnum.Cancelled;
                    }
                );
                return previewJobState != null ? previewJobState.DateSubmitted : null;
            }
        }

        /// <summary>
        /// The user requesting the job.
        /// </summary>
        public string User { get; set; }

        /// <summary>
        /// Whether the job was submitted or not.
        /// </summary>
        public bool IsSubmitted => this.State.Contains(new PreviewJobState(JobStateEnum.Submitted));

        /// <summary>
        /// Whether or not the job has started execution or not.
        /// </summary>
        public bool IsStarted => this.State.Contains(new PreviewJobState(JobStateEnum.Started));

        /// <summary>
        /// Whether the job has been completed or not.
        /// </summary>
        public bool IsCompleted => this.State.Contains(new PreviewJobState(JobStateEnum.PreviewGenerated));

        /// <summary>
        ///  Whether the job has been cancelled or not.
        /// </summary>
        public bool IsCancelled => this.State.Contains(new PreviewJobState(JobStateEnum.Cancelled));

        /// <summary>
        /// Whether or not there was an error during job execution.
        /// </summary>
        public bool IsError => this.State.Contains(new PreviewJobState(JobStateEnum.Error));

        /// <summary>
        /// The set of all states of the Preview Job over time
        /// </summary>
        public List<PreviewJobState> State { get; set; } = new List<PreviewJobState> {
        };

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
            this.ErrorMessage = errorMessage ?? throw new ArgumentNullException(nameof(errorMessage));
            this.ErrorDetail = errorDetail;

            this.State.Add(new PreviewJobState(JobStateEnum.Error, jobStateSourceEnum));
        }
    }
}