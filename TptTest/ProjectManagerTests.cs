using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using TptMain.InDesign;
using TptMain.Projects;

namespace TptTest
{
    [TestClass]
    public class ProjectManagerTests
    {
        // test config keys
        public const string TEST_IDTT_DIR_KEY = "Docs:IDTT:Directory";
        public const string TEST_IDT_CHECK_INTERVAL_IN_SEC_KEY = "Docs:IDTT:CheckIntervalInSec";

        // test config values
        public const string TEST_IDTT_DIR = "C:\\Work\\IDTT";
        public const string TEST_IDT_CHECK_INTERVAL_IN_SEC = "120";

        /// <summary>
        /// Mock project manager logger.
        /// </summary>
        private Mock<ILogger<ProjectManager>> _mockLogger;

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
            _mockLogger = new Mock<ILogger<ProjectManager>>();

            // setup for ctor
            IDictionary<string, string> configKeys = new Dictionary<string, string>();
            configKeys[TEST_IDTT_DIR_KEY] = TEST_IDTT_DIR;
            configKeys[TEST_IDT_CHECK_INTERVAL_IN_SEC_KEY] = TEST_IDT_CHECK_INTERVAL_IN_SEC;
            _testConfiguration = new TestConfiguration(configKeys);
        }

        /// <summary>
        /// Tests setup & ctor.
        /// </summary>
        [TestMethod]
        public void InstantiateTest()
        {
            var projectManager =
                new ProjectManager(
                    _mockLogger.Object,
                    _testConfiguration);
            _testConfiguration.AssertIfNotAllKeysChecked();
        }
    }
}
