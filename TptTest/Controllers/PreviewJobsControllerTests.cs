/*
Copyright © 2021 by Biblica, Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TptMain.Controllers;
using Moq;
using Microsoft.Extensions.Logging;
using TptMain.Jobs;
using TptMain.Models;
using Microsoft.AspNetCore.Mvc;

namespace TptTest.Controllers
{
    [TestClass()]
    public class PreviewJobsControllerTests
    {
        // mocks
        Mock<IJobManager> mockJobManager = new Mock<IJobManager>();

        // controller under test
        PreviewJobsController jobsController;
        /// <summary>
        /// Test setup.
        /// </summary>
        [TestInitialize]
        public void TestSetup()
        {
            jobsController = new PreviewJobsController(
                Mock.Of<ILogger<PreviewJobsController>>(),
                mockJobManager.Object);
        }

        delegate void TryGetJob(string jobId, out PreviewJob previewJob); // needed for Callback
        delegate void TryDeleteJob(string jobId, out PreviewJob previewJob); // needed for Callback
        delegate void TryAddJob(PreviewJob inputJob, out PreviewJob outputJob); // needed for Callback

        [TestMethod()]
        public void GetSuccessfulPreviewTest()
        {
            var jobId = "1234";
            mockJobManager
                .Setup(jm => jm.TryGetJob(jobId, out It.Ref<PreviewJob>.IsAny))
                .Callback(new TryGetJob((string jobId, out PreviewJob previewJob) =>
                {
                    previewJob = new PreviewJob()
                    {
                        Id = jobId
                    };
                }))
                .Returns(true);

            ActionResult<PreviewJob> result = jobsController.GetPreviewJob(jobId);
            Assert.AreEqual(jobId, result.Value.Id);
        }

        [TestMethod()]
        public void GetFailedPreviewTest()
        {
            var jobId = "4567";
            mockJobManager
                .Setup(jm => jm.TryGetJob(jobId, out It.Ref<PreviewJob>.IsAny))
                .Returns(false);

            ActionResult<PreviewJob> result = jobsController.GetPreviewJob(jobId);
            Assert.AreEqual(typeof(NotFoundResult), result.Result.GetType());
        }

        [TestMethod()]
        public void PostSuccessfulPreviewTest()
        {
            var jobId = "1234";
            var postedJob = new PreviewJob()
            {
                Id = jobId
            };

            mockJobManager
                .Setup(jm => jm.TryAddJob(postedJob, out It.Ref<PreviewJob>.IsAny))
                .Callback(new TryAddJob((PreviewJob newJob, out PreviewJob returnJob) =>
                {
                    returnJob = newJob;
                }))
                .Returns(true);

            ActionResult<PreviewJob> result = jobsController.PostPreviewJob(postedJob);
            Assert.AreEqual(postedJob, ((CreatedAtActionResult)result.Result).Value);
        }

        [TestMethod()]
        public void PostFailedPreviewTest()
        {
            var jobId = "4567";

            var postedJob = new PreviewJob()
            {
                Id = jobId
            };

            mockJobManager
                .Setup(jm => jm.TryAddJob(postedJob, out It.Ref<PreviewJob>.IsAny))
                .Returns(false);

            ActionResult<PreviewJob> result = jobsController.PostPreviewJob(postedJob);
            Assert.AreEqual(typeof(BadRequestResult), result.Result.GetType());
        }

        [TestMethod()]
        public void SuccessfulDeletePreviewTest()
        {
            var jobId = "1234";
            mockJobManager
                .Setup(jm => jm.TryDeleteJob(jobId, out It.Ref<PreviewJob>.IsAny))
                .Callback(new TryDeleteJob((string jobId, out PreviewJob previewJob) =>
                {
                    previewJob = new PreviewJob()
                    {
                        Id = jobId
                    };
                }))
                .Returns(true);

            ActionResult<PreviewJob> result = jobsController.DeletePreviewJob(jobId);
            Assert.AreEqual(jobId, result.Value.Id);
        }

        [TestMethod()]
        public void FailedDeletePreviewTest()
        {
            var jobId = "4567";
            mockJobManager
                .Setup(jm => jm.TryDeleteJob(jobId, out It.Ref<PreviewJob>.IsAny))
                .Returns(false);

            ActionResult<PreviewJob> result = jobsController.DeletePreviewJob(jobId);
            Assert.AreEqual(typeof(NotFoundResult), result.Result.GetType());
        }
    }
}