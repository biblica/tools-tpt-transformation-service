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
        /// <param name="logger">Type-specific logger (required).</param>
        /// <param name="configuration">System configuration (required).</param>
        public ScriptRunner(ILogger<ScriptRunner> logger,
            IConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _ = configuration ?? throw new ArgumentNullException(nameof(configuration));

            _idsTimeoutInMSec = (int) TimeSpan.FromSeconds(int.Parse(configuration[ConfigConsts.IdsTimeoutInSecKey]
                                                                     ?? throw new ArgumentNullException(ConfigConsts
                                                                         .IdsTimeoutInSecKey)))
                .TotalMilliseconds;
            _idsPreviewScriptDirectory = new DirectoryInfo((configuration[ConfigConsts.IdsPreviewScriptDirKey]
                                                            ?? throw new ArgumentNullException(ConfigConsts
                                                                .IdsPreviewScriptDirKey)));
            _idsPreviewScriptNameFormat = (configuration[ConfigConsts.IdsPreviewScriptNameFormatKey]
                                           ?? throw new ArgumentNullException(
                                               ConfigConsts.IdsPreviewScriptNameFormatKey));

            _serviceClient = SetUpInDesignClient(configuration);

            _defaultDocScriptFile = new FileInfo(Path.Combine(_idsPreviewScriptDirectory.FullName,
                "CreateDocument.jsx"));

            _defaultBookScriptFile = new FileInfo(Path.Combine(_idsPreviewScriptDirectory.FullName,
                "CreateBook.jsx"));

            _logger.LogDebug("ScriptRunner()");
        }

        /// <summary>
        /// This function sets up the InDesign server client.
        /// </summary>
        /// <param name="configuration">The configuration to aid setting up the Client.</param>
        /// <returns>The instanstiated InDesign server client.</returns>
        public virtual ServicePortTypeClient SetUpInDesignClient(IConfiguration configuration)
        {
            var serviceClient = new ServicePortTypeClient(
                ServicePortTypeClient.EndpointConfiguration.Service,
                configuration[ConfigConsts.IdsUriKey]
                ?? throw new ArgumentNullException(ConfigConsts.IdsUriKey));

            serviceClient.Endpoint.Binding.SendTimeout = TimeSpan.FromMilliseconds(_idsTimeoutInMSec);
            serviceClient.Endpoint.Binding.ReceiveTimeout = TimeSpan.FromMilliseconds(_idsTimeoutInMSec);

            return serviceClient;
        }

        /// <summary>
        /// Execute typesetting preview generation synchronously, with optional cancellation.
        /// </summary>
        /// <param name="inputJob">Input preview job (required).</param>
        /// <param name="footnoteMarkers">Custom footnote markers (optional).</param>
        /// <param name="overrideFont">A font name. If specified, overrides the IDML font settings (optional).</param>
        /// <param name="cancellationToken">Cancellation token (optional, may be null).</param>
        public virtual void RunScript(PreviewJob inputJob, string[] footnoteMarkers, string overrideFont,
            CancellationToken? cancellationToken)
        {
            _logger.LogDebug($"RunScriptAsync() - inputJob.Id={inputJob.Id}.");

            // Simplify our logic later on by establishing a cancellation token if none was passed
            var ct = cancellationToken ?? new CancellationToken();

            // Establish base variables
            var jobId = inputJob.Id;
            var projectName = inputJob.ProjectName;
            var bookFormat = inputJob.BookFormat.ToString();

            // Set top-level base and output dirs
            var idttDir = @"C:\Work\IDTT\";
            var idmlDir = @"C:\Work\IDML\";
            var pdfDir = @"C:\Work\PDF\";

            // Set project input dir and output file
            var txtDir = $@"{idttDir}{bookFormat}\{projectName}\";
            var bookPath = $"{idmlDir}preview-{jobId}.indb";
            var pdfPath = $"{pdfDir}preview-{jobId}.pdf";
            var idmlPath = $"{idmlDir}preview-{jobId}.idml";

            // Create a list of all the tagged text files that will be turned into documents
            var txtFiles = GetTaggedTextFiles(txtDir);
            
            // build the custom footnotes into a CSV string. EG: "a,d,e,ñ,h,Ä".
            String customFootnotes = footnoteMarkers != null ? String.Join(',', footnoteMarkers) : null;

            _logger.LogDebug("Creating InDesign Documents");
            for (int i = 0; i < txtFiles.Length; i++)
            {
                ct.ThrowIfCancellationRequested();
                
                var txtFileName = new FileInfo(txtFiles[i]).Name;
                var txtFilePath = txtFiles[i];
                var docPath = $"{idmlDir}preview-{jobId}-{txtFileName.Replace(".txt", ".indd")}";
                
                _logger.LogDebug($"Creating '{docPath}' from '{txtFileName}'");
                CreateDocument(txtFileName, txtFilePath, docPath, idmlPath, overrideFont, customFootnotes);
            }
            _logger.LogDebug("Finished creating InDesign Documents");

            ct.ThrowIfCancellationRequested();
            _logger.LogDebug("Creating InDesign Book and PDF");
            CreateBook(jobId, idmlDir, bookPath, pdfPath);
            _logger.LogDebug("Finished creating InDesign Book and PDF");

            _logger.LogDebug($"Finished RunScriptAsync() - inputJob.Id={inputJob.Id}.");
        }

        /// <summary>
        /// This method creates an InDesign Document from a specified tagged text file.
        /// </summary>
        /// <param name="txtFileName">The name of the tagged text file</param>
        /// <param name="txtFilePath">The file path of the tagged text to use for the document</param>
        /// <param name="idmlPath">The file path of the IDML to use for the document</param>
        /// <param name="docOutputPath">The file path where the document will be saved</param>
        /// <param name="overrideFont">A font to use instead of the one specified in the IDML</param>
        /// <param name="customFootnotes">Custom footnotes to use in the document</param>
        /// <exception cref="ScriptException">An InDesign Server exception that resulted from executing the script</exception>
        private void CreateDocument(string txtFileName, string txtFilePath, string idmlPath,
            string docOutputPath,
            string overrideFont, string customFootnotes)
        {
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
        /// <param name="docPath">The full path to the directory where the generated documents can be found</param>
        /// <param name="bookOutputPath">The file path where the book will be created</param>
        /// <param name="pdfOutputPath">The file path where the PDF will be created</param>
        /// <exception cref="ScriptException">An InDesign Server exception that resulted from executing the script</exception>
        private void CreateBook(string jobId, string docPath, string bookOutputPath, string pdfOutputPath)
        {
            var docPattern = $"preview-{jobId}-*.indd";

            IList<IDSPScriptArg> bookScriptArgs = new List<IDSPScriptArg>();
            AddNewArgToIdsArgs(ref bookScriptArgs, "docPath", docPath);
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
        }

        /// <summary>
        /// Add a new IDS argument to the collection of IDS arguments.
        /// </summary>
        /// <param name="scriptArgs">The IDS arguments collection to add to. (required)</param>
        /// <param name="newArgName">The new argument key name. (required)</param>
        /// <param name="newArgValue">The new argument value. (optional)</param>
        private void AddNewArgToIdsArgs(ref IList<IDSPScriptArg> scriptArgs, string newArgName,
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