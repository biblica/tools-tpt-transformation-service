using System;
using System.Text.Json.Serialization;

namespace TptMain.Models
{
    /// <summary>
    /// Represents the possible states of a Preview Job
    /// </summary>
    public enum PreviewJobState
    {
        Created,
        Submitted,
        Started,
        GeneratingTemplate,
        TemplateGenerated,
        GeneratingTaggedText,
        TaggedTextGenerated,
        GeneratingPreview,
        PreviewGenerated,
        Error
    }
    
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
        public bool IsError { get; set; } = false;

        /// <summary>
        /// The current state of the Preview Job
        /// </summary>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public PreviewJobState State { get; set; } = PreviewJobState.Created;

        /// <summary>
        /// User-friendly message regarding the error; <c>null</c> otherwise.
        /// </summary>
        public string ErrorMessage { get; private set; }

        /// <summary>
        /// More technical reason as to why the error occurred; <c>null</c> otherwise.
        /// </summary>
        public string ErrorDetail { get; private set; }

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
        /// (Optional) Whether or not to use custom footnotes. Defaults to false.
        /// 
        /// Note: The footnotes would be pulled from the project's respective Paratext footnote caller sequence.
        /// </summary>
        public bool UseCustomFootnotes { get; set; } = false;

        /// <summary>
        /// (Optional) Whether or not to use the project's defined font. Defaults to true.
        /// </summary>
        public bool UseProjectFont { get; set; } = true;

        /// <summary>
        /// Function used for indicating an error occurred and provide a message for the reason.
        /// </summary>
        /// <param name="errorMessage">User-friendly error message. (Required)</param>
        /// <param name="errorDetail">Information about why the error occurred. (Required)</param>
        public void SetError(string errorMessage, string errorDetail)
        {
            // validate inputs
            this.ErrorMessage = errorMessage ?? throw new ArgumentNullException(nameof(errorMessage));
            this.ErrorDetail = errorDetail ?? throw new ArgumentNullException(nameof(errorDetail));

            this.IsError = true;
        }
    }
}