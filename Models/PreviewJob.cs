using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace tools_tpt_transformation_service.Models
{
    public class PreviewJob
    {
        public string Id { get; set; }

        public DateTime? DateSubmitted { get; set; }

        public DateTime? DateStarted { get; set; }

        public DateTime? DateCompleted { get; set; }

        public DateTime? DateCancelled { get; set; }

        public string ProjectName { get; set; }

        public bool IsSubmitted { get => this.DateSubmitted != null; }

        public bool IsStarted { get => this.DateStarted != null; }

        public bool IsCompleted { get => this.DateCompleted != null; }

        public bool IsCancelled { get => this.DateCancelled != null; }

        public bool IsError { get; set; }
    }
}
