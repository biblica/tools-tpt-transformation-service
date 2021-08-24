using System;

namespace TptMain.Models
{
    /// <summary>
    /// InDesign server configuration parameters. This is used for defining multiple InDesign server connections.
    /// </summary>
    public class InDesignServerConfig
    {
        /// <summary>
        /// Arbitrary Server Name. This is used for logging/debugging purposes.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Server URI. Example: http://localhost:9876/service
        /// </summary>
        public string ServerUri { get; set; }
    }
}