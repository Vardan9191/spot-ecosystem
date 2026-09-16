namespace Spot.Api.DTOs;

public record StoryDto(
    Guid Id,
    Guid StoreId,
    string StoreName,
    string Category,
    bool IsPartner,
    string Title,
    string Description,
    string MediaUrl,
    string? ThumbnailUrl,
    string PromoBadge,
    double? DistanceMeters,
    int ViewCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt
);

public record CreateStoryRequest(
    string Title,
    string Description,
    string MediaUrl,
    string? ThumbnailUrl,
    string PromoBadge,
    int DurationHours = 24
);

public record PresignedUploadRequest(
    string FileName,
    string ContentType,
    long FileSizeBytes,
    int? VideoDurationSeconds = null,
    string? AspectRatio = "9:16"
);

public record PresignedUploadResponse(
    string UploadUrl,
    string FinalMediaUrl,
    string StorageKey,
    DateTimeOffset ExpiresAt,
    Dictionary<string, string> RequiredHeaders
);
