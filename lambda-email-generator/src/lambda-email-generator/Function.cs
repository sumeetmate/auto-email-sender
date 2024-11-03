using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace lambda_email_generator;

public class Function
{
    private static readonly IAmazonDynamoDB _dynamoDBClient = new AmazonDynamoDBClient();
    private static readonly AmazonSimpleEmailServiceClient sesClient = new AmazonSimpleEmailServiceClient();
    private static readonly string senderEmail = "matesumeet19@gmail.com";

    public async Task FunctionHandler(S3Event evnt, ILambdaContext context)
    {
        var s3Record = evnt.Records?[0].S3;
        if(s3Record == null) return;

        var bucketName = s3Record.Bucket.Name;
        var key = s3Record.Object.Key;
        var fileContent = await GetObjectFromS3(bucketName, key);
        var (name, email) = await GetDataFromDynamoDB(key);
        if(email != null)
            await SendEmail(email, "Email from Lambda", fileContent.Replace("__NAME__", name));
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
        var table = Table.LoadTable(_dynamoDBClient, "UserData");
        var document = await table.GetItemAsync(objectKey);
        var name = document["Name"]?.AsString();
        var email = document["Email"]?.AsString();
    
        return (name, email);
    }

    private async Task SendEmail(string email, string subject, string body)
    {
        var sesRequest = new SendEmailRequest
        {
            Source = senderEmail,
            Destination = {
                ToAddresses = new List<string> { email }
            },
            Message = new Message
            {
                Subject = new Content(subject),
                Body = new Body
                {
                    Text = new Content(body)
                }
            }
        };

        await sesClient.SendEmailAsync(sesRequest);
    }
}
