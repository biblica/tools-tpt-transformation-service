using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TptMain.Http;
using TptMain.InDesign;
using TptMain.Jobs;
using TptMain.Models;
using TptMain.Toolbox;

namespace TptTest
{
    [TestClass]
    public class JobWorkflowTests
    {
        /// <summary>
        /// Mock job manager logger.
        /// </summary>
        private Mock<ILogger<JobManager>> _mockLogger;

        /// <summary>
        /// Mock job manager.
        /// </summary>
        private Mock<JobManager> _mockJobManager;

        /// <summary>
        /// Mock script runner.
        /// </summary>
        private Mock<ScriptRunner> _mockScriptRunner;

        /// <summary>
        /// Mock template manager.
        /// </summary>
        private Mock<TemplateManager> _mockTemplateManager;

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
            _mockLogger = new Mock<ILogger<JobManager>>();
            IDictionary<string, string> configKeys = new Dictionary<string, string>();
            _testConfiguration = new TestConfiguration(configKeys);

            // mock: preview context
            var mockContext = new Mock<PreviewContext>(MockBehavior.Strict,
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
            var mockRequestFactory = new Mock<WebRequestFactory>(MockBehavior.Strict,
                mockRequestFactoryLogger.Object);

            // mock: template manager
            var mockTemplateManagerLogger = new Mock<ILogger<TemplateManager>>();
            configKeys[TemplateManagerTests.TEST_TEMPLATE_SERVER_URI_KEY] = TemplateManagerTests.TEST_TEMPLATE_SERVER_URI;
            configKeys[TemplateManagerTests.TEST_TEMPLATE_TIMEOUT_IN_SEC_KEY] = TemplateManagerTests.TEST_TEMPLATE_TIMEOUT_IN_SEC;
            _mockTemplateManager = new Mock<TemplateManager>(MockBehavior.Strict,
                mockTemplateManagerLogger.Object, _testConfiguration, mockRequestFactory.Object);

            // mock: job scheduler
            var mockJobSchedulerLogger = new Mock<ILogger<JobScheduler>>();
            configKeys[JobSchedulerTests.TEST_MAX_CONCURRENT_JOBS_KEY] = JobSchedulerTests.TEST_MAX_CONCURRENT_JOBS;
            var mockJobScheduler = new Mock<JobScheduler>(MockBehavior.Strict,
                mockJobSchedulerLogger.Object, _testConfiguration);

            // mock: job manager
            var mockJobManagerLogger = new Mock<ILogger<JobManager>>();
            configKeys[JobManagerTests.TEST_IDML_DOC_DIR_KEY] = JobManagerTests.TEST_IDML_DOC_DIR;
            configKeys[JobManagerTests.TEST_PDF_DOC_DIR_KEY] = JobManagerTests.TEST_PDF_DOC_DIR;
            configKeys[JobManagerTests.TEST_DOC_MAX_AGE_IN_SEC_KEY] = JobManagerTests.TEST_DOC_MAX_AGE_IN_SEC;
            _mockJobManager = new Mock<JobManager>(MockBehavior.Strict,
                _mockLogger.Object, _testConfiguration, mockContext.Object, _mockScriptRunner.Object, _mockTemplateManager.Object, mockJobScheduler.Object);
        }

        /// <summary>
        /// Tests setup & ctor.
        /// </summary>
        [TestMethod]
        public void InstantiateTest()
        {
            var jobWorkflow =
                new JobWorkflow(
                    _mockLogger.Object,
                    _mockJobManager.Object,
                    _mockScriptRunner.Object,
                    _mockTemplateManager.Object,
                    TestUtils.CreateTestPreviewJob());
            _testConfiguration.AssertIfNotAllKeysChecked();
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
                JobManagerTests.TEST_IDML_DOC_DIR, $"preview-{testPreviewJob.Id}.idml"));
            var mockWorkflow =
                new Mock<JobWorkflow>(MockBehavior.Strict,
                    _mockLogger.Object,
                    _mockJobManager.Object,
                    _mockScriptRunner.Object,
                    _mockTemplateManager.Object,
                    testPreviewJob);
            mockWorkflow.Setup(workflowItem =>
                workflowItem.RunJob()).CallBase();
            mockWorkflow.Setup(workflowItem =>
                workflowItem.IsJobCanceled).CallBase();

            // setup mocks
            IList<PreviewJob> jobUpdates = new List<PreviewJob>();
            var isTaskRun = false;
            _mockTemplateManager.Setup(managerItem =>
                managerItem.DownloadTemplateFile(testPreviewJob,
                    It.Is<FileInfo>(it => it.FullName.Equals(testFileInfo.FullName))))
                .Verifiable();
            _mockScriptRunner.Setup(runnerItem =>
                runnerItem.RunScript(testPreviewJob))
                .Callback<PreviewJob>(jobItem =>
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
                    It.Is<FileInfo>(it => it.FullName.Equals(testFileInfo.FullName))),
                Times.Once);
            _mockScriptRunner.Verify(runnerItem =>
                runnerItem.RunScript(testPreviewJob), Times.Once);
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
                JobManagerTests.TEST_IDML_DOC_DIR, $"preview-{testPreviewJob.Id}.idml"));
            var mockWorkflow =
                new Mock<JobWorkflow>(MockBehavior.Strict,
                    _mockLogger.Object,
                    _mockJobManager.Object,
                    _mockScriptRunner.Object,
                    _mockTemplateManager.Object,
                    testPreviewJob);
            mockWorkflow.Setup(workflowItem =>
                workflowItem.RunJob()).CallBase();
            mockWorkflow.Setup(workflowItem =>
                workflowItem.CancelJob()).CallBase();
            mockWorkflow.Setup(workflowItem =>
                workflowItem.IsJobCanceled).CallBase();

            // setup mocks
            IList<PreviewJob> jobUpdates = new List<PreviewJob>();
            var isTaskRun = false;
            _mockTemplateManager.Setup(managerItem =>
                managerItem.DownloadTemplateFile(testPreviewJob,
                    It.Is<FileInfo>(it => it.FullName.Equals(testFileInfo.FullName))))
                .Verifiable();
            _mockScriptRunner.Setup(runnerItem =>
                runnerItem.RunScript(testPreviewJob))
                .Callback<PreviewJob>(jobItem =>
                {
                    isTaskRun = true;
                    Thread.Sleep(TimeSpan.FromSeconds(5));
                })
                .Verifiable();
            _mockJobManager.Setup(managerItem =>
                    managerItem.TryUpdateJob(testPreviewJob))
                .Callback<PreviewJob>(previewItem => jobUpdates.Add(previewItem.DeepClone()))
                .Returns(true);

            // execute
            // execute: start check in background thread, then cancel from test thread.
            Thread workThread = new Thread(() =>
                mockWorkflow.Object.RunJob())
            { IsBackground = true };
            workThread.Start();

            // wait a sec, then check for task started & still going
            Thread.Sleep(TimeSpan.FromSeconds(1));
            Assert.IsTrue(isTaskRun);
            Assert.IsTrue(workThread.IsAlive);

            // cancel
            mockWorkflow.Object.CancelJob();

            // assert: check calls & results
            _mockTemplateManager.Verify(managerItem =>
                managerItem.DownloadTemplateFile(testPreviewJob,
                    It.Is<FileInfo>(it => it.FullName.Equals(testFileInfo.FullName))),
                Times.Once);
            _mockScriptRunner.Verify(runnerItem =>
                runnerItem.RunScript(testPreviewJob), Times.Once);
            _mockJobManager.Verify(managerItem =>
                managerItem.TryUpdateJob(testPreviewJob), Times.AtLeastOnce);
            Assert.IsTrue(mockWorkflow.Object.IsJobCanceled); // job wasn't cancelled

            // assert: check job updates
            Assert.AreEqual(2, jobUpdates.Count); // job was updated 2 times
            Assert.IsFalse(jobUpdates.Any(jobItem => jobItem.IsError)); // no errors
            Assert.AreEqual(2, jobUpdates.Sum(jobItem => (jobItem.IsStarted ? 1 : 0))); // both have start flags
            Assert.AreEqual(0, jobUpdates.Sum(jobItem => (jobItem.IsCompleted ? 1 : 0))); // none has completed flags
            Assert.AreEqual(1, jobUpdates.Sum(jobItem => (jobItem.IsCancelled ? 1 : 0))); // one is cancelled
            Assert.AreEqual(2, jobUpdates.Sum(jobItem => (jobItem.DateStarted != null ? 1 : 0))); // both have start times
            Assert.AreEqual(0, jobUpdates.Sum(jobItem => (jobItem.DateCompleted != null ? 1 : 0))); // none has completed times
            Assert.AreEqual(1, jobUpdates.Sum(jobItem => (jobItem.DateCancelled != null ? 1 : 0))); // one is cancelled
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
                JobManagerTests.TEST_IDML_DOC_DIR, $"preview-{testPreviewJob.Id}.idml"));
            var mockWorkflow =
                new Mock<JobWorkflow>(MockBehavior.Strict,
                    _mockLogger.Object,
                    _mockJobManager.Object,
                    _mockScriptRunner.Object,
                    _mockTemplateManager.Object,
                    testPreviewJob);
            mockWorkflow.Setup(workflowItem =>
                workflowItem.RunJob()).CallBase();
            mockWorkflow.Setup(workflowItem =>
                workflowItem.IsJobCanceled).CallBase();

            // setup mocks
            IList<PreviewJob> jobUpdates = new List<PreviewJob>();
            _mockTemplateManager.Setup(managerItem =>
                managerItem.DownloadTemplateFile(testPreviewJob,
                    It.Is<FileInfo>(it => it.FullName.Equals(testFileInfo.FullName))))
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
                    It.Is<FileInfo>(it => it.FullName.Equals(testFileInfo.FullName))),
                Times.Once);
            _mockScriptRunner.Verify(runnerItem =>
                runnerItem.RunScript(testPreviewJob), Times.Once);
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
        /// Tests exceptions are generated from IDS script failures.
        /// </summary>
        [TestMethod]
        public void TestRunScriptError()
        {
            // setup service under test
            var testPreviewJob = TestUtils.CreateTestPreviewJob();
            var testFileInfo = new FileInfo(Path.Combine(
                JobManagerTests.TEST_IDML_DOC_DIR, $"preview-{testPreviewJob.Id}.idml"));
            var mockWorkflow =
                new Mock<JobWorkflow>(MockBehavior.Strict,
                    _mockLogger.Object,
                    _mockJobManager.Object,
                    _mockScriptRunner.Object,
                    _mockTemplateManager.Object,
                    testPreviewJob);
            mockWorkflow.Setup(workflowItem =>
                workflowItem.RunJob()).CallBase();
            mockWorkflow.Setup(workflowItem =>
                workflowItem.IsJobCanceled).CallBase();

            // setup mocks
            IList<PreviewJob> jobUpdates = new List<PreviewJob>();
            var isTaskRun = false;
            _mockTemplateManager.Setup(managerItem =>
                    managerItem.DownloadTemplateFile(testPreviewJob,
                        It.Is<FileInfo>(it => it.FullName.Equals(testFileInfo.FullName))))
                .Verifiable();
            _mockScriptRunner.Setup(runnerItem =>
                    runnerItem.RunScript(testPreviewJob))
                .Callback<PreviewJob>(jobItem =>
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
                    It.Is<FileInfo>(it => it.FullName.Equals(testFileInfo.FullName))),
                Times.Once);
            _mockScriptRunner.Verify(runnerItem =>
                runnerItem.RunScript(testPreviewJob), Times.Once);
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
