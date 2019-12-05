using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace tools_tpt_transformation_service.Util
{
    /// <summary>
    /// General-purpose string utilties.
    /// </summary>
    static public class StringUtil
    {
        /// <summary>
        /// Converts query parameter map to string.
        /// </summary>
        /// <param name="queryMap">Query parameter map.</param>
        /// <returns>Query parameter string prefixed with "?" for non-empty map, empty string otherwise.</returns>
        public static string ToQueryString(IDictionary<string, string> queryMap)
        {
            string result = string.Join("&",
                queryMap.Select(pair =>
                string.Format("{0}={1}",
                    HttpUtility.UrlEncode(pair.Key),
                    HttpUtility.UrlEncode(pair.Value))));

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
            UnicodeCategory firstCategory = Char.GetUnicodeCategory(projectName[0]);
            StringBuilder prefixBuilder = new StringBuilder();

            foreach (char charItem in projectName)
            {
                if (firstCategory == UnicodeCategory.LowercaseLetter &&
                    Char.IsLower(charItem))
                {
                    prefixBuilder.Append(charItem);
                }
                else if (firstCategory == UnicodeCategory.UppercaseLetter &&
                          Char.IsUpper(charItem))
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
