using Microsoft.AspNetCore.Mvc;
using Spot.Api.DTOs;
using Spot.Domain.Entities;
using Spot.Infrastructure.Repositories;

namespace Spot.Api.Controllers;

[ApiController]
[Route("api/v1/stores/{storeId:guid}/reviews")]
public class StoreReviewsController : ControllerBase
{
    private readonly IStoreReviewRepository _reviewRepository;
    private readonly IStoreRepository _storeRepository;

    public StoreReviewsController(IStoreReviewRepository reviewRepository, IStoreRepository storeRepository)
    {
        _reviewRepository = reviewRepository;
        _storeRepository = storeRepository;
    }

    [HttpGet]
    [ProducesResponseType(typeof(StoreRatingSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StoreRatingSummaryDto>> GetStoreReviews(Guid storeId, CancellationToken ct = default)
    {
        var store = await _storeRepository.GetByIdAsync(storeId, ct);
        if (store == null) return NotFound($"Store with ID {storeId} not found.");

        var reviews = await _reviewRepository.GetByStoreIdAsync(storeId, ct);
        var reviewDtos = reviews.Select(r => new StoreReviewDto(
            r.Id, r.StoreId, r.UserId, r.RatingService, r.RatingQuality, r.RatingOverall, r.Comment, r.CreatedAt
        )).ToList();

        double avgOverall = reviews.Count != 0 ? Math.Round(reviews.Average(r => r.RatingOverall), 1) : 0.0;
        double avgService = reviews.Count != 0 ? Math.Round(reviews.Average(r => r.RatingService), 1) : 0.0;
        double avgQuality = reviews.Count != 0 ? Math.Round(reviews.Average(r => r.RatingQuality), 1) : 0.0;

        return Ok(new StoreRatingSummaryDto(
            StoreId: storeId,
            AverageOverall: avgOverall,
            AverageService: avgService,
            AverageQuality: avgQuality,
            TotalReviews: reviews.Count,
            RecentReviews: reviewDtos
        ));
    }

    [HttpPost]
    [ProducesResponseType(typeof(StoreReviewDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StoreReviewDto>> CreateReview(
        Guid storeId, 
        [FromBody] CreateStoreReviewRequest request, 
        CancellationToken ct = default)
    {
        var store = await _storeRepository.GetByIdAsync(storeId, ct);
        if (store == null) return NotFound($"Store with ID {storeId} not found.");

        if (request.RatingService < 1 || request.RatingService > 5 ||
            request.RatingQuality < 1 || request.RatingQuality > 5 ||
            request.RatingOverall < 1 || request.RatingOverall > 5)
        {
            return BadRequest("All ratings (Service, Quality, Overall) must be between 1 and 5.");
        }

        var review = new StoreReview
        {
            StoreId = storeId,
            UserId = request.UserId,
            RatingService = request.RatingService,
            RatingQuality = request.RatingQuality,
            RatingOverall = request.RatingOverall,
            Comment = request.Comment?.Trim() ?? string.Empty,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var created = await _reviewRepository.AddAsync(review, ct);

        var dto = new StoreReviewDto(
            created.Id, created.StoreId, created.UserId,
            created.RatingService, created.RatingQuality, created.RatingOverall,
            created.Comment, created.CreatedAt
        );

        return CreatedAtAction(nameof(GetStoreReviews), new { storeId }, dto);
    }
}
