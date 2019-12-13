using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using TptMain.InDesign;

namespace TptTest
{
    [TestClass]
    public class ScriptRunnerTests
    {
        // config keys
        public const string TEST_IDS_URI_KEY = "InDesign:ServerUri";
        public const string TEST_IDS_TIMEOUT_KEY = "InDesign:TimeoutInSec";
        public const string TEST_IDS_PREVIEW_SCRIPT_DIR_KEY = "InDesign:PreviewScriptDirectory";
        public const string TEST_IDS_PREVIEW_SCRIPT_NAME_FORMAT_KEY = "InDesign:PreviewScriptNameFormat";

        // config values
        public const string TEST_IDS_URI = "http://172.31.10.90:9876/service";
        public const string TEST_IDS_TIMEOUT = "600";
        public const string TEST_IDS_PREVIEW_SCRIPT_DIR = "C:\\Work\\JSX";
        public const string TEST_IDS_PREVIEW_SCRIPT_NAME_FORMAT = "TypesettingPreview{0}.jsx";

        /// <summary>
        /// Mock script runner logger.
        /// </summary>
        private Mock<ILogger<ScriptRunner>> _mockLogger;

        /// <summary>
        /// Test configuration.
        /// </summary>
        private TestConfiguration _testConfiguration;

        /// <summary>
        /// Test setup.
        /// </summary>
        [TestInitialize]
        public void TestSetup()
        {
            // create mocks
            _mockLogger = new Mock<ILogger<ScriptRunner>>();

            // setup for ctor
            IDictionary<string, string> configKeys = new Dictionary<string, string>();
            configKeys[TEST_IDS_URI_KEY] = TEST_IDS_URI;
            configKeys[TEST_IDS_TIMEOUT_KEY] = TEST_IDS_TIMEOUT;
            configKeys[TEST_IDS_PREVIEW_SCRIPT_DIR_KEY] = TEST_IDS_PREVIEW_SCRIPT_DIR;
            configKeys[TEST_IDS_PREVIEW_SCRIPT_NAME_FORMAT_KEY] = TEST_IDS_PREVIEW_SCRIPT_NAME_FORMAT;
            _testConfiguration = new TestConfiguration(configKeys);
        }

        /// <summary>
        /// Tests setup & ctor.
        /// </summary>
        [TestMethod]
        public void InstantiateTest()
        {
            var scriptRunner =
                new ScriptRunner(
                    _mockLogger.Object,
                    _testConfiguration);
            _testConfiguration.AssertIfNotAllKeysChecked();
        }
    }
}
