/*
Copyright © 2021 by Biblica, Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
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