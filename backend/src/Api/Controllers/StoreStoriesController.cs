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
    /// Uploads a merchant video short (15-30s vertical video) linked to a store geofence.
    /// Supports multipart video upload with size and format validation.
    /// </summary>
    [HttpPost("merchants/{merchantId:guid}/shorts")]
    [ProducesResponseType(typeof(StoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StoryDto>> UploadMerchantShort(
        Guid merchantId,
        [FromForm] IFormFile videoFile,
        [FromForm] string title,
        [FromForm] string? description,
        [FromForm] string? promoBadge,
        [FromForm] int? videoDurationSeconds,
        [FromForm] string? aspectRatio = "9:16",
        CancellationToken ct = default)
    {
        if (videoFile == null || videoFile.Length == 0)
        {
            return BadRequest("Video file is required.");
        }

        const long maxBytes = 50 * 1024 * 1024; // 50MB
        if (videoFile.Length > maxBytes)
        {
            return BadRequest("Video file exceeds the maximum allowed size of 50MB.");
        }

        var ext = Path.GetExtension(videoFile.FileName).ToLowerInvariant();
        var allowedExts = new[] { ".mp4", ".mov", ".webm", ".m4v" };
        if (!allowedExts.Contains(ext))
        {
            return BadRequest($"Invalid video format '{ext}'. Allowed formats: .mp4, .mov, .webm, .m4v.");
        }

        if (videoDurationSeconds.HasValue && (videoDurationSeconds.Value < 5 || videoDurationSeconds.Value > 60))
        {
            return BadRequest("Video shorts duration must be between 5 and 60 seconds (ideal: 15-30s).");
        }

        var store = await _storeRepository.GetByIdAsync(merchantId, ct);
        if (store == null)
        {
            return NotFound($"Merchant store with ID {merchantId} does not exist.");
        }

        using var stream = videoFile.OpenReadStream();
        string mediaUrl = await _storageService.UploadMediaAsync(stream, videoFile.FileName, videoFile.ContentType, ct);

        var story = new StoreStory
        {
            StoreId = merchantId,
            Title = string.IsNullOrWhiteSpace(title) ? Path.GetFileNameWithoutExtension(videoFile.FileName) : title.Trim(),
            Description = description?.Trim() ?? "",
            MediaUrl = mediaUrl,
            PromoBadge = promoBadge?.Trim() ?? "HOT SHORT",
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(24)
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
    /// Generates S3/MinIO compatible presigned upload URL for direct client-to-cloud video ingestion.
    /// </summary>
    [HttpPost("merchants/{merchantId:guid}/shorts/presigned-url")]
    [ProducesResponseType(typeof(PresignedUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PresignedUploadResponse>> GeneratePresignedUploadUrl(
        Guid merchantId,
        [FromBody] PresignedUploadRequest request,
        CancellationToken ct = default)
    {
        var store = await _storeRepository.GetByIdAsync(merchantId, ct);
        if (store == null)
        {
            return NotFound($"Merchant store with ID {merchantId} does not exist.");
        }

        if (string.IsNullOrWhiteSpace(request.FileName))
        {
            return BadRequest("FileName is required.");
        }

        var ext = Path.GetExtension(request.FileName).ToLowerInvariant();
        var allowedExts = new[] { ".mp4", ".mov", ".webm", ".m4v", ".jpg", ".png", ".webp" };
        if (!allowedExts.Contains(ext))
        {
            return BadRequest($"Invalid file extension '{ext}'. Allowed extensions: {string.Join(", ", allowedExts)}");
        }

        string storageKey = $"{merchantId}/{Guid.NewGuid():N}{ext}";
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(15);
        string uploadUrl = $"https://s3.spot.local/merchants-shorts/{storageKey}?X-Amz-Expires=900";
        string finalMediaUrl = $"/media/shorts/{storageKey}";

        var headers = new Dictionary<string, string>
        {
            { "Content-Type", request.ContentType },
            { "x-amz-acl", "public-read" }
        };

        return Ok(new PresignedUploadResponse(
            UploadUrl: uploadUrl,
            FinalMediaUrl: finalMediaUrl,
            StorageKey: storageKey,
            ExpiresAt: expiresAt,
            RequiredHeaders: headers
        ));
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
