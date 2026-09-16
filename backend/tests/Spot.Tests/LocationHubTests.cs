using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Spot.Api.DTOs;
using Spot.Api.Hubs;
using Spot.Infrastructure.Persistence;
using Spot.Infrastructure.Repositories;
using Xunit;

namespace Spot.Tests;

public class LocationHubTests
{
    private static SpotDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<SpotDbContext>()
            .UseInMemoryDatabase(databaseName: $"SpotHubTest_{Guid.NewGuid()}")
            .Options;

        var context = new SpotDbContext(options);
        DbSeeder.SeedAsync(context).GetAwaiter().GetResult();
        return context;
    }

    private class TestLocationClient : ILocationClient
    {
        public List<StoreDto>? ReceivedStores { get; private set; }
        public List<GeofenceAlertDto> ReceivedAlerts { get; } = new();
        public List<(string Title, string Message)> ReceivedBroadcasts { get; } = new();
        public (double Lat, double Lng, int Count)? AcknowledgedUpdate { get; private set; }

        public Task ReceiveNearbyStores(List<StoreDto> stores)
        {
            ReceivedStores = stores;
            return Task.CompletedTask;
        }

        public Task ReceiveGeofenceTrigger(GeofenceAlertDto alert)
        {
            ReceivedAlerts.Add(alert);
            return Task.CompletedTask;
        }

        public Task ReceiveBroadcastNotification(string title, string message)
        {
            ReceivedBroadcasts.Add((title, message));
            return Task.CompletedTask;
        }

        public Task LocationUpdatedAcknowledged(double latitude, double longitude, int storesFound)
        {
            AcknowledgedUpdate = (latitude, longitude, storesFound);
            return Task.CompletedTask;
        }
    }

    private class TestHubCallerClients : IHubCallerClients<ILocationClient>
    {
        public TestLocationClient TestClient { get; } = new();

        public ILocationClient Caller => TestClient;
        public ILocationClient Others => TestClient;
        public ILocationClient All => TestClient;
        public ILocationClient AllExcept(IReadOnlyList<string> excludedConnectionIds) => TestClient;
        public ILocationClient Client(string connectionId) => TestClient;
        public ILocationClient ClientMethod(string connectionId) => TestClient;
        public ILocationClient Clients(IReadOnlyList<string> connectionIds) => TestClient;
        public ILocationClient Group(string groupName) => TestClient;
        public ILocationClient Groups(IReadOnlyList<string> groupNames) => TestClient;
        public ILocationClient GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => TestClient;
        public ILocationClient OthersInGroup(string groupName) => TestClient;
        public ILocationClient User(string userId) => TestClient;
        public ILocationClient Users(IReadOnlyList<string> userIds) => TestClient;
    }

    [Fact]
    public async Task SendLocationUpdate_NearStoreInsideRadius_TriggersNearbyStoresAndGeofenceAlert()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var storeRepo = new StoreRepository(context);
        var hub = new LocationHub(storeRepo, NullLogger<LocationHub>.Instance);

        var clientsMock = new TestHubCallerClients();
        hub.Clients = clientsMock;

        // Position: right next to Green Grocer (Lat: 40.7145, Lng: -74.0080, Radius: 150m)
        double userLat = 40.7145;
        double userLng = -74.0080;

        // Act
        await hub.SendLocationUpdate(userLat, userLng, searchRadiusMeters: 1500);

        // Assert
        clientsMock.TestClient.ReceivedStores.Should().NotBeNull();
        clientsMock.TestClient.ReceivedStores!.Count.Should().Be(3); // 3 stores inside 1.5km

        // Should trigger geofence alert for Green Grocer (distance ~0m <= 150m radius)
        clientsMock.TestClient.ReceivedAlerts.Should().NotBeEmpty();
        var alert = clientsMock.TestClient.ReceivedAlerts.First(a => a.StoreName == "Green Grocer Organic Market");
        alert.DistanceMeters.Should().BeLessThanOrEqualTo(150);
        alert.IsPartner.Should().BeTrue();

        // Acknowledgment should match
        clientsMock.TestClient.AcknowledgedUpdate.Should().NotBeNull();
        clientsMock.TestClient.AcknowledgedUpdate!.Value.Count.Should().Be(3);
    }

    [Fact]
    public async Task SendLocationUpdate_InvalidCoordinates_DoesNotDispatchUpdates()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var storeRepo = new StoreRepository(context);
        var hub = new LocationHub(storeRepo, NullLogger<LocationHub>.Instance);

        var clientsMock = new TestHubCallerClients();
        hub.Clients = clientsMock;

        // Act with invalid latitude > 90
        await hub.SendLocationUpdate(120.0, -74.0060);

        // Assert
        clientsMock.TestClient.ReceivedStores.Should().BeNull();
        clientsMock.TestClient.ReceivedAlerts.Should().BeEmpty();
    }
}
