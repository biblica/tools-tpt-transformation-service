using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Language.Flow;
using Moq.Protected;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using TptMain.Paratext;
using TptMain.Paratext.Models;

namespace TptTest
{
    [TestClass]
    public class ParatextApiTests
    {
        // test config values
        public const string TEST_PT_API_SERVER_URI = "http://example.com";
        public const string TEST_PT_API_USERNAME = "JohnSmith";
        public const string TEST_PT_API_PASSWORD = "ComplexPassword1!";
        public const double TEST_PT_API_PROJECT_CACHE_AGE_IN_SEC = 1;
        public static readonly List<MemberRole> TEST_PT_API_ALLOWED_MEMBER_ROLES = new List<MemberRole>() {
            MemberRole.pt_administrator,
            MemberRole.pt_consultant
        };
        public static readonly MemberRole INVALID_MEMBER_ROLE = MemberRole.pt_write_note;

        // test users
        public const string USER_1 = "Phillip J. Fry";
        public const string USER_2 = "John A. Zoidberg";

        // test projects
        public const string PROJECT_1_PT_ID = "aaaaaaaaaaaaaaaaaaaaaa";
        public const string PROJECT_1_PT_NAME = "Goliath";
        public const string PROJECT_1_DBL_ID = "111111111111111111111";
        public const string PROJECT_2_PT_ID = "bbbbbbbbbbbbbbbbbbbbbb";
        public const string PROJECT_2_PT_NAME = "David";

        /// <summary>
        /// HTTP Client
        /// </summary>
        private Mock<ILogger<ParatextApi>> _mockParatextApiLogger;

        /// <summary>
        /// Mock HttpMessageHandler
        /// </summary>
        private Mock<HttpMessageHandler> _mockHttpMessageHandler;

        /// <summary>
        /// Mock HttpMessageHandler
        /// </summary>
        private ISetup<HttpMessageHandler, Task<HttpResponseMessage>> _mockHttpMessageHandlerSetup;

        /// <summary>
        /// Test configuration.
        /// </summary>
        private IConfiguration _testConfiguration;

        /// <summary>
        /// Mock Paratext API.
        /// </summary>
        private Mock<ParatextApi> _mockParatextApi;

        /// <summary>
        /// Test setup.
        /// </summary>
        [TestInitialize]
        public void TestSetup()
        {
            // Configuration Parameters
            IDictionary<string, string> configKeys = new Dictionary<string, string>();

            // - ParatextApi
            configKeys[ParatextApi.ParatextApiServerUriKey] = ParatextApiTests.TEST_PT_API_SERVER_URI;
            configKeys[ParatextApi.ParatextApiUsernameKey] = ParatextApiTests.TEST_PT_API_USERNAME;
            configKeys[ParatextApi.ParatextApiPasswordKey] = ParatextApiTests.TEST_PT_API_PASSWORD;
            configKeys[ParatextApi.ParatextApiProjectCacheAgeInSecKey] = ParatextApiTests.TEST_PT_API_PROJECT_CACHE_AGE_IN_SEC.ToString();
            for (var i = 0; i < ParatextApiTests.TEST_PT_API_ALLOWED_MEMBER_ROLES.Count; i++)
            {
                configKeys[ParatextApi.ParatextApiAllowedMemberRolesKey + ":" + i] = ParatextApiTests.TEST_PT_API_ALLOWED_MEMBER_ROLES[i].ToString();
            }

            // The InMemoryCollection will snapshot the parameters upon creation, have to first populate the dictionary before passing it.
            _testConfiguration = new ConfigurationBuilder()
                .AddInMemoryCollection(configKeys)
                .Build();

            // create mocks
            // mock: paratext API
            _mockParatextApiLogger = new Mock<ILogger<ParatextApi>>();
            _mockParatextApi = new Mock<ParatextApi>(
                MockBehavior.Strict,
                _mockParatextApiLogger.Object, _testConfiguration
                );

            // mock: paratext's HttpClient.
            // The HttpMessageHandler is being mocked instead of the HttpClient, because HttpClient doesn't allow for overriding it's members needed by mocking.
            // Ref: https://gingter.org/2018/07/26/how-to-mock-httpclient-in-your-net-c-unit-tests/
            // The idea is to mock responses on the HttpMessageHandler as needed.
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            _mockHttpMessageHandlerSetup = _mockHttpMessageHandler
               .Protected()
               // Setup the PROTECTED method to mock
               .Setup<Task<HttpResponseMessage>>(
                  "SendAsync",
                  ItExpr.IsAny<HttpRequestMessage>(),
                  ItExpr.IsAny<CancellationToken>()
               );
            var httpClient = new HttpClient(_mockHttpMessageHandler.Object)
            {
                BaseAddress = new Uri(TEST_PT_API_SERVER_URI)
            };

            _mockParatextApi
                .Setup(ptApi => ptApi.HttpClient)
                .Returns(httpClient);
        }

        /// <summary>
        /// Tests setup & ctor.
        /// </summary>
        [TestMethod]
        public void InstantiateTest()
        {
            var paratextApi = new ParatextApi(_mockParatextApiLogger.Object, _testConfiguration);
            Assert.IsNotNull(paratextApi);
        }

        /// <summary>
        /// Test authorization of valid member roles
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task TestIsUserAuthorizedOnProjectValidMemberRole()
        {
            // populate allowed user return and supported user
            var validUser = USER_1;
            var project = PROJECT_1_PT_NAME;
            var returnUsers = new List<ProjectMember> { new ProjectMember { Role = TEST_PT_API_ALLOWED_MEMBER_ROLES[0], UserName = validUser } };

            // setup support call to return allowed project users
            _mockParatextApi
                .Setup(ptApi => ptApi.GetAllowedProjectMembersAsync(project))
                .Returns(Task.FromResult(returnUsers));

            // setup functions that should work normally
            _mockParatextApi
                .Setup(ptApi => ptApi.IsUserAuthorizedOnProject(validUser, project))
                .CallBase();

            // make call and verify response is correct
            var authBool = await _mockParatextApi.Object.IsUserAuthorizedOnProject(validUser, project);

            Assert.IsTrue(authBool);
        }

        /// <summary>
        /// Test to ensure we correctly determine invalid user roles as not authorized.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task TestIsUserAuthorizedOnProjectInvalidMemberRole()
        {
            // populate allowed user return and supported user
            var validUser = USER_1;
            var invalidUser = USER_2;
            var project = "arbitrary";
            var returnUsers = new List<ProjectMember> { new ProjectMember { Role = TEST_PT_API_ALLOWED_MEMBER_ROLES[0], UserName = validUser } };

            // setup support call to return allowed project users
            _mockParatextApi
                .Setup(ptApi => ptApi.GetAllowedProjectMembersAsync(project))
                .Returns(Task.FromResult(returnUsers));

            // setup functions that should work normally
            _mockParatextApi
                .Setup(ptApi => ptApi.IsUserAuthorizedOnProject(It.IsAny<string>(), project))
                .CallBase();

            // validate user with wrong role is marked as unauthorized.
            var authBool = await _mockParatextApi.Object.IsUserAuthorizedOnProject(invalidUser, project);
            Assert.IsFalse(authBool);
        }

        /// <summary>
        /// Test that the GetAllowedProjectMembersAsync function successfully distinguishes between allowed members from prohibted.
        /// </summary>
        [TestMethod]
        public async Task TestGetAllowedProjectMembersAsync()
        {
            var validUser = USER_1;
            var validRole = TEST_PT_API_ALLOWED_MEMBER_ROLES[0];
            var invalidUser = USER_2;
            var invalidRole = INVALID_MEMBER_ROLE;
            var projectId = PROJECT_1_PT_ID;
            var projectName = PROJECT_1_PT_NAME;

            // setup return of project ID for specified project name
            _mockParatextApi
                .Setup(ptApi => ptApi.GetProjectIdFromShortname(projectName))
                .Returns(Task.FromResult(projectId));


            // setup HTTP call and response from Paratext for retrieving project members
            _mockHttpMessageHandlerSetup
                .ReturnsAsync(new HttpResponseMessage()
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = new StringContent($@"[
                              {{
                                ""role"": ""{validRole}"",
                                ""userId"": ""gDK89teGJLa5FeXvs"",
                                ""username"": ""{validUser}""
                              }},
                              {{
                                ""role"": ""{invalidRole}"",
                                ""userId"": ""hPKoHCFLaQowgoQoS"",
                                ""username"": ""{invalidUser}""
                              }}
                        ]")
                })
               .Verifiable();

            // setup functions that should work normally
            _mockParatextApi
                .Setup(ptApi => ptApi.GetAllowedProjectMembersAsync(projectName))
                .CallBase();

            // validate we get only the allowed project members
            var allowedProjectMembers = await _mockParatextApi.Object.GetAllowedProjectMembersAsync(projectName);
            Assert.AreEqual(1, allowedProjectMembers.Count);
            Assert.AreEqual(validUser, allowedProjectMembers[0].UserName);
            Assert.AreEqual(validRole, allowedProjectMembers[0].Role);
        }

        /// <summary>
        /// Test that the GetParatextProjectsAsync function works as expected. Ensure that the caching works correctly to reduce calls to the API.
        /// </summary>
        [TestMethod]
        public async Task TestGetParatextProjectsAsync()
        {
            // setup HTTP call and response from Paratext for retrieving project members
            _mockHttpMessageHandlerSetup
                .ReturnsAsync(new HttpResponseMessage()
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = new StringContent(GenerateProjectsListJson())
                })
               .Verifiable();

            // setup functions that should work normally
            _mockParatextApi
                .Setup(ptApi => ptApi.GetParatextProjectsAsync())
                .CallBase();
            _mockParatextApi
                .Setup(ptApi => ptApi.ApiCache)
                .CallBase();

            // 1. First call, make sure cache doesn't have projects loaded yet.
            Assert.IsNull(_mockParatextApi.Object.ApiCache.Get(ParatextApi.ProjectsCacheKey));

            // 1. get projects and validate successful return
            var firstProjects = await _mockParatextApi.Object.GetParatextProjectsAsync();
            ValidateProjects(firstProjects);

            // 1. validate the HTTP client was used and the cache is now populated
            Assert.IsNotNull(_mockParatextApi.Object.ApiCache.Get(ParatextApi.ProjectsCacheKey));
            _mockHttpMessageHandler.Protected().Verify(
               "SendAsync",
               Times.Exactly(1),
               ItExpr.IsAny<HttpRequestMessage>(),
               ItExpr.IsAny<CancellationToken>()
            );

            // 2. Second call to ensure the cache returned projects instead of using the API
            var secondProjects = await _mockParatextApi.Object.GetParatextProjectsAsync();
            ValidateProjects(secondProjects);

            Assert.AreEqual(1, _mockParatextApi.Object.ApiCache.GetCount());
            _mockHttpMessageHandler.Protected().Verify(
               "SendAsync",
               Times.Exactly(1),
               ItExpr.IsAny<HttpRequestMessage>(),
               ItExpr.IsAny<CancellationToken>()
            );

            // 3. 3rd run is to ensure the cached projects expires correctly and that the API is called again.
            // 3. Sleep for the cache age and validate that the cache is empty
            Thread.Sleep((int)(TEST_PT_API_PROJECT_CACHE_AGE_IN_SEC * 1000));
            Assert.IsNull(_mockParatextApi.Object.ApiCache.Get(ParatextApi.ProjectsCacheKey));

            // 3. Get projects 
            _mockHttpMessageHandlerSetup
                .ReturnsAsync(new HttpResponseMessage()
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = new StringContent(GenerateProjectsListJson())
                })
               .Verifiable();

            var thirdProjects = await _mockParatextApi.Object.GetParatextProjectsAsync();
            ValidateProjects(thirdProjects);

            // 3. Make sure that the API was called and the cache is populated again
            Assert.IsNotNull(_mockParatextApi.Object.ApiCache.Get(ParatextApi.ProjectsCacheKey));
            _mockHttpMessageHandler.Protected().Verify(
               "SendAsync",
               Times.Exactly(2),
               ItExpr.IsAny<HttpRequestMessage>(),
               ItExpr.IsAny<CancellationToken>()
            );
        }

        private void ValidateProjects(List<Project> projects)
        {
            // # of projects
            Assert.AreEqual(2, projects.Count);

            // Project 1's contents
            var project1 = projects[0];
            Assert.AreEqual(PROJECT_1_PT_NAME, project1.Identification_ShortName);
            Assert.AreEqual(2, project1.Identification_SystemId.Count);
            Assert.AreEqual(PROJECT_1_PT_NAME, project1.Identification_SystemId[0].Name);
            Assert.AreEqual(PROJECT_1_PT_ID, project1.Identification_SystemId[0].Text);
            Assert.AreEqual(PROJECT_1_DBL_ID, project1.Identification_SystemId[1].Text);

            var project2 = projects[1];
            Assert.AreEqual(PROJECT_2_PT_NAME, project2.Identification_ShortName);
            Assert.AreEqual(1, project2.Identification_SystemId.Count);
            Assert.AreEqual(PROJECT_2_PT_NAME, project2.Identification_SystemId[0].Name);
            Assert.AreEqual(PROJECT_2_PT_ID, project2.Identification_SystemId[0].Text);
        }

        /// <summary>
        /// Generate the needed parts of the Paratext API get all projects JSON response for repeated testing purpoeses.
        /// </summary>
        /// <returns>Projects JSON response</returns>
        private string GenerateProjectsListJson()
        {
            return $@"[{{
                ""identification_shortName"": ""{PROJECT_1_PT_NAME}"",
                ""identification_systemId"": [
                  {{
                    ""type"": ""paratext"",
                    ""text"": ""{PROJECT_1_PT_ID}"",
                    ""name"": ""{PROJECT_1_PT_NAME}"",
                    ""fullname"": ""Project 1""
                  }},
                  {{
                    ""type"": ""dbl"",
                    ""text"": ""{PROJECT_1_DBL_ID}""
                  }}
                ]
              }},
              {{
                ""identification_shortName"": ""{PROJECT_2_PT_NAME}"",
                ""identification_systemId"": [
                  {{
                    ""type"": ""paratext"",
                    ""text"": ""{PROJECT_2_PT_ID}"",
                    ""name"": ""{PROJECT_2_PT_NAME}"",
                    ""fullname"": ""Project 2""
                  }}
                ]
              }}]";
        }
    }
}
