using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spot.Api.Controllers;
using Spot.Api.DTOs;
using Spot.Infrastructure.Persistence;
using Spot.Infrastructure.Repositories;
using Xunit;

namespace Spot.Tests;

public class StoreReviewsControllerTests
{
    private static SpotDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<SpotDbContext>()
            .UseInMemoryDatabase(databaseName: $"SpotReviewsTest_{Guid.NewGuid()}")
            .Options;

        var context = new SpotDbContext(options);
        DbSeeder.SeedAsync(context).GetAwaiter().GetResult();
        return context;
    }

    [Fact]
    public async Task GetStoreReviews_ReturnsSummaryAndIndividualMetrics()
    {
        using var context = CreateInMemoryContext();
        var reviewRepo = new StoreReviewRepository(context);
        var storeRepo = new StoreRepository(context);
        var controller = new StoreReviewsController(reviewRepo, storeRepo);

        var response = await controller.GetStoreReviews(DbSeeder.StoreGroceryId);
        var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;
        var summary = okResult.Value.Should().BeOfType<StoreRatingSummaryDto>().Subject;

        summary.TotalReviews.Should().Be(2);
        summary.AverageOverall.Should().Be(5.0);
        summary.AverageQuality.Should().Be(5.0);
        summary.AverageService.Should().Be(4.5);
    }

    [Fact]
    public async Task CreateReview_WithValidRatings_AddsReviewSuccessfully()
    {
        using var context = CreateInMemoryContext();
        var reviewRepo = new StoreReviewRepository(context);
        var storeRepo = new StoreRepository(context);
        var controller = new StoreReviewsController(reviewRepo, storeRepo);

        var request = new CreateStoreReviewRequest(
            UserId: Guid.NewGuid(),
            RatingService: 5,
            RatingQuality: 4,
            RatingOverall: 5,
            Comment: "Barista made a phenomenal pour-over."
        );

        var response = await controller.CreateReview(DbSeeder.StoreCoffeeId, request);
        var createdResult = response.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var created = createdResult.Value.Should().BeOfType<StoreReviewDto>().Subject;

        created.StoreId.Should().Be(DbSeeder.StoreCoffeeId);
        created.RatingOverall.Should().Be(5);
        created.Comment.Should().Be("Barista made a phenomenal pour-over.");
    }

    [Fact]
    public async Task CreateReview_WithInvalidRating_ReturnsBadRequest()
    {
        using var context = CreateInMemoryContext();
        var reviewRepo = new StoreReviewRepository(context);
        var storeRepo = new StoreRepository(context);
        var controller = new StoreReviewsController(reviewRepo, storeRepo);

        var request = new CreateStoreReviewRequest(
            UserId: Guid.NewGuid(),
            RatingService: 6, // Invalid > 5
            RatingQuality: 4,
            RatingOverall: 5,
            Comment: "Too high rating"
        );

        var response = await controller.CreateReview(DbSeeder.StoreCoffeeId, request);
        response.Result.Should().BeOfType<BadRequestObjectResult>();
    }
}
