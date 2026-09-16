using Microsoft.AspNetCore.Mvc;
using Spot.Api.DTOs;
using Spot.Domain.Entities;
using Spot.Infrastructure.Repositories;
using Spot.Infrastructure.Services;

namespace Spot.Api.Controllers;

[ApiController]
[Route("api/v1/stores/{storeId:guid}/reviews")]
public class StoreReviewsController : ControllerBase
{
    private readonly IStoreReviewRepository _reviewRepository;
    private readonly IStoreRepository _storeRepository;
    private readonly IReviewFraudDetector? _fraudDetector;

    public StoreReviewsController(
        IStoreReviewRepository reviewRepository, 
        IStoreRepository storeRepository,
        IReviewFraudDetector? fraudDetector = null)
    {
        _reviewRepository = reviewRepository;
        _storeRepository = storeRepository;
        _fraudDetector = fraudDetector;
    }

    [HttpGet]
    [ProducesResponseType(typeof(StoreRatingSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StoreRatingSummaryDto>> GetStoreReviews(
        Guid storeId, 
        [FromQuery] bool includeFlagged = false,
        CancellationToken ct = default)
    {
        var store = await _storeRepository.GetByIdAsync(storeId, ct);
        if (store == null) return NotFound($"Store with ID {storeId} not found.");

        var allReviews = await _reviewRepository.GetByStoreIdAsync(storeId, ct);
        var filteredReviews = includeFlagged ? allReviews : allReviews.Where(r => !r.IsFlaggedAsSpam).ToList();

        var reviewDtos = filteredReviews.Select(r => new StoreReviewDto(
            r.Id, 
            r.StoreId, 
            r.UserId, 
            r.RatingService, 
            r.RatingQuality, 
            r.RatingOverall, 
            r.Comment, 
            r.CreatedAt,
            r.RatingAtmosphere,
            r.IsFlaggedAsSpam,
            r.SpamConfidence,
            r.SpamReason,
            r.SentimentService,
            r.SentimentQuality,
            r.SentimentAtmosphere
        )).ToList();

        double avgOverall = filteredReviews.Count != 0 ? Math.Round(filteredReviews.Average(r => r.RatingOverall), 1) : 0.0;
        double avgService = filteredReviews.Count != 0 ? Math.Round(filteredReviews.Average(r => r.RatingService), 1) : 0.0;
        double avgQuality = filteredReviews.Count != 0 ? Math.Round(filteredReviews.Average(r => r.RatingQuality), 1) : 0.0;
        double avgAtmosphere = filteredReviews.Count != 0 ? Math.Round(filteredReviews.Average(r => r.RatingAtmosphere), 1) : 5.0;

        int positiveCount = filteredReviews.Count(r => r.SentimentService == "Positive" || r.SentimentQuality == "Positive");
        int negativeCount = filteredReviews.Count(r => r.SentimentService == "Negative" || r.SentimentQuality == "Negative");
        int neutralCount = Math.Max(0, filteredReviews.Count - (positiveCount + negativeCount));

        var sentimentSummary = new SentimentBreakdownDto(positiveCount, neutralCount, negativeCount);

        return Ok(new StoreRatingSummaryDto(
            StoreId: storeId,
            AverageOverall: avgOverall,
            AverageService: avgService,
            AverageQuality: avgQuality,
            TotalReviews: filteredReviews.Count,
            RecentReviews: reviewDtos,
            AverageAtmosphere: avgAtmosphere,
            SentimentBreakdown: sentimentSummary
        ));
    }

    [HttpPost]
    [ProducesResponseType(typeof(StoreReviewDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<StoreReviewDto>> CreateReview(
        Guid storeId, 
        [FromBody] CreateStoreReviewRequest request, 
        CancellationToken ct = default)
    {
        var store = await _storeRepository.GetByIdAsync(storeId, ct);
        if (store == null) return NotFound($"Store with ID {storeId} not found.");

        if (request.RatingService < 1 || request.RatingService > 5 ||
            request.RatingQuality < 1 || request.RatingQuality > 5 ||
            request.RatingAtmosphere < 1 || request.RatingAtmosphere > 5 ||
            request.RatingOverall < 1 || request.RatingOverall > 5)
        {
            return BadRequest("All ratings (Service, Quality, Atmosphere, Overall) must be between 1 and 5.");
        }

        var clientIp = HttpContext?.Connection?.RemoteIpAddress?.ToString();

        // Run AI Fraud Detection & Sentiment Screening
        bool isFlagged = false;
        double spamConfidence = 0.0;
        string? spamReason = null;
        string sentimentService = "Neutral";
        string sentimentQuality = "Neutral";
        string sentimentAtmosphere = "Neutral";

        if (_fraudDetector != null)
        {
            var screening = await _fraudDetector.ScreenReviewAsync(new ReviewScreeningRequest(
                StoreId: storeId,
                UserId: request.UserId,
                RatingService: request.RatingService,
                RatingQuality: request.RatingQuality,
                RatingAtmosphere: request.RatingAtmosphere,
                RatingOverall: request.RatingOverall,
                Comment: request.Comment,
                IpAddress: clientIp
            ), ct);

            if (screening.VelocityExceeded)
            {
                return StatusCode(StatusCodes.Status429TooManyRequests, new 
                { 
                    error = "Rate limit / velocity violation", 
                    reason = screening.SpamReason 
                });
            }

            isFlagged = screening.IsFlaggedAsSpam;
            spamConfidence = screening.SpamConfidence;
            spamReason = screening.SpamReason;
            sentimentService = screening.SentimentService;
            sentimentQuality = screening.SentimentQuality;
            sentimentAtmosphere = screening.SentimentAtmosphere;
        }

        var review = new StoreReview
        {
            StoreId = storeId,
            UserId = request.UserId,
            RatingService = request.RatingService,
            RatingQuality = request.RatingQuality,
            RatingAtmosphere = request.RatingAtmosphere,
            RatingOverall = request.RatingOverall,
            Comment = request.Comment?.Trim() ?? string.Empty,
            CreatedAt = DateTimeOffset.UtcNow,
            IsFlaggedAsSpam = isFlagged,
            SpamConfidence = spamConfidence,
            SpamReason = spamReason,
            SentimentService = sentimentService,
            SentimentQuality = sentimentQuality,
            SentimentAtmosphere = sentimentAtmosphere,
            AuthorIpAddress = clientIp
        };

        var created = await _reviewRepository.AddAsync(review, ct);

        var dto = new StoreReviewDto(
            created.Id, 
            created.StoreId, 
            created.UserId,
            created.RatingService, 
            created.RatingQuality, 
            created.RatingOverall,
            created.Comment, 
            created.CreatedAt,
            created.RatingAtmosphere,
            created.IsFlaggedAsSpam,
            created.SpamConfidence,
            created.SpamReason,
            created.SentimentService,
            created.SentimentQuality,
            created.SentimentAtmosphere
        );

        return CreatedAtAction(nameof(GetStoreReviews), new { storeId }, dto);
    }
}
