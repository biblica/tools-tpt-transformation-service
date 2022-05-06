/*
Copyright © 2021 by Biblica, Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using TptMain.Projects;

namespace TptTest
{
    [TestClass]
    public class ProjectManagerTests
    {
        // test config values
        public readonly string TEST_PARATEXT_DIR = @"Resources/projectManagerDetails/paratext";
        public readonly string TEST_CHECK_INTERVAL_SECS = "5";

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
            configKeys[ProjectManager.ParatextDirKey] = TEST_PARATEXT_DIR;
            configKeys[ProjectManager.ProjectUpdateIntervalKey] = TEST_CHECK_INTERVAL_SECS;
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
