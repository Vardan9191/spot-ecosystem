using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spot.Api.Controllers;
using Spot.Api.DTOs;
using Spot.Infrastructure.Persistence;
using Spot.Infrastructure.Repositories;
using Spot.Infrastructure.Storage;
using Xunit;

namespace Spot.Tests;

public class StoreStoriesControllerTests
{
    private static SpotDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<SpotDbContext>()
            .UseInMemoryDatabase(databaseName: $"SpotStoriesTest_{Guid.NewGuid()}")
            .Options;

        var context = new SpotDbContext(options);
        DbSeeder.SeedAsync(context).GetAwaiter().GetResult();
        return context;
    }

    [Fact]
    public async Task GetNearbyStories_Within1500m_ReturnsActiveStoriesAndExcludesExpiredAndFarStories()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var storyRepo = new StoreStoryRepository(context);
        var storeRepo = new StoreRepository(context);
        var storage = new LocalStorageService(Path.Combine(Path.GetTempPath(), $"spot_test_{Guid.NewGuid()}"));
        var controller = new StoreStoriesController(storyRepo, storeRepo, storage);

        // Center: NYC Civic Center
        double centerLat = 40.7128;
        double centerLon = -74.0060;

        // Act
        var response = await controller.GetNearbyStories(centerLat, centerLon, radiusMeters: 1500);

        // Assert
        var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;
        var stories = okResult.Value.Should().BeOfType<List<StoryDto>>().Subject;

        // Exactly 2 active stories inside 1.5km
        stories.Should().HaveCount(2);

        // Contains active stories from Cortado and Green Grocer
        stories.Should().Contain(s => s.StoreName == "Green Grocer Organic Market");
        stories.Should().Contain(s => s.StoreName == "Cortado Artisan Coffee Roasters");

        // Strictly excludes expired story
        stories.Should().NotContain(s => s.Title == "Yesterday's Flash Morning Deal");

        // Strictly excludes story from store >1.5km away (~2600m)
        stories.Should().NotContain(s => s.Title == "Sunset Pilates on the Hudson Pier 🧘");

        // Sorted by distance ascending
        stories[0].DistanceMeters!.Value.Should().BeLessThanOrEqualTo(stories[1].DistanceMeters!.Value);
    }

    [Fact]
    public async Task GetStoreStories_ForSpecificStore_ReturnsOnlyActiveStories()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var storyRepo = new StoreStoryRepository(context);
        var storeRepo = new StoreRepository(context);
        var storage = new LocalStorageService(Path.Combine(Path.GetTempPath(), $"spot_test_{Guid.NewGuid()}"));
        var controller = new StoreStoriesController(storyRepo, storeRepo, storage);

        // Act
        var response = await controller.GetStoreStories(DbSeeder.StoreCoffeeId);

        // Assert
        var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;
        var stories = okResult.Value.Should().BeOfType<List<StoryDto>>().Subject;

        stories.Should().HaveCount(1);
        stories.First().Title.Should().Be("Fresh Single-Origin Ethiopian Roast Just Dropped! ☕");
        stories.First().ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task CreateStory_WithValidRequest_AddsStorySuccessfully()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var storyRepo = new StoreStoryRepository(context);
        var storeRepo = new StoreRepository(context);
        var storage = new LocalStorageService(Path.Combine(Path.GetTempPath(), $"spot_test_{Guid.NewGuid()}"));
        var controller = new StoreStoriesController(storyRepo, storeRepo, storage);

        var request = new CreateStoryRequest(
            Title: "Custom Mechanical Keyboards Showcase",
            Description: "Come test novel tactile switches in person.",
            MediaUrl: "https://assets.spot.dev/media/pixel_demo.mp4",
            ThumbnailUrl: "https://assets.spot.dev/media/pixel_thumb.jpg",
            PromoBadge: "FREE KEYCAP SET",
            DurationHours: 48
        );

        // Act
        var response = await controller.CreateStory(DbSeeder.StoreTechId, request);

        // Assert
        var createdResult = response.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var created = createdResult.Value.Should().BeOfType<StoryDto>().Subject;

        created.StoreId.Should().Be(DbSeeder.StoreTechId);
        created.Title.Should().Be("Custom Mechanical Keyboards Showcase");
        created.PromoBadge.Should().Be("FREE KEYCAP SET");
        created.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow.AddHours(47));
    }

    [Fact]
    public async Task UploadStoryMedia_WithValidFormFile_SavesMediaAndCreatesStory()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var storyRepo = new StoreStoryRepository(context);
        var storeRepo = new StoreRepository(context);
        string tempFolder = Path.Combine(Path.GetTempPath(), $"spot_upload_test_{Guid.NewGuid()}");
        var storage = new LocalStorageService(tempFolder);
        var controller = new StoreStoriesController(storyRepo, storeRepo, storage);

        byte[] fakeVideoBytes = Encoding.UTF8.GetBytes("fake-mp4-video-stream-content");
        using var stream = new MemoryStream(fakeVideoBytes);
        var formFile = new FormFile(stream, 0, fakeVideoBytes.Length, "file", "story_clip.mp4")
        {
            Headers = new HeaderDictionary(),
            ContentType = "video/mp4"
        };

        // Act
        var response = await controller.UploadStoryMedia(
            DbSeeder.StoreCoffeeId,
            file: formFile,
            title: "Cold Brew Nitro Tap Live! 🍺",
            description: "Fresh nitrogen cold brew on tap right now.",
            promoBadge: "BUY 1 GET 1",
            durationHours: 12
        );

        // Assert
        var createdResult = response.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var created = createdResult.Value.Should().BeOfType<StoryDto>().Subject;

        created.StoreId.Should().Be(DbSeeder.StoreCoffeeId);
        created.Title.Should().Be("Cold Brew Nitro Tap Live! 🍺");
        created.MediaUrl.Should().StartWith("/media/").And.EndWith(".mp4");

        // Verify physical file was written
        string writtenFileName = Path.GetFileName(created.MediaUrl);
        File.Exists(Path.Combine(tempFolder, writtenFileName)).Should().BeTrue();

        // Cleanup
        if (Directory.Exists(tempFolder)) Directory.Delete(tempFolder, true);
    }

    [Fact]
    public async Task IncrementView_IncreasesViewCount()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var storyRepo = new StoreStoryRepository(context);
        var storeRepo = new StoreRepository(context);
        var storage = new LocalStorageService(Path.Combine(Path.GetTempPath(), $"spot_test_{Guid.NewGuid()}"));
        var controller = new StoreStoriesController(storyRepo, storeRepo, storage);

        var storyId = Guid.Parse("12121212-1212-1212-1212-121212121212");

        // Act
        var response = await controller.IncrementView(storyId);

        // Assert
        response.Should().BeOfType<NoContentResult>();

        var updatedStory = await storyRepo.GetByIdAsync(storyId);
        updatedStory.Should().NotBeNull();
        updatedStory!.ViewCount.Should().Be(1);
    }
}
