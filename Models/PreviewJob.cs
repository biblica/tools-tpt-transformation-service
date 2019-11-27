using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace tools_tpt_transformation_service.Models
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
        /// Whether the job was submitted or not.
        /// </summary>
        public bool IsSubmitted { get => this.DateSubmitted != null; }

        /// <summary>
        /// Whether or not the job has started execution or not.
        /// </summary>
        public bool IsStarted { get => this.DateStarted != null; }

        /// <summary>
        /// Whether the job has been completed or not.
        /// </summary>
        public bool IsCompleted { get => this.DateCompleted != null; }

        /// <summary>
        ///  Whether the job has been cancelled or not.
        /// </summary>
        public bool IsCancelled { get => this.DateCancelled != null; }

        /// <summary>
        /// Whether or not there was an error during job execution.
        /// </summary>
        public bool IsError { get; set; }

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
    }
}
