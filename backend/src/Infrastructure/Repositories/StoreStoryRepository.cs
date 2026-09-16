using Microsoft.EntityFrameworkCore;
using Spot.Domain.Common;
using Spot.Domain.Entities;
using Spot.Infrastructure.Persistence;

namespace Spot.Infrastructure.Repositories;

public class StoreStoryRepository : IStoreStoryRepository
{
    private const double MetersPerDegreeLat = 111320.0;
    private readonly SpotDbContext _context;

    public StoreStoryRepository(SpotDbContext context)
    {
        _context = context;
    }

    public async Task<List<NearbyStoryResult>> GetNearbyActiveStoriesAsync(
        double latitude, 
        double longitude, 
        double radiusMeters = 1500, 
        CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;

        // Bounding box pre-filter for spatial index efficiency
        double latDelta = radiusMeters / MetersPerDegreeLat;
        double radLat = latitude * Math.PI / 180.0;
        double lonScale = Math.Max(0.01, Math.Cos(radLat));
        double lonDelta = radiusMeters / (MetersPerDegreeLat * lonScale);

        double minLat = latitude - latDelta;
        double maxLat = latitude + latDelta;
        double minLon = longitude - lonDelta;
        double maxLon = longitude + lonDelta;

        var stories = await _context.StoreStories
            .Include(s => s.Store)
                .ThenInclude(st => st.Category)
            .Where(s => s.ExpiresAt > now &&
                        s.Store.Location.Y >= minLat && s.Store.Location.Y <= maxLat &&
                        s.Store.Location.X >= minLon && s.Store.Location.X <= maxLon)
            .AsNoTracking()
            .ToListAsync(ct);

        var results = new List<NearbyStoryResult>();
        foreach (var story in stories)
        {
            double dist = GeoUtils.DistanceMeters(latitude, longitude, story.Store.Location.Y, story.Store.Location.X);
            if (dist <= radiusMeters)
            {
                results.Add(new NearbyStoryResult
                {
                    Story = story,
                    DistanceMeters = Math.Round(dist, 1)
                });
            }
        }

        // Sort by distance ascending, then creation date descending
        return results
            .OrderBy(r => r.DistanceMeters)
            .ThenByDescending(r => r.Story.CreatedAt)
            .ToList();
    }

    public async Task<List<StoreStory>> GetActiveByStoreIdAsync(Guid storeId, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        return await _context.StoreStories
            .Include(s => s.Store)
            .Where(s => s.StoreId == storeId && s.ExpiresAt > now)
            .OrderByDescending(s => s.CreatedAt)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<StoreStory?> GetByIdAsync(Guid storyId, CancellationToken ct = default)
    {
        return await _context.StoreStories
            .Include(s => s.Store)
            .FirstOrDefaultAsync(s => s.Id == storyId, ct);
    }

    public async Task AddAsync(StoreStory story, CancellationToken ct = default)
    {
        await _context.StoreStories.AddAsync(story, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task IncrementViewCountAsync(Guid storyId, CancellationToken ct = default)
    {
        var story = await _context.StoreStories.FirstOrDefaultAsync(s => s.Id == storyId, ct);
        if (story != null)
        {
            story.ViewCount++;
            await _context.SaveChangesAsync(ct);
        }
    }
}
