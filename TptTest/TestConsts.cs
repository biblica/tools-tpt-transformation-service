/*
Copyright © 2021 by Biblica, Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
using System.Net.Http;
using TptMain.Util;

namespace TptTest
{
    /// <summary>
    /// Utility constants for testing.
    /// </summary>
    public static class TestConsts
    {
        /* Configuration-related constants */
        // Configuration values
        public const string TEST_IDML_DOC_DIR = "C:\\Work\\IDML";
        public const string TEST_IDTT_DOC_DIR = "C:\\Work\\IDTT";
        public const string TEST_PARATEXT_DOC_DIR = "C:\\Work\\Paratext";
        public const string TEST_PDF_DOC_DIR = "C:\\Work\\PDF";
        public const string TEST_ZIP_DOC_DIR = "C:\\Work\\Zip";
        public const string TEST_DOC_MAX_AGE_IN_SEC = "86400";

        /* Template-related constants & similar. */
        /// <summary>
        /// Test project name.
        /// </summary>
        public const string TEST_PROJECT_NAME = "testProjectName";

        /// <summary>
        /// Test project name.
        /// </summary>
        public const string TEST_USER = "Chaddington";

        /// <summary>
        /// Test HTTP method.
        /// </summary>
        public static readonly string TestHttpMethodName = HttpMethod.Get.Method;

        /// <summary>
        /// Test template timeout in milliseconds.
        /// </summary>
        public const int TEST_TEMPLATE_TIMEOUT_IN_M_SEC = 600000;

        /// <summary>
        /// Expected test request URI.
        /// </summary>
        public static readonly string TestRequestUri =
            "http://localhost:3000/idtt/template/create?book_type=cav&"
            + "font_size=" + MainConsts.ALLOWED_FONT_SIZE_IN_PTS.Default.ToString()
            + "&header_size=" + MainConsts.ALLOWED_PAGE_HEADER_IN_PTS.Default.ToString()
            + "&leading=" + MainConsts.ALLOWED_FONT_LEADING_IN_PTS.Default.ToString()
            + "&page_height=" + MainConsts.ALLOWED_PAGE_HEIGHT_IN_PTS.Default.ToString()
            + "&page_width=" + MainConsts.ALLOWED_PAGE_WIDTH_IN_PTS.Default.ToString();
    }
}
