using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spot.Api.Controllers;
using Spot.Api.DTOs;
using Spot.Infrastructure.Persistence;
using Spot.Infrastructure.Repositories;
using Xunit;

namespace Spot.Tests;

public class StoresControllerIntegrationTests
{
    private static SpotDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<SpotDbContext>()
            .UseInMemoryDatabase(databaseName: $"SpotTest_{Guid.NewGuid()}")
            .Options;

        var context = new SpotDbContext(options);
        DbSeeder.SeedAsync(context).GetAwaiter().GetResult();
        return context;
    }

    [Fact]
    public async Task GetNearby_Within1500m_ReturnsStoresInsideAndExcludesStoreOutside()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repo = new StoreRepository(context);
        var controller = new StoresController(repo);

        // Center: NYC Civic Center
        double centerLat = 40.7128;
        double centerLon = -74.0060;
        double radiusMeters = 1500;

        // Act
        var actionResult = await controller.GetNearby(centerLat, centerLon, radiusMeters);

        // Assert
        actionResult.Result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)actionResult.Result!;
        var stores = okResult.Value as List<StoreDto>;

        stores.Should().NotBeNull();
        stores!.Count.Should().Be(3);

        // Verify stores inside 1.5km boundary are included
        stores.Should().Contain(s => s.Name == "Green Grocer Organic Market");
        stores.Should().Contain(s => s.Name == "Cortado Artisan Coffee Roasters");
        stores.Should().Contain(s => s.Name == "Pixel & Wire Electronics");

        // Verify store at ~2600m is strictly excluded!
        stores.Should().NotContain(s => s.Name == "Hudson River Fitness & Spa");

        // Verify distances are calculated and strictly <= 1500m
        foreach (var s in stores)
        {
            s.DistanceMeters.Should().NotBeNull();
            s.DistanceMeters!.Value.Should().BeLessThanOrEqualTo(1500);
        }

        // Verify ordering: closest to farthest
        for (int i = 0; i < stores.Count - 1; i++)
        {
            stores[i].DistanceMeters!.Value.Should().BeLessThanOrEqualTo(stores[i + 1].DistanceMeters!.Value);
        }
    }

    [Fact]
    public async Task GetNearby_WithCategoryFilter_ReturnsOnlyMatchingNearbyStores()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repo = new StoreRepository(context);
        var controller = new StoresController(repo);

        double centerLat = 40.7128;
        double centerLon = -74.0060;

        // Act
        var actionResult = await controller.GetNearby(centerLat, centerLon, radiusMeters: 1500, category: "coffee");

        // Assert
        var okResult = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        var stores = okResult.Value.Should().BeOfType<List<StoreDto>>().Subject;

        stores.Should().HaveCount(1);
        stores.First().Name.Should().Be("Cortado Artisan Coffee Roasters");
        stores.First().Category.Slug.Should().Be("coffee");
    }

    [Fact]
    public async Task GetNearby_WithInvalidCoordinates_ReturnsBadRequest()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repo = new StoreRepository(context);
        var controller = new StoresController(repo);

        // Act & Assert invalid latitude
        var result1 = await controller.GetNearby(latitude: 95.0, longitude: 0.0);
        result1.Result.Should().BeOfType<BadRequestObjectResult>();

        // Act & Assert invalid radius
        var result2 = await controller.GetNearby(latitude: 40.0, longitude: 0.0, radiusMeters: -10);
        result2.Result.Should().BeOfType<BadRequestObjectResult>();
    }
}
