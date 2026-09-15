using System.Threading.Channels;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Spot.Api.DTOs;
using Spot.Infrastructure.Persistence;
using Xunit;

namespace Spot.Tests;

public class LocationHubTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public LocationHubTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                // Ensure fresh seeded in-memory database
                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<SpotDbContext>();
                db.Database.EnsureCreated();
                DbSeeder.SeedAsync(db).GetAwaiter().GetResult();
            });
        });
    }

    private HubConnection CreateHubConnection()
    {
        return new HubConnectionBuilder()
            .WithUrl("http://localhost/hubs/location", options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            })
            .Build();
    }

    [Fact]
    public async Task SendLocation_NearCivicCenter_StreamsNearbyStoresWithin1500m()
    {
        // Arrange
        await using var connection = CreateHubConnection();
        var tcs = new TaskCompletionSource<List<StoreDto>>();

        connection.On<List<StoreDto>>("ReceiveNearbyStores", stores =>
        {
            tcs.TrySetResult(stores);
        });

        await connection.StartAsync();

        // Center: NYC Civic Center
        double centerLat = 40.7128;
        double centerLon = -74.0060;

        // Act
        await connection.InvokeAsync("SendLocation", centerLat, centerLon, 1500.0, null);

        var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(5000));
        completedTask.Should().Be(tcs.Task, "Hub should respond with ReceiveNearbyStores within 5 seconds.");

        var nearbyStores = await tcs.Task;

        // Assert
        nearbyStores.Should().NotBeNull();
        nearbyStores.Count.Should().Be(3);

        nearbyStores.Should().Contain(s => s.Name == "Green Grocer Organic Market");
        nearbyStores.Should().Contain(s => s.Name == "Cortado Artisan Coffee Roasters");
        nearbyStores.Should().Contain(s => s.Name == "Pixel & Wire Electronics");

        // Hudson River Fitness & Spa is ~2600m away, so it must be excluded
        nearbyStores.Should().NotContain(s => s.Name == "Hudson River Fitness & Spa");

        // Distance validation
        foreach (var s in nearbyStores)
        {
            s.DistanceMeters.Should().NotBeNull();
            s.DistanceMeters!.Value.Should().BeLessThanOrEqualTo(1500);
        }

        // Ordered by proximity ascending
        for (int i = 0; i < nearbyStores.Count - 1; i++)
        {
            nearbyStores[i].DistanceMeters!.Value.Should().BeLessThanOrEqualTo(nearbyStores[i + 1].DistanceMeters!.Value);
        }

        await connection.StopAsync();
    }

    [Fact]
    public async Task SendLocation_WithCategoryFilter_ReturnsOnlyMatchingStores()
    {
        // Arrange
        await using var connection = CreateHubConnection();
        var tcs = new TaskCompletionSource<List<StoreDto>>();

        connection.On<List<StoreDto>>("ReceiveNearbyStores", stores =>
        {
            tcs.TrySetResult(stores);
        });

        await connection.StartAsync();

        // Act
        await connection.InvokeAsync("SendLocation", 40.7128, -74.0060, 1500.0, "coffee");

        var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(5000));
        completedTask.Should().Be(tcs.Task);

        var nearbyStores = await tcs.Task;

        // Assert
        nearbyStores.Should().HaveCount(1);
        nearbyStores.First().Name.Should().Be("Cortado Artisan Coffee Roasters");
        nearbyStores.First().Category.Slug.Should().Be("coffee");

        await connection.StopAsync();
    }

    [Fact]
    public async Task SendLocation_InsideStoreRadius_TriggersGeofenceAlert()
    {
        // Arrange
        await using var connection = CreateHubConnection();
        var alertTcs = new TaskCompletionSource<GeofenceAlertNotificationDto>();

        connection.On<GeofenceAlertNotificationDto>("ReceiveGeofenceAlert", alert =>
        {
            alertTcs.TrySetResult(alert);
        });

        await connection.StartAsync();

        // Green Grocer coordinates: 40.7145, -74.0080 with 150m radius
        double atStoreLat = 40.7145;
        double atStoreLon = -74.0080;

        // Act
        await connection.InvokeAsync("SendLocation", atStoreLat, atStoreLon, 500.0, null);

        var completedTask = await Task.WhenAny(alertTcs.Task, Task.Delay(5000));
        completedTask.Should().Be(alertTcs.Task, "Hub should emit ReceiveGeofenceAlert when inside store radius.");

        var alert = await alertTcs.Task;

        // Assert
        alert.StoreName.Should().Be("Green Grocer Organic Market");
        alert.DistanceMeters.Should().BeLessThanOrEqualTo(150);
        alert.Message.Should().Contain("Green Grocer Organic Market");

        await connection.StopAsync();
    }

    [Fact]
    public async Task BroadcastStoreDeal_DeliversDealToUsersInStoreZone()
    {
        // Arrange
        await using var connection = CreateHubConnection();
        var dealTcs = new TaskCompletionSource<StoreDealDto>();

        connection.On<StoreDealDto>("ReceiveStoreDeal", deal =>
        {
            dealTcs.TrySetResult(deal);
        });

        await connection.StartAsync();

        string storeId = DbSeeder.StoreCoffeeId.ToString();

        // 1. Join store zone
        await connection.InvokeAsync("JoinStoreZone", storeId);

        // 2. Broadcast deal to this zone
        await connection.InvokeAsync("BroadcastStoreDeal", storeId, "Flash 30% Off", "Freshly roasted Ethiopian beans 30% off for 1 hour!");

        var completedTask = await Task.WhenAny(dealTcs.Task, Task.Delay(5000));
        completedTask.Should().Be(dealTcs.Task, "Client in zone should receive broadcast deal.");

        var deal = await dealTcs.Task;

        // Assert
        deal.StoreId.Should().Be(DbSeeder.StoreCoffeeId);
        deal.DealTitle.Should().Be("Flash 30% Off");
        deal.DealMessage.Should().Contain("Ethiopian beans");

        await connection.StopAsync();
    }

    [Fact]
    public async Task SendLocation_WithInvalidCoordinates_EmitsReceiveError()
    {
        // Arrange
        await using var connection = CreateHubConnection();
        var errorTcs = new TaskCompletionSource<string>();

        connection.On<string>("ReceiveError", err =>
        {
            errorTcs.TrySetResult(err);
        });

        await connection.StartAsync();

        // Act: Invalid latitude 99.0 (> 90)
        await connection.InvokeAsync("SendLocation", 99.0, 0.0, 1500.0, null);

        var completedTask = await Task.WhenAny(errorTcs.Task, Task.Delay(5000));
        completedTask.Should().Be(errorTcs.Task);

        var errorMsg = await errorTcs.Task;
        errorMsg.Should().Contain("Invalid coordinates");

        await connection.StopAsync();
    }
}
