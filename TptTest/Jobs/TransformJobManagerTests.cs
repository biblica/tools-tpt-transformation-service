using Microsoft.VisualStudio.TestTools.UnitTesting;
using TptMain.Jobs;
using System;
using System.Collections.Generic;
using Moq;
using Microsoft.Extensions.Logging;
using TptMain.Models;
using TptMain.Util;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using static TptMain.Jobs.TransformService;

namespace TptTest.Jobs
{
    [TestClass()]
    public class TransformJobManagerTests
    {
        private IConfiguration _testConfiguration;

        private Mock<ILogger<TemplateJobManager>> _mockTemplateManagerLogger;
        private Mock<ILogger<TransformService>> _mockTransformServiceLogger;
        private Mock<TransformService> _mockTransformService;

        private const string TIMEOUT_IN_SECS = "3600";
        private const string NEG_TIMEOUT_IN_SECS = "-3600";

        /// <summary>
        /// Set up for all tests
        /// </summary>
        [TestInitialize]
        public void TestSetup()
        {

            IDictionary<string, string> configKeys = new Dictionary<string, string>();
            configKeys[ConfigConsts.TemplateGenerationTimeoutInSecKey] = TIMEOUT_IN_SECS;

            _testConfiguration = new ConfigurationBuilder()
               .AddInMemoryCollection(configKeys)
               .Build();

            // mock: template job manager
            _mockTemplateManagerLogger = new Mock<ILogger<TemplateJobManager>>();

            // mock: transform service
            _mockTransformServiceLogger = new Mock<ILogger<TransformService>>();
            _mockTransformService = new Mock<TransformService>(_mockTransformServiceLogger.Object);

        }

        /// <summary>
        /// Test getting status
        /// </summary>
        [TestMethod()]
        public void GetStatusTest()
        {
            TemplateJobManager jobTemplateManager = new TemplateJobManager(_mockTemplateManagerLogger.Object, _testConfiguration, _mockTransformService.Object);

            var jobId = "1234";
            var previewJob = new PreviewJob()
            {
                Id = jobId
            };

            jobTemplateManager.ProcessJob(previewJob);
            jobTemplateManager.GetStatus(previewJob);

            Assert.IsFalse(previewJob.IsError);
            Assert.AreEqual(1, previewJob.State.Count);
        }

        /// <summary>
        /// Test getting an error
        /// </summary>
        [TestMethod()]
        public void ErrorTest()
        {
            TemplateJobManager jobTemplateManager = new TemplateJobManager(_mockTemplateManagerLogger.Object, _testConfiguration, _mockTransformService.Object);

            var jobId = "1234";
            var previewJob = new PreviewJob()
            {
                Id = jobId
            };

            jobTemplateManager.ProcessJob(previewJob);

            _mockTransformService.Setup(ts => ts.GetTransformJobStatus(It.IsAny<string>())).Returns(TransformJobStatus.ERROR);

            jobTemplateManager.GetStatus(previewJob);

            Assert.AreEqual(2, previewJob.State.Count);
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

            TemplateJobManager jobTemplateManager = new TemplateJobManager(_mockTemplateManagerLogger.Object, testConfiguration, _mockTransformService.Object);

            var jobId = "1234";
            var previewJob = new PreviewJob()
            {
                Id = jobId
            };

            jobTemplateManager.ProcessJob(previewJob);

            _mockTransformService.Setup(ts => ts.GetTransformJobStatus(It.IsAny<string>())).Returns(TransformJobStatus.TAGGED_TEXT_COMPLETE);

            // show that muliple calls adds to the stack of status in the list
            // Verifying that we are using the first, not the last, to calculate timeout
            jobTemplateManager.GetStatus(previewJob);
            jobTemplateManager.GetStatus(previewJob);
            jobTemplateManager.GetStatus(previewJob);

            Assert.AreEqual(4, previewJob.State.Count);
            Assert.IsTrue(previewJob.IsError);
        }
    }
}