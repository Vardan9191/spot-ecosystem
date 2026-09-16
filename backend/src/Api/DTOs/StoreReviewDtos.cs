namespace Spot.Api.DTOs;

public record StoreReviewDto(
    Guid Id,
    Guid StoreId,
    Guid UserId,
    int RatingService,
    int RatingQuality,
    int RatingOverall,
    string Comment,
    DateTimeOffset CreatedAt,
    int RatingAtmosphere = 5,
    bool IsFlaggedAsSpam = false,
    double SpamConfidence = 0.0,
    string? SpamReason = null,
    string SentimentService = "Neutral",
    string SentimentQuality = "Neutral",
    string SentimentAtmosphere = "Neutral"
);

public record CreateStoreReviewRequest(
    Guid UserId,
    int RatingService,
    int RatingQuality,
    int RatingOverall,
    string Comment,
    int RatingAtmosphere = 5
);

public record SentimentBreakdownDto(
    int Positive,
    int Neutral,
    int Negative
);

public record StoreRatingSummaryDto(
    Guid StoreId,
    double AverageOverall,
    double AverageService,
    double AverageQuality,
    int TotalReviews,
    List<StoreReviewDto> RecentReviews,
    double AverageAtmosphere = 5.0,
    SentimentBreakdownDto? SentimentBreakdown = null
);
