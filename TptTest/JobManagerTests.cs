using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using TptMain.Http;
using TptMain.InDesign;
using TptMain.Jobs;
using TptMain.Models;
using TptMain.Paratext;
using TptMain.Toolbox;
using TptMain.Util;

namespace TptTest
{
    [TestClass]
    public class JobManagerTests
    {
        // test config values
        public const string TEST_IDML_DOC_DIR = "C:\\Work\\IDML";
        public const string TEST_IDTT_DOC_DIR = "C:\\Work\\IDTT";
        public const string TEST_PDF_DOC_DIR = "C:\\Work\\PDF";
        public const string TEST_ZIP_DOC_DIR = "C:\\Work\\Zip";
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
            configKeys[JobManager.IdmlDocDirKey] = TEST_IDML_DOC_DIR;
            configKeys[JobManager.IdttDocDirKey] = TEST_IDTT_DOC_DIR;
            configKeys[JobManager.PdfDocDirKey] = TEST_PDF_DOC_DIR;
            configKeys[JobManager.ZipDocDirKey] = TEST_ZIP_DOC_DIR;
            configKeys[JobManager.MaxDocAgeInSecKey] = TEST_DOC_MAX_AGE_IN_SEC;

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

        delegate void TryGetJobCallback(string jobId, out PreviewJob previewJob);     // needed for Callback
        delegate bool TryGetJobReturns(out PreviewJob previewJob);      // needed for Returns

        /// <summary>
        /// Test the ability to download an archive of the typesetting files.
        /// </summary>
        [TestMethod]
        public void TestDownloadArchive()
        {

            // setup service under test
            var mockJobManager =
                new Mock<JobManager>(MockBehavior.Strict,
                    _mockLogger.Object,
                _testConfiguration,
                _mockContext.Object,
                _mockScriptRunner.Object,
                _mockTemplateManager.Object,
                _mockParatextApi.Object,
                _mockJobScheduler.Object);

            var testPreviewJob = TestUtils.CreateTestPreviewJob();

            var expectedZipFileName = $@"{TEST_ZIP_DOC_DIR}\{MainConsts.PREVIEW_FILENAME_PREFIX}{testPreviewJob.Id}.zip";

            // delete the file if it already exists
            if (File.Exists(expectedZipFileName))
            {
                File.Delete(expectedZipFileName);
            }

            // Mock expected calls for services NOT under test.
            mockJobManager
                .Setup(jobManager => jobManager.TryGetJob(testPreviewJob.Id, out It.Ref<PreviewJob>.IsAny))
                .Callback(new TryGetJobCallback((string testJobId, out PreviewJob previewJob) =>
                {
                    previewJob = testPreviewJob;
                }))
                .Returns(true);

            // Setup calls for service under test.
            mockJobManager
                .Setup(jobManager => jobManager
                .TryGetPreviewStream(testPreviewJob.Id, out It.Ref<FileStream>.IsAny, true))
                .CallBase();

            // Test archive creation.
            var jobManager = mockJobManager.Object;

            // Ensure the file was successfully created and is a zip file.
            Assert.IsTrue(jobManager.TryGetPreviewStream(testPreviewJob.Id, out var filestream, true), "File not successfully created.");
            Assert.IsNotNull(filestream, "Filestream not created.");
            Assert.AreEqual(Path.GetExtension(filestream.Name), ".zip");
        }
    }
}
