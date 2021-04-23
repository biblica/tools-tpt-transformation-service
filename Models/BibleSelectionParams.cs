namespace TptMain.Models
{
    /// <summary>
    /// Defines parameters that select which books of the Bible to include in a Preview Job
    /// </summary>
    public class BibleSelectionParams
    {
        /// <summary>
        /// Unique identifier for params
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The Paratext short project name
        /// </summary>
        public string ProjectName { get; set; }

        /// <summary>
        /// A comma-separated list of Bible books to include
        /// </summary>
        public string SelectedBooks { get; set; }
        
        /// <summary>
        /// Whether to include ancillary material
        /// </summary>
        public bool IncludeAncillary { get; set; } = false;
    }
}