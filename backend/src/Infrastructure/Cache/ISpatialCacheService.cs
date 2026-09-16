using Spot.Domain.Entities;

namespace Spot.Infrastructure.Cache;

public record GeoCacheHit(Guid StoreId, double DistanceMeters);

public interface ISpatialCacheService
{
    bool IsConnected { get; }
    Task<bool> AddOrUpdateStoreGeoAsync(Guid storeId, double latitude, double longitude, CancellationToken ct = default);
    Task<bool> RemoveStoreGeoAsync(Guid storeId, CancellationToken ct = default);
    Task<List<GeoCacheHit>?> SearchNearbyStoreIdsAsync(double latitude, double longitude, double radiusMeters, CancellationToken ct = default);
    Task<int> WarmupCacheAsync(IEnumerable<Store> stores, CancellationToken ct = default);
}
