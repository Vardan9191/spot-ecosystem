using Spot.Domain.Entities;

namespace Spot.Infrastructure.Repositories;

public class NearbyStoreResult
{
    public Store Store { get; set; } = null!;
    public double DistanceMeters { get; set; }
}

public interface IStoreRepository
{
    Task<List<NearbyStoreResult>> GetNearbyStoresAsync(
        double latitude, 
        double longitude, 
        double radiusMeters, 
        string? categorySlug = null, 
        CancellationToken ct = default);

    Task<Store?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<Store>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(Store store, CancellationToken ct = default);
}
