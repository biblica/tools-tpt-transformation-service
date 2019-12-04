using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace tools_tpt_transformation_service.Models
{
    /// <summary>
    /// Project details (high-level summary) model object.
    /// </summary>
    public class ProjectDetails
    {
        /// <summary>
        /// Project name.
        /// </summary>
        public string ProjectName { get; set; }

        /// <summary>
        /// Project updated date/time.
        /// 
        /// This is currently the latest file modification time found in the Paratext project directory.
        /// </summary>
        public DateTime ProjectUpdated { get; set; }
    }
}
