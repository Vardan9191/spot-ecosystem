namespace Spot.Api.DTOs;

public record MerchantAnalyticsDto(
    Guid MerchantId,
    string MerchantName,
    int GeofenceRadiusMeters,
    int TotalFootTrafficToday,
    int TotalTriggersThisWeek,
    double ConversionRatePercent,
    int ActiveShortsCount,
    double AverageRating,
    int TotalReviews,
    List<HourlyTrafficDto> HourlyFootTraffic,
    List<CampaignMetricDto> RecentCampaigns
);

public record HourlyTrafficDto(int Hour, int VisitCount);

public record CampaignMetricDto(
    string CampaignName,
    string PromoBadge,
    int ViewsCount,
    int ClaimsCount,
    DateTimeOffset CreatedAt
);

public record UpdateMerchantGeofenceRequest(
    double Latitude,
    double Longitude,
    int RadiusMeters
);
