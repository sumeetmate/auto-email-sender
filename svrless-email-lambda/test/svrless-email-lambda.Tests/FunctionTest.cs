using Xunit;
using Amazon.Lambda;
using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;
using Amazon.Lambda.S3Events;

using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using System.Collections.Generic;

namespace svrless_email_lambda.Tests;

public class FunctionTest
{
    [Fact]
    public async Task TestS3EventLambdaFunction()
    {
        IAmazonS3 s3Client = new AmazonS3Client(RegionEndpoint.USEast2);

        var bucketName = "lambda-svrless-email-lambda-".ToLower() + DateTime.Now.Ticks;
        var key = "text.txt";
        var content = "sample data";

        await s3Client.PutBucketAsync(bucketName);
        try
        {
            await s3Client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = bucketName,
                Key = key,
                ContentBody = content
            });
            
            var s3Event = new S3Event
            {
                Records = new List<S3Event.S3EventNotificationRecord>
                {
                    new S3Event.S3EventNotificationRecord
                    {
                        S3 = new S3Event.S3Entity
                        {
                            Bucket = new S3Event.S3BucketEntity {Name = bucketName },
                            Object = new S3Event.S3ObjectEntity {Key = key }
                        }
                    }
                }
            };

            var function = new Function(s3Client);
            var result = await function.FunctionHandler(s3Event,new TestLambdaContext());

            Assert.Equal(content, result);
        }
        finally
        {
            await AmazonS3Util.DeleteS3BucketWithObjectsAsync(s3Client, bucketName);
        }
    }
}