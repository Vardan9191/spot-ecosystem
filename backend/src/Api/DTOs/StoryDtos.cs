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
