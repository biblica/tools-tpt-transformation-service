using InDesignServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using tools_tpt_transformation_service.Models;

namespace tools_tpt_transformation_service.InDesign
{
    /// <summary>
    /// InDesign server script runner class.
    /// </summary>
    public class ScriptRunner
    {
        private readonly ILogger<ScriptRunner> _logger;
        private readonly IConfiguration _configuration;
        private readonly ServicePortTypeClient _serviceClient;
        private readonly int _idsTimeoutInSec;
        private readonly string _idsPreviewScriptPath;

        /// <summary>
        /// Constructor. Populated by dependency injection.
        /// </summary>
        /// <param name="logger">Logger.</param>
        /// <param name="configuration">Service configuration object.</param>
        public ScriptRunner(ILogger<ScriptRunner> logger,
            IConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            _serviceClient = new ServicePortTypeClient(
                ServicePortTypeClient.EndpointConfiguration.Service,
                _configuration.GetValue<string>("InDesign.ServerUri") ?? "http://localhost:9876/service");
            _idsTimeoutInSec = int.Parse(_configuration.GetValue<string>("InDesign.TimeoutInSec") ?? "600");
            _serviceClient.Endpoint.Binding.SendTimeout = TimeSpan.FromSeconds(_idsTimeoutInSec);
            _serviceClient.Endpoint.Binding.ReceiveTimeout = TimeSpan.FromSeconds(_idsTimeoutInSec);
            _idsPreviewScriptPath = (_configuration.GetValue<string>("InDesign.PreviewScriptPath") ?? "C:\\Work\\JSX\\TypesettingPreviewRoman.jsx");

            _logger.LogDebug("ScriptRunner()");
        }

        /// <summary>
        /// Kick off async request to create start typesetting preview generation.
        /// </summary>
        /// <param name="job">Typesetting preview job request.</param>
        /// <returns></returns>
        public Task RunScriptAsync(PreviewJob job)
        {
            RunScriptRequest scriptRequest = new RunScriptRequest();

            RunScriptParameters scriptParameters = new RunScriptParameters();
            scriptRequest.runScriptParameters = scriptParameters;

            scriptParameters.scriptLanguage = "javascript";
            scriptParameters.scriptFile = _idsPreviewScriptPath;

            IList<IDSPScriptArg> scriptArgs = new List<IDSPScriptArg>();

            IDSPScriptArg jobIdArg = new IDSPScriptArg();
            scriptArgs.Add(jobIdArg);

            jobIdArg.name = "jobId";
            jobIdArg.value = Convert.ToString(job.Id);

            IDSPScriptArg projectNameArg = new IDSPScriptArg();
            scriptArgs.Add(projectNameArg);

            projectNameArg.name = "projectName";
            projectNameArg.value = Convert.ToString(job.ProjectName);

            IDSPScriptArg bookFormatArg = new IDSPScriptArg();
            scriptArgs.Add(bookFormatArg);

            bookFormatArg.name = "bookFormat";
            bookFormatArg.value = Convert.ToString(job.BookFormat);

            scriptParameters.scriptArgs = scriptArgs.ToArray();
            return _serviceClient.RunScriptAsync(scriptRequest);
        }
    }
}
