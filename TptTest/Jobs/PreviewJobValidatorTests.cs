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
        /// Mock Paratext Project Service.
        /// </summary>
        private Mock<ParatextProjectService> _mockParatextProjectService;

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

            // mock: Paratext Project Service
            configKeys[ConfigConsts.ParatextDocDirKey] = TestConsts.TEST_PARATEXT_DOC_DIR;

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

            // mock: Paratext Project Service
            var mockParatextProjectServiceLogger = new Mock<ILogger<ParatextProjectService>>();
            _mockParatextProjectService = new Mock<ParatextProjectService>(
                mockParatextProjectServiceLogger.Object,
                _testConfiguration);

            // setup service under test
            serviceUnderTest =
                new Mock<PreviewJobValidator>(
                    _mockLogger.Object,
                    _testConfiguration,
                    _mockParatextProjectService.Object,
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
            testPreviewJob1.BibleSelectionParams.SelectedBooks = "GEN,EXO,LEV,NUM,DEU,JOS,JDG,RUT,1SA,2SA,1KI,2KI,1CH,2CH,EZR,NEH,EST,JOB,PSA,PRO,ECC,SNG,ISA,JER,LAM,EZK,DAN,HOS,JOL,AMO,OBA,JON,MIC,NAM,HAB,ZEP,HAG,ZEC,MAL,MAT,MRK,LUK,JHN,ACT,ROM,1CO,2CO,GAL,EPH,PHP,COL,1TH,2TH,1TI,2TI,TIT,PHM,HEB,JAS,1PE,2PE,1JN,2JN,3JN,JUD,REV,TOB,JDT,ESG,WIS,SIR,BAR,LJE,S3Y,SUS,BEL,1MA,2MA,3MA,4MA,1ES,2ES,MAN,PS2,ODA,PSS,EZA,5EZ,6EZ,DAG,PS3,2BA,LBA,JUB,ENO,1MQ,2MQ,3MQ,REP,4BA,LAO,FRT,BAK,OTH,INT,CNC,GLO,TDX,NDX,XXA,XXB,XXC,XXD,XXE,XXF,XXG";

            TestValidation(serviceUnderTest, testPreviewJob1, new List<string>());

            var testPreviewJob2 = TestUtils.CreateTestPreviewJob();
            testPreviewJob2.BibleSelectionParams.ProjectName = "TestProject2";
            testPreviewJob2.BibleSelectionParams.SelectedBooks = " GEN , EXO , LEV , NUM , DEU , JOS , JDG , RUT , 1SA , 2SA , 1KI , 2KI , 1CH , 2CH , EZR , NEH , EST , JOB , PSA , PRO , ECC , SNG , ISA , JER , LAM , EZK , DAN , HOS , JOL , AMO , OBA , JON , MIC , NAM , HAB , ZEP , HAG , ZEC , MAL , MAT , MRK , LUK , JHN , ACT , ROM , 1CO , 2CO , GAL , EPH , PHP , COL , 1TH , 2TH , 1TI , 2TI , TIT , PHM , HEB , JAS , 1PE , 2PE , 1JN , 2JN , 3JN , JUD , REV , TOB , JDT , ESG , WIS , SIR , BAR , LJE , S3Y , SUS , BEL , 1MA , 2MA , 3MA , 4MA , 1ES , 2ES , MAN , PS2 , ODA , PSS , EZA , 5EZ , 6EZ , DAG , PS3 , 2BA , LBA , JUB , ENO , 1MQ , 2MQ , 3MQ , REP , 4BA , LAO , FRT , BAK , OTH , INT , CNC , GLO , TDX , NDX , XXA , XXB , XXC , XXD , XXE , XXF , XXG ";

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
            testPreviewJob2.BibleSelectionParams.SelectedBooks = "GEN, ,FRT";

            TestValidation(serviceUnderTest, testPreviewJob2, new List<string> { "ProjectName", "SelectedBooks" });

            var testPreviewJob3 = TestUtils.CreateTestPreviewJob();
            testPreviewJob3.BibleSelectionParams.ProjectName = "Test1";
            testPreviewJob3.BibleSelectionParams.SelectedBooks = "GEN,GEN,FRT";

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
            var errorOccured = false;

            // Perform the validation.
            validator.Object.ProcessJob(previewJob);

            if(previewJob.IsError)
            {
                errorOccured = true;

                // ensure we got the expected number of errors by counting the separators (minus 1)
                Assert.AreEqual(expectedFailedParameters.Count, previewJob.ErrorDetail.Split(PreviewJobValidator.NEWLINE_TAB).Length - 1);

                // we're expecting this exception to be thrown with a consolidation of error messages for every failed item.
                expectedFailedParameters.ForEach(expectedFailParam =>
                {
                    Assert.IsTrue(previewJob.ErrorDetail.Contains(expectedFailParam), $"Expected '{expectedFailParam}' validation error not found.");
                });
            }

            // make sure an exception was thrown when there were errors
            Assert.AreEqual(errorOccured, expectedFailedParameters.Count > 0);
        }
    }
}
