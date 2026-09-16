namespace Spot.Infrastructure.Services;

public record ReviewScreeningRequest(
    Guid StoreId,
    Guid UserId,
    int RatingService,
    int RatingQuality,
    int RatingAtmosphere,
    int RatingOverall,
    string Comment,
    string? IpAddress = null
);

public record ReviewScreeningResult(
    bool IsFlaggedAsSpam,
    double SpamConfidence,
    string? SpamReason,
    string SentimentService,
    string SentimentQuality,
    string SentimentAtmosphere,
    bool VelocityExceeded = false
);

public interface IReviewFraudDetector
{
    Task<ReviewScreeningResult> ScreenReviewAsync(ReviewScreeningRequest request, CancellationToken ct = default);
    bool CheckAndRecordVelocity(string? ipAddress, Guid userId, out string? reason);
    void ResetVelocityTracking();
}
