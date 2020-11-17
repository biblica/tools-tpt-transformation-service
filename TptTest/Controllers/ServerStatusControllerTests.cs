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
    public class ServerStatusControllerTests
    {
        // controller under test
        ServerStatusController serverStatusController;
        /// <summary>
        /// Test setup.
        /// </summary>
        [TestInitialize]
        public void TestSetup()
        {
            serverStatusController = new ServerStatusController(
                Mock.Of<ILogger<ServerStatusController>>());
        }

        delegate void TryGetProjectDetails(out IDictionary<string, ProjectDetails> projectDetails); // needed for Callback

        [TestMethod()]
        public void GetServerStatusSuccessfulTest()
        {
            ActionResult<ServerStatus> result = serverStatusController.Get();
            Assert.IsNotNull(result.Value);
            var serverStatusResponse = (ServerStatus)result.Value;

            Assert.IsNotNull(serverStatusResponse);
        }
    }
}