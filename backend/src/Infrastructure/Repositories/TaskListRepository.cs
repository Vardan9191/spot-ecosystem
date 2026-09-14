using Microsoft.EntityFrameworkCore;
using Spot.Domain.Entities;
using Spot.Infrastructure.Persistence;

namespace Spot.Infrastructure.Repositories;

public class TaskListRepository : ITaskListRepository
{
    private readonly SpotDbContext _context;

    public TaskListRepository(SpotDbContext context)
    {
        _context = context;
    }

    public async Task<List<TaskList>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await _context.TaskLists
            .Include(t => t.Category)
            .Include(t => t.Items)
            .Where(t => t.UserId == userId)
            .OrderBy(t => t.Title)
            .ToListAsync(ct);
    }

    public async Task<TaskList?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.TaskLists
            .Include(t => t.Category)
            .Include(t => t.Items)
            .FirstOrDefaultAsync(t => t.Id == id, ct);
    }

    public async Task<TaskList> CreateAsync(TaskList list, CancellationToken ct = default)
    {
        await _context.TaskLists.AddAsync(list, ct);
        await _context.SaveChangesAsync(ct);
        return list;
    }

    public async Task<TaskList?> UpdateAsync(TaskList list, CancellationToken ct = default)
    {
        var existing = await _context.TaskLists.FirstOrDefaultAsync(t => t.Id == list.Id, ct);
        if (existing == null) return null;

        existing.Title = list.Title;
        existing.CategoryId = list.CategoryId;
        existing.CustomLocation = list.CustomLocation;
        existing.RadiusMeters = list.RadiusMeters;
        existing.IsActive = list.IsActive;

        await _context.SaveChangesAsync(ct);
        return existing;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var existing = await _context.TaskLists.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (existing == null) return false;

        _context.TaskLists.Remove(existing);
        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<TaskItem?> AddItemAsync(Guid listId, TaskItem item, CancellationToken ct = default)
    {
        var list = await _context.TaskLists.Include(t => t.Items).FirstOrDefaultAsync(t => t.Id == listId, ct);
        if (list == null) return null;

        item.ListId = listId;
        await _context.TaskItems.AddAsync(item, ct);
        await _context.SaveChangesAsync(ct);
        return item;
    }

    public async Task<TaskItem?> ToggleItemCompletionAsync(Guid listId, Guid itemId, CancellationToken ct = default)
    {
        var item = await _context.TaskItems.FirstOrDefaultAsync(i => i.Id == itemId && i.ListId == listId, ct);
        if (item == null) return null;

        item.IsCompleted = !item.IsCompleted;
        await _context.SaveChangesAsync(ct);
        return item;
    }

    public async Task<bool> DeleteItemAsync(Guid listId, Guid itemId, CancellationToken ct = default)
    {
        var item = await _context.TaskItems.FirstOrDefaultAsync(i => i.Id == itemId && i.ListId == listId, ct);
        if (item == null) return false;

        _context.TaskItems.Remove(item);
        await _context.SaveChangesAsync(ct);
        return true;
    }
}
