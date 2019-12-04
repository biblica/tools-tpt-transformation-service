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
    /// InDesign Server script runner class.
    /// </summary>
    public class ScriptRunner
    {
        /// <summary>
        /// Type-specific logger (injected).
        /// </summary>
        private readonly ILogger<ScriptRunner> _logger;

        /// <summary>
        /// Configuration (injected).
        /// </summary>
        private readonly IConfiguration _configuration;

        /// <summary>
        /// IDS server client (injected).
        /// </summary>
        private readonly ServicePortTypeClient _serviceClient;

        /// <summary>
        /// IDS request timeout in seconds (configured).
        /// </summary>
        private readonly int _idsTimeoutInSec;

        /// <summary>
        /// Preview script (JSX) path (configured).
        /// </summary>
        private readonly string _idsPreviewScriptPath;

        /// <summary>
        /// Basic ctor.
        /// </summary>
        /// <param name="logger">Type-specific logger (required).</param>
        /// <param name="configuration">System configuration (required).</param>
        public ScriptRunner(ILogger<ScriptRunner> logger,
            IConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            _serviceClient = new ServicePortTypeClient(
                ServicePortTypeClient.EndpointConfiguration.Service,
                _configuration.GetValue<string>("InDesign:ServerUri")
                ?? throw new ArgumentNullException("InDesign:ServerUri"));
            _idsTimeoutInSec = int.Parse(_configuration.GetValue<string>("InDesign:TimeoutInSec")
                ?? throw new ArgumentNullException("InDesign:TimeoutInSec"));
            _serviceClient.Endpoint.Binding.SendTimeout = TimeSpan.FromSeconds(_idsTimeoutInSec);
            _serviceClient.Endpoint.Binding.ReceiveTimeout = TimeSpan.FromSeconds(_idsTimeoutInSec);
            _idsPreviewScriptPath = (_configuration.GetValue<string>("InDesign:PreviewScriptPath")
                ?? throw new ArgumentNullException("InDesign:PreviewScriptPath"));

            _logger.LogDebug("ScriptRunner()");
        }

        /// <summary>
        /// Kick off async request to create start typesetting preview generation.
        /// </summary>
        /// <param name="inputJob">Input preview job (required).</param>
        /// <returns>Async request task.</returns>
        public Task RunScriptAsync(PreviewJob inputJob)
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
            jobIdArg.value = Convert.ToString(inputJob.Id);

            IDSPScriptArg projectNameArg = new IDSPScriptArg();
            scriptArgs.Add(projectNameArg);

            projectNameArg.name = "projectName";
            projectNameArg.value = Convert.ToString(inputJob.ProjectName);

            IDSPScriptArg bookFormatArg = new IDSPScriptArg();
            scriptArgs.Add(bookFormatArg);

            bookFormatArg.name = "bookFormat";
            bookFormatArg.value = Convert.ToString(inputJob.BookFormat);

            scriptParameters.scriptArgs = scriptArgs.ToArray();
            return _serviceClient.RunScriptAsync(scriptRequest);
        }
    }
}
