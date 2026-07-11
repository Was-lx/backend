using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WaslX.Application.Abstractions.Media;
using WaslX.Domain.Results;
using WaslX.Infrastructure.Settings;

namespace WaslX.Infrastructure.Media;

/// <summary>Uploads media to Cloudinary and returns a permanent, publicly reachable URL.</summary>
internal sealed class CloudinaryMediaStorageService : IMediaStorageService
{
    private readonly Cloudinary _cloudinary;
    private readonly ILogger<CloudinaryMediaStorageService> _logger;

    public CloudinaryMediaStorageService(IOptions<CloudinarySettings> options, ILogger<CloudinaryMediaStorageService> logger)
    {
        var settings = options.Value;
        _cloudinary = new Cloudinary(new Account(settings.CloudName, settings.ApiKey, settings.ApiSecret))
        {
            Api = { Secure = true }
        };
        _logger = logger;
    }

    public async Task<Result<UploadedMediaResult>> UploadAsync(
        byte[] content, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        try
        {
            using var stream = new MemoryStream(content);
            RawUploadResult result;
            if (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                result = await _cloudinary.UploadAsync(new ImageUploadParams { File = new FileDescription(fileName, stream) }, cancellationToken);
            else if (contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase))
                result = await _cloudinary.UploadAsync(new VideoUploadParams { File = new FileDescription(fileName, stream) }, cancellationToken);
            else
                result = await _cloudinary.UploadAsync(new RawUploadParams { File = new FileDescription(fileName, stream) }, "auto", cancellationToken);

            if (result.Error is not null)
            {
                _logger.LogError("Cloudinary upload failed: {Error}", result.Error.Message);
                return Result.Failure<UploadedMediaResult>(AppErrors.MediaUploadFailed);
            }

            return Result.Success(new UploadedMediaResult(result.SecureUrl.ToString(), contentType, fileName));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cloudinary upload threw");
            return Result.Failure<UploadedMediaResult>(AppErrors.MediaUploadFailed);
        }
    }
}
