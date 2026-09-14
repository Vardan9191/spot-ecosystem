using Spot.Domain.Entities;

namespace Spot.Infrastructure.Repositories;

public interface ITaskListRepository
{
    Task<List<TaskList>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<TaskList?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<TaskList> CreateAsync(TaskList list, CancellationToken ct = default);
    Task<TaskList?> UpdateAsync(TaskList list, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);

    Task<TaskItem?> AddItemAsync(Guid listId, TaskItem item, CancellationToken ct = default);
    Task<TaskItem?> ToggleItemCompletionAsync(Guid listId, Guid itemId, CancellationToken ct = default);
    Task<bool> DeleteItemAsync(Guid listId, Guid itemId, CancellationToken ct = default);
}
