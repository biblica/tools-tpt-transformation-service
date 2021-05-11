
using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using TptMain.Exceptions;
using TptMain.Models;
using TVPMain.Util;

namespace TptMain.Jobs
{
    /// <summary>
    /// This class is for submitting jobs, either TEMPLATE GENERATION, or TAGGED TEXT, to the SQS queue in AWS.
    /// These jobs will then be picked up by the template generation ability and processed.
    /// The job status and 
    /// </summary>
    public class TransformService
    {
        /// <summary>
        /// Default URL to the Queue, a test queue
        /// </summary>
        public static string AWS_SQS_QUEUE_DEFAULT = "https://sqs.us-east-2.amazonaws.com/007611731121/TestQueue.fifo";

        /// <summary>
        /// The URL to the queue based on an environment variable (AWS_SQS_QUEUE_URL), or the default.
        /// </summary>
        private string _awsSqsQueueURL;

        /// <summary>
        /// Type-specific logger (injected).
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// The current two types of jobs to submit to the queue. These turn into group ids to separate the two
        /// types of jobs in the queue.
        /// </summary>
        public enum TRANSFORM_TYPE
        {
            TAGGED_TEXT,
            TEMPLATE
        }

        /// <summary>
        /// AWS security key as baked into the application from development environment variables
        /// </summary>
        string accessKey = AWSCredentials.AWS_TVP_ACCESS_KEY_ID;

        /// <summary>
        ///  AWS security secret as baked into the application from development environment variables
        /// </summary>
        string secretKey = AWSCredentials.AWS_TVP_ACCESS_KEY_SECRET;

        /// <summary>
        /// Unless otherwise specified, as baked into the application from development envrionment variables, use us-east-2 for testing
        /// </summary>
        RegionEndpoint region = RegionEndpoint.GetBySystemName(AWSCredentials.AWS_TVP_REGION) ?? RegionEndpoint.USEast2;

        /// <summary>
        /// The AWS SQS client
        /// </summary>
        private AmazonSQSClient _amazonSQSClient;

        /// <summary>
        /// Simple constructor to be used by the managers. Creates the connection to AWS. It's ok if there
        /// are more than one of these at a time.
        /// </summary>
        public TransformService(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Get the queue settting from the environment variables, if available. Otherwise use default
            var queueURL = Environment.GetEnvironmentVariable("AWS_SQS_QUEUE_URL");
            if (!string.IsNullOrEmpty(queueURL))
            {
                _awsSqsQueueURL = queueURL;
            }
            else
            {
                _awsSqsQueueURL = AWS_SQS_QUEUE_DEFAULT;
            }

            _amazonSQSClient = new AmazonSQSClient(accessKey, secretKey, region);
        }

        /// <summary>
        /// Places a job on the SQS queue for creating the IDML, the input to the InDesign step of the preview job process
        /// </summary>
        /// <param name="previewJob">The job to submit for Template generation</param>
        public string GenerateTemplate(PreviewJob previewJob)
        {
            try
            {
                string jobJson = JsonConvert.SerializeObject(previewJob);
                return SubmitMessage(jobJson, TRANSFORM_TYPE.TEMPLATE);

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
        /// Places a job on the SQS queue for createing the Tagged Text from the USX
        /// </summary>
        /// <param name="previewJob">The job to submit for Tagged Text generation</param>
        public string GenerateTaggedText(PreviewJob previewJob)
        {
            try
            {
                string jobJson = JsonConvert.SerializeObject(previewJob);
                return SubmitMessage(jobJson, TRANSFORM_TYPE.TAGGED_TEXT);
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
        /// Submits the given message to the queue, identifying the group of the message based on the type of the transform.
        /// </summary>
        /// <param name="message">The message: the json representation of the previewJob itself</param>
        /// <param name="transformType">Whether to do template generation or tagged text</param>
        /// <returns>A unique id for the submitted job & generation work. This should be used to look up job status in S3.</returns>
        string SubmitMessage(string message, TRANSFORM_TYPE transformType)
        {
            _logger.LogDebug("Submitting new {TYPE} job: {MSG}", transformType.ToString(), message);

            // Current time to add o the message de-duplication id
            // This helps make every message unique
            long currentEpochMilliseconds = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            string uniqueQueueId = currentEpochMilliseconds.ToString() + "-" + Guid.NewGuid().ToString();

            // Put the message onto the queue
            SendMessageRequest sendMessageRequest = new SendMessageRequest
            {
                QueueUrl = _awsSqsQueueURL,
                MessageBody = message,
                MessageGroupId = transformType.ToString(),
                MessageDeduplicationId = uniqueQueueId
            };

            // Wait for the message to be submitted
            Task<SendMessageResponse> responseTask = _amazonSQSClient.SendMessageAsync(sendMessageRequest);
            responseTask.Wait();

            SendMessageResponse response = responseTask.Result;

            _logger.LogDebug("Job {TYPE} job {UNIQUE_ID} / {MSG_ID} submitted successfully", transformType.ToString(), uniqueQueueId, response.MessageId);

            return uniqueQueueId;
        }
    }
}