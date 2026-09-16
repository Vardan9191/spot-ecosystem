using Microsoft.AspNetCore.Mvc;
using Spot.Api.DTOs;
using Spot.Domain.Entities;
using Spot.Infrastructure.Repositories;
using Spot.Infrastructure.Storage;

namespace Spot.Api.Controllers;

[ApiController]
[Route("api/v1")]
public class StoreStoriesController : ControllerBase
{
    private readonly IStoreStoryRepository _storyRepository;
    private readonly IStoreRepository _storeRepository;
    private readonly IMediaStorageService _storageService;

    public StoreStoriesController(
        IStoreStoryRepository storyRepository,
        IStoreRepository storeRepository,
        IMediaStorageService storageService)
    {
        _storyRepository = storyRepository;
        _storeRepository = storeRepository;
        _storageService = storageService;
    }

    /// <summary>
    /// Hyper-local merchant stories discovery: streams shorts/stories from nearby stores within proximity radius.
    /// </summary>
    [HttpGet("stories/nearby")]
    [ProducesResponseType(typeof(List<StoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<StoryDto>>> GetNearbyStories(
        [FromQuery] double latitude,
        [FromQuery] double longitude,
        [FromQuery] double radiusMeters = 1500,
        CancellationToken ct = default)
    {
        if (latitude < -90 || latitude > 90 || longitude < -180 || longitude > 180)
        {
            return BadRequest("Invalid coordinates: latitude [-90, 90], longitude [-180, 180].");
        }

        if (radiusMeters <= 0 || radiusMeters > 100000)
        {
            return BadRequest("Radius must be between 1 and 100,000 meters.");
        }

        var results = await _storyRepository.GetNearbyActiveStoriesAsync(latitude, longitude, radiusMeters, ct);
        var dtos = results.Select(r => MapToDto(r.Story, r.DistanceMeters)).ToList();
        return Ok(dtos);
    }

    /// <summary>
    /// Retrieves all active stories for a specific merchant store.
    /// </summary>
    [HttpGet("stores/{storeId:guid}/stories")]
    [ProducesResponseType(typeof(List<StoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<StoryDto>>> GetStoreStories(Guid storeId, CancellationToken ct = default)
    {
        var stories = await _storyRepository.GetActiveByStoreIdAsync(storeId, ct);
        return Ok(stories.Select(s => MapToDto(s, null)).ToList());
    }

    /// <summary>
    /// Publishes a merchant story/short with 24-hour flash deal duration.
    /// </summary>
    [HttpPost("stores/{storeId:guid}/stories")]
    [ProducesResponseType(typeof(StoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StoryDto>> CreateStory(
        Guid storeId,
        [FromBody] CreateStoryRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.MediaUrl))
        {
            return BadRequest("Title and MediaUrl are required.");
        }

        var store = await _storeRepository.GetByIdAsync(storeId, ct);
        if (store == null)
        {
            return NotFound($"Store with ID {storeId} does not exist.");
        }

        int hours = request.DurationHours > 0 && request.DurationHours <= 168 ? request.DurationHours : 24;

        var story = new StoreStory
        {
            StoreId = storeId,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim() ?? "",
            MediaUrl = request.MediaUrl.Trim(),
            ThumbnailUrl = request.ThumbnailUrl?.Trim(),
            PromoBadge = request.PromoBadge?.Trim() ?? "",
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(hours)
        };

        await _storyRepository.AddAsync(story, ct);

        // Populate store for DTO mapping
        story.Store = store;

        return CreatedAtAction(
            nameof(GetStoreStories),
            new { storeId = story.StoreId },
            MapToDto(story, null)
        );
    }

    /// <summary>
    /// Uploads merchant media file (video/image) and publishes a new story.
    /// </summary>
    [HttpPost("stores/{storeId:guid}/stories/upload")]
    [ProducesResponseType(typeof(StoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StoryDto>> UploadStoryMedia(
        Guid storeId,
        [FromForm] IFormFile file,
        [FromForm] string title,
        [FromForm] string? description,
        [FromForm] string? promoBadge,
        [FromForm] int? durationHours,
        CancellationToken ct = default)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("Media file is required.");
        }

        var store = await _storeRepository.GetByIdAsync(storeId, ct);
        if (store == null)
        {
            return NotFound($"Store with ID {storeId} does not exist.");
        }

        using var stream = file.OpenReadStream();
        string mediaUrl = await _storageService.UploadMediaAsync(stream, file.FileName, file.ContentType, ct);

        int hours = durationHours.HasValue && durationHours.Value > 0 && durationHours.Value <= 168
            ? durationHours.Value
            : 24;

        var story = new StoreStory
        {
            StoreId = storeId,
            Title = title?.Trim() ?? file.FileName,
            Description = description?.Trim() ?? "",
            MediaUrl = mediaUrl,
            PromoBadge = promoBadge?.Trim() ?? "",
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(hours)
        };

        await _storyRepository.AddAsync(story, ct);
        story.Store = store;

        return CreatedAtAction(
            nameof(GetStoreStories),
            new { storeId = story.StoreId },
            MapToDto(story, null)
        );
    }

    /// <summary>
    /// Increments story view counter for merchant analytics.
    /// </summary>
    [HttpPost("stories/{storyId:guid}/view")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> IncrementView(Guid storyId, CancellationToken ct = default)
    {
        await _storyRepository.IncrementViewCountAsync(storyId, ct);
        return NoContent();
    }

    private static StoryDto MapToDto(StoreStory story, double? distance)
    {
        return new StoryDto(
            Id: story.Id,
            StoreId: story.StoreId,
            StoreName: story.Store?.Name ?? "Merchant",
            Category: story.Store?.Category?.Name ?? "General",
            IsPartner: story.Store?.IsPartner ?? false,
            Title: story.Title,
            Description: story.Description,
            MediaUrl: story.MediaUrl,
            ThumbnailUrl: story.ThumbnailUrl,
            PromoBadge: story.PromoBadge,
            DistanceMeters: distance,
            ViewCount: story.ViewCount,
            CreatedAt: story.CreatedAt,
            ExpiresAt: story.ExpiresAt
        );
    }
}
