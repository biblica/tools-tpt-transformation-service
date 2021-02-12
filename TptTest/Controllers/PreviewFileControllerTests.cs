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