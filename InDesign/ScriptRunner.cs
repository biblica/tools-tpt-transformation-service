using InDesignServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TptMain.Models;
using TptMain.Util;

namespace TptMain.InDesign
{
    /// <summary>
    /// InDesign Server script runner class.
    /// </summary>
    public class ScriptRunner
    {
        /// <summary>
        /// InDesign server uri config key.
        /// </summary>
        private const string IdsUriKey = "InDesign:ServerUri";

        /// <summary>
        /// InDesign server request timeout config key.
        /// </summary>
        private const string IdsTimeoutInSecKey = "InDesign:TimeoutInSec";

        /// <summary>
        /// InDesign server preview script config key.
        /// </summary>
        private const string IdsPreviewScriptDirKey = "InDesign:PreviewScriptDirectory";

        /// <summary>
        /// InDesign server preview script name format config key.
        /// </summary>
        private const string IdsPreviewScriptNameFormatKey = "InDesign:PreviewScriptNameFormat";

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
        private readonly int _idsTimeoutInMSec;

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
                _configuration[IdsUriKey]
                ?? throw new ArgumentNullException(IdsUriKey));
            _idsTimeoutInMSec = (int)TimeSpan.FromSeconds(int.Parse(_configuration[IdsTimeoutInSecKey]
                ?? throw new ArgumentNullException(IdsTimeoutInSecKey)))
                .TotalMilliseconds;
            _idsPreviewScriptDirectory = new DirectoryInfo((_configuration[IdsPreviewScriptDirKey]
                ?? throw new ArgumentNullException(IdsPreviewScriptDirKey)));
            _idsPreviewScriptNameFormat = (_configuration[IdsPreviewScriptNameFormatKey]
                ?? throw new ArgumentNullException(IdsPreviewScriptNameFormatKey));

            _serviceClient.Endpoint.Binding.SendTimeout = TimeSpan.FromMilliseconds(_idsTimeoutInMSec);
            _serviceClient.Endpoint.Binding.ReceiveTimeout = TimeSpan.FromMilliseconds(_idsTimeoutInMSec);
            _defaultScriptFile = new FileInfo(Path.Combine(_idsPreviewScriptDirectory.FullName,
                string.Format(_idsPreviewScriptNameFormat, MainConsts.DEFAULT_PROJECT_PREFIX)));

            _logger.LogDebug("ScriptRunner()");
        }

        /// <summary>
        /// Execute typesetting preview generation synchronously.
        /// </summary>
        /// <param name="inputJob">Input preview job (required).</param>
        public virtual void RunScript(PreviewJob inputJob)
        {
            this.RunScript(inputJob, null);
        }

        /// <summary>
        /// Execute typesetting preview generation synchronously, with optional cancellation.
        /// </summary>
        /// <param name="inputJob">Input preview job (required).</param>
        /// <param name="cancellationToken">Cancellation token (optional, may be null).</param>
        public virtual void RunScript(PreviewJob inputJob, CancellationToken? cancellationToken)
        {
            _logger.LogDebug($"RunScriptAsync() - inputJob.Id={inputJob.Id}.");
            var scriptRequest = new RunScriptRequest();

            var scriptParameters = new RunScriptParameters();
            scriptRequest.runScriptParameters = scriptParameters;

            scriptParameters.scriptLanguage = "javascript";
            scriptParameters.scriptFile = GetScriptFile(inputJob).FullName;

            IList<IDSPScriptArg> scriptArgs = new List<IDSPScriptArg>();

            var jobIdArg = new IDSPScriptArg();
            scriptArgs.Add(jobIdArg);

            jobIdArg.name = "jobId";
            jobIdArg.value = inputJob.Id;

            var projectNameArg = new IDSPScriptArg();
            scriptArgs.Add(projectNameArg);

            projectNameArg.name = "projectName";
            projectNameArg.value = inputJob.ProjectName;

            var bookFormatArg = new IDSPScriptArg();
            scriptArgs.Add(bookFormatArg);

            bookFormatArg.name = "bookFormat";
            bookFormatArg.value = inputJob.BookFormat.ToString();

            scriptParameters.scriptArgs = scriptArgs.ToArray();

            RunScriptResponse scriptResponse;
            if (cancellationToken == null)
            {
                scriptResponse = _serviceClient.RunScript(scriptRequest);
            }
            else
            {
                var scriptTask = _serviceClient.RunScriptAsync(scriptRequest);
                scriptTask.Wait(_idsTimeoutInMSec, (CancellationToken)cancellationToken);

                scriptResponse = scriptTask.Result;
            }

            // check for result w/errors
            if (scriptResponse != null
                && scriptResponse.errorNumber != 0)
            {
                throw new ScriptException(
                    $"Can't execute script (error number: {scriptResponse.errorNumber}, message: {scriptResponse.errorString}).",
                    null);
            }
        }

        /// <summary>
        /// Gets the script file for a given project language.
        ///
        /// Looks for a script file matching the initial, non-numeric characters of the project name,
        /// then falls back to a default if a language-specific one isn't present.
        /// </summary>
        /// <param name="inputJob">Preview job (required).</param>
        /// <returns>Found script file.</returns>
        public FileInfo GetScriptFile(PreviewJob inputJob)
        {
            var projectPrefix = StringUtil.GetProjectPrefix(inputJob.ProjectName).ToUpper();
            if (projectPrefix.Length < 1)
            {
                projectPrefix = MainConsts.DEFAULT_PROJECT_PREFIX;
            }

            var scriptFile = new FileInfo(Path.Combine(_idsPreviewScriptDirectory.FullName,
                string.Format(_idsPreviewScriptNameFormat, projectPrefix)));
            return scriptFile.Exists
                ? scriptFile
                : _defaultScriptFile;
        }
    }

    /// <summary>
    /// Basic exception for IDS script errors.
    /// </summary>
    public class ScriptException : ApplicationException
    {
        /// <summary>
        /// Basic ctor.
        /// </summary>
        /// <param name="messageText">Message text (optional, may be null).</param>
        /// <param name="causeEx">Cause exception (optional, may be null).</param>
        public ScriptException(string messageText, Exception causeEx)
            : base(messageText, causeEx)
        {
        }
    }
}