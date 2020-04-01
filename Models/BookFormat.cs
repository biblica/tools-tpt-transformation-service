using System.Diagnostics.CodeAnalysis;

namespace TptMain.Models
{
    /// <summary>
    /// Book format.
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum BookFormat
    {
        /// <summary>
        /// The Books of the Bible
        /// </summary>
        tbotb,

        /// <summary>
        /// Chapter and Verse.
        /// </summary>
        cav
    }
}