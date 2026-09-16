namespace Spot.Infrastructure.Storage;

public interface IMediaStorageService
{
    Task<string> UploadMediaAsync(Stream content, string fileName, string contentType, CancellationToken ct = default);
}
