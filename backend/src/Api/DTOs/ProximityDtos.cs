namespace Spot.Api.DTOs;

public record GeofenceAlertNotificationDto(
    Guid StoreId,
    string StoreName,
    string Category,
    double DistanceMeters,
    int RadiusMeters,
    string Message
);

public record StoreDealDto(
    Guid StoreId,
    string DealTitle,
    string DealMessage,
    DateTimeOffset Timestamp
);

public record LocationPingDto(
    double Latitude,
    double Longitude,
    double RadiusMeters = 1500,
    string? Category = null
);
