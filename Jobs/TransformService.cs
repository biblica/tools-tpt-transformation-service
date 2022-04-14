/*
Copyright © 2021 by Biblica, Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using Microsoft.Extensions.Configuration;
using SIL.Linq;
using TptMain.Exceptions;
using TptMain.Models;
using TptMain.Util;

namespace TptMain.Jobs
{
    /// <summary>
    /// This class is for submitting jobs, either TEMPLATE GENERATION, or TAGGED TEXT, to the SQS queue in AWS.
    /// These jobs will then be picked up by the template generation ability and processed.
    /// </summary>
    public class TransformService : ITransformService
    {
        /// <summary>
        /// Type-specific logger (injected).
        /// </summary>
        private readonly ILogger<TransformService> _logger;

        /// <summary>
        /// The current two types of jobs to submit to the queue. These turn into group ids to separate the two
        /// types of jobs in the queue.
        /// </summary>
        public enum TransformTypeEnum
        {
            TAGGED_TEXT,
            TEMPLATE
        }

        /// <summary>
        /// A set of status for transform jobs
        /// </summary>
        public enum TransformJobStatus
        {
            WAITING,
            PROCESSING,
            CANCELED,
            TAGGED_TEXT_COMPLETE,
            TEMPLATE_COMPLETE,
            ALL_COMPLETE,
            ERROR
        }

        // A number of consts for later processing

        /// <summary>
        /// The directory where job status is stored
        /// </summary>
        private const string JobsDirectory = "jobs/";

        /// <summary>
        /// USX files bucket prefix.
        /// </summary>
        private const string UsxDirectory = "usx/";

        /// <summary>
        /// A flag/marker that a preview job has been canceled
        /// </summary>
        private const string CancelMarker = ".cancel";

        /// <summary>
        /// A flag that some part of the processing is complete. This is
        /// the prefix to a full file name like '.complete-template'
        /// </summary>
        private const string CompleteMarker = ".complete";

        /// <summary>
        /// A flag for the transform processing is complete
        /// </summary>
        private const string CompleteTemplateMarker = "-tg";

        /// <summary>
        /// A flag that the tagged text is complete
        /// </summary>
        private const string CompleteTaggedTextMarker = "-idtt";

        /// <summary>
        /// AWS security key as baked into the application from development environment variables
        /// </summary>
        private const string AccessKey = AWSCredentials.AWS_ACCESS_KEY_ID;

        /// <summary>
        ///  AWS security secret as baked into the application from development environment variables
        /// </summary>
        private const string SecretKey = AWSCredentials.AWS_ACCESS_KEY_SECRET;

        /// <summary>
        /// The URL to the queue based on an environment variable (AWS_TPT_SQS_QUEUE_URL_TAGGED_TEXT), or the default.
        /// </summary>
        private const string AwsSqsQueueUrlTaggedText = AWSCredentials.AWS_TPT_SQS_QUEUE_URL_TAGGED_TEXT;

        /// <summary>
        /// The URL to the queue based on an environment variable (AWS_TPT_SQS_QUEUE_URL_TEMPLATE), or the default.
        /// </summary>
        private const string AwsSqsQueueUrlTemplate = AWSCredentials.AWS_TPT_SQS_QUEUE_URL_TEMPLATE;

        /// <summary>
        /// Unless otherwise specified, as baked into the application from development environment variables, use us-east-2 for testing
        /// </summary>
        private readonly RegionEndpoint _region = RegionEndpoint.GetBySystemName(AWSCredentials.AWS_TPT_REGION) ?? RegionEndpoint.USEast2;

        /// <summary>
        /// The AWS SQS client
        /// </summary>
        private readonly AmazonSQSClient _amazonSqsClient;

        /// <summary>
        /// The S3Service to talk to S3 to verify status and get results
        /// </summary>
        private readonly S3Service _s3Service;

        /// <summary>
        /// Paratext project dir.
        /// </summary>
        private readonly DirectoryInfo _paratextDocDir;

        /// <summary>
        /// Simple constructor to be used by the managers. Creates the connection to AWS. It's ok if there
        /// are more than one of these at a time.
        /// </summary>
        public TransformService(
            ILogger<TransformService> logger,
            IConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _paratextDocDir = new DirectoryInfo(configuration[ConfigConsts.ParatextDocDirKey]
                                                ?? throw new ArgumentNullException(ConfigConsts.ParatextDocDirKey));

            _s3Service = new S3Service();
            _amazonSqsClient = new AmazonSQSClient(AccessKey, SecretKey, _region);
        }

        /// <summary>
        /// Places a job on the SQS queue for creating the IDML, the input to the InDesign step of the preview job process
        /// </summary>
        /// <param name="previewJob">The job to submit for Template generation</param>
        public void GenerateTemplate(PreviewJob previewJob)
        {
            try
            {
                var jobJson = JsonConvert.SerializeObject(previewJob);
                SubmitMessage(jobJson, previewJob.Id, TransformTypeEnum.TEMPLATE);

            }
            catch (AmazonSQSException ex)
            {
                _logger.LogWarning("TEMPLATE job failed to submit\n\tException: {EX}\n\tStatus Code: {CODE}\n\tError Code: {ER}\n\tError Type: {ET}",
                    ex.Message,
                     ex.StatusCode,
                     ex.ErrorCode,
                     ex.ErrorType
                    );

                throw new PreviewJobException(previewJob, "Could not submit Template job to queue", ex);
            }
        }

        /// <summary>
        /// Moves (copies & deletes) a local project's files to S3, optionally not deleting it after copying.
        /// </summary>
        /// <param name="previewJob">Preview job (required).</param>
        /// <param name="deleteAfterCopy">Delete project after copying (optional; default = true).</param>
        /// <exception cref="ArgumentNullException"></exception>
        public void MoveProjectToS3(PreviewJob previewJob, bool deleteAfterCopy = true)
        {
            var projectName = previewJob.BibleSelectionParams?.ProjectName
                ?? throw new ArgumentNullException(nameof(previewJob.BibleSelectionParams.ProjectName));
            var projectFolder = new DirectoryInfo(Path.Combine(_paratextDocDir.FullName, projectName));
            if (!projectFolder.Exists)
            {
                throw new ArgumentNullException($"missing project directory: ${projectFolder.FullName}");
            }
            projectFolder.EnumerateFiles("*.usx").ForEach(foundFile =>
            {
                using var inputStream = new FileStream(foundFile.FullName, FileMode.Open, FileAccess.Read);
                var bucketPrefix = $"${UsxDirectory}/${projectName}/";
                _s3Service.PutFileStream(bucketPrefix, inputStream);
            });
            if (deleteAfterCopy)
            {
                Directory.Delete(projectFolder.FullName, true);
            }
        }

        /// <summary>
        /// Places a job on the SQS queue for creating the Tagged Text from the USX
        /// </summary>
        /// <param name="previewJob">The job to submit for Tagged Text generation</param>
        public void GenerateTaggedText(PreviewJob previewJob)
        {
            try
            {
                var jobJson = JsonConvert.SerializeObject(previewJob);
                SubmitMessage(jobJson, previewJob.Id, TransformTypeEnum.TAGGED_TEXT);
            }
            catch (AmazonSQSException ex)
            {
                _logger.LogWarning("TAGGED_TEXT job failed to submit\n\tException: {EX}\n\tStatus Code: {CODE}\n\tError Code: {ER}\n\tError Type: {ET}",
                    ex.Message,
                     ex.StatusCode,
                     ex.ErrorCode,
                     ex.ErrorType
                    );

                throw new PreviewJobException(previewJob, "Could not submit TaggedText job to queue", ex);
            }
        }

        /// <summary>
        /// Checks the status of the job based on what is found in the S3 bucket that corresponds to the
        /// job.
        /// </summary>
        /// <param name="previewJobId">The preview job id</param>
        /// <returns>TransformJobStatus based on the state of the S3 bucket</returns>
        public virtual TransformJobStatus GetTransformJobStatus(string previewJobId)
        {

            // First, look for the job directory
            var outputJobs = _s3Service.ListAllFiles(JobsDirectory + previewJobId);
            var found = outputJobs.Find(x =>
               x.Contains(previewJobId)
            );

            // if the job directory isn't found, it must still be in the queue
            if (string.IsNullOrEmpty(found))
            {
                return TransformJobStatus.WAITING;
            }

            // if it's there, look to see if it was canceled
            var outputFiles = _s3Service.ListAllFiles(found);
            var cancelFile = outputFiles.Find(x =>
              x.Contains(CancelMarker)
            );

            // if it's canceled, report that
            if (!string.IsNullOrEmpty(cancelFile))
            {
                return TransformJobStatus.CANCELED;
            }

            // look for a complete marker
            var completeFile = outputFiles.Find(x =>
              x.Contains(CompleteMarker)
            );

            // if the complete marker is there, then respond thus
            if (!string.IsNullOrEmpty(completeFile))
            {
                var transformComplete = completeFile.Contains(CompleteTemplateMarker);
                var taggedTextComplete = completeFile.Contains(CompleteTaggedTextMarker);

                if (transformComplete && taggedTextComplete)
                {
                    return TransformJobStatus.ALL_COMPLETE;
                }

                if (transformComplete)
                {
                    return TransformJobStatus.TEMPLATE_COMPLETE;
                }

                if (taggedTextComplete)
                {
                    return TransformJobStatus.TAGGED_TEXT_COMPLETE;
                }

                // some very strange status where .complete file is there, but not specific
                return TransformJobStatus.ERROR;

            }

            // otherwise, it's in process
            return TransformJobStatus.PROCESSING;
        }

        /// <summary>
        /// Cancels transform jobs by placing a .cancel marker into the job directory
        /// </summary>
        /// <param name="previewJobId"></param>
        public void CancelTransformJobs(string previewJobId)
        {
            var stream = new MemoryStream();
            var streamWriter = new StreamWriter(stream);
            streamWriter.WriteLine("canceled"); // doesn't matter what goes in the file
            streamWriter.Flush();
            stream.Position = 0;

            var statusCode = _s3Service.PutFileStream(JobsDirectory + previewJobId + "/" + CancelMarker, stream);

            if (HttpStatusCode.OK != statusCode)
            {
                throw new PreviewJobException("Could not cancel transformation job for previewJobId: " + previewJobId);
            }
        }

        /// <summary>
        /// Submits the given message to the queue, identifying the group of the message based on the type of the transform.
        /// </summary>
        /// <param name="message">The message: the json representation of the previewJob itself</param>
        /// <param name="transformType">Whether to do template generation or tagged text</param>
        /// <returns>A unique id for the submitted job & generation work. This should be used to look up job status in S3.</returns>
        private void SubmitMessage(string message, string uniqueId, TransformTypeEnum transformType)
        {
            _logger.LogDebug("Submitting new {TYPE} job: {MSG}", transformType.ToString(), message);

            // Put the message onto the queue
            var sendMessageRequest = new SendMessageRequest
            {
                QueueUrl = transformType.Equals(TransformTypeEnum.TAGGED_TEXT)
                    ? AwsSqsQueueUrlTaggedText : AwsSqsQueueUrlTemplate,
                MessageBody = message,
                MessageGroupId = uniqueId,
                MessageDeduplicationId = uniqueId
            };

            // Wait for the message to be submitted
            var responseTask = _amazonSqsClient.SendMessageAsync(sendMessageRequest);
            responseTask.Wait();

            var response = responseTask.Result;

            _logger.LogDebug("Job {TYPE} job {UNIQUE_ID} / {MSG_ID} submitted successfully", transformType.ToString(), uniqueId, response.MessageId);
        }
    }
}