﻿using InDesignServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
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
        /// Type-specific logger (injected).
        /// </summary>
        private readonly ILogger<ScriptRunner> _logger;

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
            _ = configuration ?? throw new ArgumentNullException(nameof(configuration));

            _serviceClient = new ServicePortTypeClient(
                ServicePortTypeClient.EndpointConfiguration.Service,
                configuration[ConfigConsts.IdsUriKey]
                ?? throw new ArgumentNullException(ConfigConsts.IdsUriKey));
            _idsTimeoutInMSec = (int)TimeSpan.FromSeconds(int.Parse(configuration[ConfigConsts.IdsTimeoutInSecKey]
                ?? throw new ArgumentNullException(ConfigConsts.IdsTimeoutInSecKey)))
                .TotalMilliseconds;
            _idsPreviewScriptDirectory = new DirectoryInfo((configuration[ConfigConsts.IdsPreviewScriptDirKey]
                ?? throw new ArgumentNullException(ConfigConsts.IdsPreviewScriptDirKey)));
            _idsPreviewScriptNameFormat = (configuration[ConfigConsts.IdsPreviewScriptNameFormatKey]
                ?? throw new ArgumentNullException(ConfigConsts.IdsPreviewScriptNameFormatKey));

            _serviceClient.Endpoint.Binding.SendTimeout = TimeSpan.FromMilliseconds(_idsTimeoutInMSec);
            _serviceClient.Endpoint.Binding.ReceiveTimeout = TimeSpan.FromMilliseconds(_idsTimeoutInMSec);
            _defaultScriptFile = new FileInfo(Path.Combine(_idsPreviewScriptDirectory.FullName,
                string.Format(_idsPreviewScriptNameFormat, MainConsts.DEFAULT_PROJECT_PREFIX)));

            _logger.LogDebug("ScriptRunner()");
        }

        /// <summary>
        /// Execute typesetting preview generation synchronously, with optional cancellation.
        /// </summary>
        /// <param name="inputJob">Input preview job (required).</param>
        /// <param name="footnoteMarkers">Custom footnote markers (optional).</param>
        /// <param name="cancellationToken">Cancellation token (optional, may be null).</param>
        public virtual void RunScript(PreviewJob inputJob, string[] footnoteMarkers, CancellationToken? cancellationToken)
        {
            _logger.LogDebug($"RunScriptAsync() - inputJob.Id={inputJob.Id}.");
            var scriptRequest = new RunScriptRequest();

            var scriptParameters = new RunScriptParameters();
            scriptRequest.runScriptParameters = scriptParameters;

            scriptParameters.scriptLanguage = "javascript";
            scriptParameters.scriptFile = GetScriptFile(inputJob).FullName;

            IList<IDSPScriptArg> scriptArgs = new List<IDSPScriptArg>();

            AddNewArgToIdsArgs(ref scriptArgs, "jobId", inputJob.Id);
            AddNewArgToIdsArgs(ref scriptArgs, "projectName", inputJob.ProjectName);
            AddNewArgToIdsArgs(ref scriptArgs, "bookFormat", inputJob.BookFormat.ToString());

            // build the custom footnotes into a CSV string. EG: "a,d,e,ñ,h,Ä".
            string customFootnotes = footnoteMarkers != null ? String.Join(',', footnoteMarkers) : null;
            AddNewArgToIdsArgs(ref scriptArgs, "customFootnoteList", customFootnotes);

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

        /// <summary>
        /// Add a new IDS argument to the collection of IDS arguments.
        /// </summary>
        /// <param name="scriptArgs">The IDS arguments collection to add to. (required)</param>
        /// <param name="newArgName">The new argument key name. (required)</param>
        /// <param name="newArgValue">The new argument value. (optional)</param>
        private void AddNewArgToIdsArgs(ref IList<IDSPScriptArg> scriptArgs, string newArgName, string newArgValue = null)
        {
            var scriptArg = new IDSPScriptArg();
            scriptArgs.Add(scriptArg);

            scriptArg.name = newArgName.Trim();
            scriptArg.value = newArgValue;
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