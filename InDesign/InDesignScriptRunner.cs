using InDesignServer;
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
    public class InDesignScriptRunner
    {
        /// <summary>
        /// Logger (injected).
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// IDS server client (injected).
        /// </summary>
        private readonly ServicePortTypeClient _serviceClient;

        /// <summary>
        /// IDS configuration.
        /// </summary>
        private readonly InDesignServerConfig _serverConfig;

        /// <summary>
        /// Returns the name of the InDesign server configuration name.
        /// </summary>
        public string Name => _serverConfig.Name;

        /// <summary>
        /// IDS request timeout in seconds (configured).
        /// </summary>
        private readonly int _idsTimeoutInMSec;

        /// <summary>
        /// Preview script (JSX) path (configured).
        /// </summary>
        private readonly DirectoryInfo _idsPreviewScriptDirectory;

        /// <summary>
        /// Directory where IDML files are located.
        /// </summary>
        private readonly DirectoryInfo _idmlDocDir;

        /// <summary>
        /// Directory where IDTT files are located.
        /// </summary>
        private readonly DirectoryInfo _idttDocDir;

        /// <summary>
        /// Directory where PDF files are output.
        /// </summary>
        private readonly DirectoryInfo _pdfDocDir;

        /// <summary>
        /// Default (Document Creation) script file.
        /// </summary>
        private readonly FileInfo _defaultDocScriptFile;

        /// <summary>
        /// Default (Book Creation) script file.
        /// </summary>
        private readonly FileInfo _defaultBookScriptFile;

        /// <summary>
        /// Basic ctor.
        /// </summary>
        /// <param name="logger">Logger (required).</param>
        /// <param name="serverConfig">InDesign server configuration (required).</param>
        /// <param name="idsTimeoutInMSec">IDS request timeout in seconds (required).</param>
        /// <param name="scriptDir">Preview script (JSX) path (required).</param>
        /// <param name="idmlDocDir">Directory where IDML files are located. (required).</param>
        /// <param name="idttDocDir">Directory where IDTT files are located. (required).</param>
        /// <param name="pdfDocDir">Directory where PDF files are output (required).</param>
        public InDesignScriptRunner(
            ILogger logger,
            InDesignServerConfig serverConfig,
            int idsTimeoutInMSec,
            DirectoryInfo scriptDir,
            DirectoryInfo idmlDocDir,
            DirectoryInfo idttDocDir,
            DirectoryInfo pdfDocDir
            )
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serverConfig = serverConfig ?? throw new ArgumentNullException(nameof(serverConfig));

            _idsTimeoutInMSec = idsTimeoutInMSec;
            _idsPreviewScriptDirectory = scriptDir ?? throw new ArgumentNullException(nameof(scriptDir));
            _idmlDocDir = idmlDocDir ?? throw new ArgumentNullException(nameof(idmlDocDir));
            _idttDocDir = idttDocDir ?? throw new ArgumentNullException(nameof(idttDocDir));
            _pdfDocDir = pdfDocDir ?? throw new ArgumentNullException(nameof(pdfDocDir));

            _serviceClient = SetUpInDesignClient();

            _defaultDocScriptFile = new FileInfo(Path.Combine(_idsPreviewScriptDirectory.FullName,
                "CreateDocument.jsx"));

            _defaultBookScriptFile = new FileInfo(Path.Combine(_idsPreviewScriptDirectory.FullName,
                "CreateBook.jsx"));

            _logger.LogDebug("InDesignScriptRunner()");
        }

        /// <summary>
        /// This function sets up the InDesign server client.
        /// </summary>
        /// <returns>The instanstiated InDesign server client.</returns>
        public virtual ServicePortTypeClient SetUpInDesignClient()
        {
            var serviceClient = new ServicePortTypeClient(
                ServicePortTypeClient.EndpointConfiguration.Service,
                _serverConfig.ServerUri
                ) ?? throw new ArgumentNullException(nameof(_serverConfig.ServerUri));

            serviceClient.Endpoint.Binding.SendTimeout = TimeSpan.FromMilliseconds(_idsTimeoutInMSec);
            serviceClient.Endpoint.Binding.ReceiveTimeout = serviceClient.Endpoint.Binding.SendTimeout;

            return serviceClient;
        }

        /// <summary>
        /// Execute typesetting preview generation synchronously, with optional cancellation.
        /// </summary>
        /// <param name="inputJob">Input preview job (required).</param>
        /// <param name="cancellationToken">Cancellation token (optional, may be null).</param>
        public virtual void CreatePreview(
            PreviewJob inputJob,
            CancellationToken? cancellationToken)
        {
            _logger.LogDebug($"CreatePreview() - inputJob.Id={inputJob.Id}.");

            // Simplify our logic later on by establishing a cancellation token if none was passed
            var ct = cancellationToken ?? new CancellationToken();

            // Establish base variables
            var jobId = inputJob.Id;

            // Create a list of all the tagged text files that will be turned into documents
            var projectName = inputJob.BibleSelectionParams.ProjectName;
            var bookFormat = inputJob.TypesettingParams.BookFormat.ToString();
            var txtDir = $@"{_idttDocDir}\{bookFormat}\{projectName}\";
            var txtFiles = GetTaggedTextFiles(txtDir);

            // Build custom footnotes into a CSV string, eg "a,d,e,ñ,h,Ä".
            var customFootnotes = inputJob.AdditionalParams.CustomFootnoteMarkers != null ? String.Join(',', inputJob.AdditionalParams.CustomFootnoteMarkers) : null;

            // Create the InDesign Documents (IDTT files)
            _logger.LogDebug("Creating InDesign Documents");
            foreach (var txtFile in txtFiles)
            {
                ct.ThrowIfCancellationRequested();
                CreateDocument(jobId, txtFile, inputJob.AdditionalParams.OverrideFont, customFootnotes, inputJob.AdditionalParams.TextDirection);
            }

            _logger.LogDebug("Finished creating InDesign Documents");

            // Create a book (INDB) from the InDesign Documents and export it to PDF
            ct.ThrowIfCancellationRequested();
            CreateBook(jobId);

            _logger.LogDebug($"Finished CreatePreview() - inputJob.Id={inputJob.Id}.");
        }

        /// <summary>
        /// This method creates an InDesign Document from a specified tagged text file.
        /// </summary>
        /// <param name="jobId">The preview job ID</param>
        /// <param name="txtFilePath">The file path of the tagged text to use for the document</param>
        /// <param name="overrideFont">A font to use instead of the one specified in the IDML</param>
        /// <param name="customFootnotes">Custom footnotes to use in the document</param>
        /// <param name="textDirection">The text direction of the typesetting preview text.</param>
        /// <exception cref="ScriptException">An InDesign Server exception that resulted from executing the script</exception>
        private void CreateDocument(
            string jobId, 
            string txtFilePath,
            string overrideFont, 
            string customFootnotes,
            TextDirection textDirection
            )
        {
            var txtFileName = new FileInfo(txtFilePath).Name;
            var idmlPath = $@"{_idmlDocDir}\preview-{jobId}.idml";
            var docOutputPath = $@"{_idmlDocDir}\preview-{jobId}-{txtFileName.Replace(".txt", ".indd")}";

            _logger.LogDebug($"Creating '{docOutputPath}' from '{txtFileName}'");
            var docScriptRequest = new RunScriptRequest();
            var docScriptParameters = new RunScriptParameters();
            docScriptRequest.runScriptParameters = docScriptParameters;

            IList<IDSPScriptArg> documentScriptArgs = new List<IDSPScriptArg>();
            if (!String.IsNullOrEmpty(overrideFont))
            {
                AddNewArgToIdsArgs(ref documentScriptArgs, "overrideFont", overrideFont);
            }

            AddNewArgToIdsArgs(ref documentScriptArgs, "customFootnoteList", customFootnotes);
            AddNewArgToIdsArgs(ref documentScriptArgs, "txtFilePath", txtFilePath);
            AddNewArgToIdsArgs(ref documentScriptArgs, "idmlPath", idmlPath);
            AddNewArgToIdsArgs(ref documentScriptArgs, "docOutputPath", docOutputPath);
            AddNewArgToIdsArgs(ref documentScriptArgs, "textDirection", textDirection.ToString());

            docScriptParameters.scriptLanguage = "javascript";
            docScriptParameters.scriptFile = _defaultDocScriptFile.FullName;
            docScriptParameters.scriptArgs = documentScriptArgs.ToArray();

            var docScriptResponse = _serviceClient.RunScript(docScriptRequest);

            // check for result w/errors
            if (docScriptResponse != null
                && docScriptResponse.errorNumber != 0)
            {
                throw new ScriptException(
                    $"Can't build {txtFileName} (error number: {docScriptResponse.errorNumber}, message: {docScriptResponse.errorString}).",
                    null);
            }
        }

        /// <summary>
        /// This method collects a list of tagged text files that will be used to create new InDesign Documents.
        /// </summary>
        /// <param name="txtDir">The full path of the directory where the tagged text files are located</param>
        /// <returns>A list of tagged text file paths</returns>
        private static string[] GetTaggedTextFiles(string txtDir)
        {
            // Build list of tagged text
            // Find & sort tagged text documents to read
            var txtFiles1 = Directory.GetFiles(txtDir, "books-*.txt").OrderBy(filename => filename);
            var txtFiles2 = Directory.GetFiles(txtDir, "book-*.txt").OrderBy(filename => filename);
            var txtFiles = txtFiles1.Concat(txtFiles2).ToArray();
            return txtFiles;
        }

        /// <summary>
        /// This method creates a new InDesign Book (and PDF) from previously-generated InDesign Documents
        /// </summary>
        /// <param name="jobId">The job ID that generated the InDDesign Documents</param>
        /// <exception cref="ScriptException">An InDesign Server exception that resulted from executing the script</exception>
        private void CreateBook(string jobId)
        {
            var docPattern = $"preview-{jobId}-*.indd";
            var pdfOutputPath = $@"{_pdfDocDir}\preview-{jobId}.pdf";
            var bookOutputPath = $@"{_idmlDocDir}\preview-{jobId}.indb";

            _logger.LogDebug("Creating InDesign Book and PDF");
            IList<IDSPScriptArg> bookScriptArgs = new List<IDSPScriptArg>();
            AddNewArgToIdsArgs(ref bookScriptArgs, "docPath", _idmlDocDir.FullName);
            AddNewArgToIdsArgs(ref bookScriptArgs, "docPattern", docPattern);
            AddNewArgToIdsArgs(ref bookScriptArgs, "bookPath", bookOutputPath);
            AddNewArgToIdsArgs(ref bookScriptArgs, "pdfPath", pdfOutputPath);

            var bookScriptRequest = new RunScriptRequest();

            var bookScriptParameters = new RunScriptParameters();
            bookScriptRequest.runScriptParameters = bookScriptParameters;

            bookScriptParameters.scriptLanguage = "javascript";
            bookScriptParameters.scriptFile = _defaultBookScriptFile.FullName;
            bookScriptParameters.scriptArgs = bookScriptArgs.ToArray();

            RunScriptResponse bookScriptResponse = _serviceClient.RunScript(bookScriptRequest);

            // check for result w/errors
            if (bookScriptResponse != null
                && bookScriptResponse.errorNumber != 0)
            {
                throw new ScriptException(
                    $"Can't execute book script (error number: {bookScriptResponse.errorNumber}, message: {bookScriptResponse.errorString}).",
                    null);
            }

            _logger.LogDebug("Finished creating InDesign Book and PDF");
        }

        /// <summary>
        /// Add a new IDS argument to the collection of IDS arguments.
        /// </summary>
        /// <param name="scriptArgs">The IDS arguments collection to add to. (required)</param>
        /// <param name="newArgName">The new argument key name. (required)</param>
        /// <param name="newArgValue">The new argument value. (optional)</param>
        private void AddNewArgToIdsArgs(
            ref IList<IDSPScriptArg> scriptArgs, 
            string newArgName,
            string newArgValue = null)
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