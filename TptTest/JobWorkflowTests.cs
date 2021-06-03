
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    public class JobWorkflowTests
    {
        /// <summary>
        /// Mock job manager logger.
        /// </summary>
        private Mock<ILogger<JobManager>> _mockJobManagerLogger;

        /// <summary>
        /// Mock job manager.
        /// </summary>
        private Mock<JobManager> _mockJobManager;

        /// <summary>
        /// Mock preview manager.
        /// </summary>
        private Mock<IPreviewManager> _mockPreviewManager;

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
        /// Test configuration.
        /// </summary>
        private IConfiguration _testConfiguration;

        /// <summary>
        /// Test setup.
        /// </summary>
        [TestInitialize]
        public void TestSetup()
        {
            // Configuration Parameters
            IDictionary<string, string> configKeys = new Dictionary<string, string>();

            // - TemplateManager
            configKeys[TemplateManagerTests.TEST_TEMPLATE_SERVER_URI_KEY] = TemplateManagerTests.TEST_TEMPLATE_SERVER_URI;
            configKeys[TemplateManagerTests.TEST_TEMPLATE_TIMEOUT_IN_SEC_KEY] = TemplateManagerTests.TEST_TEMPLATE_TIMEOUT_IN_SEC;

            // - ParatextApi
            configKeys[ConfigConsts.ParatextApiServerUriKey] = ParatextApiTests.TEST_PT_API_SERVER_URI;
            configKeys[ConfigConsts.ParatextApiUsernameKey] = ParatextApiTests.TEST_PT_API_USERNAME;
            configKeys[ConfigConsts.ParatextApiPasswordKey] = ParatextApiTests.TEST_PT_API_PASSWORD;
            configKeys[ConfigConsts.ParatextApiProjectCacheAgeInSecKey] = ParatextApiTests.TEST_PT_API_PROJECT_CACHE_AGE_IN_SEC.ToString();
            for (var i = 0; i < ParatextApiTests.TEST_PT_API_ALLOWED_MEMBER_ROLES.Count; i++)
            {
                configKeys[ConfigConsts.ParatextApiAllowedMemberRolesKey + ":" + i] = ParatextApiTests.TEST_PT_API_ALLOWED_MEMBER_ROLES[i].ToString();
            }

            // - JobScheduler
            configKeys[JobSchedulerTests.TEST_MAX_CONCURRENT_JOBS_KEY] = JobSchedulerTests.TEST_MAX_CONCURRENT_JOBS;

            // - JobManager
            configKeys[ConfigConsts.IdmlDocDirKey] = TestConsts.TEST_IDML_DOC_DIR;
            configKeys[ConfigConsts.IdttDocDirKey] = TestConsts.TEST_IDTT_DOC_DIR;
            configKeys[ConfigConsts.ParatextDocDirKey] = TestConsts.TEST_PARATEXT_DOC_DIR;
            configKeys[ConfigConsts.PdfDocDirKey] = TestConsts.TEST_PDF_DOC_DIR;
            configKeys[ConfigConsts.ZipDocDirKey] = TestConsts.TEST_ZIP_DOC_DIR;
            configKeys[ConfigConsts.MaxDocAgeInSecKey] = TestConsts.TEST_DOC_MAX_AGE_IN_SEC;

            // The InMemoryCollection will snapshot the parameters upon creation, have to first populate the dictionary before passing it.
            _testConfiguration = new ConfigurationBuilder()
               .AddInMemoryCollection(configKeys)
               .Build();

            // create mocks
            _mockJobManagerLogger = new Mock<ILogger<JobManager>>();

            // preview context
            var _context = new TptServiceContext(
                new DbContextOptionsBuilder<TptServiceContext>()
                    .UseInMemoryDatabase(Guid.NewGuid().ToString())
                    .Options);

            // mock: preview manager
            _mockPreviewManager = new Mock<IPreviewManager>();

            // mock: web request factory
            var mockRequestFactoryLogger = new Mock<ILogger<WebRequestFactory>>();
            var mockRequestFactory = new Mock<WebRequestFactory>(MockBehavior.Strict,
                mockRequestFactoryLogger.Object);

            // mock: template manager
            var mockTemplateManagerLogger = new Mock<ILogger<TemplateManager>>();
            _mockTemplateManager = new Mock<TemplateManager>(MockBehavior.Strict,
                mockTemplateManagerLogger.Object, _testConfiguration, mockRequestFactory.Object);

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
            var mockJobScheduler = new Mock<JobScheduler>(MockBehavior.Strict,
                mockJobSchedulerLogger.Object, _testConfiguration);

            // mock: job manager
            _mockJobManager = new Mock<JobManager>(
                _mockJobManagerLogger.Object,
                _testConfiguration,
                _context,
                _mockPreviewManager.Object,
                _mockTemplateManager.Object,
                _mockJobValidator.Object,
                _mockParatextProjectService.Object,
                mockJobScheduler.Object);
        }

        /// <summary>
        /// Tests setup & ctor.
        /// </summary>
        [TestMethod]
        public void InstantiateTest()
        {
            new JobWorkflow(
                _mockJobManagerLogger.Object,
                _mockJobManager.Object,
                _mockPreviewManager.Object,
                _mockTemplateManager.Object,
                _mockJobValidator.Object,
                _mockParatextProjectService.Object,
                TestUtils.CreateTestPreviewJob());
        }

        /// <summary>
        /// Tests the complete job execution.
        /// </summary>
        [TestMethod]
        public void TestRunJobSuccess()
        {
            // setup service under test
            var testPreviewJob = TestUtils.CreateTestPreviewJob();
            var testFileInfo = new FileInfo(Path.Combine(
                TestConsts.TEST_IDML_DOC_DIR, $"{MainConsts.PREVIEW_FILENAME_PREFIX}{testPreviewJob.Id}.idml"));
            var mockWorkflow =
                new Mock<JobWorkflow>(MockBehavior.Strict,
                    _mockJobManagerLogger.Object,
                    _mockJobManager.Object,
                    _mockPreviewManager.Object,
                    _mockTemplateManager.Object,
                    _mockJobValidator.Object,
                    _mockParatextProjectService.Object,
                   testPreviewJob);
            mockWorkflow.Setup(workflowItem =>
                workflowItem.RunJob()).CallBase();
            mockWorkflow.Setup(workflowItem =>
                workflowItem.IsJobCanceled).CallBase();

            // setup mocks
            _mockParatextProjectService.Setup(ptProjService =>
                ptProjService.GetTextDirection(It.IsAny<string>()))
                .Returns(TextDirection.LTR)
                .Verifiable();
            IList<PreviewJob> jobUpdates = new List<PreviewJob>();
            var isTaskRun = false;
            _mockTemplateManager.Setup(managerItem =>
                managerItem.DownloadTemplateFile(testPreviewJob,
                    It.Is<FileInfo>(it => it.FullName.Equals(testFileInfo.FullName)),
                    It.IsAny<CancellationToken?>()))
                .Verifiable();
            _mockPreviewManager.Setup(runnerItem =>
                runnerItem.ProcessJob(testPreviewJob))
                .Callback(() =>
                {
                    isTaskRun = true;
                })
                .Verifiable();

            _mockJobManager.Setup(managerItem =>
                    managerItem.TryUpdateJob(testPreviewJob))
                .Callback<PreviewJob>(previewItem => jobUpdates.Add(previewItem.DeepClone()))
                .Returns(true);

            // execute
            mockWorkflow.Object.RunJob();

            // assert: check calls & results
            _mockTemplateManager.Verify(managerItem =>
                managerItem.DownloadTemplateFile(testPreviewJob,
                    It.Is<FileInfo>(it => it.FullName.Equals(testFileInfo.FullName)), It.IsAny<CancellationToken?>()),
                Times.Once);
            _mockPreviewManager.Verify(runnerItem =>
                runnerItem.ProcessJob(testPreviewJob), Times.Once);
            _mockJobManager.Verify(managerItem =>
                managerItem.TryUpdateJob(testPreviewJob), Times.AtLeastOnce);
            Assert.IsTrue(isTaskRun); // task was run
            Assert.IsFalse(mockWorkflow.Object.IsJobCanceled); // job wasn't cancelled

            // assert: check job updates
            Assert.AreEqual(2, jobUpdates.Count); // job was updated 2 times
            Assert.IsFalse(jobUpdates.Any(jobItem => jobItem.IsError)); // no errors
            Assert.AreEqual(2, jobUpdates.Sum(jobItem => (jobItem.IsStarted ? 1 : 0))); // both have start flags
            Assert.AreEqual(1, jobUpdates.Sum(jobItem => (jobItem.IsCompleted ? 1 : 0))); // one has completed flags
            Assert.AreEqual(0, jobUpdates.Sum(jobItem => (jobItem.IsCancelled ? 1 : 0))); // none are cancelled
            Assert.AreEqual(2, jobUpdates.Sum(jobItem => (jobItem.DateStarted != null ? 1 : 0))); // both have start times
            Assert.AreEqual(1, jobUpdates.Sum(jobItem => (jobItem.DateCompleted != null ? 1 : 0))); // one has completed times
            Assert.AreEqual(0, jobUpdates.Sum(jobItem => (jobItem.DateCancelled != null ? 1 : 0))); // none are cancelled
        }

        /// <summary>
        /// Tests early job termination.
        /// </summary>
        [TestMethod]
        public void TestCancelJobEarly()
        {
            // setup service under test
            var testPreviewJob = TestUtils.CreateTestPreviewJob();
            var testFileInfo = new FileInfo(Path.Combine(
                TestConsts.TEST_IDML_DOC_DIR, $"{MainConsts.PREVIEW_FILENAME_PREFIX}{testPreviewJob.Id}.idml"));
            var mockWorkflow =
                new Mock<JobWorkflow>(MockBehavior.Strict,
                    _mockJobManagerLogger.Object,
                    _mockJobManager.Object,
                    _mockPreviewManager.Object,
                    _mockTemplateManager.Object,
                    _mockJobValidator.Object,
                    _mockParatextProjectService.Object,
                    testPreviewJob);
            mockWorkflow.Setup(workflowItem =>
                workflowItem.RunJob()).CallBase();
            mockWorkflow.Setup(workflowItem =>
                workflowItem.CancelJob()).CallBase();
            mockWorkflow.Setup(workflowItem =>
                workflowItem.IsJobCanceled).CallBase();

            // setup mocks
            _mockParatextProjectService.Setup(ptProjService =>
                ptProjService.GetTextDirection(It.IsAny<string>()))
                .Returns(TextDirection.LTR);

            IList<PreviewJob> jobUpdates = new List<PreviewJob>();
            var isTaskRun = false;
            _mockTemplateManager.Setup(managerItem =>
                managerItem.DownloadTemplateFile(testPreviewJob,
                    It.Is<FileInfo>(it => it.FullName.Equals(testFileInfo.FullName)), It.IsAny<CancellationToken?>()))
                .Verifiable();
            _mockPreviewManager.Setup(runnerItem =>
                    runnerItem.ProcessJob(testPreviewJob))
                .Callback<PreviewJob>((previewItem) => {
                    isTaskRun = true;
                    while (!previewItem.IsCancelled)
                    {
                        Thread.Sleep(TimeSpan.FromMilliseconds(100));
                    }
                })
                .Verifiable();
            _mockPreviewManager.Setup(runnerItem =>
                    runnerItem.CancelJob(testPreviewJob))
                .Callback<PreviewJob>((previewItem) => {
                    previewItem.State.Add(new PreviewJobState(JobStateEnum.Cancelled));
                })
                .Verifiable();
            _mockJobManager.Setup(managerItem =>
                    managerItem.TryUpdateJob(testPreviewJob))
                .Callback<PreviewJob>(previewItem => jobUpdates.Add(previewItem.DeepClone()))
                .Returns(true);

            // execute
            // execute: start check in background thread, then cancel from test thread.
            var workThread = new Thread(() =>
                mockWorkflow.Object.RunJob())
            { IsBackground = true };
            workThread.Start();

            // wait a sec, then check for task started & still going
            Thread.Sleep(TimeSpan.FromSeconds(1));
            Assert.IsTrue(isTaskRun);
            Assert.IsTrue(workThread.IsAlive);

            // cancel
            mockWorkflow.Object.CancelJob();
            workThread.Join(TimeSpan.FromSeconds(1));
            Assert.IsFalse(workThread.IsAlive);

            // assert: check calls & results
            _mockTemplateManager.Verify(managerItem =>
                managerItem.DownloadTemplateFile(testPreviewJob,
                    It.Is<FileInfo>(it => it.FullName.Equals(testFileInfo.FullName)), It.IsAny<CancellationToken?>()),
                Times.Once);
            _mockPreviewManager.Verify(runnerItem =>
                runnerItem.ProcessJob(testPreviewJob), Times.Once);
            _mockJobManager.Verify(managerItem =>
                managerItem.TryUpdateJob(testPreviewJob), Times.AtLeastOnce);
            Assert.IsTrue(mockWorkflow.Object.IsJobCanceled); // job wasn't cancelled

            // assert: check job updates
            Assert.AreEqual(3, jobUpdates.Count); // job was updated 3 times
            Assert.IsFalse(jobUpdates.Any(jobItem => jobItem.IsError)); // no errors
            Assert.AreEqual(3, jobUpdates.Sum(jobItem => (jobItem.IsStarted ? 1 : 0))); // all have start flags
            Assert.AreEqual(1, jobUpdates.Sum(jobItem => (jobItem.IsCompleted ? 1 : 0))); // one has completed flags
            Assert.AreEqual(2, jobUpdates.Sum(jobItem => (jobItem.IsCancelled ? 1 : 0))); // two are cancelled
            Assert.AreEqual(3, jobUpdates.Sum(jobItem => (jobItem.DateStarted != null ? 1 : 0))); // all have start times
            Assert.AreEqual(1, jobUpdates.Sum(jobItem => (jobItem.DateCompleted != null ? 1 : 0))); // one has completed times
            Assert.AreEqual(2, jobUpdates.Sum(jobItem => (jobItem.DateCancelled != null ? 1 : 0))); // two are cancelled
        }

        /// <summary>
        /// Tests exceptions are generated from file downloads.
        /// </summary>
        [TestMethod]
        public void TestDownloadTemplateFileError()
        {
            // setup service under test
            var testPreviewJob = TestUtils.CreateTestPreviewJob();
            var testFileInfo = new FileInfo(Path.Combine(
                TestConsts.TEST_IDML_DOC_DIR, $"{MainConsts.PREVIEW_FILENAME_PREFIX}{testPreviewJob.Id}.idml"));
            var mockWorkflow =
                new Mock<JobWorkflow>(MockBehavior.Strict,
                    _mockJobManagerLogger.Object,
                    _mockJobManager.Object,
                    _mockPreviewManager.Object,
                    _mockTemplateManager.Object,
                    _mockJobValidator.Object,
                    _mockParatextProjectService.Object,
                    testPreviewJob);
            mockWorkflow.Setup(workflowItem =>
                workflowItem.RunJob()).CallBase();
            mockWorkflow.Setup(workflowItem =>
                workflowItem.IsJobCanceled).CallBase();

            // setup mocks
            _mockParatextProjectService.Setup(ptProjService =>
                ptProjService.GetTextDirection(It.IsAny<string>()))
                .Returns(TextDirection.LTR)
                .Verifiable();

            IList<PreviewJob> jobUpdates = new List<PreviewJob>();
            _mockTemplateManager.Setup(managerItem =>
                managerItem.DownloadTemplateFile(testPreviewJob,
                    It.Is<FileInfo>(it => it.FullName.Equals(testFileInfo.FullName)), It.IsAny<CancellationToken?>()))
                .CallBase()
                .Verifiable();
            _mockJobManager.Setup(managerItem =>
                    managerItem.TryUpdateJob(testPreviewJob))
                .Callback<PreviewJob>(previewItem => jobUpdates.Add(previewItem.DeepClone()))
                .Returns(true);

            // execute
            mockWorkflow.Object.RunJob();

            // assert: check calls & results
            _mockTemplateManager.Verify(managerItem =>
                managerItem.DownloadTemplateFile(testPreviewJob,
                    It.Is<FileInfo>(it => it.FullName.Equals(testFileInfo.FullName)), It.IsAny<CancellationToken?>()),
                Times.Once);
            _mockJobManager.Verify(managerItem =>
                managerItem.TryUpdateJob(testPreviewJob), Times.AtLeastOnce);
            Assert.IsFalse(mockWorkflow.Object.IsJobCanceled); // job wasn't cancelled

            // assert: check job updates
            Assert.AreEqual(2, jobUpdates.Count); // job was updated 2 times
            Assert.AreEqual(1, jobUpdates.Sum(jobItem => jobItem.IsError ? 1 : 0)); // one error
            Assert.AreEqual(2, jobUpdates.Sum(jobItem => (jobItem.IsStarted ? 1 : 0))); // both have start flags
            Assert.AreEqual(1, jobUpdates.Sum(jobItem => (jobItem.IsCompleted ? 1 : 0))); // one has completed flags
            Assert.AreEqual(0, jobUpdates.Sum(jobItem => (jobItem.IsCancelled ? 1 : 0))); // none are cancelled
            Assert.AreEqual(2, jobUpdates.Sum(jobItem => (jobItem.DateStarted != null ? 1 : 0))); // both have start times
            Assert.AreEqual(1, jobUpdates.Sum(jobItem => (jobItem.DateCompleted != null ? 1 : 0))); // one has completed times
            Assert.AreEqual(0, jobUpdates.Sum(jobItem => (jobItem.DateCancelled != null ? 1 : 0))); // none are cancelled
        }

        /// <summary>
        /// Test the processing of additional preview job options (EG: custom footnotes).
        /// </summary>
        [TestMethod]
        public void TestRunJobCustomOptions()
        {
            // set up job with additional options
            var testPreviewJob = TestUtils.CreateTestPreviewJob();
            testPreviewJob.TypesettingParams.UseCustomFootnotes = true;
            testPreviewJob.TypesettingParams.UseProjectFont = true;

            // setup service under test
            var mockWorkflow =
                new Mock<JobWorkflow>(
                    _mockJobManagerLogger.Object,
                    _mockJobManager.Object,
                    _mockPreviewManager.Object,
                    _mockTemplateManager.Object,
                    _mockJobValidator.Object,
                    _mockParatextProjectService.Object,
                    testPreviewJob);
            mockWorkflow.CallBase = true;

            // setup mocks of the expected custom handling
            _mockParatextProjectService.Setup(ptProjService =>
                ptProjService.GetTextDirection(It.IsAny<string>()))
                .Returns(TextDirection.LTR)
                .Verifiable();
            _mockParatextProjectService.Setup(ptProjectService =>
                ptProjectService.GetFootnoteCallerSequence(testPreviewJob.BibleSelectionParams.ProjectName))
                .Returns(new string[] { "a", "b"})
                .Verifiable();
            _mockParatextProjectService.Setup(ptProjectService =>
                ptProjectService.GetProjectFont(testPreviewJob.BibleSelectionParams.ProjectName))
                .Returns("Arial")
                .Verifiable();
            _mockTemplateManager.Setup(managerItem =>
                    managerItem.DownloadTemplateFile(testPreviewJob, It.IsAny<FileInfo>(), It.IsAny<CancellationToken?>()))
                .Verifiable();
            _mockPreviewManager.Setup(runnerItem =>
                    runnerItem.ProcessJob(testPreviewJob))
                .Verifiable();

            // execute
            mockWorkflow.Object.RunJob();

            // verify the expected customizations occurred
            _mockParatextProjectService.Verify(ptProjectService =>
                ptProjectService.GetFootnoteCallerSequence(testPreviewJob.BibleSelectionParams.ProjectName), Times.Once);
            _mockParatextProjectService.Verify(ptProjectService =>
                ptProjectService.GetProjectFont(testPreviewJob.BibleSelectionParams.ProjectName), Times.Once);
            _mockTemplateManager.Verify(managerItem =>
                    managerItem.DownloadTemplateFile(testPreviewJob, It.IsAny<FileInfo>(), It.IsAny<CancellationToken?>()), Times.Once);
            _mockPreviewManager.Verify(runnerItem =>
                runnerItem.ProcessJob(testPreviewJob), Times.Once);

        }

        /// <summary>
        /// Tests exceptions are generated from IDS script failures.
        /// </summary>
        [TestMethod]
        public void TestRunScriptError()
        {
            // setup service under test
            var testPreviewJob = TestUtils.CreateTestPreviewJob();
            var testFileInfo = new FileInfo(Path.Combine(
                TestConsts.TEST_IDML_DOC_DIR, $"{MainConsts.PREVIEW_FILENAME_PREFIX}{testPreviewJob.Id}.idml"));
            var mockWorkflow =
                new Mock<JobWorkflow>(
                    _mockJobManagerLogger.Object,
                    _mockJobManager.Object,
                    _mockPreviewManager.Object,
                    _mockTemplateManager.Object,
                    _mockJobValidator.Object,
                    _mockParatextProjectService.Object,
                    testPreviewJob);
            mockWorkflow.CallBase = true;

            // setup mocks
            _mockParatextProjectService.Setup(ptProjService =>
                ptProjService.GetTextDirection(It.IsAny<string>()))
                .Returns(TextDirection.LTR)
                .Verifiable();

            IList<PreviewJob> jobUpdates = new List<PreviewJob>();
            var isTaskRun = false;
            _mockTemplateManager.Setup(managerItem =>
                    managerItem.DownloadTemplateFile(testPreviewJob,
                        It.Is<FileInfo>(it => it.FullName.Equals(testFileInfo.FullName)), It.IsAny<CancellationToken?>()))
                .Verifiable();
            _mockPreviewManager.Setup(runnerItem =>
                    runnerItem.ProcessJob(testPreviewJob))
                .Callback(() =>
                {
                    isTaskRun = true;
                    throw new IOException();
                })
                .Verifiable();
            _mockJobManager.Setup(managerItem =>
                    managerItem.TryUpdateJob(testPreviewJob))
                .Callback<PreviewJob>(previewItem => jobUpdates.Add(previewItem.DeepClone()))
                .Returns(true);

            // execute
            mockWorkflow.Object.RunJob();

            // assert: check calls & results
            _mockTemplateManager.Verify(managerItem =>
                managerItem.DownloadTemplateFile(testPreviewJob,
                    It.Is<FileInfo>(it => it.FullName.Equals(testFileInfo.FullName)), It.IsAny<CancellationToken?>()),
                Times.Once);
            _mockPreviewManager.Verify(runnerItem =>
                runnerItem.ProcessJob(testPreviewJob), Times.Once);
            _mockJobManager.Verify(managerItem =>
                managerItem.TryUpdateJob(testPreviewJob), Times.AtLeastOnce);
            Assert.IsTrue(isTaskRun); // task was run
            Assert.IsFalse(mockWorkflow.Object.IsJobCanceled); // job wasn't cancelled

            // assert: check job updates
            Assert.AreEqual(2, jobUpdates.Count); // job was updated 2 times
            Assert.AreEqual(1, jobUpdates.Sum(jobItem => jobItem.IsError ? 1 : 0)); // one error
            Assert.AreEqual(2, jobUpdates.Sum(jobItem => (jobItem.IsStarted ? 1 : 0))); // both have start flags
            Assert.AreEqual(1, jobUpdates.Sum(jobItem => (jobItem.IsCompleted ? 1 : 0))); // one has completed flags
            Assert.AreEqual(0, jobUpdates.Sum(jobItem => (jobItem.IsCancelled ? 1 : 0))); // none are cancelled
            Assert.AreEqual(2, jobUpdates.Sum(jobItem => (jobItem.DateStarted != null ? 1 : 0))); // both have start times
            Assert.AreEqual(1, jobUpdates.Sum(jobItem => (jobItem.DateCompleted != null ? 1 : 0))); // one has completed times
            Assert.AreEqual(0, jobUpdates.Sum(jobItem => (jobItem.DateCancelled != null ? 1 : 0))); // none are cancelled
        }
    }
}
