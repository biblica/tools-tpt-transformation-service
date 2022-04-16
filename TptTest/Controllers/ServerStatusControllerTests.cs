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
            var serverStatusResponse = result.Value;

            Assert.IsNotNull(serverStatusResponse);
        }
    }
}