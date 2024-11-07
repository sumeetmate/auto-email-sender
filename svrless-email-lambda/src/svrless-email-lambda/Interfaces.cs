public interface IS3Service
{
  Task<string> GetFileContentAsync(string bucketName, string key);
}

public interface IEmailService
{
  Task<bool> SendEmailAsync(string email, string subject, string body);
}