/*
Copyright © 2021 by Biblica, Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
#define TEST
#undef TEST

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
using TptMain.Util;
using static TptMain.Jobs.TransformService;

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

#if TEST

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

            transformService.GenerateTaggedText(previewJob);

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

            transformService.GenerateTemplate(previewJob);
        }

        /// <summary>
        /// Test the ability to list files in S3
        /// 
        /// Note: Ensure 'jobs' directory exists in S3 before running this test.
        /// 
        /// </summary>
        [TestMethod]
        public void S3ListFilesTest()
        {
            S3Service s3Service = new S3Service();
            List<string> filenames = s3Service.ListAllFiles("jobs/");

            Assert.IsNotNull(filenames);
        }

        /// <summary>
        /// Test the ability to check for an unknown job
        /// </summary>
        [TestMethod]
        public void GetStatusWaitingTest()
        {
            TransformService transformService = new TransformService(_logger);

            TransformJobStatus jobStatus = transformService.GetTransformJobStatus("unknown");

            Assert.AreEqual(TransformJobStatus.WAITING, jobStatus);
        }

        /// <summary>
        /// Test the ability to get a complete status
        /// 
        /// Note: Ensure there is a directory called jobs/test-job/ with a .complete-template 
        /// file existing in that directory before running this test.
        /// 
        /// </summary>
        [TestMethod]
        public void GetStatusCompleteTest()
        {
            TransformService transformService = new TransformService(_logger);

            TransformJobStatus jobStatus = transformService.GetTransformJobStatus("test-job");

            Assert.AreEqual(TransformJobStatus.TEMPLATE_COMPLETE, jobStatus);
        }

        /// <summary>
        /// Test the ability to create a .cancel file in S3
        /// </summary>
        [TestMethod]
        public void CancelTest()
        {
            TransformService transformService = new TransformService(_logger);

            string transformJobId = Guid.NewGuid().ToString();

            transformService.CancelTransformJobs(transformJobId);

            TransformJobStatus jobStatus = transformService.GetTransformJobStatus(transformJobId);

            Assert.AreEqual(TransformJobStatus.CANCELED, jobStatus);
        }

        /// <summary>
        ///  Simple test harness for creating a bunch of messages for external testing
        /// </summary>
        [TestMethod]
        [Ignore]
        public void GenerateMessages()
        {
            for(int i = 0; i < 100; i++)
            {
                GenerateTaggedTextTest();
                GenerateTemplateTest();
            }
        }
#endif

    }
}
