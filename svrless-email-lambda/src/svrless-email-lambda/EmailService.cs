using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using System.Net;
public class EmailService : IEmailService
{
  private readonly IAmazonSimpleEmailService _sesClient; 
  private static readonly string senderEmail = "matesumeet19@gmail.com";
  public EmailService(IAmazonSimpleEmailService sesClient)
  {
    _sesClient = sesClient;
  }

  public async Task<bool> SendEmailAsync(string email, string subject, string body)
  {
    var sesRequest = new SendEmailRequest
    {
      Source = senderEmail,
      Destination = new Destination {
          ToAddresses = new List<string> { email }
      },
      Message = new Message
      {
          Subject = new Content(subject),
          Body = new Body
          {
              Html = new Content(body)
          }
      }
    };

    var response = await _sesClient.SendEmailAsync(sesRequest);
    return response.HttpStatusCode == HttpStatusCode.OK;
  }
}