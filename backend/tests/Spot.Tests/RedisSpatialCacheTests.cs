using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Spot.Domain.Entities;
using Spot.Infrastructure.Cache;
using Spot.Infrastructure.Persistence;
using Spot.Infrastructure.Repositories;
using Xunit;

namespace Spot.Tests;

public class FakeSpatialCacheService : ISpatialCacheService
{
    public bool IsConnected { get; set; } = true;
    public List<GeoCacheHit>? NextHits { get; set; }
    public Dictionary<Guid, (double Lat, double Lng)> StoredPoints { get; } = new();

    public Task<bool> AddOrUpdateStoreGeoAsync(Guid storeId, double latitude, double longitude, CancellationToken ct = default)
    {
        if (!IsConnected) return Task.FromResult(false);
        StoredPoints[storeId] = (latitude, longitude);
        return Task.FromResult(true);
    }

    public Task<bool> RemoveStoreGeoAsync(Guid storeId, CancellationToken ct = default)
    {
        if (!IsConnected) return Task.FromResult(false);
        return Task.FromResult(StoredPoints.Remove(storeId));
    }

    public Task<List<GeoCacheHit>?> SearchNearbyStoreIdsAsync(double latitude, double longitude, double radiusMeters, CancellationToken ct = default)
    {
        if (!IsConnected) return Task.FromResult<List<GeoCacheHit>?>(null);
        return Task.FromResult(NextHits);
    }

    public Task<int> WarmupCacheAsync(IEnumerable<Store> stores, CancellationToken ct = default)
    {
        if (!IsConnected) return Task.FromResult(0);
        int count = 0;
        foreach (var s in stores)
        {
            StoredPoints[s.Id] = (s.Location.Y, s.Location.X);
            count++;
        }
        return Task.FromResult(count);
    }
}

public class RedisSpatialCacheTests
{
    private static SpotDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<SpotDbContext>()
            .UseInMemoryDatabase(databaseName: $"SpotCacheTest_{Guid.NewGuid()}")
            .Options;

        var context = new SpotDbContext(options);
        DbSeeder.SeedAsync(context).GetAwaiter().GetResult();
        return context;
    }

    [Fact]
    public void RedisSpatialCacheService_WhenRedisNull_IsConnectedIsFalseAndDoesNotThrow()
    {
        // Arrange
        var service = new RedisSpatialCacheService(null, NullLogger<RedisSpatialCacheService>.Instance);

        // Act & Assert
        service.IsConnected.Should().BeFalse();
    }

    [Fact]
    public async Task RedisSpatialCacheService_WhenDisconnected_ReturnsSafeFallbacks()
    {
        // Arrange
        var service = new RedisSpatialCacheService(null, NullLogger<RedisSpatialCacheService>.Instance);
        var storeId = Guid.NewGuid();

        // Act
        var addRes = await service.AddOrUpdateStoreGeoAsync(storeId, 40.7128, -74.0060);
        var removeRes = await service.RemoveStoreGeoAsync(storeId);
        var searchRes = await service.SearchNearbyStoreIdsAsync(40.7128, -74.0060, 1000);
        var warmupRes = await service.WarmupCacheAsync(new List<Store>());

        // Assert
        addRes.Should().BeFalse();
        removeRes.Should().BeFalse();
        searchRes.Should().BeNull();
        warmupRes.Should().Be(0);
    }

    [Fact]
    public async Task StoreRepository_WhenRedisHasCacheHits_ReturnsStoresFromCacheIndex()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var allStores = await context.Stores.ToListAsync();
        var store1 = allStores[0];
        var store2 = allStores[1];

        var fakeCache = new FakeSpatialCacheService
        {
            IsConnected = true,
            NextHits = new List<GeoCacheHit>
            {
                new(store2.Id, 120.5),
                new(store1.Id, 350.0)
            }
        };

        var repo = new StoreRepository(context, fakeCache);

        // Act
        var results = await repo.GetNearbyStoresAsync(40.7128, -74.0060, 1000);

        // Assert
        results.Should().HaveCount(2);
        results[0].Store.Id.Should().Be(store2.Id);
        results[0].DistanceMeters.Should().Be(120.5);
        results[1].Store.Id.Should().Be(store1.Id);
        results[1].DistanceMeters.Should().Be(350.0);
    }

    [Fact]
    public async Task StoreRepository_WhenCacheReturnsNull_FallsBackToPostGISGracefully()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var fakeCache = new FakeSpatialCacheService
        {
            IsConnected = true,
            NextHits = null // Simulates cache miss or Redis failure
        };

        var repo = new StoreRepository(context, fakeCache);

        // Act
        var results = await repo.GetNearbyStoresAsync(40.7128, -74.0060, 1500);

        // Assert - Fallback computes Haversine distance and returns stores within 1500m
        results.Should().HaveCount(3);
        results.Should().Contain(r => r.Store.Name == "Green Grocer Organic Market");
    }
}
