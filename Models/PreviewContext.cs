using Microsoft.EntityFrameworkCore;

namespace TptMain.Models
{
    /// <summary>
    /// Standard <c>DBContext</c> class. Used for storing <c>PreviewJob</c>s.
    /// </summary>
    public class PreviewContext : DbContext
    {
        /// <summary>
        /// Basic ctor, called by framework.
        /// </summary>
        /// <param name="options">Context options, provided by framework.</param>
        public PreviewContext(DbContextOptions<PreviewContext> options)
           : base(options)
        {
        }

        /// <summary>
        /// DB collection accessor for <c>PreviewJob</c>s.
        /// </summary>
        public DbSet<PreviewJob> PreviewJobs { get; set; }
    }
}