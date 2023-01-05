/*
Copyright © 2021 by Biblica, Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
using TptMain.Models;

namespace TptMain.Util
{
    /// <summary>
    /// Utility constants, magic numbers, etc.
    /// </summary>
    public static class MainConsts
    {
        #region Typesetting Selection Constants
        /// <summary>
        /// Allowed font sizes for previews, in points.
        /// </summary>
        public static readonly FloatMinMaxDefault ALLOWED_FONT_SIZE_IN_PTS = new FloatMinMaxDefault(8f, 6f, 24f);

        /// <summary>
        /// Allowed font leading sizes for previews, in points.
        /// </summary>
        public static readonly FloatMinMaxDefault ALLOWED_FONT_LEADING_IN_PTS = new FloatMinMaxDefault(9f, 6f, 28f);

        /// <summary>
        /// Allowed page widths for previews, in points.
        /// </summary>
        public static readonly FloatMinMaxDefault ALLOWED_PAGE_WIDTH_IN_PTS = new FloatMinMaxDefault(396f, 180f, 612f);

        /// <summary>
        /// Allowed page heights for previews, in points.
        /// </summary>
        public static readonly FloatMinMaxDefault ALLOWED_PAGE_HEIGHT_IN_PTS = new FloatMinMaxDefault(612f, 216f, 1008f);

        /// <summary>
        /// Allowed page header sizes for previews, in points.
        /// </summary>
        public static readonly FloatMinMaxDefault ALLOWED_PAGE_HEADER_IN_PTS = new FloatMinMaxDefault(18f, 0f, 50f);

        /// <summary>
        /// Default book format.
        /// </summary>
        public const BookFormat DEFAULT_BOOK_FORMAT = BookFormat.cav;
        #endregion


        /// <summary>
        /// Timer startup delay, in seconds.
        /// </summary>
        public const int TIMER_STARTUP_DELAY_IN_SEC = 10;

        /// <summary>
        /// What fraction of a max age to check for expiration.
        /// </summary>
        public const double MAX_AGE_CHECK_DIVISOR = 5.0;

        /// <summary>
        /// Default project prefix, should a language prefix not be detectable or usable.
        /// </summary>
        public const string DEFAULT_PROJECT_PREFIX = "Default";

        /// <summary>
        /// Paratext project settings filename.
        /// </summary>
        public const string SETTINGS_FILE_PATH = "Settings.xml";

        /// <summary>
        /// The filepath of a file known to be in every Paratext project directory. 
        /// This assists in determining if a directory is a Paratext project directory or not.
        /// </summary>
        public const string PARATEXT_PROJECT_KNOWN_FILE_PATH = "ProjectUserAccess.xml";


    }
}