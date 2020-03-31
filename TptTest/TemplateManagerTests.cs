using System;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using TptMain.Http;
using TptMain.Models;
using TptMain.Toolbox;

namespace TptTest
{
    [TestClass]
    public class TemplateManagerTests
    {
        // test config keys
        public const string TEST_TEMPLATE_SERVER_URI_KEY = "Toolbox:Template:ServerUri";
        public const string TEST_TEMPLATE_TIMEOUT_IN_SEC_KEY = "Toolbox:Template:TimeoutInSec";

        // test config values
        public const string TEST_TEMPLATE_SERVER_URI = "http://localhost:3000/idtt/template/create";
        public const string TEST_TEMPLATE_TIMEOUT_IN_SEC = "600";

        /// <summary>
        /// Mock template manager logger.
        /// </summary>
        private Mock<ILogger<TemplateManager>> _mockLogger;

        /// <summary>
        /// Mock test configuration.
        /// </summary>
        private TestConfiguration _testConfiguration;

        /// <summary>
        /// Mock request factory.
        /// </summary>
        private Mock<WebRequestFactory> _mockRequestFactory;

        [TestInitialize]
        public void TestSetup()
        {
            // create mocks
            _mockLogger = new Mock<ILogger<TemplateManager>>();

            // mock: web request factory
            var mockRequestFactoryLogger = new Mock<ILogger<WebRequestFactory>>();
            _mockRequestFactory = new Mock<WebRequestFactory>(MockBehavior.Strict,
                mockRequestFactoryLogger.Object);

            // setup for ctor
            IDictionary<string, string> configKeys = new Dictionary<string, string>();
            configKeys["Toolbox:Template:ServerUri"] = TEST_TEMPLATE_SERVER_URI;
            configKeys["Toolbox:Template:TimeoutInSec"] = TEST_TEMPLATE_TIMEOUT_IN_SEC;
            _testConfiguration = new TestConfiguration(configKeys);
        }

        /// <summary>
        /// Tests setup & ctor.
        /// </summary>
        [TestMethod]
        public void InstantiateTest()
        {
            new TemplateManager(_mockLogger.Object,
                _testConfiguration,
                _mockRequestFactory.Object);
            _testConfiguration.AssertIfNotAllKeysChecked();
        }

        /// <summary>
        /// Test retrieving a template file from a server.
        /// </summary>
        [TestMethod]
        [DeploymentItem(@"Resources\test-template.idml", "Resources")]
        public void TestDownloadTemplateFileSuccess()
        {
            // setup service under test
            var mockManager =
                new Mock<TemplateManager>(MockBehavior.Strict,
                    _mockLogger.Object,
                    _testConfiguration,
                    _mockRequestFactory.Object);
            mockManager.Setup(managerItem =>
                    managerItem.DownloadTemplateFile(It.IsAny<PreviewJob>(), It.IsAny<FileInfo>()))
                .CallBase();
            mockManager.Setup(managerItem =>
                    managerItem.DownloadTemplateFile(It.IsAny<PreviewJob>(), It.IsAny<FileInfo>(), It.IsAny<CancellationToken?>()))
                .CallBase();

            // create local mocks & placeholders
            var mockWebRequest = new Mock<WebRequest>(MockBehavior.Strict);
            var mockWebResponse = new Mock<WebResponse>(MockBehavior.Strict);
            var testInputFile = new FileInfo(
                @"Resources\test-template.idml");
            var testOutputFile = new FileInfo($"{Guid.NewGuid().ToString()}.idml");
            using Stream testInputStream = testInputFile.OpenRead();

            // setup mocks
            _mockRequestFactory.Setup(factoryItem =>
                    factoryItem.CreateWebRequest(
                        TestConsts.TestRequestUri,
                        TestConsts.TestHttpMethodName,
                        TestConsts.TEST_TEMPLATE_TIMEOUT_IN_M_SEC))
                .Returns(mockWebRequest.Object);
            mockWebRequest.Setup(requestItem =>
                    requestItem.GetResponse())
                .Returns(mockWebResponse.Object);
            mockWebResponse.Setup(responseItem =>
                    responseItem.GetResponseStream())
                .Returns(testInputStream);

            // ensure output file doesn't exist
            testOutputFile.Refresh();
            Assert.IsFalse(testOutputFile.Exists);

            // execute
            mockManager.Object.DownloadTemplateFile(TestUtils.CreateTestPreviewJob(), testOutputFile);

            // assert, in execution order
            _mockRequestFactory.Verify(factorItem =>
                factorItem.CreateWebRequest(
                    TestConsts.TestRequestUri,
                    TestConsts.TestHttpMethodName,
                    TestConsts.TEST_TEMPLATE_TIMEOUT_IN_M_SEC), Times.Once);

            testOutputFile.Refresh();
            Assert.IsTrue(testOutputFile.Exists);
            Assert.IsTrue(TestUtils.AreFilesEqual(testInputFile, testOutputFile));

            // clean up output file
            if (testOutputFile.Exists)
            {
                testOutputFile.Delete();
            }
        }


        /// <summary>
        /// Tests exceptions are generated from file downloads.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(IOException))]
        public void TestDownloadTemplateFileError()
        {
            // setup service under test
            var mockManager =
                new Mock<TemplateManager>(MockBehavior.Strict,
                    _mockLogger.Object,
                    _testConfiguration,
                    _mockRequestFactory.Object);
            mockManager.Setup(managerItem =>
                    managerItem.DownloadTemplateFile(It.IsAny<PreviewJob>(), It.IsAny<FileInfo>()))
                .CallBase();
            mockManager.Setup(managerItem =>
                    managerItem.DownloadTemplateFile(It.IsAny<PreviewJob>(), It.IsAny<FileInfo>(), It.IsAny<CancellationToken?>()))
                .CallBase();

            // create local mocks & placeholders
            var mockWebRequest = new Mock<WebRequest>(MockBehavior.Strict);
            var mockWebResponse = new Mock<WebResponse>(MockBehavior.Strict);
            var testOutputFile = new FileInfo($"{Guid.NewGuid().ToString()}.idml");

            // setup mocks
            _mockRequestFactory.Setup(factoryItem =>
                    factoryItem.CreateWebRequest(
                        TestConsts.TestRequestUri,
                        TestConsts.TestHttpMethodName,
                        TestConsts.TEST_TEMPLATE_TIMEOUT_IN_M_SEC))
                .Returns(mockWebRequest.Object);
            mockWebRequest.Setup(requestItem =>
                    requestItem.GetResponse())
                .Returns(mockWebResponse.Object);
            mockWebResponse.Setup(responseItem =>
                    responseItem.GetResponseStream())
                .Throws(new IOException());

            // ensure output file doesn't exist
            testOutputFile.Refresh();
            Assert.IsFalse(testOutputFile.Exists);

            // execute
            mockManager.Object.DownloadTemplateFile(TestUtils.CreateTestPreviewJob(), testOutputFile);

            // assert, in execution order
            _mockRequestFactory.Verify(factorItem =>
                factorItem.CreateWebRequest(
                    TestConsts.TestRequestUri,
                    TestConsts.TestHttpMethodName,
                    TestConsts.TEST_TEMPLATE_TIMEOUT_IN_M_SEC), Times.Once);
        }
    }
}
