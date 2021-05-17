using InDesignServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
namespace TptMain.Jobs
{
    /// <summary>
    /// Preview Manager interface.
    /// </summary>
    public interface IPreviewManager : IPreviewJobProcessor
    {
    }
}