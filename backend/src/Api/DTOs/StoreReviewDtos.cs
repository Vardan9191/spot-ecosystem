namespace Spot.Api.DTOs;

public record StoreReviewDto(
    Guid Id,
    Guid StoreId,
    Guid UserId,
    int RatingService,
    int RatingQuality,
    int RatingOverall,
    string Comment,
    DateTimeOffset CreatedAt
);

public record CreateStoreReviewRequest(
    Guid UserId,
    int RatingService,
    int RatingQuality,
    int RatingOverall,
    string Comment
);

public record StoreRatingSummaryDto(
    Guid StoreId,
    double AverageOverall,
    double AverageService,
    double AverageQuality,
    int TotalReviews,
    List<StoreReviewDto> RecentReviews
);
