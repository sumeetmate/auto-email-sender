using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Amazon.S3;
using Amazon.S3.Util;
using Amazon.S3.Model;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace svrless_email_lambda;

public class Function
{
    private readonly IS3Service _s3Service;
    private readonly IEmailService _emailService;

    public Function()
    {
        this._s3Service = new S3Service(new AmazonS3Client());
        this._emailService = new EmailService(new AmazonSimpleEmailServiceClient());
    }
    public Function(IS3Service s3Service, IEmailService emailService)
    {
        this._s3Service = s3Service;
        this._emailService = emailService;
    }
    
    /// <summary>
    /// This method is called for every Lambda invocation. This method takes in an S3 event object and can be used 
    /// to respond to S3 notifications.
    /// </summary>
    /// <param name="evntThe event for the Lambda function handler to process.
    /// <param name="context">The ILambdaContext that provides methods for logging and describing the Lambda environment.</param>
    /// <returns></returns>
    public async Task FunctionHandler(S3Event evnt, ILambdaContext context)
    {
        var fileContent = string.Empty;
        var record0 = evnt.Records?[0];
        if(record0 != null)
        {
            var s3Record = record0.S3;
            try
            {
                var bucketName = s3Record.Bucket.Name;
                string key = s3Record.Object.Key;

                fileContent = (key.Contains(".html") || key.Contains(".htm")) ? await _s3Service.GetFileContentAsync(bucketName, key) : String.Empty;
            }
            catch(Exception e)
            {
                context.Logger.LogInformation($"Error getting object {s3Record.Object.Key} from bucket {s3Record.Bucket.Name}. Make sure they exist and your bucket is in the same region as this function.");
                context.Logger.LogInformation(e.Message);
                context.Logger.LogInformation(e.StackTrace);
                throw;
            }
        }

        var record1 = evnt.Records?[1];
        if(record1 != null)
        {
            var s3Record = record1.S3;
            List<UserData> users = new List<UserData>();
            try
            {
                var bucketName = s3Record.Bucket.Name;
                string key = s3Record.Object.Key;

                users = (key.Contains(".csv")) ? await GetUsersDataAsync(bucketName, key) : new List<UserData>();
                context.Logger.LogInformation($"Total user Count: {users.Count}");
            }
            catch(Exception e)
            {
                context.Logger.LogInformation($"Error getting object {s3Record.Object.Key} from bucket {s3Record.Bucket.Name}. Make sure they exist and your bucket is in the same region as this function.");
                context.Logger.LogInformation(e.Message);
                context.Logger.LogInformation(e.StackTrace);
                throw;
            }    

            foreach (var user in users)
            {
                try
                {
                    var email_body = fileContent.Replace("{{NAME}}", user.Name);
                    bool sucess =  await _emailService.SendEmailAsync(user.Email, "Email from Lambda", email_body);                    
                    context.Logger.LogInformation($"User:{user.Email} - Sent:{sucess}");
                }
                catch (Exception e)
                {
                    context.Logger.LogInformation($"Error sending email for user {user.Name}");
                    context.Logger.LogInformation(e.Message);
                    context.Logger.LogInformation(e.StackTrace);
                    throw e;
                }
            }    
        }
    }

    private async Task<List<UserData>> GetUsersDataAsync(string bucketName, string key)
    {
        List<UserData> data = new List<UserData>();
        
        var content = await _s3Service.GetFileContentAsync(bucketName, key);
        var lines = content.Split(new[] {'\n', '\r'}, StringSplitOptions.RemoveEmptyEntries);
        foreach(var line in lines)
        {    
            string[] columns = line.Split(',');
            if(columns.Length > 0)
            {
                UserData user = new UserData { Name = columns[0], Email = columns[1] };
                data.Add(user);            
            }
        }
        return data;
    }

    public class UserData
    {
        public string Name { get; set; } = String.Empty;
        public string Email { get; set; } = String.Empty;
    }
}