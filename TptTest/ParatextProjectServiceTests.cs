using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using System.Linq;
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
        /// Test the ability to download an archive of the typesetting files.
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
