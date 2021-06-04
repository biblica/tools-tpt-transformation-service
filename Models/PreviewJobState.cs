using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace TptMain.Models
{
    /// <summary>
    /// Represents the possible states of a Preview Job. These are ordered by the steps within the processing workflow.
    /// </summary>
    public enum JobStateEnum
    {
        Submitted = 1,
        Started = 2,
        GeneratingTemplate = 3,
        TemplateGenerated = 4,
        GeneratingTaggedText = 3,
        TaggedTextGenerated = 4,
        GeneratingPreview = 5,
        Cancelled = 6,
        Error = 7,
        PreviewGenerated = 8
    }

    /// <summary>
    /// The potential sources of records about the job state
    /// </summary>
    public enum JobStateSourceEnum
    {
        JobValidation,
        TemplateGeneration,
        TaggedTextGeneration,
        PreviewGeneration,
        GeneralManagement
    }

    /// <summary>
    /// For sorting based on oldest record
    /// </summary>
    public class PreviewJobStateOldestComparator : IComparer<PreviewJobState>
    {
        public int Compare(PreviewJobState x, PreviewJobState y)
        {
            return x.DateSubmitted.CompareTo(y.DateSubmitted);
        }
    }

    /// <summary>
    /// A preview job state. There may be more than one of these states associated with a preview job record
    /// </summary>
    public class PreviewJobState : IEquatable<PreviewJobState>, IComparable<PreviewJobState>
    {

        /// <summary>
        /// Constructor, specifying state
        /// </summary>
        /// <param name="state">The state to register</param>
        public PreviewJobState(JobStateEnum state)
        {
            State = state;
        }

        /// <summary>
        /// Constructor specifying state and source
        /// </summary>
        /// <param name="state">The state to register</param>
        /// <param name="source">The source of the state</param>
        public PreviewJobState(JobStateEnum state, JobStateSourceEnum source) : this(state)
        {
            State = state;
            Source = source;
        }

        /// <summary>
        /// Constructor specifying state, source, and time
        /// 
        /// This constructor is mainly used for testing purposes to expire jobs correctly
        /// </summary>
        /// <param name="state">The state to register</param>
        /// <param name="source">The source of the state</param>
        /// <param name="dateTime">The date and time of the entry if not using the default (now)</param>
        public PreviewJobState(JobStateEnum state, JobStateSourceEnum source, DateTime dateTime) : this(state)
        {
            State = state;
            Source = source;
            DateSubmitted = dateTime;
        }

        /// <summary>
        /// Database unique id
        /// </summary>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }
        /// <summary>
        /// When this state was added to the job. This is for reference purposes and too see what the most recent state is
        /// </summary>
        public DateTime DateSubmitted { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// The state of this state.
        /// </summary>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public JobStateEnum State { get; set; }
        /// <summary>
        /// The processing source of this state
        /// </summary>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public JobStateSourceEnum Source { get; set; } = JobStateSourceEnum.GeneralManagement;

        /// <summary>
        /// Override for Sorting based on Date. If they are the same, compare based on status.
        /// </summary>
        /// <param name="other">The record to compare to</param>
        /// <returns>Standard CompareTo responses, see IComparer.
        /// In this case, return the comparison b/t states if the dates are equal, otherwise the date comparison.
        /// </returns>
        public int CompareTo(PreviewJobState other)
        {
            if (other == null)
            {
                return 1;
            }
            else
            {
                if (this.DateSubmitted.Equals(other.DateSubmitted))
                {
                    return ((int)this.State).CompareTo((int)other.State);
                }
                else
                {
                    return this.DateSubmitted.CompareTo(other.DateSubmitted);
                }
            }
        }

        /// <summary>
        /// Override the Equals to just the state so that we can find if the set of states contains the looked for one
        /// </summary>
        /// <param name="other">The record to compare this one to</param>
        /// <returns>Standard responses for Equals. In this case, comparing the State.</returns>
        public bool Equals(PreviewJobState other)
        {
            if (other == null) return false;
            return this.State.Equals(other.State);
        }
    }
}
