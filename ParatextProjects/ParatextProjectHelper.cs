using Paratext.Data.ProjectSettingsAccess;
using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using TptMain.ParatextProjects.Models;
using TptMain.Util;

namespace TptMain.ParatextProjects
{
    /// <summary>
    /// Helper functions related to working with Paratext projects.
    /// </summary>
    public static class ParatextProjectHelper
    {
        /// <summary>
        /// Returns whether or not a specified directory is a Paratext Project directory or not.
        /// </summary>
        /// <param name="projectDir">Directory path of potential Paratext project</param>
        /// <returns>true: Valid Paratext project; otherwise false</returns>
        public static bool IsValidParatextProjectDirectory(string projectDir)
        {
            // ensure the project directory exists
            if (!Directory.Exists(projectDir))
            {
                throw new DirectoryNotFoundException(string.Concat(projectDir, " directory not found"));
            }

            // check for the existence of a known PT project file
            return File.Exists(Path.Combine(projectDir, MainConsts.PARATEXT_PROJECT_KNOWN_FILE_PATH));
        }

        /// <summary>
        /// Opens and deserializes the project's settings.
        /// </summary>
        /// <param name="settingsPath">The full path of the Paratext project directory, containing a Settings.xml file.</param>
        /// <returns>A ProjectSettings object</returns>
        public static ProjectSettings GetProjectSettings(string projectPath)
        {
            // validate input
            _ = projectPath ?? throw new ArgumentNullException(nameof(projectPath));

            // load and return the settings.
            using var settingsFileReader = File.OpenText(Path.Combine(projectPath, MainConsts.SETTINGS_FILE_PATH));
            var projectSettings = new ProjectSettings(settingsFileReader);
            return projectSettings;
        }

        /// <summary>
        /// Deserialize a Paratext project <c>.ldml</c> file into a corresonding object.
        /// </summary>
        /// <param name="ldmlFilePath">The absolute path of the <c>.ldml</c> file. (required)</param>
        /// <returns>The corresponding <c>ldml</c> object.</returns>
        private static ldml LoadLdmlFromXmlFile(string ldmlFilePath)
        {
            // validate input
            _ = ldmlFilePath ?? throw new ArgumentNullException(nameof(ldmlFilePath));

            // deserialize the LDML file into an object
            var serializer = new XmlSerializer(typeof(ldml));
            var ldmlObj = (ldml)serializer.Deserialize(new XmlTextReader(ldmlFilePath));

            return ldmlObj;
        }

        /// <summary>
        /// Extracts the footnote caller sequence markers from a Paratext project's LDML file.
        /// </summary>
        /// <param name="ldmlFilePath">The absolute path of the <c>.ldml</c> file. (required)</param>
        /// <returns>An array of footnote markers, if there are any; Otherwise, <c>null</c></returns>
        public static string[] ExtractFootnoteMarkers(string ldmlFilePath)
        {
            // validate input
            _ = ldmlFilePath ?? throw new ArgumentNullException(nameof(ldmlFilePath));

            // Deserialize LDML from file
            var ldml = LoadLdmlFromXmlFile(ldmlFilePath);

            return ExtractFootnoteMarkers(ldml);
        }

        /// <summary>
        /// Extracts the footnote caller sequence markers from a Paratext project's LDML object.
        /// </summary>
        /// <param name="ldmlObj">The <c>.ldml</c> object. (required)</param>
        /// <returns>An array of footnote markers, if there are any; Otherwise, <c>null</c></returns>
        public static string[] ExtractFootnoteMarkers(ldml ldmlObj)
        {
            // validate input
            _ = ldmlObj ?? throw new ArgumentNullException(nameof(ldmlObj));

            var specialCharacterList = ldmlObj?.characters?.special;

            if (specialCharacterList != null)
            {
                // find the footnotes characters in the LDML
                var exemplarCharacters = Array.Find(specialCharacterList, (specialCharacters) => specialCharacters.type.ToLower().Equals("footnotes"));
                var footnoteRawList = exemplarCharacters.Value;

                // remove surrounding '[',']' characters, and split. EG: The strring "[a b c]" turns into an array with elements "a", "b", "c"
                var cleanedAndSplitFootnoteList = footnoteRawList.Replace("[", "").Replace("]", "").Split(" ");

                // find all non-empty values. This helps most when a language has an empty footnote list, like usNIV11
                var footnoteMarkers = Array.FindAll(cleanedAndSplitFootnoteList, (val => val.Length != 0));

                // only return something when there's values.
                if (footnoteMarkers.Length > 0)
                {
                    return footnoteMarkers;
                }
            }

            return null;
        }
    }
}
