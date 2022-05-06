/*
Copyright © 2021 by Biblica, Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using TptMain.Exceptions;
using TptMain.Models;
using TptMain.ParatextProjects;
using TptMain.Text;
using TptMain.Util;
using static System.String;

namespace TptMain.Jobs
{
    /// <summary>
    /// Job manager for handling typesetting preview job request management and execution.
    /// </summary>
    public class PreviewJobValidator : IDisposable, IPreviewJobValidator
    {
        /// <summary>
        /// NEWLINE and tab constant. Use for print clean indented error messages.
        /// </summary>
        public static readonly string NEWLINE_TAB = $"{Environment.NewLine}\t";

        /// <summary>
        /// Type-specific logger (injected).
        /// </summary>
        private readonly ILogger<PreviewJobValidator> _logger;

        /// <summary>
        /// Paratext API service used to authorize user access.
        /// </summary>
        private readonly ParatextApi _paratextApi;

        /// <summary>
        /// Paratext Project service used to get information related to local Paratext projects.
        /// </summary>
        private readonly ParatextProjectService _paratextProjectService;


        /// <summary>
        /// Basic ctor.
        /// </summary>
        /// <param name="logger">Type-specific logger (required).</param>
        /// <param name="configuration">System configuration (required).</param>
        /// <param name="paratextApi">Paratext API for verifiying user authorization on projects (required).</param>
        public PreviewJobValidator(
            ILogger<PreviewJobValidator> logger,
            IConfiguration configuration,
            ParatextProjectService paratextProjectService,
            ParatextApi paratextApi)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _ = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _paratextApi = paratextApi ?? throw new ArgumentNullException(nameof(paratextApi));
            _paratextProjectService = paratextProjectService ?? throw new ArgumentNullException(nameof(paratextProjectService));
        }

        /// <summary>
        /// Validates a <para>PreviewJob</para> for eligibility.
        /// </summary>
        /// <param name="previewJob"><para>PreviewJob</para> to validate. (required)</param>
        /// <exception cref="ArgumentNullException">Thrown if the parameters is null.</exception>
        /// <exception cref="ArgumentException">Thrown for an invalid parameter.</exception>
        public void ProcessJob(PreviewJob previewJob)
        {
            // Input validation
            _ = previewJob ?? throw new ArgumentNullException(nameof(previewJob));

            // error tracking list and handler
            var errors = new List<string>();
            Action<string> errorHandlerFunc = (errorMessage) => errors.Add(errorMessage);

            // track parent prefix strings for cleaner error printouts and organization
            var modelRootPrefix = $"{nameof(previewJob)}.";
            var modelBibleSelectionPrefix = $"{modelRootPrefix}{nameof(previewJob.BibleSelectionParams)}.";
            var modelTypesetSelectionPrefix = $"{modelRootPrefix}{nameof(previewJob.TypesettingParams)}.";

            // validate root level parameters
            /// allowed to be invalid upon submission: Id, DateStarted, DateCompleted, DateCancelled, State
            ValidateString($"{modelRootPrefix}{nameof(previewJob.User)}", previewJob.User, errorHandlerFunc);

            // only relevant if we're dealing with a paratext project
            if (previewJob.ContentSource == ContentSource.ParatextRepository)
            {
                // User authorization
                try
                {
                    _paratextApi.IsUserAuthorizedOnProject(previewJob);
                }
                catch (PreviewJobException ex)
                {
                    errorHandlerFunc($"'{modelRootPrefix}{nameof(previewJob.User)}' is not valid. {ex.Message}");
                }
            }


            // validate bible selection parameters
            ValidateString(
                $"{modelBibleSelectionPrefix}{nameof(previewJob.BibleSelectionParams.ProjectName)}",
                previewJob.BibleSelectionParams.ProjectName,
                errorHandlerFunc);
            var bookCodeSet = ValidateBookIdList(
                $"{modelBibleSelectionPrefix}{nameof(previewJob.BibleSelectionParams.SelectedBooks)}",
                previewJob.BibleSelectionParams.SelectedBooks,
                errorHandlerFunc);

            // validate typesetting parameters
            ValidateObject(
                $"{modelTypesetSelectionPrefix}{nameof(previewJob.TypesettingParams.BookFormat)}",
                previewJob.TypesettingParams.BookFormat,
                errorHandlerFunc);
            ValidateFloat(
                $"{modelTypesetSelectionPrefix}{nameof(previewJob.TypesettingParams.FontSizeInPts)}",
                previewJob.TypesettingParams.FontSizeInPts,
                MainConsts.ALLOWED_FONT_SIZE_IN_PTS,
                errorHandlerFunc);
            ValidateFloat(
                $"{modelTypesetSelectionPrefix}{nameof(previewJob.TypesettingParams.FontLeadingInPts)}",
                previewJob.TypesettingParams.FontLeadingInPts,
                MainConsts.ALLOWED_FONT_LEADING_IN_PTS,
                errorHandlerFunc);
            ValidateFloat(
                $"{modelTypesetSelectionPrefix}{nameof(previewJob.TypesettingParams.PageWidthInPts)}",
                previewJob.TypesettingParams.PageWidthInPts,
                MainConsts.ALLOWED_PAGE_WIDTH_IN_PTS,
                errorHandlerFunc);
            ValidateFloat(
                $"{modelTypesetSelectionPrefix}{nameof(previewJob.TypesettingParams.PageHeightInPts)}",
                previewJob.TypesettingParams.PageHeightInPts,
                MainConsts.ALLOWED_PAGE_HEIGHT_IN_PTS,
                errorHandlerFunc);
            ValidateFloat(
                $"{modelTypesetSelectionPrefix}{nameof(previewJob.TypesettingParams.PageHeaderInPts)}",
                previewJob.TypesettingParams.PageHeaderInPts,
                MainConsts.ALLOWED_PAGE_HEADER_IN_PTS,
                errorHandlerFunc);

            // Generate calculated needed fields
            if (errors.Count <= 0)
            {
                try
                {
                    GenerateAdditionalParams(previewJob, bookCodeSet);
                }
                catch (Exception ex)
                {
                    errorHandlerFunc($"'{modelRootPrefix}{nameof(previewJob.AdditionalParams)}' was unable to be generated. {ex.Message}");
                }
            }

            // throw exception containing found errors, if any.
            if (errors.Count > 0)
            {
                previewJob.SetError("There were validation errors.", $" '{nameof(previewJob)}' validation errors were encountered:{NEWLINE_TAB}"
                                            + Join(NEWLINE_TAB, errors.ToArray()));
            }
            else
            {
                previewJob.State.Add(new PreviewJobState(JobStateEnum.Started, JobStateSourceEnum.JobValidation));
            }
        }

        /// <summary>
        /// Validate the string is non-null, non-empty, non-whitespace.
        /// </summary>
        /// <param name="parameterName">Parameter name. (required)</param>
        /// <param name="input">Entity to validate. (required)</param>
        /// <param name="handleError">Error handling function. (required)</param>
        private void ValidateString(string parameterName, string input, Action<string> handleError)
        {
            // parameter validation
            _ = parameterName ?? throw new ArgumentNullException(nameof(parameterName));
            _ = handleError ?? throw new ArgumentNullException(nameof(handleError));

            // input validation
            if (IsNullOrWhiteSpace(input))
            {
                handleError($"'{parameterName}' string was null, empty, or only whitespace.");
            }
        }

        /// <summary>
        /// Validate a float against its allowed values.
        /// </summary>
        /// <param name="parameterName">Parameter name. (required)</param>
        /// <param name="input">Entity to validate. (required)</param>
        /// <param name="allowedValues">Allowed values for provided parameter. (required)</param>
        /// <param name="handleError">Error handling function. (required)</param>
        /// <returns>The validated value or default. Null will be returned if there was an error.</returns>
        private float? ValidateFloat(string parameterName, float? input, FloatMinMaxDefault allowedValues, Action<string> handleError)
        {
            // parameter validation
            _ = parameterName ?? throw new ArgumentNullException(nameof(parameterName));
            _ = allowedValues ?? throw new ArgumentNullException(nameof(allowedValues));
            _ = handleError ?? throw new ArgumentNullException(nameof(handleError));

            // input validation
            try
            {
                return allowedValues.ValidateValue(input);
            }
            catch (ArgumentException ex)
            {
                handleError($"'{parameterName}' value was invalid: " + ex.Message);
            }

            return null;
        }

        /// <summary>
        /// Validate the object is non-null.
        /// </summary>
        /// <param name="parameterName">Parameter name. (required)</param>
        /// <param name="input">Entity to validate. (required)</param>
        /// <param name="handleError">Error handling function. (required)</param>
        private void ValidateObject(string parameterName, object input, Action<string> handleError)
        {
            // parameter validation
            _ = parameterName ?? throw new ArgumentNullException(nameof(parameterName));
            _ = handleError ?? throw new ArgumentNullException(nameof(handleError));

            // input validation
            if (input == null)
            {
                handleError($"'{parameterName}' object was null.");
            }
        }

        /// <summary>
        /// Validates a list of book code for eligibility.
        /// </summary>
        /// <param name="parameterName">Parameter name. (required)</param>
        /// <param name="bookIdList">Comma-delimited book codes list to validate. (optional, may be null)</param>
        /// <param name="handleError">Error handling function. (required)</param>
        /// <returns>A hashset of the validated book codes</returns>
        private HashSet<string> ValidateBookIdList(string parameterName, string bookIdList, Action<string> handleError)
        {
            // parameter validation
            _ = parameterName ?? throw new ArgumentNullException(nameof(parameterName));
            _ = handleError ?? throw new ArgumentNullException(nameof(handleError));

            // null/empty = no book list, which is ok
            var idSet = new HashSet<string>();
            if (IsNullOrWhiteSpace(bookIdList))
            {
                return idSet;
            }

            // split & iterate book list
            foreach (var bookId in bookIdList.Split(","))
            {
                if (IsNullOrWhiteSpace(bookId))
                {
                    handleError($"Book id in list '{parameterName}' was empty or only whitespace.");
                }
                else
                {
                    var tempBookId = bookId.Trim().ToUpper();
                    if (!BookUtil.UsxCompKeyByCode.ContainsKey(tempBookId))
                    {
                        handleError($"Invalid book id '{tempBookId}' in book id list '{parameterName}'.");
                    }
                    if (!idSet.Add(tempBookId))
                    {
                        handleError($"Duplicate book id '{tempBookId}' in book id list '{parameterName}'.");
                    }
                }
            }

            return idSet;
        }

        /// <summary>
        /// Generate any calculated fields that we will need.
        /// </summary>
        /// <param name="previewJob">The Job to generate the calculated fields for.</param>
        /// <param name="bookCodeList">Set of validated book codes we want included in our preview. Empty set means all.</param>
        private void GenerateAdditionalParams(PreviewJob previewJob, HashSet<string> bookCodeList)
        {
            // parameter validation
            _ = previewJob ?? throw new ArgumentNullException(nameof(previewJob));

            // only relevant if we're dealing with a paratext project
            if (previewJob.ContentSource == ContentSource.ParatextRepository)
            {
                previewJob.AdditionalParams.TextDirection =
                    _paratextProjectService.GetTextDirection(previewJob.BibleSelectionParams.ProjectName);

                // Grab the project's footnote markers if configured to do so.
                if (previewJob.TypesettingParams.UseCustomFootnotes)
                {
                    previewJob.AdditionalParams.CustomFootnoteMarkers = String.Join(',',
                        _paratextProjectService.GetFootnoteCallerSequence(previewJob.BibleSelectionParams.ProjectName));
                    // Throw an error, if custom footnotes are requested but are not available.
                    // This allows us to set the user's expectations early, rather than waiting
                    // for a preview.
                    if (String.IsNullOrEmpty(previewJob.AdditionalParams.CustomFootnoteMarkers))
                    {
                        throw new PreviewJobException(previewJob,
                            "Custom footnotes requested, but aren't specified in the project.");
                    }

                    _logger.LogInformation("Custom footnotes requested and found. Custom footnotes: " +
                                           previewJob.AdditionalParams.CustomFootnoteMarkers);
                }

                // If we're using the project font (rather than what's in the IDML) pass it as an override.
                if (previewJob.TypesettingParams.UseProjectFont)
                {
                    previewJob.AdditionalParams.OverrideFont =
                        _paratextProjectService.GetProjectFont(previewJob.BibleSelectionParams.ProjectName);

                    if (String.IsNullOrEmpty(previewJob.AdditionalParams.OverrideFont))
                    {
                        _logger.LogWarning(
                            $"No font specified for project '{previewJob.BibleSelectionParams.ProjectName}'. IDML font settings will not be modified.");
                        previewJob.AdditionalParams.OverrideFont = null;
                    }
                    else
                    {
                        _logger.LogInformation(
                            $"Override font '{previewJob.AdditionalParams.OverrideFont}' specified for project '{previewJob.BibleSelectionParams.ProjectName}' and will be used.");
                    }
                }
            }

            // If a custom book list is provided, calculate the expected USX composite keys.
            if (bookCodeList != null && bookCodeList.Count > 0)
            {
                var usxCompKeys = new List<string>();
                foreach (var bookCode in bookCodeList)
                {
                    usxCompKeys.Add(BookUtil.UsxCompKeyByCode[bookCode]);
                }

                // add any missing ancillary books if specified.
                if (previewJob.BibleSelectionParams.IncludeAncillary)
                {
                    BookUtil.AncillaryBooks.ForEach(bookCode =>
                    {
                        var ancillaryUsfmCompKey = BookUtil.UsxCompKeyByCode[bookCode];

                        if (!usxCompKeys.Contains(ancillaryUsfmCompKey))
                        {
                            usxCompKeys.Add(ancillaryUsfmCompKey);
                        }
                    });
                }

                previewJob.AdditionalParams.CustomBookListUsxCompKeys = string.Join(',', usxCompKeys);
            }
        }

        /// <summary>
        /// Disposes of class resources.
        /// </summary>
        public void Dispose()
        {
            _logger.LogDebug("Dispose().");
        }

        ///<inheritdoc/>
        public void GetStatus(PreviewJob previewJob)
        {
            // no op
        }

        ///<inheritdoc/>
        public void CancelJob(PreviewJob previewJob)
        {
            previewJob.State.Add(new PreviewJobState(JobStateEnum.Cancelled, JobStateSourceEnum.JobValidation));
        }
    }
}