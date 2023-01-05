/*
Copyright © 2021 by Biblica, Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace TptMain.Util
{
    /// <summary>
    /// This class communicates with an S3-based remote repository.
    /// 
    /// This was copied from TVP and updated for the latest S3 library support
    /// </summary>
    public class S3Service : IRemoteService
    {
        private static int MAX_KEYS_TO_FETCH = 1000;

        // AWS configuration parameters.
        string accessKey = AWSCredentials.AWS_ACCESS_KEY_ID;
        string secretKey = AWSCredentials.AWS_ACCESS_KEY_SECRET;
        RegionEndpoint region = RegionEndpoint.GetBySystemName(AWSCredentials.AWS_TPT_REGION) ?? RegionEndpoint.USEast2;
        public virtual string BucketName { get; set; } = AWSCredentials.AWS_TPT_BUCKET_NAME;

        /// <summary>
        /// The client used to communicate with S3.
        /// </summary>
        public virtual AmazonS3Client S3Client { get; set; }

        public S3Service()
        {
            S3Client = new AmazonS3Client(accessKey, secretKey, region);
        }

        public List<string> ListAllFiles(string prefix)
        {
            List<string> fileNames = new List<string>();
            ListObjectsV2Request request = new ListObjectsV2Request
            {
                BucketName = BucketName,
                MaxKeys = MAX_KEYS_TO_FETCH,
                Prefix = prefix
            };
            ListObjectsV2Response response;
            do
            {
                Task<ListObjectsV2Response> responseTask = S3Client.ListObjectsV2Async(request);
                responseTask.Wait();

                response = responseTask.Result;

                // Process the response.
                foreach (S3Object entry in response.S3Objects)
                {
                    fileNames.Add(entry.Key);
                }
                request.ContinuationToken = response.NextContinuationToken;
            } while (response.IsTruncated);

            return fileNames;
        }

        public Stream GetFileStream(string file)
        {
            GetObjectRequest getObjectRequest = new GetObjectRequest
            {
                BucketName = BucketName,
                Key = file
            };

            Task<GetObjectResponse> responseTask = S3Client.GetObjectAsync(getObjectRequest);
            responseTask.Wait();

            GetObjectResponse getObjectResponse = responseTask.Result;

            return getObjectResponse.ResponseStream;
        }

        public HttpStatusCode PutFileStream(string filename, Stream file)
        {
            PutObjectRequest putObjectRequest = new PutObjectRequest
            {
                BucketName = BucketName,
                Key = filename,
                InputStream = file
            };

            Task< PutObjectResponse> responseTask = S3Client.PutObjectAsync(putObjectRequest);
            responseTask.Wait();

            PutObjectResponse putObjectResponse = responseTask.Result;

            return putObjectResponse.HttpStatusCode;
        }

        public async Task<HttpStatusCode> PutFileStreamAsync(string filename, Stream file)
        {
            PutObjectRequest putObjectRequest = new PutObjectRequest
            {
                BucketName = BucketName,
                Key = filename,
                InputStream = file
            };

            PutObjectResponse putObjectResponse = await S3Client.PutObjectAsync(putObjectRequest);

            return putObjectResponse.HttpStatusCode;
        }

        public HttpStatusCode DeleteFile(string filename)
        {
            DeleteObjectRequest deleteObjectRequest = new DeleteObjectRequest
            {
                BucketName = BucketName,
                Key = filename
            };

            Task< DeleteObjectResponse> responseTask = S3Client.DeleteObjectAsync(deleteObjectRequest);
            responseTask.Wait();

            DeleteObjectResponse deleteObjectResponse = responseTask.Result;

            return deleteObjectResponse.HttpStatusCode;
        }
    }
}
