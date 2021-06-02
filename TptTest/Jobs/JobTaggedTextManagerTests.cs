using Microsoft.VisualStudio.TestTools.UnitTesting;
using TptMain.Jobs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using Microsoft.Extensions.Logging;
using Castle.Core.Configuration;
using TptMain.Models;
using TptMain.Util;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using static TptMain.Jobs.TransformService;

namespace TptMain.Jobs.Tests
{
    [TestClass()]
    public class JobTaggedTextManagerTests
    {
        private Microsoft.Extensions.Configuration.IConfiguration _testConfiguration;
        private TptServiceContext _context;

        private Mock<ILogger<JobTaggedTextManager>> _mockLogger;
        private Mock<ILoggerFactory> _mockLoggerFactory;
        private Mock<TransformService> _mockTransformService;

        private const string TIMEOUT_IN_SECS = "3600";
        private const string NEG_TIMEOUT_IN_SECS = "-3600";

        /// <summary>
        /// Set up for all tests
        /// </summary>
        [TestInitialize]
        public void TestSetup()
        {

            _mockLogger = new Mock<ILogger<JobTaggedTextManager>>();
            _mockLoggerFactory = new Mock<ILoggerFactory>();
            _mockLoggerFactory.Setup(lm => lm.CreateLogger(It.IsAny<string>())).Returns(_mockLogger.Object);

            IDictionary<string, string> configKeys = new Dictionary<string, string>();
            configKeys[ConfigConsts.TemplateGenerationTimeoutInSecKey] = TIMEOUT_IN_SECS;

            _testConfiguration = new ConfigurationBuilder()
               .AddInMemoryCollection(configKeys)
               .Build();

            _context = new TptServiceContext(
                new DbContextOptionsBuilder<TptServiceContext>()
                    .UseInMemoryDatabase(Guid.NewGuid().ToString())
                    .Options);

            _mockTransformService = new Mock<TransformService>(_mockLogger.Object);

        }

        /// <summary>
        /// Test getting status
        /// </summary>
        [TestMethod()]
        public void GetStatusTest()
        {
            JobTemplateManager jobTemplateManager = new JobTemplateManager(_mockLoggerFactory.Object, _testConfiguration, _mockTransformService.Object);

            var jobId = "1234";
            var previewJob = new PreviewJob()
            {
                Id = jobId
            };

            jobTemplateManager.ProcessJob(previewJob);
            jobTemplateManager.GetStatus(previewJob);

            Assert.IsFalse(previewJob.IsError);
            Assert.AreEqual(3, previewJob.State.Count);
        }

        /// <summary>
        /// Test getting an error
        /// </summary>
        [TestMethod()]
        public void ErrorTest()
        {
            JobTemplateManager jobTemplateManager = new JobTemplateManager(_mockLoggerFactory.Object, _testConfiguration, _mockTransformService.Object);

            var jobId = "1234";
            var previewJob = new PreviewJob()
            {
                Id = jobId
            };

            jobTemplateManager.ProcessJob(previewJob);

            _mockTransformService.Setup(ts => ts.GetTransformJobStatus(It.IsAny<string>())).Returns(TransformJobStatus.ERROR);

            jobTemplateManager.GetStatus(previewJob);

            Assert.AreEqual(3, previewJob.State.Count);
            Assert.IsTrue(previewJob.IsError);
        }

        /// <summary>
        /// Test getting error when the timeout is reached
        /// </summary>
        [TestMethod()]
        public void TimeoutTest()
        {

            IDictionary<string, string> configKeys = new Dictionary<string, string>();
            configKeys[ConfigConsts.TemplateGenerationTimeoutInSecKey] = NEG_TIMEOUT_IN_SECS;

            Microsoft.Extensions.Configuration.IConfiguration testConfiguration = new ConfigurationBuilder()
               .AddInMemoryCollection(configKeys)
               .Build();

            JobTemplateManager jobTemplateManager = new JobTemplateManager(_mockLoggerFactory.Object, testConfiguration, _mockTransformService.Object);

            var jobId = "1234";
            var previewJob = new PreviewJob()
            {
                Id = jobId
            };

            jobTemplateManager.ProcessJob(previewJob);

            _mockTransformService.Setup(ts => ts.GetTransformJobStatus(It.IsAny<string>())).Returns(TransformJobStatus.TAGGED_TEXT_COMPLETE);

            jobTemplateManager.GetStatus(previewJob);
            jobTemplateManager.GetStatus(previewJob);
            jobTemplateManager.GetStatus(previewJob);

            Assert.AreEqual(8, previewJob.State.Count);
            Assert.IsTrue(previewJob.IsError);
        }
    }
}