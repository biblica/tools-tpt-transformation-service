using InDesignServer;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using TptMain.InDesign;
using TptMain.Models;

namespace TptTest
{
    [TestClass]
    public class ScriptRunnerTests
    {
        // config values
        public const string TEST_IDS_URI = "http://172.31.10.90:9876/service";
        public const int TEST_IDS_TIMEOUT = 600;
        public const string TEST_IDS_PREVIEW_SCRIPT_DIR = "C:\\Work\\JSX";
        public const string TEST_IDTT_DOC_DIR = "C:\\Work\\IDTT";
        public const string TEST_IDML_DOC_DIR = "C:\\Work\\IDML";
        public const string TEST_PDF_DOC_DIR = "C:\\Work\\PDF";
        


        /// <summary>
        /// Mock script runner logger.
        /// </summary>
        private Mock<ILogger<InDesignScriptRunner>> _mockLogger;

        /// <summary>
        /// Test IDS configuration.
        /// </summary>
        private InDesignServerConfig _testIdsConfig;

        /// <summary>
        /// Test setup.
        /// </summary>
        [TestInitialize]
        public void TestSetup()
        {
            // create mocks
            _mockLogger = new Mock<ILogger<InDesignScriptRunner>>();

            _testIdsConfig = new InDesignServerConfig()
            {
                Name = "Arbitrary InDesign Server Name",
                ServerUri = TEST_IDS_URI
            };
        }

        /// <summary>
        /// Tests setup & ctor.
        /// </summary>
        [TestMethod]
        public void InstantiateTest()
        {
            new InDesignScriptRunner(
                _mockLogger.Object,
                _testIdsConfig,
                TEST_IDS_TIMEOUT,
                new DirectoryInfo(TEST_IDS_PREVIEW_SCRIPT_DIR),
                new DirectoryInfo(TEST_IDML_DOC_DIR),
                new DirectoryInfo(TEST_IDTT_DOC_DIR),
                new DirectoryInfo(TEST_PDF_DOC_DIR)
                );
        }

        [TestMethod]
        public void TestRunScript()
        {
            // We're mocking the runner only for the InDesign client set up portion. Otherwise, call the base functionality.
            var scriptRunner = new Mock<InDesignScriptRunner>(
                _mockLogger.Object,
                _testIdsConfig,
                TEST_IDS_TIMEOUT,
                new DirectoryInfo(TEST_IDS_PREVIEW_SCRIPT_DIR),
                new DirectoryInfo(TEST_IDML_DOC_DIR),
                new DirectoryInfo(TEST_IDTT_DOC_DIR),
                new DirectoryInfo(TEST_PDF_DOC_DIR)
                );
            scriptRunner.CallBase = true;

            // Mock up InDesign client
            Mock<ServicePortTypeClient> indesignClient = new Mock<ServicePortTypeClient>();

            // verify that the RunScript command is called as expected
            indesignClient
                .Setup(idClient => idClient.RunScript(It.IsNotNull<RunScriptRequest>()))
                .Returns(new RunScriptResponse())
                .Verifiable();

            scriptRunner
                .Setup(runner => runner.SetUpInDesignClient())
                .Returns(indesignClient.Object);

            scriptRunner.Object.CreatePreview(
                new PreviewJob()
                {
                    BibleSelectionParams = new BibleSelectionParams
                    {
                        ProjectName = ""
                    },
                    AdditionalParams = new AdditionalPreviewParams()
                    {
                        CustomFootnoteMarkers = "abcdef".Split(),
                        OverrideFont = "A New Font"
                    }
                },
                null
            );

            // verify expected calls were made
            indesignClient.Verify(idClient => idClient.RunScript(It.IsNotNull<RunScriptRequest>()), Times.Once);
        }
    }
}
