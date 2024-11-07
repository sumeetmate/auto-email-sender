using Xunit;
using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Moq;

namespace svrless_email_lambda.Tests;

public class FunctionTest
{
    private readonly Mock<IS3Service> _mockS3Service;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<ILambdaContext> _mockLambdaContext;
    private readonly Mock<ILambdaLogger> _mockLambdaLogger;
    private readonly Function _function;

    public FunctionTest()
    {
        _mockS3Service = new Mock<IS3Service>();
        _mockEmailService = new Mock<IEmailService>();
        _mockLambdaContext = new Mock<ILambdaContext>();
        _mockLambdaLogger = new Mock<ILambdaLogger>();

        _mockLambdaContext.SetupGet(c => c.Logger).Returns(_mockLambdaLogger.Object);
        _function = new Function(_mockS3Service.Object, _mockEmailService.Object);
    }

    [Fact]
    public async Task Lambda_SendEmailWhenFileIsuploaded()
    {
        //Arrange
        var s3Event = new S3Event
        {
            Records = new List<S3Event.S3EventNotificationRecord>
            {
                new S3Event.S3EventNotificationRecord
                {
                    S3 = new S3Event.S3Entity
                    {
                        Bucket = new S3Event.S3BucketEntity { Name = "my-bucket" },
                        Object = new S3Event.S3ObjectEntity { Key = "download.html" }
                    }   
                },
                new S3Event.S3EventNotificationRecord
                {
                    S3 = new S3Event.S3Entity
                    {
                        Bucket = new S3Event.S3BucketEntity { Name = "my-bucket" },
                        Object = new S3Event.S3ObjectEntity { Key = "myfile.csv" }
                    } 
                }
            }
        };

        _mockS3Service
            .Setup(s => s.GetFileContentAsync("my-bucket", "download.html"))
            .ReturnsAsync("<p>Hello {{NAME}}</p>");

        _mockS3Service
            .Setup(s => s.GetFileContentAsync("my-bucket", "myfile.csv"))
            .ReturnsAsync("John Doe,john@example.com\nJane Smith,jane@example.com");

        _mockEmailService
            .Setup(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        //Act
        await _function.FunctionHandler(s3Event, _mockLambdaContext.Object);

        //Assert
        _mockEmailService.Verify(e => e.SendEmailAsync("john@example.com", "Email from Lambda", "<p>Hello John Doe</p>"), Times.Once);
        _mockEmailService.Verify(e => e.SendEmailAsync("jane@example.com", "Email from Lambda", "<p>Hello Jane Smith</p>"), Times.Once);
    }
}