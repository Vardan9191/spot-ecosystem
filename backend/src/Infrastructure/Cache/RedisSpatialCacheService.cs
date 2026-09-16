using Microsoft.Extensions.Logging;
using Spot.Domain.Entities;
using StackExchange.Redis;

namespace Spot.Infrastructure.Cache;

public class RedisSpatialCacheService : ISpatialCacheService
{
    private const string StoresGeoKey = "spot:stores:geo";
    private readonly IConnectionMultiplexer? _redis;
    private readonly ILogger<RedisSpatialCacheService> _logger;

    public RedisSpatialCacheService(
        IConnectionMultiplexer? redis,
        ILogger<RedisSpatialCacheService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public bool IsConnected => _redis != null && _redis.IsConnected;

    public async Task<bool> AddOrUpdateStoreGeoAsync(Guid storeId, double latitude, double longitude, CancellationToken ct = default)
    {
        if (!IsConnected) return false;

        try
        {
            var db = _redis!.GetDatabase();
            // Redis GEOADD format: key, longitude, latitude, member
            await db.GeoAddAsync(StoresGeoKey, longitude, latitude, storeId.ToString());
            _logger.LogDebug("Synchronized store {StoreId} to Redis GEO cache at ({Lat}, {Lng})", storeId, latitude, longitude);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write store {StoreId} to Redis GEO cache. Falling back silently.", storeId);
            return false;
        }
    }

    public async Task<bool> RemoveStoreGeoAsync(Guid storeId, CancellationToken ct = default)
    {
        if (!IsConnected) return false;

        try
        {
            var db = _redis!.GetDatabase();
            await db.SortedSetRemoveAsync(StoresGeoKey, storeId.ToString());
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to remove store {StoreId} from Redis GEO cache.", storeId);
            return false;
        }
    }

    public async Task<List<GeoCacheHit>?> SearchNearbyStoreIdsAsync(double latitude, double longitude, double radiusMeters, CancellationToken ct = default)
    {
        if (!IsConnected) return null;

        try
        {
            var db = _redis!.GetDatabase();
            
            // Query Redis Geospatial index within radius in meters, sorted ascending by distance
            var results = await db.GeoRadiusAsync(
                StoresGeoKey,
                longitude,
                latitude,
                radiusMeters,
                GeoUnit.Meters,
                order: Order.Ascending,
                options: GeoRadiusOptions.WithDistance
            );

            var hits = new List<GeoCacheHit>();
            foreach (var r in results)
            {
                if (r.Member.HasValue && Guid.TryParse(r.Member.ToString(), out var storeId))
                {
                    double dist = r.Distance.HasValue ? Math.Round(r.Distance.Value, 1) : 0.0;
                    hits.Add(new GeoCacheHit(storeId, dist));
                }
            }

            _logger.LogDebug("Redis GEO search at ({Lat}, {Lng}, {Radius}m) returned {Count} hits",
                latitude, longitude, radiusMeters, hits.Count);

            return hits;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis GEO lookup failed at ({Lat}, {Lng}). Gracefully falling back to PostGIS.", latitude, longitude);
            return null; // Signals StoreRepository to execute PostGIS query
        }
    }

    public async Task<int> WarmupCacheAsync(IEnumerable<Store> stores, CancellationToken ct = default)
    {
        if (!IsConnected) return 0;

        try
        {
            var db = _redis!.GetDatabase();
            var entries = stores.Select(s => new GeoEntry(s.Location.X, s.Location.Y, s.Id.ToString())).ToArray();
            if (entries.Length == 0) return 0;

            long added = await db.GeoAddAsync(StoresGeoKey, entries);
            _logger.LogInformation("Warmed up Redis GEO cache with {Count} store coordinates", entries.Length);
            return (int)added;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis GEO cache warmup encountered an error.");
            return 0;
        }
    }
}
