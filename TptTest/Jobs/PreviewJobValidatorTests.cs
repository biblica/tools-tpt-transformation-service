using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using TptMain.Exceptions;
using TptMain.Http;
using TptMain.InDesign;
using TptMain.Jobs;
using TptMain.Models;
using TptMain.ParatextProjects;
using TptMain.Toolbox;
using TptMain.Util;

namespace TptTest
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
                    _mockParatextApi.Object);
            // call base functions unless overriden
            serviceUnderTest.CallBase = true;
        }

        /// <summary>
        /// Test validation fails for invalid parameter values.
        /// </summary>
        [TestMethod]
        public void TestValidationFails()
        {

            var testPreviewJob = TestUtils.CreateTestPreviewJob();

            // Set a couple preview job parameters value outside their allowed ranges
            testPreviewJob.TypesettingParams.FontSizeInPts = MainConsts.ALLOWED_FONT_SIZE_IN_PTS.Min - 1;
            testPreviewJob.TypesettingParams.FontLeadingInPts = MainConsts.ALLOWED_FONT_LEADING_IN_PTS.Max + 1;

            TestValidation(serviceUnderTest, testPreviewJob, new List<string> { "FontSizeInPts", "FontLeadingInPts" });
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
        public void TestValidationSuccess()
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
                validator.Object.ValidatePreviewJob(previewJob);
            }
            catch (ArgumentException ex)
            {
                exceptionThrown = true;

                // ensure we got the expected number of errors by counting the separators (minus 1)
                Assert.AreEqual(expectedFailedParameters.Count, ex.Message.Split(PreviewJobValidator.NEWLINE_TAB).Length - 1);

                // we're expecting this exception to be thrown with a consolidation of error messages for every failed item.
                expectedFailedParameters.ForEach(expectedFailParam => {
                    Assert.IsTrue(ex.Message.Contains(expectedFailParam), $"Expected '{expectedFailParam}' validation error not found.");
                });
            }

            // make sure an exception was thrown when there were errors
            Assert.AreEqual(exceptionThrown, expectedFailedParameters.Count > 0);
        }
    }
}
