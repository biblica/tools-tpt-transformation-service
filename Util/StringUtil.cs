using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;

namespace TptMain.Util
{
    /// <summary>
    /// General-purpose string utilties.
    /// </summary>
    public static class StringUtil
    {
        /// <summary>
        /// Converts query parameter map to string.
        /// </summary>
        /// <param name="queryMap">Query parameter map.</param>
        /// <returns>Query parameter string prefixed with "?" for non-empty map, empty string otherwise.</returns>
        public static string ToQueryString(IDictionary<string, string> queryMap)
        {
            var result = string.Join("&",
                queryMap.Select(pair =>
                    $"{HttpUtility.UrlEncode(pair.Key)}={HttpUtility.UrlEncode(pair.Value)}"));

            return (result.Length > 0)
                ? string.Concat("?", result)
                : result;
        }

        /// <summary>
        /// Gets a project prefix (usually a language abbreviation) from a project name.
        ///
        /// Specifically isolates the first (N) characters that are either
        /// (a) lower case or (b) upper case, checked in that order.
        /// </summary>
        /// <param name="projectName">Project name (required).</param>
        /// <returns>Project prefix.</returns>
        public static string GetProjectPrefix(string projectName)
        {
            var firstCategory = char.GetUnicodeCategory(projectName[0]);
            var prefixBuilder = new StringBuilder();

            foreach (var charItem in projectName)
            {
                if (firstCategory == UnicodeCategory.LowercaseLetter &&
                    char.IsLower(charItem))
                {
                    prefixBuilder.Append(charItem);
                }
                else if (firstCategory == UnicodeCategory.UppercaseLetter &&
                          char.IsUpper(charItem))
                {
                    prefixBuilder.Append(charItem);
                }
                else
                {
                    break;
                }
            }

            return prefixBuilder.ToString();
        }
    }
}