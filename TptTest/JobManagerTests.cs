using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using TptMain.Http;
using TptMain.InDesign;
using TptMain.Jobs;
using TptMain.Models;
using TptMain.ParatextProjects;
using TptMain.Toolbox;
using TptMain.Util;

namespace TptTest
{
    [TestClass]
    public class JobManagerTests
    {
        /// <summary>
        /// Mock job manager logger.
        /// </summary>
        private Mock<ILogger<JobManager>> _mockLogger;

        /// <summary>
        /// Test configuration.
        /// </summary>
        private IConfiguration _testConfiguration;

        /// <summary>
        /// DB context.
        /// </summary>
        private TptServiceContext _context;

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
        /// Mock Preview Job Validator.
        /// </summary>
        private Mock<IPreviewJobValidator> _mockJobValidator;

        /// <summary>
        /// Mock Paratext Project Service API.
        /// </summary>
        private Mock<ParatextProjectService> _mockParatextProjectService;

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

            // - TemplateManager
            configKeys[TemplateManagerTests.TEST_TEMPLATE_SERVER_URI_KEY] = TemplateManagerTests.TEST_TEMPLATE_SERVER_URI;
            configKeys[TemplateManagerTests.TEST_TEMPLATE_TIMEOUT_IN_SEC_KEY] = TemplateManagerTests.TEST_TEMPLATE_TIMEOUT_IN_SEC;

            // - JobManager
            configKeys[ConfigConsts.IdmlDocDirKey] = TestConsts.TEST_IDML_DOC_DIR;
            configKeys[ConfigConsts.IdttDocDirKey] = TestConsts.TEST_IDTT_DOC_DIR;
            configKeys[ConfigConsts.ParatextDocDirKey] = TestConsts.TEST_PARATEXT_DOC_DIR;
            configKeys[ConfigConsts.PdfDocDirKey] = TestConsts.TEST_PDF_DOC_DIR;
            configKeys[ConfigConsts.ZipDocDirKey] = TestConsts.TEST_ZIP_DOC_DIR;
            configKeys[ConfigConsts.MaxDocAgeInSecKey] = TestConsts.TEST_DOC_MAX_AGE_IN_SEC;

            // - JobScheduler
            configKeys[JobSchedulerTests.TEST_MAX_CONCURRENT_JOBS_KEY] = JobSchedulerTests.TEST_MAX_CONCURRENT_JOBS;

            // The InMemoryCollection will snapshot the parameters upon creation, have to first populate the dictionary before passing it.
            _testConfiguration = new ConfigurationBuilder()
               .AddInMemoryCollection(configKeys)
               .Build();

            // preview context
            _context = new TptServiceContext(
                new DbContextOptionsBuilder<TptServiceContext>()
                    .UseInMemoryDatabase(Guid.NewGuid().ToString())
                    .Options);

            // mock: script runner
            var mockScriptRunnerLogger = new Mock<ILogger<ScriptRunner>>();
            _mockScriptRunner = new Mock<ScriptRunner>(
                mockScriptRunnerLogger.Object, _testConfiguration);

            // mock: web request factory
            var mockRequestFactoryLogger = new Mock<ILogger<WebRequestFactory>>();
            _mockRequestFactory = new Mock<WebRequestFactory>(MockBehavior.Strict,
                mockRequestFactoryLogger.Object);

            // mock: template manager
            var mockTemplateManagerLogger = new Mock<ILogger<TemplateManager>>();
            _mockTemplateManager = new Mock<TemplateManager>(MockBehavior.Strict,
                mockTemplateManagerLogger.Object, _testConfiguration, _mockRequestFactory.Object);

            // mock: preview job validator
            _mockJobValidator = new Mock<IPreviewJobValidator>();
            _mockJobValidator.Setup(validator =>
                validator.ValidatePreviewJob(It.IsAny<PreviewJob>()))
                .Verifiable();

            // mock: paratext project service
            var _mockParatextProjectServiceLogger = new Mock<ILogger<ParatextProjectService>>();
            _mockParatextProjectService = new Mock<ParatextProjectService>(MockBehavior.Strict,
                _mockParatextProjectServiceLogger.Object, _testConfiguration);

            // mock: job scheduler
            var mockJobSchedulerLogger = new Mock<ILogger<JobScheduler>>();
            _mockJobScheduler = new Mock<JobScheduler>(
                mockJobSchedulerLogger.Object, _testConfiguration);
        }

        /// <summary>
        /// Tests setup & ctor.
        /// </summary>
        [TestMethod]
        public void StartupTest()
        {
            // ctor
            var jobManager = new JobManager(
                _mockLogger.Object,
                _testConfiguration,
                _context,
                _mockScriptRunner.Object,
                _mockTemplateManager.Object,
                _mockJobValidator.Object,
                _mockParatextProjectService.Object,
                _mockJobScheduler.Object);

            jobManager.Dispose();
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
                _context,
                _mockScriptRunner.Object,
                _mockTemplateManager.Object,
                _mockJobValidator.Object,
                _mockParatextProjectService.Object,
                _mockJobScheduler.Object);

            var testPreviewJob = TestUtils.CreateTestPreviewJob();

            var expectedZipFileName = $@"{TestConsts.TEST_ZIP_DOC_DIR}\{MainConsts.PREVIEW_FILENAME_PREFIX}{testPreviewJob.Id}.zip";

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

        /// <summary>
        /// Test that the CheckPreviewJobs is called as expected and deletes old jobs.
        /// </summary>
        [TestMethod]
        public void TestCheckPreviewJobDeletion()
        {
            // setup service under test
            var mockJobManager =
                new Mock<JobManager>(
                    _mockLogger.Object,
                    _testConfiguration,
                    _context,
                    _mockScriptRunner.Object,
                    _mockTemplateManager.Object,
                    _mockJobValidator.Object,
                    _mockParatextProjectService.Object,
                    _mockJobScheduler.Object);
            // call base functions unless overriden
            mockJobManager.CallBase = true;
            _mockJobScheduler.CallBase = false;

            mockJobManager
                .Setup(jm => jm.CheckPreviewJobs())
                .CallBase()
                .Verifiable();

            // Add a couple of jobs to check
            var testPreviewJob1 = TestUtils.CreateTestPreviewJob();
            // this function expects a null Id.
            testPreviewJob1.Id = null;

            Assert.IsTrue(mockJobManager.Object.TryAddJob(testPreviewJob1, out var outputJob1));

            var testPreviewJob2 = TestUtils.CreateTestPreviewJob();
            // this function expects a null Id.
            testPreviewJob2.Id = null;

            Assert.IsTrue(mockJobManager.Object.TryAddJob(testPreviewJob2, out var outputJob2));

            // set the job's completed time to be before the threshold for deletion.
            outputJob2.DateCompleted = DateTime.Now.AddSeconds(Int32.Parse(TestConsts.TEST_DOC_MAX_AGE_IN_SEC) * -2);

            // allow enough time for the check scheduled job to execute
            Thread.Sleep((int)(MainConsts.TIMER_STARTUP_DELAY_IN_SEC + (2 * MainConsts.MAX_AGE_CHECK_DIVISOR)) * 1000);

            // verify that check jobs was called.
            mockJobManager.Verify(jm => jm.CheckPreviewJobs(), Times.Once);
            // verify that the "old" job was deleted, and the "young" job hasn't been.
            Assert.IsNotNull(_context.PreviewJobs.Find(outputJob1.Id));
            Assert.IsNull(_context.PreviewJobs.Find(outputJob2.Id));
        }

        /// <summary>
        /// Test that the CheckPreviewJobs and that works with the JobScheduler.
        /// </summary>
        [TestMethod]
        public void TestCheckPreviewJobRun()
        {
            // setup service under test
            var mockJobManager =
                new Mock<JobManager>(
                    _mockLogger.Object,
                    _testConfiguration,
                    _context,
                    _mockScriptRunner.Object,
                    _mockTemplateManager.Object,
                    _mockJobValidator.Object,
                    _mockParatextProjectService.Object,
                    _mockJobScheduler.Object);
            // call base functions unless overriden
            mockJobManager.CallBase = true;
            _mockJobScheduler.CallBase = true;

            mockJobManager
                .Setup(jm => jm.CheckPreviewJobs())
                .CallBase()
                .Verifiable();

            // Add a couple of jobs to check
            var testPreviewJob1 = TestUtils.CreateTestPreviewJob();
            // this function expects a null Id.
            testPreviewJob1.Id = null;

            Assert.IsTrue(mockJobManager.Object.TryAddJob(testPreviewJob1, out var outputJob1));

            var testPreviewJob2 = TestUtils.CreateTestPreviewJob();
            // this function expects a null Id.
            testPreviewJob2.Id = null;

            Assert.IsTrue(mockJobManager.Object.TryAddJob(testPreviewJob2, out var outputJob2));

            // allow enough time for the check scheduled job to execute
            Thread.Sleep((int)(MainConsts.TIMER_STARTUP_DELAY_IN_SEC + (2 * MainConsts.MAX_AGE_CHECK_DIVISOR)) * 1000);

            // verify that check jobs was called.
            mockJobManager.Verify(jm => jm.CheckPreviewJobs(), Times.Once);
            // verify that the "old" job was deleted, and the "young" job hasn't been.
            Assert.IsNotNull(_context.PreviewJobs.Find(outputJob1.Id));
            Assert.IsNotNull(_context.PreviewJobs.Find(outputJob2.Id));
        }

        /// <summary>
        /// test a failed job add
        /// </summary>
        [TestMethod]
        public void TestFailedAddJob()
        {
            // setup service under test
            var mockJobManager =
                new Mock<JobManager>(
                    _mockLogger.Object,
                    _testConfiguration,
                    _context,
                    _mockScriptRunner.Object,
                    _mockTemplateManager.Object,
                    _mockJobValidator.Object,
                    _mockParatextProjectService.Object,
                    _mockJobScheduler.Object);
            // call base functions unless overriden
            mockJobManager.CallBase = true;

            var testPreviewJob = TestUtils.CreateTestPreviewJob();

            // This will fail due to an Id being set on the input job.
            Assert.IsFalse(mockJobManager.Object.TryAddJob(testPreviewJob, out var outputJob));
        }

        /// <summary>
        /// test a successful job add
        /// </summary>
        [TestMethod]
        public void TestSuccessfulAddAndDeleteJob()
        {
            // setup service under test
            var mockJobManager =
                new Mock<JobManager>(
                    _mockLogger.Object,
                    _testConfiguration,
                    _context,
                    _mockScriptRunner.Object,
                    _mockTemplateManager.Object,
                    _mockJobValidator.Object,
                    _mockParatextProjectService.Object,
                    _mockJobScheduler.Object);
            // call base functions unless overriden
            mockJobManager.CallBase = true;

            var testPreviewJob = TestUtils.CreateTestPreviewJob();
            // this function expects a null Id.
            testPreviewJob.Id = null;
            // reset some of the values so that defaults are set.
            testPreviewJob.TypesettingParams.FontSizeInPts = null;
            testPreviewJob.TypesettingParams.FontLeadingInPts = null;
            testPreviewJob.TypesettingParams.PageWidthInPts = null;
            testPreviewJob.TypesettingParams.PageHeightInPts = null;
            testPreviewJob.TypesettingParams.PageHeaderInPts = null;
            testPreviewJob.TypesettingParams.BookFormat = null;

            // This will fail due to an Id being set on the input job.
            Assert.IsTrue(mockJobManager.Object.TryAddJob(testPreviewJob, out var createdJob));

            // Let's ensure that we can now delete this new job
            Assert.IsTrue(mockJobManager.Object.TryDeleteJob(createdJob.Id, out var deletedJob));

            // make sure that it's the job that we expect
            Assert.AreEqual(deletedJob, createdJob);
        }
    }
}
