using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using TptMain.Http;
using TptMain.InDesign;
using TptMain.Jobs;
using TptMain.Models;
using TptMain.Toolbox;

namespace TptTest
{
    [TestClass]
    public class JobManagerTests
    {
        // test config keys
        public const string TEST_IDML_DOC_DIR_KEY = "Docs:IDML:Directory";
        public const string TEST_PDF_DOC_DIR_KEY = "Docs:PDF:Directory";
        public const string TEST_DOC_MAX_AGE_IN_SEC_KEY = "Docs:MaxAgeInSec";

        // test config values
        public const string TEST_IDML_DOC_DIR = "C:\\Work\\IDML";
        public const string TEST_PDF_DOC_DIR = "C:\\Work\\PDF";
        public const string TEST_DOC_MAX_AGE_IN_SEC = "86400";

        /// <summary>
        /// Mock job manager logger.
        /// </summary>
        private Mock<ILogger<JobManager>> _mockLogger;

        /// <summary>
        /// Test configuration.
        /// </summary>
        private TestConfiguration _testConfiguration;

        /// <summary>
        /// Mock DB context.
        /// </summary>
        private Mock<PreviewContext> _mockContext;

        /// <summary>
        /// Mock script runner.
        /// </summary>
        private Mock<ScriptRunner> _mockScriptRunner;

        /// <summary>
        /// Mock request factory.
        /// </summary>
        private Mock<WebRequestFactory> _mockRequestFactory;

        /// <summary>
        /// Mock template manager.
        /// </summary>
        private Mock<TemplateManager> _mockTemplateManager;

        /// <summary>
        /// Mock job scheduler.
        /// </summary>
        private Mock<JobScheduler> _mockJobScheduler;

        /// <summary>
        /// Test setup.
        /// </summary>
        [TestInitialize]
        public void TestSetup()
        {
            // create mocks
            _mockLogger = new Mock<ILogger<JobManager>>();
            IDictionary<string, string> configKeys = new Dictionary<string, string>();
            _testConfiguration = new TestConfiguration(configKeys);

            // mock: preview context
            _mockContext = new Mock<PreviewContext>(MockBehavior.Strict,
                new DbContextOptionsBuilder<PreviewContext>()
                    .UseInMemoryDatabase(Guid.NewGuid().ToString())
                    .Options);

            // mock: script runner
            var mockScriptRunnerLogger = new Mock<ILogger<ScriptRunner>>();
            configKeys[ScriptRunnerTests.TEST_IDS_URI_KEY] = ScriptRunnerTests.TEST_IDS_URI;
            configKeys[ScriptRunnerTests.TEST_IDS_TIMEOUT_KEY] = ScriptRunnerTests.TEST_IDS_TIMEOUT;
            configKeys[ScriptRunnerTests.TEST_IDS_PREVIEW_SCRIPT_DIR_KEY] = ScriptRunnerTests.TEST_IDS_TIMEOUT;
            configKeys[ScriptRunnerTests.TEST_IDS_PREVIEW_SCRIPT_NAME_FORMAT_KEY] = ScriptRunnerTests.TEST_IDS_PREVIEW_SCRIPT_NAME_FORMAT;
            _mockScriptRunner = new Mock<ScriptRunner>(MockBehavior.Strict,
                mockScriptRunnerLogger.Object, _testConfiguration);

            // mock: web request factory
            var mockRequestFactoryLogger = new Mock<ILogger<WebRequestFactory>>();
            _mockRequestFactory = new Mock<WebRequestFactory>(MockBehavior.Strict,
                mockRequestFactoryLogger.Object);

            // mock: template manager
            var mockTemplateManagerLogger = new Mock<ILogger<TemplateManager>>();
            configKeys[TemplateManagerTests.TEST_TEMPLATE_SERVER_URI_KEY] = TemplateManagerTests.TEST_TEMPLATE_SERVER_URI;
            configKeys[TemplateManagerTests.TEST_TEMPLATE_TIMEOUT_IN_SEC_KEY] = TemplateManagerTests.TEST_TEMPLATE_TIMEOUT_IN_SEC;
            _mockTemplateManager = new Mock<TemplateManager>(MockBehavior.Strict,
                mockTemplateManagerLogger.Object, _testConfiguration, _mockRequestFactory.Object);

            // mock: job scheduler
            var mockJobSchedulerLogger = new Mock<ILogger<JobScheduler>>();
            configKeys[JobSchedulerTests.TEST_MAX_CONCURRENT_JOBS_KEY] = JobSchedulerTests.TEST_MAX_CONCURRENT_JOBS;
            _mockJobScheduler = new Mock<JobScheduler>(MockBehavior.Strict,
                mockJobSchedulerLogger.Object, _testConfiguration);

            // additional config keys
            configKeys[TEST_IDML_DOC_DIR_KEY] = TEST_IDML_DOC_DIR;
            configKeys[TEST_PDF_DOC_DIR_KEY] = TEST_PDF_DOC_DIR;
            configKeys[TEST_DOC_MAX_AGE_IN_SEC_KEY] = TEST_DOC_MAX_AGE_IN_SEC;

        }

        /// <summary>
        /// Tests setup & ctor.
        /// </summary>
        [TestMethod]
        public void InstantiateTest()
        {
            // ctor
            var jobManager =
                new JobManager(
                    _mockLogger.Object,
                    _testConfiguration,
                    _mockContext.Object,
                    _mockScriptRunner.Object,
                    _mockTemplateManager.Object,
                    _mockJobScheduler.Object);
            _testConfiguration.AssertIfNotAllKeysChecked();
        }
    }
}
