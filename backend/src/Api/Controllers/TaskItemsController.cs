using Microsoft.AspNetCore.Mvc;
using Spot.Api.DTOs;
using Spot.Domain.Entities;
using Spot.Infrastructure.Repositories;

namespace Spot.Api.Controllers;

[ApiController]
[Route("api/v1/tasklists/{listId:guid}/items")]
public class TaskItemsController : ControllerBase
{
    private readonly ITaskListRepository _taskListRepository;

    public TaskItemsController(ITaskListRepository taskListRepository)
    {
        _taskListRepository = taskListRepository;
    }

    [HttpPost]
    [ProducesResponseType(typeof(TaskItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskItemDto>> AddItem(Guid listId, [FromBody] CreateTaskItemRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest("Item title cannot be empty.");
        }

        var item = new TaskItem
        {
            Title = request.Title.Trim(),
            Quantity = string.IsNullOrWhiteSpace(request.Quantity) ? "1" : request.Quantity.Trim(),
            IsCompleted = false
        };

        var created = await _taskListRepository.AddItemAsync(listId, item, ct);
        if (created == null) return NotFound($"TaskList with ID {listId} not found.");

        return CreatedAtAction(nameof(AddItem), new { listId, itemId = created.Id }, 
            new TaskItemDto(created.Id, created.ListId, created.Title, created.IsCompleted, created.Quantity));
    }

    [HttpPatch("{itemId:guid}/toggle")]
    [ProducesResponseType(typeof(TaskItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskItemDto>> ToggleItem(Guid listId, Guid itemId, CancellationToken ct = default)
    {
        var item = await _taskListRepository.ToggleItemCompletionAsync(listId, itemId, ct);
        if (item == null) return NotFound($"Item with ID {itemId} in List {listId} not found.");

        return Ok(new TaskItemDto(item.Id, item.ListId, item.Title, item.IsCompleted, item.Quantity));
    }

    [HttpDelete("{itemId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteItem(Guid listId, Guid itemId, CancellationToken ct = default)
    {
        var deleted = await _taskListRepository.DeleteItemAsync(listId, itemId, ct);
        if (!deleted) return NotFound($"Item with ID {itemId} in List {listId} not found.");

        return NoContent();
    }
}
