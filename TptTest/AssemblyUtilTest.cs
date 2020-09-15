using Microsoft.VisualStudio.TestTools.UnitTesting;
using TptMain.Util;

namespace TptTest
{
    [TestClass]
    class AssemblyUtilTest
    {
        /// <summary>
        /// Test that the GetAssemblyVersion returns a version.
        /// </summary>
        [TestMethod]
        public void TestGetAssemblyVersion()
        {
            Assert.IsNotNull(AssemblyUtil.GetAssemblyVersion());
        }
    }
}
