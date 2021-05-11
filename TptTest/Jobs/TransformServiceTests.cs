using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TptMain.Jobs;
using TptMain.Models;

namespace TptTest.Jobs
{
    /// <summary>
    /// Test class for submitting jobs to the queue
    /// </summary>
    [TestClass]
    public class TransformServiceTests
    {
        private ILogger<TransformServiceTests> _logger;

        public TransformServiceTests()
        {
            // create a real logger
            var serviceProvider = new ServiceCollection()
            .AddLogging()
            .BuildServiceProvider();

            var factory = serviceProvider.GetService<ILoggerFactory>();

            _logger = factory.CreateLogger<TransformServiceTests>();
        }

        /// <summary>
        /// Test submitting jobs for tagged text generation to the queue
        /// </summary>
        [TestMethod]
        public void GenerateTaggedTextTest()
        {
            PreviewJob previewJob = TestUtils.CreateTestPreviewJob();
            previewJob.Id = Guid.NewGuid().ToString();
            previewJob.BibleSelectionParams.Id = Guid.NewGuid().ToString();
            previewJob.BibleSelectionParams.SelectedBooks = "GEN";
            previewJob.TypesettingParams.Id = Guid.NewGuid().ToString();

            TransformService transformService = new TransformService(_logger);

            string uniqueMessageId = transformService.GenerateTaggedText(previewJob);

            Assert.IsNotNull(uniqueMessageId);
        }

        /// <summary>
        /// Test submitting jobs for template generation to the queue
        /// </summary>
        [TestMethod]
        public void GenerateTemplateTest()
        {
            PreviewJob previewJob = TestUtils.CreateTestPreviewJob();
            previewJob.Id = Guid.NewGuid().ToString();
            previewJob.BibleSelectionParams.Id = Guid.NewGuid().ToString();
            previewJob.BibleSelectionParams.SelectedBooks = "GEN";
            previewJob.TypesettingParams.Id = Guid.NewGuid().ToString();

            TransformService transformService = new TransformService(_logger);

            string uniqueMessageId = transformService.GenerateTemplate(previewJob);

            Assert.IsNotNull(uniqueMessageId);
        }

        [TestMethod]
        public void GenerateMessages()
        {
            for(int i = 0; i < 100; i++)
            {
                GenerateTaggedTextTest();
                GenerateTemplateTest();
            }
        }

    }
}
