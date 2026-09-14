using Microsoft.AspNetCore.Mvc;
using Spot.Api.DTOs;
using Spot.Domain.Common;
using Spot.Domain.Entities;
using Spot.Infrastructure.Repositories;

namespace Spot.Api.Controllers;

[ApiController]
[Route("api/v1/tasklists")]
public class TaskListsController : ControllerBase
{
    private readonly ITaskListRepository _taskListRepository;

    public TaskListsController(ITaskListRepository taskListRepository)
    {
        _taskListRepository = taskListRepository;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<TaskListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<TaskListDto>>> GetByUserId([FromQuery] Guid userId, CancellationToken ct = default)
    {
        if (userId == Guid.Empty)
        {
            return BadRequest("A valid userId is required.");
        }

        var lists = await _taskListRepository.GetByUserIdAsync(userId, ct);
        return Ok(lists.Select(MapToDto).ToList());
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TaskListDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskListDto>> GetById(Guid id, CancellationToken ct = default)
    {
        var list = await _taskListRepository.GetByIdAsync(id, ct);
        if (list == null) return NotFound($"TaskList with ID {id} not found.");

        return Ok(MapToDto(list));
    }

    [HttpPost]
    [ProducesResponseType(typeof(TaskListDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TaskListDto>> Create([FromBody] CreateTaskListRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest("List title cannot be empty.");
        }

        var entity = new TaskList
        {
            UserId = request.UserId,
            Title = request.Title.Trim(),
            CategoryId = request.CategoryId,
            RadiusMeters = request.RadiusMeters,
            IsActive = request.IsActive,
            CustomLocation = (request.Latitude.HasValue && request.Longitude.HasValue)
                ? GeoUtils.CreatePoint(request.Latitude.Value, request.Longitude.Value)
                : null
        };

        var created = await _taskListRepository.CreateAsync(entity, ct);
        // Reload with navigation properties
        var reloaded = await _taskListRepository.GetByIdAsync(created.Id, ct) ?? created;

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, MapToDto(reloaded));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(TaskListDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskListDto>> Update(Guid id, [FromBody] UpdateTaskListRequest request, CancellationToken ct = default)
    {
        var entity = new TaskList
        {
            Id = id,
            Title = request.Title.Trim(),
            CategoryId = request.CategoryId,
            RadiusMeters = request.RadiusMeters,
            IsActive = request.IsActive,
            CustomLocation = (request.Latitude.HasValue && request.Longitude.HasValue)
                ? GeoUtils.CreatePoint(request.Latitude.Value, request.Longitude.Value)
                : null
        };

        var updated = await _taskListRepository.UpdateAsync(entity, ct);
        if (updated == null) return NotFound($"TaskList with ID {id} not found.");

        var reloaded = await _taskListRepository.GetByIdAsync(id, ct) ?? updated;
        return Ok(MapToDto(reloaded));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        var deleted = await _taskListRepository.DeleteAsync(id, ct);
        if (!deleted) return NotFound($"TaskList with ID {id} not found.");

        return NoContent();
    }

    private static TaskListDto MapToDto(TaskList list)
    {
        return new TaskListDto(
            Id: list.Id,
            UserId: list.UserId,
            Title: list.Title,
            Category: list.Category != null 
                ? new CategoryDto(list.Category.Id, list.Category.Name, list.Category.Slug, list.Category.Icon) 
                : null,
            Latitude: list.CustomLocation?.Y,
            Longitude: list.CustomLocation?.X,
            RadiusMeters: list.RadiusMeters,
            IsActive: list.IsActive,
            Items: list.Items.Select(i => new TaskItemDto(i.Id, i.ListId, i.Title, i.IsCompleted, i.Quantity)).ToList()
        );
    }
}
