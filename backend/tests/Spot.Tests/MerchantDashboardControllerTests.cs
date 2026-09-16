using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spot.Api.Controllers;
using Spot.Api.DTOs;
using Spot.Infrastructure.Persistence;
using Spot.Infrastructure.Repositories;
using Xunit;

namespace Spot.Tests;

public class MerchantDashboardControllerTests
{
    private static SpotDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<SpotDbContext>()
            .UseInMemoryDatabase(databaseName: $"SpotMerchantTest_{Guid.NewGuid()}")
            .Options;

        var context = new SpotDbContext(options);
        DbSeeder.SeedAsync(context).GetAwaiter().GetResult();
        return context;
    }

    [Fact]
    public async Task GetAnalytics_WithValidMerchant_ReturnsDashboardMetrics()
    {
        using var context = CreateInMemoryContext();
        var storeRepo = new StoreRepository(context);
        var storyRepo = new StoreStoryRepository(context);
        var controller = new MerchantDashboardController(context, storeRepo, storyRepo);

        var response = await controller.GetAnalytics(DbSeeder.StoreCoffeeId);
        var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;
        var analytics = okResult.Value.Should().BeOfType<MerchantAnalyticsDto>().Subject;

        analytics.MerchantId.Should().Be(DbSeeder.StoreCoffeeId);
        analytics.MerchantName.Should().Be("Cortado Artisan Coffee Roasters");
        analytics.GeofenceRadiusMeters.Should().Be(100);
        analytics.TotalFootTrafficToday.Should().BeGreaterThan(0);
        analytics.HourlyFootTraffic.Should().NotBeEmpty();
    }

    [Fact]
    public async Task UpdateGeofence_WithValidRadius_UpdatesStoreLocationAndRadius()
    {
        using var context = CreateInMemoryContext();
        var storeRepo = new StoreRepository(context);
        var storyRepo = new StoreStoryRepository(context);
        var controller = new MerchantDashboardController(context, storeRepo, storyRepo);

        var request = new UpdateMerchantGeofenceRequest(
            Latitude: 40.7185,
            Longitude: -74.0025,
            RadiusMeters: 300
        );

        var response = await controller.UpdateGeofence(DbSeeder.StoreCoffeeId, request);
        response.Should().BeOfType<OkObjectResult>();

        var updatedStore = await context.Stores.FindAsync(DbSeeder.StoreCoffeeId);
        updatedStore.Should().NotBeNull();
        updatedStore!.RadiusMeters.Should().Be(300);
        updatedStore.Location.Y.Should().Be(40.7185);
        updatedStore.Location.X.Should().Be(-74.0025);
    }

    [Fact]
    public async Task UpdateGeofence_WithInvalidRadius_ReturnsBadRequest()
    {
        using var context = CreateInMemoryContext();
        var storeRepo = new StoreRepository(context);
        var storyRepo = new StoreStoryRepository(context);
        var controller = new MerchantDashboardController(context, storeRepo, storyRepo);

        var request = new UpdateMerchantGeofenceRequest(
            Latitude: 40.7185,
            Longitude: -74.0025,
            RadiusMeters: 10 // Invalid < 25m
        );

        var response = await controller.UpdateGeofence(DbSeeder.StoreCoffeeId, request);
        response.Should().BeOfType<BadRequestObjectResult>();
    }
}
