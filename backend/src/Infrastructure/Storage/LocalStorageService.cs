namespace Spot.Infrastructure.Storage;

public class LocalStorageService : IMediaStorageService
{
    private readonly string _storagePath;

    public LocalStorageService(string? customPath = null)
    {
        _storagePath = customPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "media");
        if (!Directory.Exists(_storagePath))
        {
            Directory.CreateDirectory(_storagePath);
        }
    }

    public async Task<string> UploadMediaAsync(Stream content, string fileName, string contentType, CancellationToken ct = default)
    {
        string extension = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = contentType switch
            {
                "video/mp4" => ".mp4",
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                "image/webp" => ".webp",
                _ => ".bin"
            };
        }

        string uniqueFileName = $"{Guid.NewGuid():N}{extension}";
        string fullPath = Path.Combine(_storagePath, uniqueFileName);

        using (var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
        {
            await content.CopyToAsync(fileStream, ct);
        }

        return $"/media/{uniqueFileName}";
    }
}
