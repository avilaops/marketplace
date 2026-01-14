namespace MarketplaceBuilder.Core.Interfaces;

/// <summary>
/// Resultado do upload de arquivo
/// </summary>
public record UploadResult(
    string ObjectKey,
    string PublicUrl,
    long SizeBytes,
    string? ContentType
);

/// <summary>
/// Interface para armazenamento de objetos (S3-compatible)
/// </summary>
public interface IObjectStorage
{
    /// <summary>
    /// Faz upload de um arquivo para o storage
    /// </summary>
    Task<UploadResult> UploadAsync(
        Guid tenantId,
        string key,
        Stream stream,
        string contentType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Constrói a URL pública de um objeto
    /// </summary>
    string BuildPublicUrl(string objectKey);

    /// <summary>
    /// Deleta um objeto do storage
    /// </summary>
    Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default);
}
