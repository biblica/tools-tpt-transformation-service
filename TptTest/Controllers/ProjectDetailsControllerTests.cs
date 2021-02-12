using Microsoft.VisualStudio.TestTools.UnitTesting;
using TptMain.Controllers;
using Moq;
using Microsoft.Extensions.Logging;
using TptMain.Models;
using Microsoft.AspNetCore.Mvc;
using TptMain.Projects;
using System.Collections.Generic;

namespace TptTest.Controllers
{
    [TestClass()]
    public class ProjectDetailsControllerTests
    {
        // mocks
        Mock<IProjectManager> mockProjectManager = new Mock<IProjectManager>();

        // controller under test
        ProjectDetailsController detailsController;
        /// <summary>
        /// Test setup.
        /// </summary>
        [TestInitialize]
        public void TestSetup()
        {
            detailsController = new ProjectDetailsController(
                Mock.Of<ILogger<ProjectDetailsController>>(),
                mockProjectManager.Object);
        }

        delegate void TryGetProjectDetails(out IDictionary<string, ProjectDetails> projectDetails); // needed for Callback

        [TestMethod()]
        public void GetDetailsSuccessfulTest()
        {
            var projectDetail1 = new ProjectDetails()
            {
                ProjectName = "ABC",
                ProjectUpdated = System.DateTime.Now
            };
            var projectDetail2 = new ProjectDetails()
            {
                ProjectName = "DEF",
                ProjectUpdated = System.DateTime.Now
            };

            mockProjectManager
                .Setup(pm => pm.TryGetProjectDetails(out It.Ref<IDictionary<string, ProjectDetails>>.IsAny))
                .Callback(new TryGetProjectDetails((out IDictionary<string, ProjectDetails> projectDetails) =>
                {
                    projectDetails = new Dictionary<string, ProjectDetails>();
                    projectDetails.Add(projectDetail1.ProjectName, projectDetail1);
                    projectDetails.Add(projectDetail2.ProjectName, projectDetail2);
                }))
                .Returns(true);

            ActionResult<IList<ProjectDetails>> result = detailsController.Get();
            Assert.IsNotNull(result.Value);
            var projectDetailsResponse = (IList<ProjectDetails>)result.Value;

            Assert.AreEqual(2, projectDetailsResponse.Count);
            Assert.AreEqual(projectDetail1, projectDetailsResponse[0]);
            Assert.AreEqual(projectDetail2, projectDetailsResponse[1]);
        }

        [TestMethod()]
        public void GetDetailsFailedTest()
        {
            mockProjectManager
                .Setup(jm => jm.TryGetProjectDetails(out It.Ref<IDictionary<string, ProjectDetails>>.IsAny))
                .Returns(false);

            ActionResult<IList<ProjectDetails>> result = detailsController.Get();
            Assert.AreEqual(typeof(NotFoundResult), result.Result.GetType());
        }
    }
}