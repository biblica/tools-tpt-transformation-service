using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using tools_tpt_transformation_service.Jobs;

namespace tools_tpt_transformation_service.Models
{
    public class PreviewContext : DbContext
    {
        public PreviewContext(DbContextOptions<PreviewContext> options)
           : base(options)
        {
        }

        public DbSet<PreviewJob> PreviewJobs { get; set; }
    }
}
