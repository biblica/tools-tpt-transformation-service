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
using System.IO;
using Microsoft.AspNetCore.Mvc;

namespace TptTest.Controllers
{
    [TestClass()]
    public class PreviewFileControllerTests
    {
        // mocks
        Mock<IJobManager> mockJobManager = new Mock<IJobManager>();

        // controller under test
        PreviewFileController previewController;

        const string TEST_PDF_PATH = @"Resources\test-pdf.pdf";
        const string TEST_ARCHIVE_PATH = @"Resources\test-archive.zip";

        /// <summary>
        /// Test setup.
        /// </summary>
        [TestInitialize]
        public void TestSetup()
        {
            previewController = new PreviewFileController(
                Mock.Of<ILogger<PreviewFileController>>(),
                mockJobManager.Object);
        }

        delegate void TryGetPreviewStream(string jobId, out FileStream fileStream, bool archive); // needed for Callback

        [TestMethod()]
        [DeploymentItem(TEST_ARCHIVE_PATH, "Resources")]
        public void GetArchiveTest()
        {
            // test that the get preview archive call returns as expected.
            var testPreviewJob = new FileStream(TEST_ARCHIVE_PATH, FileMode.Open, FileAccess.Read);
            var isArchive = true;
            var jobId = "1234";

            mockJobManager
                .Setup(jm => jm.TryGetPreviewStream(jobId, out It.Ref<FileStream>.IsAny, isArchive))
                .Callback(new TryGetPreviewStream((string jobId, out FileStream fileStream, bool archive) =>
                {
                    fileStream = testPreviewJob;
                }))
                .Returns(true);

            FileStreamResult fileResult = (FileStreamResult) previewController.Get(jobId, isArchive);
            Assert.AreEqual(fileResult.ContentType, "application/zip");
        }

        [TestMethod()]
        [DeploymentItem(TEST_PDF_PATH, "Resources")]
        public void GetPdfTest()
        {
            // test that the get preview pdf call returns as expected.
            var testPreviewJob = new FileStream(TEST_PDF_PATH, FileMode.Open, FileAccess.Read);
            var isArchive = false;
            var jobId = "1234";

            mockJobManager
                .Setup(jm => jm.TryGetPreviewStream(jobId, out It.Ref<FileStream>.IsAny, isArchive))
                .Callback(new TryGetPreviewStream((string jobId, out FileStream fileStream, bool archive) =>
                {
                    fileStream = testPreviewJob;
                }))
                .Returns(true);

            FileStreamResult fileResult = (FileStreamResult) previewController.Get(jobId, isArchive);
            Assert.AreEqual(fileResult.ContentType, "application/pdf");
        }
    }
}