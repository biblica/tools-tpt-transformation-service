using InDesignServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using TptMain.Models;
using TptMain.Util;

namespace TptMain.Jobs
{
    /// <summary>
    /// Preview Manager interface.
    /// </summary>
    public interface IPreviewManager
    {
        void Test();
    }
}