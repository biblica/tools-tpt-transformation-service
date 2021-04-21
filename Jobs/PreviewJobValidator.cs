using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using TptMain.Models;

namespace TptMain.Jobs
{
    /// <summary>
    /// Job manager for handling typesetting preview job request management and execution.
    /// </summary>
    public class PreviewJobValidator : IDisposable, IPreviewJobValidator
    {
        /// <summary>
        /// Type-specific logger (injected).
        /// </summary>
        private readonly ILogger<PreviewJobValidator> _logger;

        /// <summary>
        /// Job preview context (persistence; injected).
        /// </summary>
        private readonly TptServiceContext _tptServiceContext;


        /// <summary>
        /// Basic ctor.
        /// </summary>
        /// <param name="logger">Type-specific logger (required).</param>
        /// <param name="configuration">System configuration (required).</param>
        /// <param name="tptServiceContext">Database context (persistence; required).</param>
        /// <param name="scriptRunner">Script runner (required).</param>
        /// <param name="templateManager">Template manager (required).</param>
        /// <param name="paratextApi">Paratext API for verifying user authorization on projects (required).</param>
        /// <param name="paratextProjectService">Paratext Project service for getting information related to local Paratext projects. (required).</param>
        /// <param name="jobScheduler">Job scheduler (required).</param>
        public PreviewJobValidator(
            ILogger<PreviewJobValidator> logger,
            IConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _ = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public void ValidatePreviewJob(PreviewJob previewJob)
        {
            _ = previewJob ?? throw new ArgumentNullException(nameof(previewJob));
            var modelRootPrefix = $"{nameof(previewJob)}.";

            // validate root level parameters
            /// allowed to be invalid upon submission: Id, DateStarted, DateCompleted, DateCancelled, State
            _ = previewJob.User ?? throw new ArgumentNullException($"{modelRootPrefix}{nameof(previewJob.User)}");

            // TODO validate project selection parameters

            // TODO validate typesetting parameters

            throw new ArgumentException();
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