using System;
using System.Collections.Generic;
using System.Linq;
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
    }
}
