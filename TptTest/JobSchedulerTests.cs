using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using TptMain.Jobs;

namespace TptTest
{
    [TestClass]
    public class JobSchedulerTests
    {
        // test config keys
        public const string TEST_MAX_CONCURRENT_JOBS_KEY = "Jobs:MaxConcurrent";

        // test config values
        public const string TEST_MAX_CONCURRENT_JOBS = "4";

        /// <summary>
        /// Mock job scheduler logger.
        /// </summary>
        private Mock<ILogger<JobScheduler>> _mockLogger;

        /// <summary>
        /// Mock test configuration.
        /// </summary>
        private TestConfiguration _testConfiguration;

        /// <summary>
        /// Test setup.
        /// </summary>
        [TestInitialize]
        public void TestSetup()
        {
            // create mocks
            _mockLogger = new Mock<ILogger<JobScheduler>>();

            // setup for ctor
            IDictionary<string, string> configKeys = new Dictionary<string, string>();
            configKeys[TEST_MAX_CONCURRENT_JOBS_KEY] = TEST_MAX_CONCURRENT_JOBS;
            _testConfiguration = new TestConfiguration(configKeys);
        }

        /// <summary>
        /// Tests setup & ctor.
        /// </summary>
        [TestMethod]
        public void InstantiateTest()
        {
            new JobScheduler(
                _mockLogger.Object,
                _testConfiguration);
            _testConfiguration.AssertIfNotAllKeysChecked();
        }
    }
}
