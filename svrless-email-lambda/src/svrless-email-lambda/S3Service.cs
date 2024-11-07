using Amazon.S3;
using Amazon.S3.Model;

public class S3Service : IS3Service
{
  private readonly IAmazonS3 _s3Client;

  public S3Service(IAmazonS3 s3Client)
  {
    _s3Client = s3Client;    
  }

  public async Task<string> GetFileContentAsync(string bucketName, string key)
  {
    var request = new GetObjectRequest
    {
        BucketName = bucketName,
        Key = key
    };

    using (var response = await _s3Client.GetObjectAsync(request))
    using (var reader = new StreamReader(response.ResponseStream))
    {
        string fileContent = await reader.ReadToEndAsync();
        return fileContent;
    }
  }
}