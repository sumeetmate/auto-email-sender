using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Amazon.S3;
using Amazon.S3.Util;
using Amazon.S3.Model;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace svrless_email_lambda;

public class Function
{
    IAmazonS3 S3Client { get; set; }

    IAmazonDynamoDB  DynamoDBClient {get; set;}
    /// <summary>
    /// Default constructor. This constructor is used by Lambda to construct the instance. When invoked in a Lambda environment
    /// the AWS credentials will come from the IAM role associated with the function and the AWS region will be set to the
    /// region the Lambda function is executed in.
    /// </summary>
    public Function()
    {
        S3Client = new AmazonS3Client();
        DynamoDBClient = new AmazonDynamoDBClient();
    }

    /// <summary>
    /// Constructs an instance with a preconfigured S3 client. This can be used for testing the outside of the Lambda environment.
    /// </summary>
    /// <param name="s3Client">The service client to access Amazon S3.</param>
    public Function(IAmazonS3 s3Client)
    {
        this.S3Client = s3Client;
    }
    
    /// <summary>
    /// This method is called for every Lambda invocation. This method takes in an S3 event object and can be used 
    /// to respond to S3 notifications.
    /// </summary>
    /// <param name="evntThe event for the Lambda function handler to process.
    /// <param name="context">The ILambdaContext that provides methods for logging and describing the Lambda environment.</param>
    /// <returns></returns>
    public async Task<string?> FunctionHandler(S3Event evnt, ILambdaContext context)
    {
        var s3Record = evnt.Records?[0].S3;
        if(s3Record == null)
            return null;

        try
        {
            var bucketName = s3Record.Bucket.Name;
            var key = s3Record.Object.Key;
            var fileContent = await GetObjectFromS3(bucketName, key);
            //var (name, email) = await GetDataFromDynamoDB(key);
            return fileContent;
        }
        catch(Exception e)
        {
            context.Logger.LogInformation($"Error getting object {s3Record.Object.Key} from bucket {s3Record.Bucket.Name}. Make sure they exist and your bucket is in the same region as this function.");
            context.Logger.LogInformation(e.Message);
            context.Logger.LogInformation(e.StackTrace);
            throw;
        }
    }

    private async Task<string> GetObjectFromS3(string bucketName, string objectKey)
    {
        using (var _s3Client = new AmazonS3Client())
        {
            var request = new GetObjectRequest
            {
                BucketName = bucketName,
                Key = objectKey
            };

            using (var response = await _s3Client.GetObjectAsync(request))
            using (var reader = new StreamReader(response.ResponseStream))
            {
                string fileContent = await reader.ReadToEndAsync();
                return fileContent;
            }
        }
    }

    private async Task<(string?, string?)> GetDataFromDynamoDB(string objectKey)
    {
        var table = Table.LoadTable(DynamoDBClient, "UserData");
        var document = await table.GetItemAsync(objectKey);
        var name = document["Name"]?.AsString();
        var email = document["Email"]?.AsString();
    
        return (name, email);
    }
}