/*
Copyright © 2021 by Biblica, Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using System.Linq;
using TptMain.Models;
using TptMain.ParatextProjects;
using TptMain.Util;

namespace TptTest
{
    [TestClass]
    public class ParatextProjectServiceTest
    {
        /// <summary>
        /// Mock job manager logger.
        /// </summary>
        private Mock<ILogger<ParatextProjectService>> _mockLogger;

        /// <summary>
        /// Test configuration.
        /// </summary>
        private IConfiguration _testConfiguration;

        /// <summary>
        /// Mock Paratext Project Service API.
        /// </summary>
        private Mock<ParatextProjectService> _mockParatextProjectService;

        /// <summary>
        /// Faux Paratext Project with footnotes.
        /// </summary>
        private const string TEST_PROJECT_WITH_FOOTNOTES = "projectWithFootnotes";

        /// <summary>
        /// Faux Paratext Project without footnotes.
        /// </summary>
        private const string TEST_PROJECT_WITHOUT_FOOTNOTES = "projectWithoutFootnotes";

        /// <summary>
        /// Faux Paratext Project that's right-to-left text direction AKA character orientation.
        /// </summary>
        private const string TEST_PROJECT_RTL_TEXT_DIRECTION = "projectRtlTextDirection";

        /// <summary>
        /// Test setup.
        /// </summary>
        [TestInitialize]
        public void TestSetup()
        {
            // create mocks
            _mockLogger = new Mock<ILogger<ParatextProjectService>>();
            IDictionary<string, string> configKeys = new Dictionary<string, string>();

            // Configuration Parameters
            configKeys[ConfigConsts.ParatextDocDirKey] = @"Resources";

            // The InMemoryCollection will snapshot the parameters upon creation, have to first populate the dictionary before passing it.
            _testConfiguration = new ConfigurationBuilder()
               .AddInMemoryCollection(configKeys)
               .Build();

            // mock: paratext project service
            var _mockParatextProjectServiceLogger = new Mock<ILogger<ParatextProjectService>>();
            _mockParatextProjectService = new Mock<ParatextProjectService>(MockBehavior.Strict,
                _mockParatextProjectServiceLogger.Object, _testConfiguration);
        }

        /// <summary>
        /// Tests setup & ctor.
        /// </summary>
        [TestMethod]
        public void InstantiateTest()
        {
            // ctor
            new ParatextProjectService(
                _mockLogger.Object,
                _testConfiguration);
        }

        /// <summary>
        /// Test the ability to download an archive of the typesetting files.
        /// </summary>
        [TestMethod]
        public void TestCustomFootnotesSuccessPath()
        {
            // setup service under test
            _mockParatextProjectService.Setup(service => service.GetFootnoteCallerSequence(TEST_PROJECT_WITH_FOOTNOTES)).CallBase();

            var footnotes = _mockParatextProjectService.Object.GetFootnoteCallerSequence(TEST_PROJECT_WITH_FOOTNOTES);

            Assert.IsNotNull(footnotes);
            Assert.AreEqual(32, footnotes.Length);
            // test first
            Assert.AreEqual("ሀ", footnotes[0]);
            // test middle
            Assert.AreEqual("ከ", footnotes[16]);
            // test last
            Assert.AreEqual("ፐ", footnotes.Last());
        }

        /// <summary>
        /// Test the ability to determine RTL projects.
        /// </summary>
        [TestMethod]
        public void TestProjectRtlTextDirection()
        {
            // setup service under test
            _mockParatextProjectService.Setup(service => service.GetTextDirection(TEST_PROJECT_RTL_TEXT_DIRECTION)).CallBase();

            var textDirection = _mockParatextProjectService.Object.GetTextDirection(TEST_PROJECT_RTL_TEXT_DIRECTION);

            Assert.AreEqual(TextDirection.RTL, textDirection);
        }

        /// <summary>
        /// Test the ability to determine LTR projects.
        /// </summary>
        [TestMethod]
        public void TestProjectLtrTextDirection()
        {
            // setup service under test
            _mockParatextProjectService.Setup(service => service.GetTextDirection(TEST_PROJECT_WITHOUT_FOOTNOTES)).CallBase();

            var textDirection = _mockParatextProjectService.Object.GetTextDirection(TEST_PROJECT_WITHOUT_FOOTNOTES);

            Assert.AreEqual(TextDirection.LTR, textDirection);
        }

        /// <summary>
        /// Test that we correctly determine when there's no project specific footnotes.
        /// </summary>
        [TestMethod]
        public void TestCustomFootnotesFailPath()
        {
            // setup service under test
            _mockParatextProjectService.Setup(service => service.GetFootnoteCallerSequence(TEST_PROJECT_WITHOUT_FOOTNOTES)).CallBase();

            var footnotes = _mockParatextProjectService.Object.GetFootnoteCallerSequence(TEST_PROJECT_WITHOUT_FOOTNOTES);

            Assert.IsNull(footnotes);
        }
    }
}
