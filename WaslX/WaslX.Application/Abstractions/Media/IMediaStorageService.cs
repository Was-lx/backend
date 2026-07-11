using WaslX.Domain.Results;

namespace WaslX.Application.Abstractions.Media;

/// <summary>
/// Uploads media (images, video, documents) to permanent public storage. WhatsApp's own media
/// URLs are short-lived, so anything shown in the inbox or sent as a reply must first live here.
/// </summary>
public interface IMediaStorageService
{
    Task<Result<UploadedMediaResult>> UploadAsync(
        byte[] content, string fileName, string contentType, CancellationToken cancellationToken = default);
}

/// <summary>A permanently-hosted media asset.</summary>
public record UploadedMediaResult(string Url, string MimeType, string? FileName);
