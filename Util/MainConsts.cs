using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using tools_tpt_transformation_service.Models;

namespace tools_tpt_transformation_service.Util
{
    /// <summary>
    /// Utility constants, magic numbers, etc.
    /// </summary>
    public static class MainConsts
    {
        /// <summary>
        /// Default font size for previews, in points.
        /// </summary>
        public const float DEFAULT_FONT_SIZE_IN_PTS = 8f;

        /// <summary>
        /// Default font leading for previews, in points.
        /// </summary>
        public const float DEFAULT_FONT_LEADING_IN_PTS = 9f;

        /// <summary>
        /// Default page width for previews, in points.
        /// </summary>
        public const float DEFAULT_PAGE_WIDTH_IN_PTS = 396f;

        /// <summary>
        /// Default page height for previews, in points.
        /// </summary>
        public const float DEFAULT_PAGE_HEIGHT_IN_PTS = 612f;

        /// <summary>
        /// Default page header for previews, in points.
        /// </summary>
        public const float DEFAULT_PAGE_HEADER_IN_PTS = 18f;

        /// <summary>
        /// Default book format.
        /// </summary>
        public const BookFormat DEFAULT_BOOK_FORMAT = BookFormat.cav;

        /// <summary>
        /// Timer startup delay, in seconds.
        /// </summary>
        public const int TIMER_STARTUP_DELAY_IN_SEC = 60;

        /// <summary>
        /// What fraction of a max age to check for expiration.
        /// </summary>
        public const double MAX_AGE_CHECK_DIVISOR = 10.0;

        /// <summary>
        /// Default project prefix, should a language prefix not be detectable or usable.
        /// </summary>
        public const string DEFAULT_PROJECT_PREFIX = "Default";
    }
}
