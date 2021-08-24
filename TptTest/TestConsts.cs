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
