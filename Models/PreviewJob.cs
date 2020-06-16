using System;

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
        public DateTime? DateSubmitted { get; set; }

        /// <summary>
        /// The <c>DateTime</c> a job actually started execution.
        /// </summary>
        public DateTime? DateStarted { get; set; }

        /// <summary>
        /// The <c>DateTime</c> a job was completed/
        /// </summary>
        public DateTime? DateCompleted { get; set; }

        /// <summary>
        /// The <c>DateTime</c> a job was cancelled.
        /// </summary>
        public DateTime? DateCancelled { get; set; }

        /// <summary>
        /// The PT short project name to generate a typesetting preview of.
        /// </summary>
        public string ProjectName { get; set; }

        /// <summary>
        /// The user requesting the job.
        /// </summary>
        public string User { get; set; }

        /// <summary>
        /// Whether the job was submitted or not.
        /// </summary>
        public bool IsSubmitted => this.DateSubmitted != null;

        /// <summary>
        /// Whether or not the job has started execution or not.
        /// </summary>
        public bool IsStarted => this.DateStarted != null;

        /// <summary>
        /// Whether the job has been completed or not.
        /// </summary>
        public bool IsCompleted => this.DateCompleted != null;

        /// <summary>
        ///  Whether the job has been cancelled or not.
        /// </summary>
        public bool IsCancelled => this.DateCancelled != null;

        /// <summary>
        /// Whether or not there was an error during job execution.
        /// </summary>
        public bool IsError { get; set; }

        /// <summary>
        /// Error text of why error occurred; <c>null</c> otherwise.
        /// </summary>
        public string ErrorMessage { get; private set; }

        /// <summary>
        /// Font size in points.
        /// </summary>
        public float? FontSizeInPts { get; set; }

        /// <summary>
        /// Font leading in points.
        /// </summary>
        public float? FontLeadingInPts { get; set; }

        /// <summary>
        /// Page width in points.
        /// </summary>
        public float? PageWidthInPts { get; set; }

        /// <summary>
        /// Page height in points.
        /// </summary>
        public float? PageHeightInPts { get; set; }

        /// <summary>
        /// Page header in points.
        /// </summary>
        public float? PageHeaderInPts { get; set; }

        /// <summary>
        /// Book format, either TBOTB or CAV.
        /// </summary>
        public BookFormat? BookFormat { get; set; }

        /// <summary>
        /// Function used for indicating an error occurred and provide a message for the reason.
        /// </summary>
        /// <param name="errorMessage">Information about why the error occurred. (Required)</param>
        public void SetError(string errorMessage)
        {
            this.ErrorMessage = errorMessage ?? throw new ArgumentNullException(nameof(errorMessage));
            this.IsError = true;
        }

        /// <summary>
        /// PreviewJob constructor. Used for setting defaults.
        /// </summary>
        public PreviewJob()
        {
            // Set PreviewJob defaults.
            IsError = false;
        }
    }
}