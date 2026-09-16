using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Spot.Api.Controllers;
using Spot.Api.DTOs;
using Spot.Infrastructure.Persistence;
using Spot.Infrastructure.Repositories;
using Spot.Infrastructure.Services;
using Xunit;

namespace Spot.Tests;

public class ReviewFraudDetectionTests
{
    private static SpotDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<SpotDbContext>()
            .UseInMemoryDatabase(databaseName: $"SpotFraudTest_{Guid.NewGuid()}")
            .Options;

        var context = new SpotDbContext(options);
        DbSeeder.SeedAsync(context).GetAwaiter().GetResult();
        return context;
    }

    private static ReviewFraudDetector CreateDetector()
    {
        var config = new ConfigurationBuilder().Build();
        return new ReviewFraudDetector(config, NullLogger<ReviewFraudDetector>.Instance);
    }

    [Fact]
    public async Task ScreenReviewAsync_WithPromotionalLink_FlagsAsSpam()
    {
        // Arrange
        var detector = CreateDetector();
        var request = new ReviewScreeningRequest(
            StoreId: Guid.NewGuid(),
            UserId: Guid.NewGuid(),
            RatingService: 5,
            RatingQuality: 5,
            RatingAtmosphere: 5,
            RatingOverall: 5,
            Comment: "Best discounts available at https://bit.ly/cheap-deals check now!",
            IpAddress: "192.168.1.10"
        );

        // Act
        var result = await detector.ScreenReviewAsync(request);

        // Assert
        result.IsFlaggedAsSpam.Should().BeTrue();
        result.SpamConfidence.Should().BeGreaterThanOrEqualTo(0.9);
        result.SpamReason.Should().Contain("link");
    }

    [Fact]
    public async Task ScreenReviewAsync_WithCryptoScamKeyword_FlagsAsSpam()
    {
        // Arrange
        var detector = CreateDetector();
        var request = new ReviewScreeningRequest(
            StoreId: Guid.NewGuid(),
            UserId: Guid.NewGuid(),
            RatingService: 5,
            RatingQuality: 5,
            RatingAtmosphere: 5,
            RatingOverall: 5,
            Comment: "Earn passive income and bitcoin with free bonus now",
            IpAddress: "192.168.1.11"
        );

        // Act
        var result = await detector.ScreenReviewAsync(request);

        // Assert
        result.IsFlaggedAsSpam.Should().BeTrue();
        result.SpamReason.Should().Contain("keyword");
    }

    [Fact]
    public async Task ScreenReviewAsync_WithHighVelocity_FlagsRateLimitViolation()
    {
        // Arrange
        var detector = CreateDetector();
        var ip = "203.0.113.50";
        var userId = Guid.NewGuid();

        // Submit 5 reviews (allowed)
        for (int i = 0; i < 5; i++)
        {
            var allowedReq = new ReviewScreeningRequest(
                StoreId: Guid.NewGuid(),
                UserId: userId,
                RatingService: 4,
                RatingQuality: 4,
                RatingAtmosphere: 4,
                RatingOverall: 4,
                Comment: $"Normal review number {i}",
                IpAddress: ip
            );
            var res = await detector.ScreenReviewAsync(allowedReq);
            res.VelocityExceeded.Should().BeFalse();
        }

        // Act - 6th review within same minute
        var spamReq = new ReviewScreeningRequest(
            StoreId: Guid.NewGuid(),
            UserId: userId,
            RatingService: 4,
            RatingQuality: 4,
            RatingAtmosphere: 4,
            RatingOverall: 4,
            Comment: "Rapid spam burst",
            IpAddress: ip
        );
        var spamRes = await detector.ScreenReviewAsync(spamReq);

        // Assert
        spamRes.VelocityExceeded.Should().BeTrue();
        spamRes.IsFlaggedAsSpam.Should().BeTrue();
        spamRes.SpamReason.Should().Contain("Velocity violation");
    }

    [Fact]
    public async Task ScreenReviewAsync_ExtractsMultiDimensionalSentimentAccurately()
    {
        // Arrange
        var detector = CreateDetector();
        
        // Positive review
        var positiveReq = new ReviewScreeningRequest(
            StoreId: Guid.NewGuid(),
            UserId: Guid.NewGuid(),
            RatingService: 5,
            RatingQuality: 5,
            RatingAtmosphere: 5,
            RatingOverall: 5,
            Comment: "Super friendly barista, delicious fresh pastries, and very cozy aesthetic vibe.",
            IpAddress: "10.0.0.1"
        );

        // Negative review
        var negativeReq = new ReviewScreeningRequest(
            StoreId: Guid.NewGuid(),
            UserId: Guid.NewGuid(),
            RatingService: 1,
            RatingQuality: 1,
            RatingAtmosphere: 1,
            RatingOverall: 1,
            Comment: "Rude staff ignored us, the coffee was stale and disgusting, and the room was loud and dirty.",
            IpAddress: "10.0.0.2"
        );

        // Act
        var positiveRes = await detector.ScreenReviewAsync(positiveReq);
        var negativeRes = await detector.ScreenReviewAsync(negativeReq);

        // Assert
        positiveRes.SentimentService.Should().Be("Positive");
        positiveRes.SentimentQuality.Should().Be("Positive");
        positiveRes.SentimentAtmosphere.Should().Be("Positive");

        negativeRes.SentimentService.Should().Be("Negative");
        negativeRes.SentimentQuality.Should().Be("Negative");
        negativeRes.SentimentAtmosphere.Should().Be("Negative");
    }

    [Fact]
    public async Task StoreReviewsController_WhenSpamSubmitted_SavesWithFlagAndExcludesFromPublicView()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var reviewRepo = new StoreReviewRepository(context);
        var storeRepo = new StoreRepository(context);
        var detector = CreateDetector();
        var controller = new StoreReviewsController(reviewRepo, storeRepo, detector);

        var spamRequest = new CreateStoreReviewRequest(
            UserId: Guid.NewGuid(),
            RatingService: 5,
            RatingQuality: 5,
            RatingOverall: 5,
            Comment: "Click here https://t.me/free_crypto for huge bonus!",
            RatingAtmosphere: 5
        );

        // Act - Post spam review
        var postResult = await controller.CreateReview(DbSeeder.StoreCoffeeId, spamRequest);
        var createdResult = postResult.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var createdReview = createdResult.Value.Should().BeOfType<StoreReviewDto>().Subject;

        // Assert spam flag is saved
        createdReview.IsFlaggedAsSpam.Should().BeTrue();
        createdReview.SpamReason.Should().NotBeNullOrEmpty();

        // Query public reviews - flagged review should NOT be visible by default
        var publicResult = await controller.GetStoreReviews(DbSeeder.StoreCoffeeId, includeFlagged: false);
        var okPublic = publicResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        var publicSummary = okPublic.Value.Should().BeOfType<StoreRatingSummaryDto>().Subject;
        publicSummary.RecentReviews.Should().NotContain(r => r.Id == createdReview.Id);

        // Query with includeFlagged: true - review is visible to merchant admin
        var adminResult = await controller.GetStoreReviews(DbSeeder.StoreCoffeeId, includeFlagged: true);
        var okAdmin = adminResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        var adminSummary = okAdmin.Value.Should().BeOfType<StoreRatingSummaryDto>().Subject;
        adminSummary.RecentReviews.Should().Contain(r => r.Id == createdReview.Id);
    }
}
