using InDesignServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using tools_tpt_transformation_service.Models;
using System.Text;
using System.IO;
using tools_tpt_transformation_service.Util;

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
        private readonly DirectoryInfo _idsPreviewScriptDirectory;

        /// <summary>
        /// Preview script (JSX) name format (configured).
        /// </summary>
        private readonly string _idsPreviewScriptNameFormat;

        /// <summary>
        /// Default (Roman) script file.
        /// </summary>
        private readonly FileInfo _defaultScriptFile;

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
            _idsPreviewScriptDirectory = new DirectoryInfo((_configuration.GetValue<string>("InDesign:PreviewScriptDirectory")
                ?? throw new ArgumentNullException("InDesign:PreviewScriptDirectory")));
            _idsPreviewScriptNameFormat = (_configuration.GetValue<string>("InDesign:PreviewScriptNameFormat")
                ?? throw new ArgumentNullException("InDesign:PreviewScriptNameFormat"));
            _defaultScriptFile = new FileInfo(Path.Combine(_idsPreviewScriptDirectory.FullName,
                string.Format(_idsPreviewScriptNameFormat, MainConsts.DEFAULT_PROJECT_PREFIX)));

            _logger.LogDebug("ScriptRunner()");
        }

        /// <summary>
        /// Kick off async request to start typesetting preview generation.
        /// </summary>
        /// <param name="inputJob">Input preview job (required).</param>
        /// <returns>Async request task.</returns>
        public Task RunScriptAsync(PreviewJob inputJob)
        {
            RunScriptRequest scriptRequest = new RunScriptRequest();

            RunScriptParameters scriptParameters = new RunScriptParameters();
            scriptRequest.runScriptParameters = scriptParameters;

            scriptParameters.scriptLanguage = "javascript";
            scriptParameters.scriptFile = GetScriptFile(inputJob).FullName;

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

        /// <summary>
        /// Gets the script file for a given project language.
        /// 
        /// Looks for a script file matching the initial, lower-case/non-numeric characters of the project name, 
        /// then falls back to a default if a language-specific one isn't present.
        /// </summary>
        /// <param name="inputJob">Preview job (required).</param>
        /// <returns>Found script file.</returns>
        public FileInfo GetScriptFile(PreviewJob inputJob)
        {
            string projectPrefix = StringUtil.GetProjectPrefix(inputJob.ProjectName).ToUpper();
            if (projectPrefix.Length < 1)
            {
                projectPrefix = MainConsts.DEFAULT_PROJECT_PREFIX;
            }

            FileInfo scriptFile = new FileInfo(Path.Combine(_idsPreviewScriptDirectory.FullName,
                string.Format(_idsPreviewScriptNameFormat, projectPrefix)));
            return scriptFile.Exists
                ? scriptFile
                : _defaultScriptFile;
        }
    }
}
