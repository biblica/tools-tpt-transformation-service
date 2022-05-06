/*
Copyright © 2021 by Biblica, Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
using InDesignServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using System.IO;
using TptMain.InDesign;
using TptMain.Jobs;
using TptMain.Models;
using TptMain.Util;

namespace TptTest
{
    [TestClass]
    public class ScriptRunnerTests
    {
        // config values
        private IConfiguration _testConfiguration;
        public const string TEST_IDS_URI = "http://172.31.10.90:9876/service";
        public const int TEST_IDS_TIMEOUT = 600;
        public const string TEST_IDS_PREVIEW_SCRIPT_DIR = "C:\\Work\\JSX";
        public const string TEST_IDTT_DOC_DIR = "C:\\Work\\IDTT";
        public const string TEST_IDML_DOC_DIR = "C:\\Work\\IDML";
        public const string TEST_PDF_DOC_DIR = "C:\\Work\\PDF";
        private const string JOB_FILE_MANAGER_ROOT_DIR = "Resources";

        /// <summary>
        /// Mock script runner logger.
        /// </summary>
        private Mock<LoggerFactory> _mockLoggerFactory;

        /// <summary>
        /// Mock Job File Manager.
        /// </summary>
        private Mock<JobFileManager> _mockJobFileManager;

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
            IDictionary<string, string> configKeys = new Dictionary<string, string>();
            configKeys[ConfigConsts.ProcessedJobFilesRootDirKey] = JOB_FILE_MANAGER_ROOT_DIR;

            _testConfiguration = new ConfigurationBuilder()
               .AddInMemoryCollection(configKeys)
               .Build();


            // create mocks
            _mockLoggerFactory = new Mock<LoggerFactory>();

            _mockJobFileManager = new Mock<JobFileManager>(
                new Mock<ILogger<JobFileManager>>().Object,
                _testConfiguration
                );

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
                _mockLoggerFactory.Object,
                null,
                _testIdsConfig,
                TEST_IDS_TIMEOUT,
                new DirectoryInfo(TEST_IDS_PREVIEW_SCRIPT_DIR),
                _mockJobFileManager.Object
                );
        }

        [TestMethod]
        public void TestRunScript()
        {
            // We're mocking the runner only for the InDesign client set up portion. Otherwise, call the base functionality.
            var scriptRunner = new Mock<InDesignScriptRunner>(
                _mockLoggerFactory.Object,
                null,
                _testIdsConfig,
                TEST_IDS_TIMEOUT,
                new DirectoryInfo(TEST_IDS_PREVIEW_SCRIPT_DIR),
                _mockJobFileManager.Object
                );
            scriptRunner.CallBase = true;

            // Mock up InDesign client
            Mock<ServicePortTypeClient> indesignClient = new Mock<ServicePortTypeClient>();

            // Mock up FileJobManager
            _mockJobFileManager
                .Setup(jobFileManager => jobFileManager.GetTemplateDirectoryById(It.IsAny<string>()))
                .Returns(new DirectoryInfo(JOB_FILE_MANAGER_ROOT_DIR));
            _mockJobFileManager
                .Setup(jobFileManager => jobFileManager.GetTaggedTextDirectoryById(It.IsAny<string>()))
                .Returns(new DirectoryInfo(JOB_FILE_MANAGER_ROOT_DIR));
            _mockJobFileManager
                .Setup(jobFileManager => jobFileManager.GetPreviewDirectoryById(It.IsAny<string>()))
                .Returns(new DirectoryInfo(JOB_FILE_MANAGER_ROOT_DIR));

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
                    TypesettingParams = new TypesettingParams
                    {
                        BookFormat = BookFormat.cav
                    },
                    AdditionalParams = new AdditionalPreviewParams()
                    {
                        CustomFootnoteMarkers = "a,b,c,d,e,f",
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
