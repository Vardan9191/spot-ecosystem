using Microsoft.EntityFrameworkCore;
using Spot.Domain.Common;
using Spot.Domain.Entities;
using Spot.Infrastructure.Cache;
using Spot.Infrastructure.Persistence;

namespace Spot.Infrastructure.Repositories;

public class StoreRepository : IStoreRepository
{
    private const double MetersPerDegreeLat = 111320.0;
    private readonly SpotDbContext _context;
    private readonly ISpatialCacheService? _cacheService;

    public StoreRepository(SpotDbContext context, ISpatialCacheService? cacheService = null)
    {
        _context = context;
        _cacheService = cacheService;
    }

    public async Task<List<NearbyStoreResult>> GetNearbyStoresAsync(
        double latitude, 
        double longitude, 
        double radiusMeters, 
        string? categorySlug = null, 
        CancellationToken ct = default)
    {
        // 1. First Tier: Check Redis Geospatial Index (< 1ms lookup)
        if (_cacheService != null && _cacheService.IsConnected)
        {
            var cacheHits = await _cacheService.SearchNearbyStoreIdsAsync(latitude, longitude, radiusMeters, ct);
            if (cacheHits != null && cacheHits.Count > 0)
            {
                var hitIds = cacheHits.Select(h => h.StoreId).ToList();
                var distMap = cacheHits.ToDictionary(h => h.StoreId, h => h.DistanceMeters);

                var queryCached = _context.Stores
                    .Include(s => s.Category)
                    .Include(s => s.Reviews)
                    .AsNoTracking()
                    .Where(s => hitIds.Contains(s.Id));

                if (!string.IsNullOrWhiteSpace(categorySlug))
                {
                    queryCached = queryCached.Where(s => s.Category.Slug.ToLower() == categorySlug.ToLower());
                }

                var cachedStores = await queryCached.ToListAsync(ct);
                var cachedResults = cachedStores.Select(s => new NearbyStoreResult
                {
                    Store = s,
                    DistanceMeters = distMap.TryGetValue(s.Id, out var d) ? d : GeoUtils.DistanceMeters(latitude, longitude, s.Location.Y, s.Location.X)
                }).OrderBy(r => r.DistanceMeters).ToList();

                return cachedResults;
            }
        }

        // 2. Second Tier: Fallback to PostGIS Spatial Index & Haversine Geodesic Engine
        var query = _context.Stores
            .Include(s => s.Category)
            .Include(s => s.Reviews)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(categorySlug))
        {
            query = query.Where(s => s.Category.Slug.ToLower() == categorySlug.ToLower());
        }

        // Bounding box pre-filter for spatial index efficiency
        // Latitude delta:
        double latDelta = radiusMeters / MetersPerDegreeLat;
        // Longitude delta scaled by cosine of latitude:
        double radLat = latitude * Math.PI / 180.0;
        double lonScale = Math.Max(0.01, Math.Cos(radLat));
        double lonDelta = radiusMeters / (MetersPerDegreeLat * lonScale);

        double minLat = latitude - latDelta;
        double maxLat = latitude + latDelta;
        double minLon = longitude - lonDelta;
        double maxLon = longitude + lonDelta;

        // Bounding box filter (leverages spatial / coordinate index)
        var candidates = await query
            .Where(s => s.Location.Y >= minLat && s.Location.Y <= maxLat &&
                        s.Location.X >= minLon && s.Location.X <= maxLon)
            .ToListAsync(ct);

        // Precise geodesic distance filter using Haversine formula (WGS 84 ellipsoid)
        var results = new List<NearbyStoreResult>();
        foreach (var store in candidates)
        {
            double dist = GeoUtils.DistanceMeters(latitude, longitude, store.Location.Y, store.Location.X);
            if (dist <= radiusMeters)
            {
                results.Add(new NearbyStoreResult
                {
                    Store = store,
                    DistanceMeters = Math.Round(dist, 1)
                });
            }
        }

        // Sort ascending by proximity
        return results.OrderBy(r => r.DistanceMeters).ToList();
    }

    public async Task<Store?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Stores
            .Include(s => s.Category)
            .Include(s => s.Reviews)
            .FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    public async Task<List<Store>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.Stores
            .Include(s => s.Category)
            .Include(s => s.Reviews)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Store store, CancellationToken ct = default)
    {
        await _context.Stores.AddAsync(store, ct);
        await _context.SaveChangesAsync(ct);
    }
}
