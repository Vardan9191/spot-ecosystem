using Spot.Domain.Entities;

namespace Spot.Infrastructure.Repositories;

public class NearbyStoryResult
{
    public StoreStory Story { get; set; } = null!;
    public double DistanceMeters { get; set; }
}

public interface IStoreStoryRepository
{
    Task<List<NearbyStoryResult>> GetNearbyActiveStoriesAsync(
        double latitude, 
        double longitude, 
        double radiusMeters = 1500, 
        CancellationToken ct = default);

    Task<List<StoreStory>> GetActiveByStoreIdAsync(Guid storeId, CancellationToken ct = default);
    Task<StoreStory?> GetByIdAsync(Guid storyId, CancellationToken ct = default);
    Task AddAsync(StoreStory story, CancellationToken ct = default);
    Task IncrementViewCountAsync(Guid storyId, CancellationToken ct = default);
}
