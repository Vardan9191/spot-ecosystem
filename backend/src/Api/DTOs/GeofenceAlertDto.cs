namespace Spot.Api.DTOs;

public record GeofenceAlertDto(
    Guid StoreId,
    string StoreName,
    string CategoryName,
    double DistanceMeters,
    int StoreRadiusMeters,
    string AlertMessage,
    bool IsPartner,
    DateTimeOffset Timestamp
);

public record UserLocationUpdate(
    double Latitude,
    double Longitude,
    double SearchRadiusMeters = 1500
);
