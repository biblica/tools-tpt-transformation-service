using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace TptTest
{
    /// <summary>
    /// Utility constants for testing.
    /// </summary>
    public static class TestConsts
    {
        /* Template-related constants & similar. */

        /// <summary>
        /// Test project name.
        /// </summary>
        public const string TEST_PROJECT_NAME = "testProjectName";

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
        public const string TestRequestUri =
            "http://localhost:3000/idtt/template/create?book_type=cav&font_size=123.4&header_size=567.8&leading=234.5&page_height=345.6&page_width=456.7";
    }
}
