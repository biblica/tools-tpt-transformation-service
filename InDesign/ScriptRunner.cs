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
        private readonly int _scriptTimeoutInSec;
        private readonly string _mainScriptPath;

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
                _configuration.GetValue<string>("InDesignServerUri") ?? "http://localhost:9876/service");
            _scriptTimeoutInSec = int.Parse(_configuration.GetValue<string>("ScriptTimeoutInSec") ?? "600");
            _serviceClient.Endpoint.Binding.SendTimeout = TimeSpan.FromSeconds(_scriptTimeoutInSec);
            _serviceClient.Endpoint.Binding.ReceiveTimeout = TimeSpan.FromSeconds(_scriptTimeoutInSec);
            _mainScriptPath = (_configuration.GetValue<string>("PreviewScriptPath") ?? "C:\\Work\\Scripts\\TypesettingPreviewRoman.jsx");
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
            scriptParameters.scriptFile = _mainScriptPath;

            IList<IDSPScriptArg> scriptArgs = new List<IDSPScriptArg>();

            IDSPScriptArg jobIdArg = new IDSPScriptArg();
            scriptArgs.Add(jobIdArg);

            jobIdArg.name = "jobId";
            jobIdArg.value = job.Id;

            IDSPScriptArg projectNameArg = new IDSPScriptArg();
            scriptArgs.Add(projectNameArg);

            projectNameArg.name = "projectName";
            projectNameArg.value = job.ProjectName;

            scriptParameters.scriptArgs = scriptArgs.ToArray();
            return _serviceClient.RunScriptAsync(scriptRequest);
        }
    }
}
