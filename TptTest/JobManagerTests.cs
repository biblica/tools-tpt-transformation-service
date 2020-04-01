using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using TptMain.Http;
using TptMain.InDesign;
using TptMain.Jobs;
using TptMain.Models;
using TptMain.Paratext;
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
        private IConfiguration _testConfiguration;

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
        /// Mock Paratext API.
        /// </summary>
        private Mock<ParatextApi> _mockParatextApi;

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

            // Configuration Parameters
            // - ScriptRunner
            configKeys[ScriptRunnerTests.TEST_IDS_URI_KEY] = ScriptRunnerTests.TEST_IDS_URI;
            configKeys[ScriptRunnerTests.TEST_IDS_TIMEOUT_KEY] = ScriptRunnerTests.TEST_IDS_TIMEOUT;
            configKeys[ScriptRunnerTests.TEST_IDS_PREVIEW_SCRIPT_DIR_KEY] = ScriptRunnerTests.TEST_IDS_TIMEOUT;
            configKeys[ScriptRunnerTests.TEST_IDS_PREVIEW_SCRIPT_NAME_FORMAT_KEY] = ScriptRunnerTests.TEST_IDS_PREVIEW_SCRIPT_NAME_FORMAT;

            // - TemplateManager
            configKeys[TemplateManagerTests.TEST_TEMPLATE_SERVER_URI_KEY] = TemplateManagerTests.TEST_TEMPLATE_SERVER_URI;
            configKeys[TemplateManagerTests.TEST_TEMPLATE_TIMEOUT_IN_SEC_KEY] = TemplateManagerTests.TEST_TEMPLATE_TIMEOUT_IN_SEC;

            // - ParatextApi
            configKeys[ParatextApi.ParatextApiServerUriKey] = ParatextApiTests.TEST_PT_API_SERVER_URI;
            configKeys[ParatextApi.ParatextApiUsernameKey] = ParatextApiTests.TEST_PT_API_USERNAME;
            configKeys[ParatextApi.ParatextApiPasswordKey] = ParatextApiTests.TEST_PT_API_PASSWORD;
            configKeys[ParatextApi.ParatextApiProjectCacheAgeInSecKey] = ParatextApiTests.TEST_PT_API_PROJECT_CACHE_AGE_IN_SEC.ToString();
            for (var i = 0; i < ParatextApiTests.TEST_PT_API_ALLOWED_MEMBER_ROLES.Count; i++)
            {
                configKeys[ParatextApi.ParatextApiAllowedMemberRolesKey + ":" + i] = ParatextApiTests.TEST_PT_API_ALLOWED_MEMBER_ROLES[i].ToString();
            }

            // - JobManager
            configKeys[TEST_IDML_DOC_DIR_KEY] = TEST_IDML_DOC_DIR;
            configKeys[TEST_PDF_DOC_DIR_KEY] = TEST_PDF_DOC_DIR;
            configKeys[TEST_DOC_MAX_AGE_IN_SEC_KEY] = TEST_DOC_MAX_AGE_IN_SEC;

            // - JobScheduler
            configKeys[JobSchedulerTests.TEST_MAX_CONCURRENT_JOBS_KEY] = JobSchedulerTests.TEST_MAX_CONCURRENT_JOBS;

            // The InMemoryCollection will snapshot the parameters upon creation, have to first populate the dictionary before passing it.
            _testConfiguration = new ConfigurationBuilder()
               .AddInMemoryCollection(configKeys)
               .Build();

            // mock: preview context
            _mockContext = new Mock<PreviewContext>(MockBehavior.Strict,
                new DbContextOptionsBuilder<PreviewContext>()
                    .UseInMemoryDatabase(Guid.NewGuid().ToString())
                    .Options);

            // mock: script runner
            var mockScriptRunnerLogger = new Mock<ILogger<ScriptRunner>>();
            _mockScriptRunner = new Mock<ScriptRunner>(MockBehavior.Strict,
                mockScriptRunnerLogger.Object, _testConfiguration);

            // mock: web request factory
            var mockRequestFactoryLogger = new Mock<ILogger<WebRequestFactory>>();
            _mockRequestFactory = new Mock<WebRequestFactory>(MockBehavior.Strict,
                mockRequestFactoryLogger.Object);

            // mock: template manager
            var mockTemplateManagerLogger = new Mock<ILogger<TemplateManager>>();
            _mockTemplateManager = new Mock<TemplateManager>(MockBehavior.Strict,
                mockTemplateManagerLogger.Object, _testConfiguration, _mockRequestFactory.Object);

            // mock: paratext API
            var mockParatextApiLogger = new Mock<ILogger<ParatextApi>>();
            _mockParatextApi = new Mock<ParatextApi>(MockBehavior.Strict,
                mockParatextApiLogger.Object, _testConfiguration);

            // mock: job scheduler
            var mockJobSchedulerLogger = new Mock<ILogger<JobScheduler>>();
            _mockJobScheduler = new Mock<JobScheduler>(MockBehavior.Strict,
                mockJobSchedulerLogger.Object, _testConfiguration);
        }

        /// <summary>
        /// Tests setup & ctor.
        /// </summary>
        [TestMethod]
        public void InstantiateTest()
        {
            // ctor
            new JobManager(
                _mockLogger.Object,
                _testConfiguration,
                _mockContext.Object,
                _mockScriptRunner.Object,
                _mockTemplateManager.Object,
                _mockParatextApi.Object,
                _mockJobScheduler.Object);
        }
    }
}
