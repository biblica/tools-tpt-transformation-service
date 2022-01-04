/*
Copyright © 2021 by Biblica, Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
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