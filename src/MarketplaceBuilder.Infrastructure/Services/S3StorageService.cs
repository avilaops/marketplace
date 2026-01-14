using Amazon.S3;
using Amazon.S3.Model;
using MarketplaceBuilder.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MarketplaceBuilder.Infrastructure.Services;

public class S3StorageService : IObjectStorage
{
    private readonly IAmazonS3 _s3Client;
    private readonly IConfiguration _configuration;
    private readonly ILogger<S3StorageService> _logger;
    private readonly string _bucketName;
    private readonly string _publicBaseUrl;

    public S3StorageService(
        IAmazonS3 s3Client,
        IConfiguration configuration,
        ILogger<S3StorageService> logger)
    {
        _s3Client = s3Client;
        _configuration = configuration;
        _logger = logger;
        _bucketName = configuration["Storage:Bucket"] ?? "marketplace";
        _publicBaseUrl = configuration["Storage:PublicBaseUrl"] ?? "http://localhost:9000/marketplace";
    }

    public async Task<UploadResult> UploadAsync(
        Guid tenantId,
        string key,
        Stream stream,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = key,
                InputStream = stream,
                ContentType = contentType,
                AutoCloseStream = false
            };

            var response = await _s3Client.PutObjectAsync(request, cancellationToken);

            if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception($"S3 upload failed with status code: {response.HttpStatusCode}");
            }

            var publicUrl = BuildPublicUrl(key);
            var sizeBytes = stream.Length;

            _logger.LogInformation(
                "Successfully uploaded object to S3. Key: {Key}, Size: {Size} bytes, Tenant: {TenantId}",
                key, sizeBytes, tenantId);

            return new UploadResult(key, publicUrl, sizeBytes, contentType);
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "S3 error uploading object. Key: {Key}, Tenant: {TenantId}", key, tenantId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading object to S3. Key: {Key}, Tenant: {TenantId}", key, tenantId);
            throw;
        }
    }

    public string BuildPublicUrl(string objectKey)
    {
        // Remove trailing slash from base URL if exists
        var baseUrl = _publicBaseUrl.TrimEnd('/');
        
        // Ensure object key doesn't start with slash
        var key = objectKey.TrimStart('/');
        
        return $"{baseUrl}/{key}";
    }

    public async Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new DeleteObjectRequest
            {
                BucketName = _bucketName,
                Key = objectKey
            };

            var response = await _s3Client.DeleteObjectAsync(request, cancellationToken);

            _logger.LogInformation("Successfully deleted object from S3. Key: {Key}", objectKey);
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "S3 error deleting object. Key: {Key}", objectKey);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting object from S3. Key: {Key}", objectKey);
            throw;
        }
    }
}
