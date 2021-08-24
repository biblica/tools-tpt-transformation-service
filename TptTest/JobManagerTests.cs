using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using TptMain.Jobs;
using TptMain.Models;
using TptMain.Util;

namespace TptTest
{
    [TestClass]
    public class JobManagerTests
    {
        // Consts
        private const string JOB_FILE_MANAGER_ROOT_DIR = @"Resources\JobData";

        /// <summary>
        /// Test configuration.
        /// </summary>
        private IConfiguration _testConfiguration;

        /// <summary>
        /// DB context.
        /// </summary>
        private TptServiceContext _context;

        /// <summary>
        /// Mock job manager logger.
        /// </summary>
        private Mock<ILogger<JobManager>> _mockLogger;

        /// <summary>
        /// Mock Job File Manager.
        /// </summary>
        private Mock<JobFileManager> _mockJobFileManager;

        /// <summary>
        /// Mock Preview Job Validator.
        /// </summary>
        private Mock<IPreviewJobValidator> _mockJobValidator;

        /// <summary>
        /// Mock Transform Service.
        /// </summary>
        private Mock<ITransformService> _mockTransformService;

        /// <summary>
        /// Mock Template Job Manager.
        /// </summary>
        private Mock<TemplateJobManager> _mockTemplateJobManager;

        /// <summary>
        /// Mock Tagged Text JobManager.
        /// </summary>
        private Mock<TaggedTextJobManager> _mockTaggedTextJobManager;

        /// <summary>
        /// Mock Preview Manager.
        /// </summary>
        private Mock<IPreviewManager> _mockPreviewManager;

        /// <summary>
        /// Test setup.
        /// </summary>
        [TestInitialize]
        public void TestSetup()
        {
            // Configuration Parameters
            IDictionary<string, string> configKeys = new Dictionary<string, string>();

            // - JobFileManager
            configKeys[ConfigConsts.ProcessedJobFilesRootDirKey] = JOB_FILE_MANAGER_ROOT_DIR;
            // - TemplateJobManager
            configKeys[ConfigConsts.TemplateGenerationTimeoutInSecKey] = "3600";
            // - TaggedTextJobManager
            configKeys[ConfigConsts.TaggedTextGenerationTimeoutInSecKey] = "3600";
            // - JobManager
            configKeys[ConfigConsts.JobProcessIntervalInSecKey] = "30";
            configKeys[ConfigConsts.MaxDocAgeInSecKey] = "60";

            // The InMemoryCollection will snapshot the parameters upon creation, have to first populate the dictionary before passing it.
            _testConfiguration = new ConfigurationBuilder()
               .AddInMemoryCollection(configKeys)
               .Build();


            // create mocks
            _mockLogger = new Mock<ILogger<JobManager>>();

            // preview context
            _context = new TptServiceContext(
                new DbContextOptionsBuilder<TptServiceContext>()
                    .UseInMemoryDatabase(Guid.NewGuid().ToString())
                    .Options);

            // mock: JobFileManager
            _mockJobFileManager = new Mock<JobFileManager>(
                new Mock<ILogger<JobFileManager>>().Object,
                _testConfiguration);
            _mockJobFileManager
                .Setup(jobFileManager => jobFileManager.GetTemplateDirectoryById(It.IsAny<string>()))
                .Returns(new DirectoryInfo(JOB_FILE_MANAGER_ROOT_DIR));
            _mockJobFileManager
                .Setup(jobFileManager => jobFileManager.GetTaggedTextDirectoryById(It.IsAny<string>()))
                .Returns(new DirectoryInfo(JOB_FILE_MANAGER_ROOT_DIR));
            _mockJobFileManager
                .Setup(jobFileManager => jobFileManager.GetPreviewDirectoryById(It.IsAny<string>()))
                .Returns(new DirectoryInfo(JOB_FILE_MANAGER_ROOT_DIR));


            // mock: IPreviewJobValidator
            _mockJobValidator = new Mock<IPreviewJobValidator>();
            _mockJobValidator.Setup(validator =>
                validator.ProcessJob(It.IsAny<PreviewJob>()))
                .Verifiable();

            // mock: ITransformService
            _mockTransformService = new Mock<ITransformService>();

            // mock: TemplateJobManager
            _mockTemplateJobManager = new Mock<TemplateJobManager>(
                new Mock<ILogger<TemplateJobManager>>().Object,
                _testConfiguration,
                _mockTransformService.Object);

            // mock: TaggedTextJobManager
            _mockTaggedTextJobManager = new Mock<TaggedTextJobManager>(
                new Mock<ILogger<TaggedTextJobManager>>().Object,
                _testConfiguration,
                _mockTransformService.Object);

            // mock: IPreviewManager
            _mockPreviewManager = new Mock<IPreviewManager>();
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
                _mockJobFileManager.Object,
                _mockJobValidator.Object,
                _mockTemplateJobManager.Object,
                _mockTaggedTextJobManager.Object,
                _mockPreviewManager.Object);

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
                new Mock<JobManager>(
                    _mockLogger.Object,
                    _testConfiguration,
                    _context,
                    _mockJobFileManager.Object,
                    _mockJobValidator.Object,
                    _mockTemplateJobManager.Object,
                    _mockTaggedTextJobManager.Object,
                    _mockPreviewManager.Object);

            var testPreviewJob = TestUtils.CreateTestPreviewJob();

            var expectedZipFileName = $@"{TestConsts.TEST_ZIP_DOC_DIR}\{testPreviewJob.Id}.zip";

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
                    _mockJobFileManager.Object,
                    _mockJobValidator.Object,
                    _mockTemplateJobManager.Object,
                    _mockTaggedTextJobManager.Object,
                    _mockPreviewManager.Object);
            // call base functions unless overriden
            mockJobManager.CallBase = true;

            mockJobManager
                .Setup(jm => jm.ProcessJobs())
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
            mockJobManager.Verify(jm => jm.ProcessJobs(), Times.Once);
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
                    _mockJobFileManager.Object,
                    _mockJobValidator.Object,
                    _mockTemplateJobManager.Object,
                    _mockTaggedTextJobManager.Object,
                    _mockPreviewManager.Object);
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
                    _mockJobFileManager.Object,
                    _mockJobValidator.Object,
                    _mockTemplateJobManager.Object,
                    _mockTaggedTextJobManager.Object,
                    _mockPreviewManager.Object);
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
