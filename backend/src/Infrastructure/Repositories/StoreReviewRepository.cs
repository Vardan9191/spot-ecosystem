using Microsoft.EntityFrameworkCore;
using Spot.Domain.Entities;
using Spot.Infrastructure.Persistence;

namespace Spot.Infrastructure.Repositories;

public interface IStoreReviewRepository
{
    Task<List<StoreReview>> GetByStoreIdAsync(Guid storeId, CancellationToken ct = default);
    Task<StoreReview> AddAsync(StoreReview review, CancellationToken ct = default);
}

public class StoreReviewRepository : IStoreReviewRepository
{
    private readonly SpotDbContext _context;

    public StoreReviewRepository(SpotDbContext context)
    {
        _context = context;
    }

    public async Task<List<StoreReview>> GetByStoreIdAsync(Guid storeId, CancellationToken ct = default)
    {
        return await _context.StoreReviews
            .Where(r => r.StoreId == storeId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<StoreReview> AddAsync(StoreReview review, CancellationToken ct = default)
    {
        await _context.StoreReviews.AddAsync(review, ct);
        await _context.SaveChangesAsync(ct);
        return review;
    }
}
