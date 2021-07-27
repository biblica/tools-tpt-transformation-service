using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TptMain.Exceptions;
using TptMain.Jobs;
using TptMain.Models;
using TptMain.ParatextProjects;
using TptMain.Util;

namespace TptTest.Jobs
{
    [TestClass]
    public class PreviewJobValidatorTests
    {
        /// <summary>
        /// Mock logger.
        /// </summary>
        private Mock<ILogger<PreviewJobValidator>> _mockLogger;

        /// <summary>
        /// Test configuration.
        /// </summary>
        private IConfiguration _testConfiguration;

        /// <summary>
        /// Mock Paratext API.
        /// </summary>
        private Mock<ParatextApi> _mockParatextApi;

        /// <summary>
        /// The service under test: PreviewJobValidator
        /// </summary>
        private Mock<PreviewJobValidator> serviceUnderTest;

        /// <summary>
        /// Test setup.
        /// </summary>
        [TestInitialize]
        public void TestSetup()
        {
            // create mocks
            _mockLogger = new Mock<ILogger<PreviewJobValidator>>();

            // Configuration Parameters
            IDictionary<string, string> configKeys = new Dictionary<string, string>();

            // - ParatextApi
            configKeys[ConfigConsts.ParatextApiServerUriKey] = ParatextApiTests.TEST_PT_API_SERVER_URI;
            configKeys[ConfigConsts.ParatextApiUsernameKey] = ParatextApiTests.TEST_PT_API_USERNAME;
            configKeys[ConfigConsts.ParatextApiPasswordKey] = ParatextApiTests.TEST_PT_API_PASSWORD;
            configKeys[ConfigConsts.ParatextApiProjectCacheAgeInSecKey] = ParatextApiTests.TEST_PT_API_PROJECT_CACHE_AGE_IN_SEC.ToString();
            for (var i = 0; i < ParatextApiTests.TEST_PT_API_ALLOWED_MEMBER_ROLES.Count; i++)
            {
                configKeys[ConfigConsts.ParatextApiAllowedMemberRolesKey + ":" + i] = ParatextApiTests.TEST_PT_API_ALLOWED_MEMBER_ROLES[i].ToString();
            }

            // The InMemoryCollection will snapshot the parameters upon creation, have to first populate the dictionary before passing it.
            _testConfiguration = new ConfigurationBuilder()
               .AddInMemoryCollection(configKeys)
               .Build();

            // mock: paratext API
            var mockParatextApiLogger = new Mock<ILogger<ParatextApi>>();
            _mockParatextApi = new Mock<ParatextApi>(MockBehavior.Strict,
                mockParatextApiLogger.Object, _testConfiguration);
            // default user authorization to pass
            _mockParatextApi.Setup(paratextApi =>
                paratextApi.IsUserAuthorizedOnProject(It.IsAny<PreviewJob>()))
                .Verifiable();

            // setup service under test
            serviceUnderTest =
                new Mock<PreviewJobValidator>(
                    _mockLogger.Object,
                    _testConfiguration,
                    _mockParatextApi.Object)
                { CallBase = true };
            // call base functions unless overriden
        }

        /// <summary>
        /// Test validation fails for invalid typesetting parameter values.
        /// </summary>
        [TestMethod]
        public void TestTypesettingParamValidationFails()
        {
            var testPreviewJob = TestUtils.CreateTestPreviewJob();

            // Set a couple preview job parameters value outside their allowed ranges
            testPreviewJob.TypesettingParams.FontSizeInPts = MainConsts.ALLOWED_FONT_SIZE_IN_PTS.Min - 1;
            testPreviewJob.TypesettingParams.FontLeadingInPts = MainConsts.ALLOWED_FONT_LEADING_IN_PTS.Max + 1;

            TestValidation(serviceUnderTest, testPreviewJob, new List<string> { "FontSizeInPts", "FontLeadingInPts" });
        }

        /// <summary>
        /// Test validation success for valid bible selection parameter values.
        /// </summary>
        [TestMethod]
        public void TestBibleSelectionParamsValidationSuccess()
        {
            var testPreviewJob1 = TestUtils.CreateTestPreviewJob();
            testPreviewJob1.BibleSelectionParams.ProjectName = "TestProject1";
            testPreviewJob1.BibleSelectionParams.SelectedBooks = "01GEN,02EXO,03LEV,04NUM,05DEU,06JOS,07JDG,08RUT,091SA,102SA,111KI,122KI,131CH,142CH,15EZR,16NEH,17EST,18JOB,19PSA,20PRO,21ECC,22SNG,23ISA,24JER,25LAM,26EZK,27DAN,28HOS,29JOL,30AMO,31OBA,32JON,33MIC,34NAM,35HAB,36ZEP,37HAG,38ZEC,39MAL,41MAT,42MRK,43LUK,44JHN,45ACT,46ROM,471CO,482CO,49GAL,50EPH,51PHP,52COL,531TH,542TH,551TI,562TI,57TIT,58PHM,59HEB,60JAS,611PE,622PE,631JN,642JN,653JN,66JUD,67REV,68TOB,69JDT,70ESG,71WIS,72SIR,73BAR,74LJE,75S3Y,76SUS,77BEL,781MA,792MA,803MA,814MA,821ES,832ES,84MAN,85PS2,86ODA,87PSS,A4EZA,A55EZ,A66EZ,B2DAG,B3PS3,B42BA,B5LBA,B6JUB,B7ENO,B81MQ,B92MQ,C03MQ,C1REP,C24BA,C3LAO,A0FRT,A1BAK,A2OTH,A7INT,A8CNC,A9GLO,B0TDX,B1NDX,94XXA,95XXB,96XXC,97XXD,98XXE,99XXF,100XXG";

            TestValidation(serviceUnderTest, testPreviewJob1, new List<string>());

            var testPreviewJob2 = TestUtils.CreateTestPreviewJob();
            testPreviewJob2.BibleSelectionParams.ProjectName = "TestProject2";
            testPreviewJob2.BibleSelectionParams.SelectedBooks = " 01GEN , 02EXO , 03LEV , 04NUM , 05DEU , 06JOS , 07JDG , 08RUT , 091SA , 102SA , 111KI , 122KI , 131CH , 142CH , 15EZR , 16NEH , 17EST , 18JOB , 19PSA , 20PRO , 21ECC , 22SNG , 23ISA , 24JER , 25LAM , 26EZK , 27DAN , 28HOS , 29JOL , 30AMO , 31OBA , 32JON , 33MIC , 34NAM , 35HAB , 36ZEP , 37HAG , 38ZEC , 39MAL , 41MAT , 42MRK , 43LUK , 44JHN , 45ACT , 46ROM , 471CO , 482CO , 49GAL , 50EPH , 51PHP , 52COL , 531TH , 542TH , 551TI , 562TI , 57TIT , 58PHM , 59HEB , 60JAS , 611PE , 622PE , 631JN , 642JN , 653JN , 66JUD , 67REV , 68TOB , 69JDT , 70ESG , 71WIS , 72SIR , 73BAR , 74LJE , 75S3Y , 76SUS , 77BEL , 781MA , 792MA , 803MA , 814MA , 821ES , 832ES , 84MAN , 85PS2 , 86ODA , 87PSS , A4EZA , A55EZ , A66EZ , B2DAG , B3PS3 , B42BA , B5LBA , B6JUB , B7ENO , B81MQ , B92MQ , C03MQ , C1REP , C24BA , C3LAO , A0FRT , A1BAK , A2OTH , A7INT , A8CNC , A9GLO , B0TDX , B1NDX , 94XXA , 95XXB , 96XXC , 97XXD , 98XXE , 99XXF , 100XXG ";

            TestValidation(serviceUnderTest, testPreviewJob2, new List<string>());
        }

        /// <summary>
        /// Test validation fails for invalid bible selection parameter values.
        /// </summary>
        [TestMethod]
        public void TestBibleSelectionParamsValidationFails()
        {
            var testPreviewJob1 = TestUtils.CreateTestPreviewJob();
            testPreviewJob1.BibleSelectionParams.ProjectName = "    ";
            testPreviewJob1.BibleSelectionParams.SelectedBooks = "FOO,bar";

            TestValidation(serviceUnderTest, testPreviewJob1, new List<string> { "ProjectName", "SelectedBooks", "SelectedBooks" });

            var testPreviewJob2 = TestUtils.CreateTestPreviewJob();
            testPreviewJob2.BibleSelectionParams.ProjectName = "";
            testPreviewJob2.BibleSelectionParams.SelectedBooks = "01GEN, ,A0FRT";

            TestValidation(serviceUnderTest, testPreviewJob2, new List<string> { "ProjectName", "SelectedBooks" });

            var testPreviewJob3 = TestUtils.CreateTestPreviewJob();
            testPreviewJob3.BibleSelectionParams.ProjectName = "Test1";
            testPreviewJob3.BibleSelectionParams.SelectedBooks = "01GEN,01GEN,A0FRT";

            TestValidation(serviceUnderTest, testPreviewJob3, new List<string> { "SelectedBooks" });
        }

        /// <summary>
        /// Test validation auth fail.
        /// </summary>
        [TestMethod]
        public void TestValidationAuthFail()
        {
            var testPreviewJob = TestUtils.CreateTestPreviewJob();

            // default user authorization to pass
            _mockParatextApi.Setup(paratextApi =>
                paratextApi.IsUserAuthorizedOnProject(It.IsAny<PreviewJob>()))
                .Throws(new PreviewJobException(testPreviewJob))
                .Verifiable();

            TestValidation(serviceUnderTest, testPreviewJob, new List<string> { "User" });
        }

        /// <summary>
        /// Test validation success for parameter values.
        /// </summary>
        [TestMethod]
        public void TestGeneralValidationSuccess()
        {
            var testPreviewJob = TestUtils.CreateTestPreviewJob();

            // Perform the validation and expect no errors
            TestValidation(serviceUnderTest, testPreviewJob, new List<string> { });
        }

        /// <summary>
        /// Perform a validation and handle expectations around invalid values
        /// </summary>
        /// <param name="validator">The Preview Job validator. (required)</param>
        /// <param name="previewJob">The preview job to validate. (required)</param>
        /// <param name="expectedFailedParameters">The parameters that we expect to fail. (required)</param>
        private void TestValidation(Mock<PreviewJobValidator> validator, PreviewJob previewJob, List<string> expectedFailedParameters)
        {
            var exceptionThrown = false;
            try
            {
                // Perform the validation.
                validator.Object.ProcessJob(previewJob);
            }
            catch (ArgumentException ex)
            {
                exceptionThrown = true;

                // ensure we got the expected number of errors by counting the separators (minus 1)
                Assert.AreEqual(expectedFailedParameters.Count, ex.Message.Split(PreviewJobValidator.NEWLINE_TAB).Length - 1);

                // we're expecting this exception to be thrown with a consolidation of error messages for every failed item.
                expectedFailedParameters.ForEach(expectedFailParam =>
                {
                    Assert.IsTrue(ex.Message.Contains(expectedFailParam), $"Expected '{expectedFailParam}' validation error not found.");
                });
            }

            // make sure an exception was thrown when there were errors
            Assert.AreEqual(exceptionThrown, expectedFailedParameters.Count > 0);
        }
    }
}
