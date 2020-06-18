using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using TptMain.ParatextProjects;
using TptMain.ParatextProjects.Models;

namespace TptTest
{
    [TestClass]
    public class ParatextProjectsServiceTest
    {
        [TestMethod]
        public void InstantiateTest()
        {
            //var projectPath = @"C:\Paratext 8 Projects\amNASV01";
            var projectPath = @"C:\Paratext 8 Projects\yoBYO17";
            //var projectPath = @"C:\Paratext 8 Projects\usNIV11";
            var projectSettings = ParatextProjectHelper.GetProjectSettings(projectPath);
            //var test = ParatextProjectHelper.GetProjectSettings(@"C:\Paratext 8 Projects\amNASV01");

            // Grab the LDML file
            var ldmlPath = Path.Combine(projectPath, projectSettings.LdmlFileName);

            var footnotes = ParatextProjectHelper.ExtractFootnoteMarkers(ldmlPath);
            Console.WriteLine();
        }
    }
}
