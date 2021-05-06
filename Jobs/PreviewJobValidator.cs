using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using TptMain.Exceptions;
using TptMain.Models;
using TptMain.ParatextProjects;
using TptMain.Util;

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
        /// Basic ctor.
        /// </summary>
        /// <param name="logger">Type-specific logger (required).</param>
        /// <param name="configuration">System configuration (required).</param>
        /// <param name="paratextApi">Paratext API for verifiying user authorization on projects (required).</param>
        public PreviewJobValidator(
            ILogger<PreviewJobValidator> logger,
            IConfiguration configuration,
            ParatextApi paratextApi)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _ = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _paratextApi = paratextApi ?? throw new ArgumentNullException(nameof(paratextApi));
        }

        /// <summary>
        /// Validates a <para>PreviewJob</para> for eligibility.
        /// </summary>
        /// <param name="previewJob"><para>PreviewJob</para> to validate. (required)</param>
        /// <exception cref="ArgumentNullException">Thrown if the parameters is null.</exception>
        /// <exception cref="ArgumentException">Thrown for an invalid parameter.</exception>
        public void ValidatePreviewJob(PreviewJob previewJob)
        {
            // Input validation
            _ = previewJob ?? throw new ArgumentNullException(nameof(previewJob));

            // error tracking list and handler
            var errors = new List<String>();
            Action<string> errorHandlerFunc = (errorMessage) => errors.Add(errorMessage);

            // track parent prefix strings for cleaner error printouts and organization
            var modelRootPrefix = $"{nameof(previewJob)}.";
            var modelBibleSelectionPrefix = $"{modelRootPrefix}{nameof(previewJob.BibleSelectionParams)}.";
            var modelTypesetSelectionPrefix = $"{modelRootPrefix}{nameof(previewJob.TypesettingParams)}.";

            // validate root level parameters
            /// allowed to be invalid upon submission: Id, DateStarted, DateCompleted, DateCancelled, State
            ValidateString($"{modelRootPrefix}{nameof(previewJob.User)}", previewJob.User, errorHandlerFunc);

            // User authorization
            try
            {
                _paratextApi.IsUserAuthorizedOnProject(previewJob);
            } catch (PreviewJobException ex)
            {
                errorHandlerFunc($"'{modelRootPrefix}{nameof(previewJob.User)}' is not valid. {ex.Message}");
            }


            // validate bible selection parameters
            ValidateString(
                $"{modelBibleSelectionPrefix}{nameof(previewJob.BibleSelectionParams.ProjectName)}",
                previewJob.BibleSelectionParams.ProjectName,
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

            // throw exception containing found errors, if any.
            if (errors.Count > 0)
            {
                throw new ArgumentException($" '{nameof(previewJob)}' validation errors were encountered:{NEWLINE_TAB}" 
                    + String.Join(NEWLINE_TAB, errors.ToArray()));
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
            if (String.IsNullOrWhiteSpace(input))
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
            } catch (ArgumentException ex)
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
        /// Disposes of class resources.
        /// </summary>
        public void Dispose()
        {
            _logger.LogDebug("Dispose().");
        }
    }
}