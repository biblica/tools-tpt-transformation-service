using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using TptMain.Projects;

namespace TptTest
{
    [TestClass]
    public class ProjectManagerTests
    {
        // test config keys
        public const string TEST_IDTT_DIR_KEY = "Docs:IDTT:Directory";
        public const string TEST_PARATEXT_DIR_KEY = "Docs:Paratext:Directory";

        // test config values
        public readonly string TEST_IDTT_DIR = @"Resources/projectManagerDetails/idtt";
        public readonly string TEST_PARATEXT_DIR = @"Resources/projectManagerDetails/paratext";

        /// <summary>
        /// Mock project manager logger.
        /// </summary>
        private Mock<ILogger<ProjectManager>> _mockLogger;

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
            _mockLogger = new Mock<ILogger<ProjectManager>>();

            // setup for ctor
            IDictionary<string, string> configKeys = new Dictionary<string, string>();
            configKeys[TEST_IDTT_DIR_KEY] = TEST_IDTT_DIR;
            configKeys[TEST_PARATEXT_DIR_KEY] = TEST_PARATEXT_DIR;
            _testConfiguration = new TestConfiguration(configKeys);
        }

        /// <summary>
        /// Tests setup & ctor.
        /// </summary>
        [TestMethod]
        public void InstantiateTest()
        {
            new ProjectManager(
                _mockLogger.Object,
                _testConfiguration);
            _testConfiguration.AssertIfNotAllKeysChecked();
        }

        /// <summary>
        /// Test that the TestCheckProjectFiles is called as expected.
        /// </summary>
        [TestMethod]
        public void TestCheckProjectFiles()
        {
            // setup service under test
            var mockProjectManager =
                new Mock<ProjectManager>(
                    _mockLogger.Object,
                    _testConfiguration);
            // call base functions unless overriden
            mockProjectManager.CallBase = true;

            // Add a couple of jobs to check
            var testPreviewJob1 = TestUtils.CreateTestPreviewJob();
            // this function expects a null Id.
            testPreviewJob1.Id = null;

            Assert.IsTrue(mockProjectManager.Object.TryGetProjectDetails(out var projectDetails));
            Assert.AreEqual(1, projectDetails.Count);
            Assert.IsNotNull(projectDetails["test1"]);
        }
    }
}
