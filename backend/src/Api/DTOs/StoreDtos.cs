namespace Spot.Api.DTOs;

public record CategoryDto(Guid Id, string Name, string Slug, string Icon);

public record StoreDto(
    Guid Id,
    string Name,
    CategoryDto Category,
    double Latitude,
    double Longitude,
    int RadiusMeters,
    string Address,
    bool IsPartner,
    double? DistanceMeters,
    double AverageRating,
    int ReviewCount
);

public record NearbyStoresQuery(
    double Latitude,
    double Longitude,
    double RadiusMeters = 1500,
    string? Category = null
);
